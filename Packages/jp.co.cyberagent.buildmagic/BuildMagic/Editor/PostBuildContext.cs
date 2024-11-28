// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEditor;
using UnityEditor.Build.Reporting;

namespace BuildMagicEditor
{
    /// <summary>
    ///     The post build context.
    /// </summary>
    public class PostBuildContext : IPostBuildContext
    {
        internal PostBuildContext(BuildTarget activeBuildTarget, BuildReport buildReport)
        {
            ActiveBuildTarget = activeBuildTarget;
            BuildReport = buildReport;
        }

        /// <inheritdoc cref="IBuildContext.ActiveBuildTarget" />
        public BuildTarget ActiveBuildTarget { get; }


        /// <inheritdoc cref="IPostBuildContext.BuildReport" />
        public BuildReport BuildReport { get; }

        /// <summary>
        ///     Create a new instance of <see cref="PostBuildContext" />.
        /// </summary>
        public static PostBuildContext Create(BuildReport buildReport)
        {
            return new PostBuildContext(
                EditorUserBuildSettings.activeBuildTarget, buildReport);
        }
    }
}
