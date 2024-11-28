// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEditor;
using UnityEditor.Build.Reporting;

namespace BuildMagicEditor
{
    /// <summary>
    ///     The context for building the player.
    /// </summary>
    public class BuildPlayerContext : IBuildPlayerContext
    {
        /// <inheritdoc cref="IBuildContext.ActiveBuildTarget" />
        public BuildTarget ActiveBuildTarget { get; }

        /// <inheritdoc cref="IBuildPlayerContext.BuildPlayerOptions" />
        public BuildPlayerOptions BuildPlayerOptions { get; }

        internal BuildPlayerContext(BuildTarget activeBuildTarget, BuildPlayerOptions buildPlayerOptions)
        {
            ActiveBuildTarget = activeBuildTarget;
            BuildPlayerOptions = buildPlayerOptions;
        }

        /// <inheritdoc cref="IBuildPlayerContext.SetResult" />
        public void SetResult(BuildReport buildReport)
        {
            _buildReport = buildReport;
        }

        /// <inheritdoc cref="IBuildPlayerContext.TryGetResult" />
        public bool TryGetResult(out BuildReport buildReport)
        {
            buildReport = _buildReport;
            return buildReport != null;
        }

        /// <summary>
        ///     Create a new instance of <see cref="BuildPlayerContext" />.
        /// </summary>
        public static BuildPlayerContext Create(BuildPlayerOptions buildPlayerOptions)
        {
            return new BuildPlayerContext(
                EditorUserBuildSettings.activeBuildTarget, buildPlayerOptions);
        }

        private BuildReport _buildReport;
    }
}
