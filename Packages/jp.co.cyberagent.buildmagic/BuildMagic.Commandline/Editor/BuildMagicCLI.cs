// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using BuildMagicEditor;
using BuildMagicEditor.BuiltIn;
using BuildMagicEditor.Commandline.Error;
using BuildMagicEditor.Commandline.Internal;
using BuildMagicEditor.Extensions;
using UnityEditor;
using UnityEngine;
using BuildPipeline = BuildMagicEditor.BuildPipeline;

/// <summary>
/// The interface for editor batch mode command line.
/// </summary>
public static class BuildMagicCLI
{
    private static readonly int SuccessCode = 0;
    private static readonly int FailedCode = 1;

    private static readonly string SchemeOption = "-scheme";
    private static readonly string OverrideOption = "-override";
    private static readonly string StrictOption = "-strict";

    private static readonly string LoggerTag = "BuildMagic";

    private static readonly string SetLocationPathPropertyName = "BuildPlayerOptions.assetBundleManifestPath";

    private static readonly Dictionary<string, string> OverrideAliases = new()
    {
        { "-output", SetLocationPathPropertyName }
    };

    /// <summary>
    ///     Perform the switch.
    /// </summary>
    public static void PreBuild()
    {
        // TODO: System.Commandline-like implementation
        RunCommand(context =>
        {
            var configurations =
                BuildSchemeUtility.EnumerateComposedConfigurations<IPreBuildContext>(context
                    .InheritanceTreeFromLeafToRoot);

            var preBuildTasks = configurations
                .Select(c => CreateBuildTask(c, context))
                .OfType<IBuildTask<IPreBuildContext>>()
                .ToList();

            preBuildTasks.AddRange(GetUnresolvedOverrideProperties(context).OfType<IBuildTask<IPreBuildContext>>());

            BuildPipeline.PreBuild(preBuildTasks);
        });
    }

    /// <summary>
    ///     Perform the build.
    /// </summary>
    public static void Build()
    {
        // TODO: System.Commandline-like implementation
        RunCommand(context =>
        {
            var internalPrepareTasks = CreateInternalPrepareTasks(context);

            var configurations =
                BuildSchemeUtility.EnumerateComposedConfigurations<IPostBuildContext>(context
                    .InheritanceTreeFromLeafToRoot);

            var postBuildTasks = configurations
                .Select(c => CreateBuildTask(c, context))
                .OfType<IBuildTask<IPostBuildContext>>()
                .ToList();

            postBuildTasks.AddRange(GetUnresolvedOverrideProperties(context).OfType<IBuildTask<IPostBuildContext>>());

            var isStrict = context.CommandLineParser.HasFlag(StrictOption);

            BuildPipeline.Build(internalPrepareTasks.GenerateBuildPlayerOptions(), postBuildTasks, isStrict);
        });
    }

    private static IEnumerable<IBuildTask> GetUnresolvedOverrideProperties(Context context)
    {
        return context.BuildPropertyResolver.GetUnresolvedOverrideProperties().Select(prop =>
        {
            if (!BuildConfigurationTypeUtility.TryGetConfigurationType(prop.Name,
                    out var configurationType))
                throw new CommandLineArgumentException($"No such property found: {prop.Name}");

            var configuration = Activator.CreateInstance(configurationType) as IBuildConfiguration;

            return CreateBuildTask(configuration, context);
        });
    }

    private static IBuildTask<IInternalPrepareContext>[] CreateInternalPrepareTasks(Context context)
    {
        var setLocationPathConfiguration = new BuildPlayerOptionsSetLocationPathNameTaskConfiguration();
        var property = context.BuildPropertyResolver.ResolveProperty(setLocationPathConfiguration);

        var setConfigurationPathTaskBuilder =
            context.TaskBuilderProvider.GetBuilder(setLocationPathConfiguration.TaskType, property.ValueType);

        List<IBuildTask<IInternalPrepareContext>> tasks = new()
        {
            new BuildPlayerOptionsApplyEditorSettingsTask(),
            setConfigurationPathTaskBuilder.Build(property.Value) as IBuildTask<IInternalPrepareContext>
        };

        tasks.AddRange(GetUnresolvedOverrideProperties(context).OfType<IBuildTask<IInternalPrepareContext>>());

        return tasks.ToArray();
    }

    private static void RunCommand(Action<Context> command)
    {
        EditorApplication.Exit(InvokeCommand(command));
    }

    private static int InvokeCommand(Action<Context> command)
    {
        // NOTE: output to Unity's console log for now (we may have to make it possible to output to a different log)
        var logger = Debug.unityLogger;

        try
        {
            command(Context.Create(logger));
            return SuccessCode;
        }
        catch (Exception e)
        {
            logger.LogError(LoggerTag, $"[BuildMagic] Failed to run command.");
            logger.LogException(e);
            return FailedCode;
        }
    }

    internal static BuildScheme LoadScheme(CommandLineParser parser, IEnumerable<BuildScheme> allSchemes = null)
    {
        var name = parser.ParseFirst(SchemeOption);

        allSchemes ??= BuildSchemeLoader.LoadAll<BuildScheme>();

        var scheme = allSchemes.FirstOrDefault(s => s.Name == name);
        if (scheme == null)
        {
            throw new CommandLineArgumentException($"No such scheme found: {name}");
        }

        return scheme;
    }

    internal static List<OverrideProperty> ParseOverrideProperties(
        CommandLineParser parser,
        IDictionary<string, string> aliases = null)
    {
        var properties = new List<OverrideProperty>();

        // No override properties specified.
        if (parser.TryParse(OverrideOption, out var options))
            // override options are separated by comma such as "KEY1=VALUE1".
            foreach (var keyValue in options)
            {
                // the override property is separated by equal sign such as "KEY1=VALUE1".
                // use the first equal sign to split the key and value.
                var ps = keyValue.IndexOf('=');
                if (ps < 0)
                {
                    Debug.LogWarning($"Override property does not parsed correctly:\n{keyValue}");
                    continue;
                }

                properties.Add(new OverrideProperty(keyValue[..ps], keyValue[(ps+1)..]));
            }

        if (aliases != null)
            foreach (var alias in aliases)
                if (parser.TryParse(alias.Key, out var value) && value.Length == 1)
                    properties.Add(new OverrideProperty(alias.Value, value[0]));

        return properties;
    }

    internal static IBuildTask CreateBuildTask(IBuildConfiguration configuration, Context context)
    {
        var buildProperty = context.BuildPropertyResolver.ResolveProperty(configuration);
        var taskBuilder = context.TaskBuilderProvider.GetBuilder(configuration.TaskType, buildProperty.ValueType);
        return taskBuilder.Build(buildProperty.Value);
    }

    internal class Context
    {
        public CommandLineParser CommandLineParser { get; }
        public IBuildScheme CurrentScheme { get; }
        public IEnumerable<IBuildScheme> InheritanceTreeFromLeafToRoot { get; }
        public BuildPropertyResolver BuildPropertyResolver { get; }

        public BuildTaskBuilderProvider TaskBuilderProvider { get; }

        // NOTE: logger may be wrapped for BuildMagic (since specifying tags and prefixes is cumbersome)
        public ILogger Logger { get; }

        public static Context Create(ILogger logger)
        {
            var parser = CommandLineParser.Create();
            var allSchemes = BuildSchemeLoader.LoadAll<BuildScheme>();
            var scheme = LoadScheme(parser, allSchemes);
            var tree = BuildSchemeUtility.EnumerateSchemeTreeFromLeafToRoot(scheme, allSchemes);
            var resolver = BuildPropertyResolver.CreateDefault(ParseOverrideProperties(parser, OverrideAliases));
            var provider = BuildTaskBuilderProvider.CreateDefault();

            return new Context(parser, scheme, tree, provider, resolver, logger);
        }

        private Context(
            CommandLineParser parser,
            IBuildScheme scheme,
            IEnumerable<IBuildScheme> inheritanceTreeFromLeafToRoot,
            BuildTaskBuilderProvider provider,
            BuildPropertyResolver resolver,
            ILogger logger)
        {
            CommandLineParser = parser;
            CurrentScheme = scheme;
            InheritanceTreeFromLeafToRoot = inheritanceTreeFromLeafToRoot;
            BuildPropertyResolver = resolver;
            TaskBuilderProvider = provider;
            Logger = logger;
        }
    }
}
