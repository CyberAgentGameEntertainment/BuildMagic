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
        // TODO: System.Commandline ライクな実装に仕上げたいが後回し
        RunCommand(context =>
        {
            var configurations = BuildConfigurationUtility.ResolveConfigurations(
                context.BaseScheme?.PreBuildConfigurations.Where(c => c != null) ?? Enumerable.Empty<IBuildConfiguration>(),
                context.CurrentScheme.PreBuildConfigurations.Where(c => c != null));

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
        // TODO: System.Commandline ライクな実装に仕上げたいが後回し
        RunCommand(context =>
        {
            var internalPrepareTasks = CreateInternalPrepareTasks(context);

            var configurations = BuildConfigurationUtility.ResolveConfigurations(
                context.BaseScheme?.PostBuildConfigurations.Where(c => c != null) ?? Enumerable.Empty<IBuildConfiguration>(),
                context.CurrentScheme.PostBuildConfigurations.Where(c => c != null));

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
        // NOTE: いったん固定でUnityのコンソールログに吐き出す
        //       もしログを別で書き出したい場合はここを変えられるようにする
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

    internal static BuildScheme LoadScheme(CommandLineParser parser)
    {
        var name = parser.ParseFirst(SchemeOption);

        var scheme = BuildSchemeLoader.LoadAll<BuildScheme>().FirstOrDefault(s => s.Name == name);
        if (scheme == null)
        {
            throw new CommandLineArgumentException($"No such scheme found: {name}");
        }

        return scheme;
    }

    internal static BuildScheme LoadBaseScheme(BuildScheme scheme)
    {
        if (string.IsNullOrEmpty(scheme.BaseSchemeName))
            return null;

        var baseScheme = BuildSchemeLoader.LoadAll<BuildScheme>().FirstOrDefault(s => s.Name == scheme.BaseSchemeName);
        if (baseScheme == null)
            throw new CommandLineArgumentException($"No such base scheme found: {scheme.BaseSchemeName}");

        return baseScheme;
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
                var ps = keyValue.Split('=');
                if (ps.Length != 2) continue;

                properties.Add(new OverrideProperty(ps[0], ps[1]));
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
        public IBuildScheme BaseScheme { get; }
        public BuildPropertyResolver BuildPropertyResolver { get; }

        public BuildTaskBuilderProvider TaskBuilderProvider { get; }

        // NOTE: ロガーは、BuildMagic用に一枚ラップしてもよい（タグやプレフィックスの指定が面倒なので）
        public ILogger Logger { get; }

        public static Context Create(ILogger logger)
        {
            var parser = CommandLineParser.Create();
            var scheme = LoadScheme(parser);
            var baseScheme = LoadBaseScheme(scheme);
            var resolver = BuildPropertyResolver.CreateDefault(ParseOverrideProperties(parser, OverrideAliases));
            var provider = BuildTaskBuilderProvider.CreateDefault();

            return new Context(parser, scheme, baseScheme, provider, resolver, logger);
        }

        private Context(
            CommandLineParser parser,
            IBuildScheme scheme,
            IBuildScheme baseScheme,
            BuildTaskBuilderProvider provider,
            BuildPropertyResolver resolver,
            ILogger logger)
        {
            CommandLineParser = parser;
            CurrentScheme = scheme;
            BaseScheme = baseScheme;
            BuildPropertyResolver = resolver;
            TaskBuilderProvider = provider;
            Logger = logger;
        }
    }
}
