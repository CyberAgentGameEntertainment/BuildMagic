// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZLogger;

namespace BuildMagic.BuiltinTaskGenerator;

// ReSharper disable once ClassNeverInstantiated.Global
public class App(ILogger<App> logger, IOptions<AppSettings> options, UnityApiAnalyzer analyzer, AnalysisLibrary library)
    : ConsoleAppBase
{
    public async Task Generate(
        [Option("o", "Output directory for ApiSignatureLock.g.cs (typically BuildMagic.BuiltinTasks.Generators/)")]
        string outputDir = ".",
        [Option("n", ".NET Framework 4.7.1 (equivalent) BCL path")]
        string? netfxBcl = null,
        [Option("min", "minimum version of unity (inclusive)")]
        string? minVersion = null,
        [Option("max", "maximum version of unity (inclusive)")]
        string? maxVersion = null,
        [Option("f", "forces to ignore cached analysis results")]
        bool forceAnalyze = false)
    {
        UnityVersion minVersionParsed = new(2022, 3, 0, 'f', 1);
        if (!string.IsNullOrEmpty(minVersion))
            if (!UnityVersion.TryParse(minVersion, out minVersionParsed))
            {
                logger.ZLogCritical($"Failed to parse minimum version \"{minVersionParsed}\".");
                return;
            }

        UnityVersion? maxVersionParsed = null;
        if (!string.IsNullOrEmpty(maxVersion))
        {
            if (!UnityVersion.TryParse(maxVersion, out var v))
            {
                logger.ZLogCritical($"Failed to parse minimum version \"{maxVersion}\".");
                return;
            }

            maxVersionParsed = v;
        }

        if (minVersionParsed.Major <= 2021 && string.IsNullOrEmpty(netfxBcl))
        {
            logger.ZLogCritical($"--netfx-bcl is required for processing Unity versions 2021 and earlier.");
            return;
        }

        logger.ZLogInformation($"Running analysis...");

        Func<UnityVersion, bool> versionFilter = v =>
        {
            if (v.ReleaseType != 'f') return false;
            if (v < minVersionParsed) return false;
            if (maxVersionParsed.HasValue && v > maxVersionParsed) return false;
            return true;
        };

        await analyzer.RunAsync(netfxBcl,
            versionFilter,
            forceAnalyze,
            CancellationToken.None);

        Dictionary<string /* categoryName */, Dictionary<string /* expectedName */, HashSet<TaskData>>>
            knownTasksForCategories = new();
        HashSet<UnityVersion> knownVersions = new();

        await foreach (var (version, result) in library.LoadAllAsync(versionFilter, CancellationToken.None))
        {
            knownVersions.Add(version);

            foreach (var (categoryName, category) in result.Categories)
            {
                if (!knownTasksForCategories.TryGetValue(categoryName, out var knownTasks))
                    knownTasks = knownTasksForCategories[categoryName] = new Dictionary<string, HashSet<TaskData>>();

                var filteredApis = category.Apis.Where(a => !(options.Value.GetApiOptions(a)?.Ignored ?? false));

                foreach (var apiGrouping in filteredApis.GroupBy(a => a.ExpectedName))
                {
                    var expectedName = apiGrouping.Key;

                    ApiData? selectedApiInCurrentVersion;
                    try
                    {
                        // If there is only one non-obsolete API in this version, use it
                        selectedApiInCurrentVersion = apiGrouping.SingleOrDefault(a => !a.IsObsolete);
                    }
                    catch (InvalidOperationException)
                    {
                        // empty=all obsolete
                        selectedApiInCurrentVersion = null;
                    }

                    if (!knownTasks.TryGetValue(expectedName, out var tasks))
                        knownTasks[expectedName] = tasks = new HashSet<TaskData>();

                    TaskData? matchedTask;
                    if (selectedApiInCurrentVersion == null)
                    {
                        // We select the most recent entry that matches the signature among known tasks
                        matchedTask = tasks.Where(t =>
                        {
                            return apiGrouping.Any(a => t.MatchesParameterSignature(a.Parameters));
                        }).MaxBy(t => t.LatestVersion);

                        if (matchedTask == null)
                        {
                            // Special case: prefer the one with NamedBuildTarget in parameters
                            if (apiGrouping.Any(d => !d.IsObsolete))
                            {
                                ApiData? singleWithNamedBuildTarget;
                                try
                                {
                                    singleWithNamedBuildTarget = apiGrouping.SingleOrDefault(d =>
                                        !d.IsObsolete && d.Parameters.Any(p =>
                                            p.TypeExpression == "global::UnityEditor.Build.NamedBuildTarget"));
                                }
                                catch (InvalidOperationException)
                                {
                                    singleWithNamedBuildTarget = null;
                                }

                                if (singleWithNamedBuildTarget != null)
                                    tasks.Add(new TaskData(version, singleWithNamedBuildTarget,
                                        options.Value.GetApiOptions(singleWithNamedBuildTarget)));
                                else
                                    logger.ZLogInformation(
                                        $"We cannot decide which API to use for {expectedName} in {version}.");
                            }

                            continue;
                        }

                        selectedApiInCurrentVersion =
                            apiGrouping.First(a => matchedTask.MatchesParameterSignature(a.Parameters));
                    }
                    else
                    {
                        matchedTask =
                            tasks.FirstOrDefault(t =>
                                t.MatchesParameterSignature(selectedApiInCurrentVersion.Parameters));
                    }

                    if (matchedTask != null)
                        // If the task has compatible signature with the known task, incorporate it
                        matchedTask.VisitNewerVersion(version, selectedApiInCurrentVersion);
                    else
                        // Otherwise, create a new task
                        tasks.Add(new TaskData(version, selectedApiInCurrentVersion,
                            options.Value.GetApiOptions(selectedApiInCurrentVersion)));
                }
            }
        }

        // Emit ApiSignatureLock.g.cs consumed by the source generator. Per emitted class
        // name we record one canonical parameter type FQN list — the latest-version
        // signature among all swept versions. The source generator uses this to prefer the
        // historically-established overload before falling back to its NamedBuildTarget
        // heuristic.
        var lockBuilder = new StringBuilder();
        lockBuilder.AppendLine("// <auto-generated />");
        lockBuilder.AppendLine("// Regenerated by BuildMagic.BuiltinTaskGenerator. Do not edit by hand.");
        lockBuilder.AppendLine();
        lockBuilder.AppendLine("using System.Collections.Generic;");
        lockBuilder.AppendLine();
        lockBuilder.AppendLine("namespace BuildMagic.BuiltinTasks.Generators;");
        lockBuilder.AppendLine();
        lockBuilder.AppendLine("internal static class ApiSignatureLock");
        lockBuilder.AppendLine("{");
        lockBuilder.AppendLine("    internal static IReadOnlyDictionary<string, string[]> Entries { get; set; } =");
        lockBuilder.AppendLine("        new Dictionary<string, string[]>");
        lockBuilder.AppendLine("        {");

        var entries = knownTasksForCategories
            .SelectMany(c => c.Value)
            .SelectMany(kvp => kvp.Value.Select(t => (className: $"{t.ExpectedName}Task", task: t)))
            // For a given class name, multiple TaskData entries can coexist when the
            // signature changed mid-history. Pick the latest version's signature — that's
            // the one a current Unity install will expose and the SG needs to match.
            .GroupBy(x => x.className)
            .Select(g => g.OrderByDescending(x => x.task.LatestVersion).First())
            .OrderBy(x => x.className, StringComparer.Ordinal);

        foreach (var (className, task) in entries)
        {
            var paramsLiteral = string.Join(", ",
                task.Parameters.Select(p => $"\"{p.TypeExpression}\""));
            lockBuilder.AppendLine($"            [\"{className}\"] = new[] {{ {paramsLiteral} }},");
        }

        lockBuilder.AppendLine("        };");
        lockBuilder.AppendLine("}");

        var lockPath = Path.Combine(outputDir, "ApiSignatureLock.g.cs");
        await File.WriteAllTextAsync(lockPath, lockBuilder.ToString(), Encoding.UTF8, CancellationToken.None);
        logger.ZLogInformation($"Wrote {lockPath}");
    }
}
