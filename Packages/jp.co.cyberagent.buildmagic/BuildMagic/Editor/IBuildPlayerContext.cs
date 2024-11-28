// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEditor;
using UnityEditor.Build.Reporting;

namespace BuildMagicEditor
{
    /// <summary>
    ///     Build context for executing build player tasks.
    /// </summary>
    public interface IBuildPlayerContext : IBuildContext
    {
        /// <summary>
        ///     Specified Build options.
        /// </summary>
        public BuildPlayerOptions BuildPlayerOptions { get; }

        /// <summary>
        ///     Set the result of the build.
        /// </summary>
        void SetResult(BuildReport buildReport);

        /// <summary>
        ///     Get the result of the build.
        /// </summary>
        bool TryGetResult(out BuildReport buildReport);
    }
}
