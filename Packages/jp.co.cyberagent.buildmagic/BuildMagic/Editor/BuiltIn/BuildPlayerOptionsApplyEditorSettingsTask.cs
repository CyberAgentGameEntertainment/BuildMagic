// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BuildMagicEditor.BuiltIn
{
    /// <summary>
    ///     Build player options apply editor settings task.
    /// </summary>
    public sealed class BuildPlayerOptionsApplyEditorSettingsTask : BuildTaskBase<IInternalPrepareContext>
    {
        public override void Run(IInternalPrepareContext context)
        {
            // see: https://github.com/Unity-Technologies/UnityCsReference/blob/9cecb4a6817863f0134896edafa84753ae2be96f/Editor/Mono/BuildProfile/BuildProfileModuleUtil.cs#L283
            var buildOptions = BuildOptions.None;

            var developmentBuild = EditorUserBuildSettings.development;

            if (developmentBuild) buildOptions |= BuildOptions.Development;
            if (EditorUserBuildSettings.allowDebugging && developmentBuild) buildOptions |= BuildOptions.AllowDebugging;
            if (EditorUserBuildSettings.symlinkSources) buildOptions |= BuildOptions.SymlinkSources;

            // NOTE: On some platforms, Unity allows profiler connection with non-development build 
            if (EditorUserBuildSettings.connectProfiler && developmentBuild)
                // buildOptions |= BuildOptions.ConnectToHost;
                buildOptions |= BuildOptions.ConnectWithProfiler;

            if (EditorUserBuildSettings.buildWithDeepProfilingSupport && developmentBuild)
                buildOptions |= BuildOptions.EnableDeepProfilingSupport;

            if (EditorUserBuildSettings.buildScriptsOnly) buildOptions |= BuildOptions.BuildScriptsOnly;

            if (!string.IsNullOrEmpty(GetCustomConnectionID()) && developmentBuild)
                buildOptions |= BuildOptions.CustomConnectionID;

            if (EditorUserBuildSettings.installInBuildFolder &&
                SupportsInstallInBuildFolder(context.ActiveBuildTargetGroup, context.ActiveBuildTarget))
                buildOptions |= BuildOptions.InstallInBuildFolder;

            if (EditorUserBuildSettings.waitForPlayerConnection) buildOptions |= BuildOptions.WaitForPlayerConnection;

            if (SupportsLz4CompressionMethod(context.ActiveBuildTargetGroup, context.ActiveBuildTarget))
            {
                var compressionType = GetCompressionTypeAsString(context.ActiveBuildTargetGroup);
                switch (compressionType)
                {
                    case "Lz4":
                        buildOptions |= BuildOptions.CompressWithLz4;
                        break;
                    case "Lz4HC":
                        buildOptions |= BuildOptions.CompressWithLz4HC;
                        break;
                    default:
                        buildOptions |= BuildOptions.None;
                        break;
                }
            }

            buildOptions |= BuildOptions.DetailedBuildReport;
            context.OptionsBuilder.AddOptions(buildOptions);

            var subtarget = GetSelectedSubtargetFor(context.ActiveBuildTarget);
            context.OptionsBuilder.SetSubtarget(subtarget);

            foreach (var scene in EditorBuildSettings.scenes) context.OptionsBuilder.AddScene(scene.path);
        }

        private static bool GetPlayerSettingsPlayModeTestRunnerEnabledProperty()
        {
            var playerSettingsType = typeof(PlayerSettings);

            var playModeTestRunnerEnabledProperty = playerSettingsType.GetProperty("playModeTestRunnerEnabled",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (playModeTestRunnerEnabledProperty == null)
                throw new MissingFieldException(playerSettingsType.Name, "playModeTestRunnerEnabled");

            return (bool)playModeTestRunnerEnabledProperty.GetValue(null);
        }

        private static Type GetPostprocessBuildPlayerType()
        {
            var coreModuleAssembly = Assembly.Load("UnityEditor.CoreModule");
            return coreModuleAssembly.GetType("UnityEditor.PostprocessBuildPlayer");
        }

        private static Type GetCompressionType()
        {
            var coreModuleAssembly = Assembly.Load("UnityEditor.CoreModule");
            return coreModuleAssembly.GetType("UnityEditor.Compression");
        }

        private static bool SupportsLz4CompressionMethod(BuildTargetGroup targetGroup, BuildTarget target)
        {
            var postprocessBuildPlayerType = GetPostprocessBuildPlayerType();

            // since 2023.3.0, overload has been added
            var supportsLz4CompressionMethod = postprocessBuildPlayerType.GetMethod("SupportsLz4Compression",
                BindingFlags.Public | BindingFlags.Static, Type.DefaultBinder,
                new[] { typeof(BuildTargetGroup), typeof(BuildTarget) }, null);

            if (supportsLz4CompressionMethod == null)
                throw new MissingMethodException(postprocessBuildPlayerType.Name, "SupportsLz4Compression");

            return (bool)supportsLz4CompressionMethod.Invoke(null, new object[] { targetGroup, target });
        }

        private static string GetCompressionTypeAsString(BuildTargetGroup targetGroup)
        {
            var editorUserBuildSettingsType = typeof(EditorUserBuildSettings);

            var getCompressionTypeMethod = editorUserBuildSettingsType.GetMethod("GetCompressionType",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (getCompressionTypeMethod == null)
                throw new MissingMethodException(editorUserBuildSettingsType.Name, "GetCompressionType");

            var compression = getCompressionTypeMethod.Invoke(null, new object[] { targetGroup });

            return Enum.GetName(GetCompressionType(), compression);
        }

        private static int GetSelectedSubtargetFor(BuildTarget buildTarget)
        {
            var editorUserBuildSettingsType = typeof(EditorUserBuildSettings);

            var getActiveSubtargetForMethod = editorUserBuildSettingsType.GetMethod("GetSelectedSubtargetFor",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (getActiveSubtargetForMethod == null)
                throw new MissingMethodException(editorUserBuildSettingsType.Name, "GetSelectedSubtargetFor");

            return (int)getActiveSubtargetForMethod.Invoke(null, new object[] { buildTarget });
        }

        private static string GetCustomConnectionID()
        {
            // signature has not changed since 2021.2.0
            return (string)typeof(Editor).Assembly
                .GetType("UnityEditor.Profiling.ProfilerUserSettings")
                .GetProperty("customConnectionID", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }

        private static bool SupportsInstallInBuildFolder(BuildTargetGroup targetGroup,
            BuildTarget target)
        {
#if UNITY_2023_3_OR_NEWER // since 2023.3.0
            return (bool)GetPostprocessBuildPlayerType().GetMethod("SupportsInstallInBuildFolder",
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { targetGroup });
#else // since 2017.1.0
            return (bool)GetPostprocessBuildPlayerType().GetMethod("SupportsInstallInBuildFolder",
                BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { targetGroup, target });
#endif
        }
    }
}
