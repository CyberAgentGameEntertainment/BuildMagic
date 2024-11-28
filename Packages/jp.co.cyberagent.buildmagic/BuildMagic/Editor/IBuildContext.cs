// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEditor;

namespace BuildMagicEditor
{
    /// <summary>
    ///     Context for executing <see cref="IBuildTask" />.
    /// </summary>
    public interface IBuildContext
    {
        /// <summary>
        ///     Current build target.
        /// </summary>
        BuildTarget ActiveBuildTarget { get; }
    }
}
