// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace BuildMagicEditor
{
    [FilePath("ProjectSettings/BuildMagic/Settings.dat", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class BuildMagicSettings : ScriptableSingleton<BuildMagicSettings>
    {
        private Action<string> _primaryBuildSchemeChanged;
        
        public event Action<string> PrimaryBuildSchemeChanged
        {
            add
            {
                _primaryBuildSchemeChanged += value;
                value(PrimaryBuildScheme);
            }
            remove => _primaryBuildSchemeChanged -= value;
        }
        
        [FormerlySerializedAs("autoPreBuildTarget")] [SerializeField]
        private string primaryBuildScheme;

        public string PrimaryBuildScheme
        {
            get => primaryBuildScheme;
            set
            {
                primaryBuildScheme = value;
                Save(false);
                _primaryBuildSchemeChanged?.Invoke(primaryBuildScheme);
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplicationUtility.projectWasLoaded += () =>
            {
                var target = instance.PrimaryBuildScheme;
                if (string.IsNullOrEmpty(target))
                    return;

                var scheme = BuildSchemeLoader.Load<BuildScheme>(target);
                if (scheme == null)
                    return;

                var preBuildTask = BuildTaskBuilderUtility.CreateBuildTasks<IPreBuildContext>(scheme.PreBuildConfigurations);
                BuildPipeline.PreBuild(preBuildTask);
            };
        }
    }
}
