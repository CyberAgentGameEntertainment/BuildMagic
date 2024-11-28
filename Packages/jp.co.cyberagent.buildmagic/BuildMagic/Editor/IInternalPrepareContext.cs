// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEditor;

namespace BuildMagicEditor
{
    /// <summary>
    ///     Build context for internal prepare build player tasks.
    /// </summary>
    public interface IInternalPrepareContext : IBuildContext
    {
        /// <summary>
        ///     Builder for <see cref="UnityEditor.BuildOptions" />
        /// </summary>
        BuildPlayerOptionsBuilder OptionsBuilder { get; }

        public BuildTargetGroup ActiveBuildTargetGroup { get; }
    }
}
