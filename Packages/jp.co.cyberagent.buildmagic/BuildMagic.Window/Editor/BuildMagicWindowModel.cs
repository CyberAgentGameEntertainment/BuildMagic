// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using BuildMagic.Window.Editor.Foundation.TinyRx.ObservableProperty;
using BuildMagicEditor;
using BuildMagicEditor.BuiltIn;
using BuildMagicEditor.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using BuildPipeline = BuildMagicEditor.BuildPipeline;

namespace BuildMagic.Window.Editor
{
    internal sealed class BuildMagicWindowModel : ScriptableObject
    {
        // HACK: this is used to indicate _selectedBase is empty without making UI Toolkit binding system confused.
        private static readonly BuildScheme EmptyBuildScheme = new();

        private const int DefaultSelectedIndex = -1;
        private const string BuiltinTemplateName = "< BuiltinTemplate >";

        [SerializeReference]
        private BuildScheme
            _selected = new(); // HACK: Assign a dummy as the initial value because if it is null at binding, the change will not be applied.

        [SerializeReference] private BuildScheme _selectedBase = EmptyBuildScheme;

        [SerializeReference] private List<BuildScheme> _schemes = new();

        [SerializeField]
        private ObservableProperty<int> _selectedIndex = new(DefaultSelectedIndex);

        public IReadOnlyObservableProperty<int> SelectedIndex => _selectedIndex;

        public ICollection<string> SchemeNamesWithTemplate => _schemes.Select(s => s.Name).Prepend(BuiltinTemplateName).ToArray();

        public ICollection<string> RootSchemeNames => _schemes.Where(s => string.IsNullOrEmpty(s.BaseSchemeName))
            .Select(s => s.Name).ToArray();

        public void Initialize()
        {
            CollectSchemes();
            EditorUtility.ClearDirty(this);
        }

        private void CollectSchemes()
        {
            _schemes.Clear();
            _schemes.AddRange(BuildSchemeLoader.LoadAll<BuildScheme>());
        }

        public void Create(string newSchemeName, string copyFromName, string baseSchemeName)
        {
            Undo.RecordObject(this, $"{UndoRedoEventName.Create}|{newSchemeName}");

            var newSetting = new BuildScheme();

            if (string.IsNullOrEmpty(copyFromName) == false)
            {
                var baseSetting = copyFromName == BuiltinTemplateName
                    ? BuildSchemeLoader.LoadTemplate()
                    : _schemes.FirstOrDefault(s => s.Name == copyFromName);
                if (baseSetting != null)
                    newSetting = JsonUtility.FromJson<BuildScheme>(JsonUtility.ToJson(baseSetting));
            }

            newSetting.Name = newSchemeName;
            newSetting.BaseSchemeName = baseSchemeName;

            BuildSchemeLoader.Save(newSetting);
            _schemes.Add(newSetting);
            _schemes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            if (_selected != null)
                _selectedIndex.Value = _schemes.IndexOf(_selected);
            EditorUtility.SetDirty(this);
        }

        public void Remove(string targetSchemeName)
        {
            Undo.RecordObject(this, $"{UndoRedoEventName.Remove}|{targetSchemeName}");

            BuildScheme targetScheme;
            if (_selected != null && _selected.Name == targetSchemeName)
            {
                targetScheme = _selected;
                _selected = null;
                _selectedBase = EmptyBuildScheme;
                _selectedIndex.Value = DefaultSelectedIndex;
            }
            else
                targetScheme = _schemes.FirstOrDefault(s => s.Name == targetSchemeName);

            Assert.IsNotNull(targetScheme, "No selected scheme");
            _schemes.Remove(targetScheme);

            BuildSchemeLoader.Remove(targetScheme);
        }

        public void PreBuild()
        {
            Assert.IsNotNull(_selected, "No selected scheme");

            PreBuildInternal(_selected);
        }
        
        public void PreBuildByName(string targetSchemeName)
        {
            var targetScheme = _schemes.FirstOrDefault(s => s.Name == targetSchemeName);
            Assert.IsNotNull(targetScheme, $"{targetSchemeName} is not found");

            PreBuildInternal(targetScheme);
        }
        
        private void PreBuildInternal(BuildScheme targetScheme)
        {
            var configurations =
                BuildSchemeUtility.EnumerateComposedConfigurations<IPreBuildContext>(targetScheme, _schemes);

            BuildPipeline.PreBuild(BuildTaskBuilderUtility.CreateBuildTasks<IPreBuildContext>(configurations));
        }

        public void Build(string buildPath)
        {
            Assert.IsNotNull(_selected, "No selected scheme");

            var internalPrepareTasks = new List<IBuildTask<IInternalPrepareContext>>();
            internalPrepareTasks.Add(new BuildPlayerOptionsApplyEditorSettingsTask());
            internalPrepareTasks.AddRange(
                BuildTaskBuilderUtility.CreateBuildTasks<IInternalPrepareContext>(
                    _selected.InternalPrepareConfigurations));

            var configurations =
                BuildSchemeUtility.EnumerateComposedConfigurations<IPostBuildContext>(_selected, _schemes);

            var postBuildTasks = BuildTaskBuilderUtility
                .CreateBuildTasks<IPostBuildContext>(configurations).ToList();
            postBuildTasks.Add(new LogBuildResultTask());

            var buildPlayerOptions = internalPrepareTasks.GenerateBuildPlayerOptions();
            buildPlayerOptions.locationPathName = buildPath;
            BuildPipeline.Build(buildPlayerOptions, postBuildTasks);
        }

        public void Select(int index)
        {
            Undo.RecordObject(this, $"{UndoRedoEventName.ChangeIndex}|{_selectedIndex.Value} -> {index}");

            if (index < 0)
            {
                _selected = null;
                _selectedBase = EmptyBuildScheme;
                _selectedIndex.Value = DefaultSelectedIndex;
            }
            else
            {
                _selected = _schemes[index];
                _selectedBase = _schemes.FirstOrDefault(s => s.Name == _selected.BaseSchemeName) ?? EmptyBuildScheme;
                _selectedIndex.Value = index;
            }
        }

        public void Select(string schemeName)
        {
            var index = _schemes.FindIndex(scheme => scheme.Name == schemeName);
            if (index < 0)
            {
                return;
            }

            Select(index);
        }

        public void UndoRedo(in UndoRedoInfo info)
        {
            if (info.undoName.Contains(UndoRedoEventName.Create))
                CreateEvent(info);
            else if (info.undoName.Contains(UndoRedoEventName.Remove))
                RemoveEvent(info);
            else if (info.undoName.Contains(UndoRedoEventName.ChangeIndex))
                ChangeIndexEvent(info);
            return;

            void CreateEvent(in UndoRedoInfo info)
            {
                var schemeName = info.undoName.Split('|')[1];
                if (info.isRedo)
                    BuildSchemeLoader.Save(_schemes.FirstOrDefault(s => s.Name == schemeName));
                else
                    BuildSchemeLoader.Remove(schemeName);
                _selectedIndex.SetValueAndNotify(_selectedIndex.Value);
                EditorUtility.SetDirty(this);
            }
            
            void RemoveEvent(in UndoRedoInfo info)
            {
                var schemeName = info.undoName.Split('|')[1];
                if (info.isRedo)
                    BuildSchemeLoader.Remove(schemeName);
                else
                    BuildSchemeLoader.Save(_schemes.FirstOrDefault(s => s.Name == schemeName));
                _selectedIndex.SetValueAndNotify(_selectedIndex.Value);
            }
            
            void ChangeIndexEvent(in UndoRedoInfo _)
            {
                _selectedIndex.SetValueAndNotify(_selectedIndex.Value);
            }
        }

        public void AddConfiguration(ConfigurationType configurationType, Type type)
        {
            Assert.IsNotNull(_selected, "No selected scheme");
            Assert.IsTrue(configurationType != ConfigurationType.None, "Invalid configuration type");
            Assert.IsNotNull(type, "type is null");
            
            Undo.RecordObject(this, $"{UndoRedoEventName.AddConfiguration}|{configurationType}|{type.Name}");
            
            var configuration = (IBuildConfiguration)Activator.CreateInstance(type);
            Assert.IsNotNull(configuration, "Failed to create configuration");

            switch (configurationType)
            {
                case ConfigurationType.PreBuild:
                    _selected.AddPreBuildConfiguration(configuration);
                    break;
#if BUILDMAGIC_DEVELOPER
                case ConfigurationType.InternalPrepare:
                    _selected.AddInternalPrepareConfiguration(configuration);
                    break;
#endif
                case ConfigurationType.PostBuild:
                    _selected.AddPostBuildConfiguration(configuration);
                    break;
                case ConfigurationType.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(configurationType), configurationType, null);
            }
            EditorUtility.SetDirty(this);
        }

        public void RemoveConfiguration(ConfigurationType configurationType, int index, IBuildConfiguration configuration)
        {
            Assert.IsNotNull(_selected, "No selected scheme");
            Assert.IsTrue(configurationType != ConfigurationType.None, "Invalid configuration type");
            
            Undo.RecordObject(this, $"{UndoRedoEventName.RemoveConfiguration}|{configurationType}|{configuration?.GetType().Name ?? "Missing"}");

            var targetList = configurationType switch
            {
                ConfigurationType.PreBuild => _selected.PreBuildConfigurations,
#if BUILDMAGIC_DEVELOPER
                ConfigurationType.InternalPrepare => _selected.InternalPrepareConfigurations,
#endif
                ConfigurationType.PostBuild => _selected.PostBuildConfigurations,
                _ => throw new ArgumentOutOfRangeException(nameof(configurationType), configurationType, null)
            };
            Assert.AreEqual(configuration, targetList[index], "Invalid configuration");

            switch (configurationType)
            {
                case ConfigurationType.PreBuild:
                    _selected.RemovePreBuildConfiguration(index);
                    break;
#if BUILDMAGIC_DEVELOPER
                case ConfigurationType.InternalPrepare:
                    _selected.RemovePrepareBuildPlayerConfiguration(index);
                    break;
#endif
                case ConfigurationType.PostBuild:
                    _selected.RemovePostBuildConfiguration(index);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(configurationType), configurationType, null);
            }

            EditorUtility.SetDirty(this);
        }
        
        public void Save()
        {
            _schemes.ForEach(BuildSchemeLoader.Save);
            EditorUtility.ClearDirty(this);
        }

        public HashSet<Type> GetConfigurationTypes()
        {
            Assert.IsNotNull(_selected, "No selected scheme");
            
            var list = new List<Type>();
            list.AddRange(_selected.PreBuildConfigurations.Select(c => c.GetType()));
#if BUILDMAGIC_DEVELOPER
            list.AddRange(_selected.InternalPrepareConfigurations.Select(c => c.GetType()));
#endif
            list.AddRange(_selected.PostBuildConfigurations.Select(c => c.GetType()));
            return list.ToHashSet();
        }

        private static class UndoRedoEventName
        {
            public const string Create = "Create scheme";
            public const string Remove = "Remove scheme";
            public const string ChangeIndex = "index changed";
            public const string AddConfiguration = "Add configuration";
            public const string RemoveConfiguration = "Remove configuration";
        }
    }
}
