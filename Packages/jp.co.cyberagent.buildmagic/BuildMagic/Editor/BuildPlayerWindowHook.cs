// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using BuildMagicEditor.BuiltIn;
using BuildMagicEditor.Extensions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BuildMagicEditor
{
    [InitializeOnLoad]
    internal class BuildPlayerWindowHook : IPostprocessBuildWithReport
    {
        private static BuildScheme _hookedScheme;

        static BuildPlayerWindowHook()
        {
            // NOTE: RegisterGetBuildPlayerOptionsHandlerは排他的なので、他のコールバックが登録されると無効化される
            BuildPlayerWindow.RegisterGetBuildPlayerOptionsHandler(options =>
            {
                _hookedScheme = null;

                // Primary Schemeがない場合はデフォルト動作（BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptionsInternal()）

                var target = ScriptableSingleton<BuildMagicSettings>.instance.PrimaryBuildScheme;
                if (string.IsNullOrEmpty(target))
                    return GetBuildPlayerOptionsInternal(true, options);

                var scheme = BuildSchemeLoader.Load<BuildScheme>(target);
                if (scheme == null)
                    return GetBuildPlayerOptionsInternal(true, options);

                var result = EditorUtility.DisplayDialogComplex("BuildMagic",
                    $"Would you like to use primary build scheme \"{scheme.Name}\"?\nIt may overwrite current build settings.",
                    "Yes", "No", "Cancel");

                if (result == 2)
                    throw new OperationCanceledException();
                if (result == 1)
                    return GetBuildPlayerOptionsInternal(true, options);

                // あとでpost buildをトリガーするためにここでセット
                _hookedScheme = scheme;

                var preBuildTask =
                    BuildTaskBuilderUtility.CreateBuildTasks<IPreBuildContext>(scheme.PreBuildConfigurations);
                BuildPipeline.PreBuild(preBuildTask);

                var internalPrepareTasks = new List<IBuildTask<IInternalPrepareContext>>();
                internalPrepareTasks.Add(new BuildPlayerOptionsApplyEditorSettingsTask());
                internalPrepareTasks.AddRange(
                    BuildTaskBuilderUtility.CreateBuildTasks<IInternalPrepareContext>(
                        scheme.InternalPrepareConfigurations));

                var overrideOptions = internalPrepareTasks.GenerateBuildPlayerOptions();

                if (!PickBuildLocation(overrideOptions.targetGroup, overrideOptions.target, overrideOptions.subtarget,
                        overrideOptions.options, out var updateExistingBuild))
                    throw new OperationCanceledException();

                if (updateExistingBuild)
                    overrideOptions.options |= BuildOptions.AcceptExternalModificationsToPlayer;

                // PickBuildLocationの結果を反映
                overrideOptions.locationPathName = EditorUserBuildSettings.GetBuildLocation(overrideOptions.target);

                return overrideOptions;
            });
        }

        #region IPostprocessBuildWithReport Members

        public int callbackOrder => int.MinValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (_hookedScheme == null)
                return;

            var scheme = _hookedScheme;
            _hookedScheme = null;

            if (BuildPlayerTask.IsCurrentThreadBuildingPlayer)
                return;

            var postBuildTasks =
                BuildTaskBuilderUtility.CreateBuildTasks<IPostBuildContext>(scheme.PostBuildConfigurations);

            foreach (var task in postBuildTasks)
                task.Run(PostBuildContext.Create(report));
        }

        #endregion

        private static BuildPlayerOptions GetBuildPlayerOptionsInternal(bool askForBuildLocation,
            BuildPlayerOptions defaultBuildPlayerOptions)
        {
            // signature is not updated since 2017.1.0
            return (BuildPlayerOptions)typeof(BuildPlayerWindow.DefaultBuildMethods)
                .GetMethod("GetBuildPlayerOptionsInternal", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null,
                    new object[] { askForBuildLocation, defaultBuildPlayerOptions });
        }

        private static bool PickBuildLocation(BuildTargetGroup targetGroup, BuildTarget target, int subtarget,
            BuildOptions options, out bool updateExistingBuild)
        {
            // signature is not updated since 2021.2.0
            var parameters = new object[] { targetGroup, target, subtarget, options, false };
            var result = (bool)typeof(BuildPlayerWindow.DefaultBuildMethods)
                .GetMethod("PickBuildLocation", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, parameters);
            updateExistingBuild = (bool)parameters[4];
            return result;
        }
    }
}
