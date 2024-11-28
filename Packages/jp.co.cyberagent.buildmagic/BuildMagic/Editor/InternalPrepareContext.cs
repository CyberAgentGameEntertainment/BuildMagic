// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Reflection;
using UnityEditor;

namespace BuildMagicEditor
{
    /// <summary>
    ///     The prepare build context.
    /// </summary>
    internal class InternalPrepareContext : IInternalPrepareContext
    {
        private InternalPrepareContext(
            BuildTarget activeBuildTarget,
            BuildTargetGroup activeBuildTargetGroup,
            BuildPlayerOptionsBuilder optionsBuilder)
        {
            ActiveBuildTarget = activeBuildTarget;
            ActiveBuildTargetGroup = activeBuildTargetGroup;
            OptionsBuilder = optionsBuilder;
        }

        /// <summary>
        ///     Current build target.
        /// </summary>
        public BuildTarget ActiveBuildTarget { get; }

        /// <summary>
        ///     Current build target group.
        /// </summary>
        public BuildTargetGroup ActiveBuildTargetGroup { get; }

        /// <summary>
        ///     Builder for <see cref="BuildOptions" />
        /// </summary>
        public BuildPlayerOptionsBuilder OptionsBuilder { get; }
        
        /// <summary>
        ///     Create a new instance of <see cref="InternalPrepareContext" />.
        /// </summary>
        public static InternalPrepareContext Create(BuildPlayerOptionsBuilder optionsBuilder)
        {
            var editorUserBuildSettingsType = typeof(EditorUserBuildSettings);

            return new InternalPrepareContext(
                EditorUserBuildSettings.activeBuildTarget,
                (BuildTargetGroup)editorUserBuildSettingsType
                    .GetProperty("activeBuildTargetGroup", BindingFlags.NonPublic | BindingFlags.Static)
                    !.GetValue(null)
                ,
                optionsBuilder);
        }
    }
}
