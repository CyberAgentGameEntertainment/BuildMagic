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
        [Option("o", "Output directory for source files")]
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
                        // このバージョンにおいてObsoleteではないAPIが一つなら、それを採用する
                        selectedApiInCurrentVersion = apiGrouping.SingleOrDefault(a => !a.IsObsolete);
                    }
                    catch (InvalidOperationException)
                    {
                        // empty=全部obsoleteの場合
                        selectedApiInCurrentVersion = null;
                    }

                    if (!knownTasks.TryGetValue(expectedName, out var tasks))
                        knownTasks[expectedName] = tasks = new HashSet<TaskData>();

                    TaskData? matchedTask;
                    if (selectedApiInCurrentVersion == null)
                    {
                        // 既知のタスクのうち、シグネチャに互換性があって最も新しいものを選択する
                        matchedTask = tasks.Where(t =>
                        {
                            return apiGrouping.Any(a => t.MatchesParameterSignature(a.Parameters));
                        }).MaxBy(t => t.LatestVersion);

                        if (matchedTask == null)
                        {
                            // 特殊対応：NamedBuildTargetをパラメータにもつものは優先的に採用する
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
                        // 既知のタスクとシグネチャに互換性があれば、そこに織り込む
                        matchedTask.VisitNewerVersion(version, selectedApiInCurrentVersion);
                    else
                        // 新設
                        tasks.Add(new TaskData(version, selectedApiInCurrentVersion,
                            options.Value.GetApiOptions(selectedApiInCurrentVersion)));
                }
            }
        }

        // 生成
        UnityVersionDefineConstraintBuilder defineConstraintBuilder = new(knownVersions);

        foreach (var (categoryName, category) in knownTasksForCategories)
        {
            StringBuilder sourceBuilder = new();

            sourceBuilder.AppendLine("// <auto-generated />");
            sourceBuilder.AppendLine("namespace BuildMagicEditor.BuiltIn");
            sourceBuilder.AppendLine("{");

            foreach (var (_, tasks) in category)
            foreach (var taskData in tasks)
                taskData.Generate(sourceBuilder, defineConstraintBuilder, logger, options.Value.DictionaryKeyTypes);

            sourceBuilder.AppendLine("} // namespace BuildMagicEditor.BuiltIn");

            await File.WriteAllTextAsync(Path.Combine(outputDir, $"Tasks.{categoryName}.g.cs"),
                sourceBuilder.ToString(), Encoding.UTF8, CancellationToken.None);
        }
    }
}
