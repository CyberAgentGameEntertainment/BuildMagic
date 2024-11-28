// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEditor.Build.Reporting;

namespace BuildMagicEditor
{
    /// <summary>
    ///     Build context for post build tasks.
    /// </summary>
    public interface IPostBuildContext : IBuildContext
    {
        /// <summary>
        ///     Report for build.
        /// </summary>
        BuildReport BuildReport { get; }
    }
}
