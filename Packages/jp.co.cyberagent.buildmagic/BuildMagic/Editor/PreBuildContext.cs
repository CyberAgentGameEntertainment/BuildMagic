// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEditor;

namespace BuildMagicEditor
{
    /// <summary>
    ///     The pre build context.
    /// </summary>
    public class PreBuildContext : IPreBuildContext
    {
        internal PreBuildContext(BuildTarget activeBuildTarget)
        {
            ActiveBuildTarget = activeBuildTarget;
        }

        /// <summary>
        ///     Current build target.
        /// </summary>
        public BuildTarget ActiveBuildTarget { get; }

        /// <summary>
        ///     Create a new instance of <see cref="PreBuildContext" />.
        /// </summary>
        public static PreBuildContext Create()
        {
            return new PreBuildContext(EditorUserBuildSettings.activeBuildTarget);
        }
    }
}
