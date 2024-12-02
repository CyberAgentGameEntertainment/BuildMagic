// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using UnityEditor;

namespace BuildMagicEditor
{
    /// <summary>
    ///     A builder for <see cref="BuildPlayerOptions" />.
    /// </summary>
    public sealed class BuildPlayerOptionsBuilder
    {
        private readonly BuildTarget _buildTarget;
        private readonly BuildTargetGroup _buildTargetGroup;
        private readonly List<string> _extraScriptingDefines;
        private readonly List<string> _scenes;

        private string _assetBundleManifestPath;
        private string _locationPathName;
        private BuildOptions _options;
        private int _subtarget;

        /// <summary>
        ///     Create a new instance of <see cref="BuildPlayerOptionsBuilder" />.
        /// </summary>
        /// <param name="buildTarget">Build target platform.</param>
        internal BuildPlayerOptionsBuilder(BuildTarget buildTarget, BuildTargetGroup buildTargetGroup)
        {
            _buildTarget = buildTarget;
            _buildTargetGroup = buildTargetGroup;

            _extraScriptingDefines = new List<string>();
            _scenes = new List<string>();

            _locationPathName = null;
            _assetBundleManifestPath = null;
            _subtarget = 0;
            _options = BuildOptions.None;
        }

        /// <summary>
        ///     Create a new instance of <see cref="BuildPlayerOptionsBuilder" />.
        /// </summary>
        public static BuildPlayerOptionsBuilder Create()
        {
            return new BuildPlayerOptionsBuilder(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.selectedBuildTargetGroup);
        }

        /// <summary>
        ///     The path where the application will be built.
        /// </summary>
        /// <param name="locationPathName">The path of artifacts.</param>
        public void SetLocationPathName(string locationPathName)
        {
            _locationPathName = locationPathName;
        }

        /// <summary>
        ///     Add a scene to the build.
        /// </summary>
        /// <param name="scene">scene path</param>
        public void AddScene(string scene)
        {
            _scenes.Add(scene);
        }

        /// <summary>
        ///     The path to an manifest file describing all of the asset bundles used in the build (optional).
        /// </summary>
        /// <param name="assetBundleManifestPath">The path to an manifest file</param>
        public void SetAssetBundleManifestPath(string assetBundleManifestPath)
        {
            _assetBundleManifestPath = assetBundleManifestPath;
        }

        /// <summary>
        ///     The Subtarget to build.
        /// </summary>
        /// <param name="subtarget">subtarget</param>
        public void SetSubtarget(int subtarget)
        {
            _subtarget = subtarget;
        }

        /// <summary>
        ///     Additional BuildOptions, like whether to run the built player.
        /// </summary>
        /// <param name="option">build options</param>
        public void AddOptions(BuildOptions option)
        {
            _options |= option;
        }

        /// <summary>
        ///     User-specified preprocessor defines used while compiling assemblies for the player.
        /// </summary>
        /// <param name="extraScriptingDefines">Additional symbols.</param>
        public void AddExtraScriptingDefines(IReadOnlyList<string> extraScriptingDefines)
        {
            _extraScriptingDefines.AddRange(extraScriptingDefines);
        }

        /// <summary>
        ///     Build the <see cref="BuildPlayerOptions" />.
        /// </summary>
        public BuildPlayerOptions Build()
        {
            var options = _options;

#if BUILDMAGIC_NO_DETAILED_BUILD_REPORT
            options &= ~BuildOptions.DetailedBuildReport;
#endif

            return new BuildPlayerOptions
            {
                locationPathName = string.IsNullOrWhiteSpace(_locationPathName) ? GetDefaultBuildPath(_buildTarget) : _locationPathName,
                assetBundleManifestPath = _assetBundleManifestPath,
                scenes = _scenes.ToArray(),
                target = _buildTarget,
                targetGroup = _buildTargetGroup,
                subtarget = _subtarget,
                options = options,
                extraScriptingDefines = _extraScriptingDefines.ToArray()
            };
        }
        
        /// <summary>
        ///     Get the default build path for the specified build target.
        /// </summary>
        /// <param name="target">Build target platform.</param>
        /// <returns>Default build path.</returns>
        public static string GetDefaultBuildPath(BuildTarget target)
        {
            return target switch
            {
                BuildTarget.iOS                 => "Builds/iOS",
                BuildTarget.Android             => $"Builds/Android/App.{GetAndroidExtension()}",
                BuildTarget.StandaloneOSX       => "Builds/MacOS/App.app",
                BuildTarget.StandaloneWindows   => "Builds/Windows/App.exe",
                BuildTarget.StandaloneWindows64 => "Builds/Windows64/App.exe",
                BuildTarget.WebGL               => "Builds/WebGL",
                _                               => ""
            };
            
            string GetAndroidExtension()
            {
                return EditorUserBuildSettings.buildAppBundle ? "aab" : "apk";
            }
        }
    }
}
