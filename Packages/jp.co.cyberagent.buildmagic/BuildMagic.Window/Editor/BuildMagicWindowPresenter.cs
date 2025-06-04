// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using BuildMagic.Window.Editor.Foundation.TinyRx;
using BuildMagic.Window.Editor.SubWindows;
using BuildMagic.Window.Editor.Utilities;
using BuildMagicEditor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor
{
    internal sealed class BuildMagicWindowPresenter : IDisposable
    {
        private readonly BuildMagicWindowModel _model;
        private readonly BuildMagicWindowView _view;

        private readonly CompositeDisposable _bindDisposable = new();
        
        private readonly FileSystemWatcher _fileSystemWatcher;

        private Action _pendingAction;
        private readonly object _pendingActionLock = new();
        private volatile bool _pendingActionInProgress;

        public BuildMagicWindowPresenter(BuildMagicWindowModel model, VisualElement rootVisualElement)
        {
            _model = model;
            _view = new BuildMagicWindowView(rootVisualElement);
            _fileSystemWatcher = BuildSchemeLoader.CreateFileSystemWatcher();
        }

        public void Dispose()
        {
            CleanupViewEventHandlers();
            CleanupFileSystemEventHandlers();
            Unbind();
            _view.Dispose();
        }

        public void Setup()
        {
            SetupViewEventHandlers();
            SetupFileSystemEventHandlers();
            Bind();
        }

        private void SetupViewEventHandlers()
        {
            Undo.undoRedoEvent += OnUndoRedo;

            var contextualOptionsFactory = new BuildSchemeContextualActionsFactory();
            contextualOptionsFactory.CopyCreateRequested += CopyCreate;
            contextualOptionsFactory.InheritCreateRequested += InheritCreate;
            contextualOptionsFactory.RemoveRequested += Remove;
            contextualOptionsFactory.PreBuildRequested += PreBuild;
            contextualOptionsFactory.BuildRequested += Build;
            contextualOptionsFactory.SetAsPrimaryRequested += SetAsPrimary;
            contextualOptionsFactory.UnsetPrimaryRequested += UnsetPrimary;
            contextualOptionsFactory.IsPrimary = schemeName =>
                BuildMagicSettings.instance.PrimaryBuildScheme == schemeName;

            _view.LeftPaneView.ContextualActionsFactory = contextualOptionsFactory;
            _view.LeftPaneView.ContextualActionsForSelectedScheme =
                contextualOptionsFactory.Create(() => _model.SelectedBuildSchemeName);
            _view.LeftPaneView.OnSelectionChanged += OnSelectionChanged;
            _view.LeftPaneView.SaveRequested += Save;
            _view.LeftPaneView.NewBuildSchemeRequested += NewScheme;

            _view.RightPaneView.AddRequested += OnAddRequested;
            _view.RightPaneView.RemoveRequested += RemoveConfiguration;
            _view.RightPaneView.PasteRequested += PasteConfiguration;
        }
        
        private void SetupFileSystemEventHandlers()
        {
            _fileSystemWatcher.Changed += OnFileSystemChanged;
            _fileSystemWatcher.Created += OnFileSystemChanged;
            _fileSystemWatcher.Deleted += OnFileSystemChanged;
            _fileSystemWatcher.Renamed += OnFileSystemChanged;

            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void CleanupViewEventHandlers()
        {
            _view.RightPaneView.PasteRequested -= PasteConfiguration;
            _view.RightPaneView.RemoveRequested -= RemoveConfiguration;
            _view.RightPaneView.AddRequested -= OnAddRequested;

            _view.LeftPaneView.SaveRequested -= Save;
            _view.LeftPaneView.OnSelectionChanged -= OnSelectionChanged;
            _view.LeftPaneView.NewBuildSchemeRequested -= NewScheme;

            Undo.undoRedoEvent -= OnUndoRedo;
        }
        
        private void CleanupFileSystemEventHandlers()
        {
            _fileSystemWatcher.Changed -= OnFileSystemChanged;
            _fileSystemWatcher.Created -= OnFileSystemChanged;
            _fileSystemWatcher.Deleted -= OnFileSystemChanged;
            _fileSystemWatcher.Renamed -= OnFileSystemChanged;
            
            _fileSystemWatcher.Dispose();
        }

        private void Bind()
        {
            // TinyRx runs OnNext immediately
            _model.SelectedIndex
                  .Subscribe(index =>
                  {
                      using (new DebugLogDisabledScope())
                          _view.Bind(new SerializedObject(_model)); // HACK: SerializeReference bindings are still not working properly in some cases. Rebind and force update
                      _view.UpdateSelectedIndex(index);
                  })
                  .DisposeWith(_bindDisposable);
        }

        private void Unbind()
        {
            _bindDisposable.Dispose();
            _view.Unbind();
        }

        private void Reload()
        {
            using var scope = new FileWatcherSuspender(_fileSystemWatcher);
            _model.Reload();
        }

        #region EventHandlers

        private void NewScheme()
        {
            CopyCreate(string.Empty);
        }

        private void CopyCreate(string copyFromName)
        {
            var baseSchemeName = _model.GetBaseSchemeName(copyFromName);
            var context = new CreateSchemeModalWindow.Context(copyFromName, baseSchemeName, _model.SchemeNamesWithTemplate,
                _model.RootSchemeNames, _model.Create);
            CreateSchemeModalWindow.OpenModal(context);
        }

        private void InheritCreate(string baseSchemeName)
        {
            var context = new CreateSchemeModalWindow.Context("", baseSchemeName, _model.SchemeNamesWithTemplate,
                _model.RootSchemeNames, _model.Create);
            CreateSchemeModalWindow.OpenModal(context);
        }

        private void Remove(string targetSettingName)
        {
            using var scope = new FileWatcherSuspender(_fileSystemWatcher);
            _model.Remove(targetSettingName);
        }

        private void PreBuild(string targetSchemeName)
        {
            _model.PreBuild(targetSchemeName);
        }

        private void Build(string targetSchemeName)
        {
            var buildPath = EditorUtility.SaveFilePanel("Build Application Path", "", "", "");
            if (string.IsNullOrWhiteSpace(buildPath))
                return;
            _model.Build(targetSchemeName, buildPath);
        }

        private void SetAsPrimary(string targetSchemeName)
        {
            BuildMagicSettings.instance.PrimaryBuildScheme = targetSchemeName;
        }

        private void UnsetPrimary(string targetSchemeName)
        {
            if (BuildMagicSettings.instance.PrimaryBuildScheme != targetSchemeName)
                return;

            BuildMagicSettings.instance.PrimaryBuildScheme = string.Empty;
        }

        private void OnSelectionChanged(int index)
        {
            _model.Select(index);
        }

        private void OnAddRequested(Rect rect)
        {
            var existConfigurations = _model.GetConfigurationTypes();
            var context = new SelectConfigurationDropdown.Context(existConfigurations, _model.AddConfiguration);
            new SelectConfigurationDropdown(context, new AdvancedDropdownState()).Show(rect);
        }

        private void RemoveConfiguration(ConfigurationType type, int index, IBuildConfiguration configuration)
        {
            _model.RemoveConfiguration(type, index, configuration);
        }

        private void PasteConfiguration(ConfigurationType type, string json)
        {
            var existConfigurations = _model.GetConfigurationTypes();
            
            try
            {
                var serializable = SerializableConfiguration.FromJson(json);
                if (existConfigurations.Contains(serializable.ConfigurationType))
                {
                    EditorUtility.DisplayDialog("Paste Error",
                                                $"Configuration of type '{serializable.ConfigurationType.Name}' already exists.", "OK");
                    return;
                }

                _model.AddConfiguration(type, serializable.ConfigurationType, serializable.ValueJson);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Paste Error",
                                            $"Failed to paste configuration from clipboard: {e.Message}", "OK");
            }
        }

        private void Save()
        {
            using var scope = new FileWatcherSuspender(_fileSystemWatcher);
            _model.Save();
        }

        private void OnUndoRedo(in UndoRedoInfo info)
        {
            using var scope = new FileWatcherSuspender(_fileSystemWatcher);
            _model.UndoRedo(info);
            if (info.undoName.StartsWith("Modified Selected.") && info.undoName.Contains("Configurations in"))
                using (new DebugLogDisabledScope())
                    _view.Bind(new SerializedObject(_model)); // HACK: Undo/Redo sorting of list with SerializeReference breaks view, so rebind and force update
        }

        #endregion

        #region FileSystemEventHandlers

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            if (_pendingActionInProgress)
                return; // If an action is already in progress, ignore this event
            
            lock (_pendingActionLock)
            {
                if (_pendingAction != null)
                    return; // If there is already a pending action, ignore this event to avoid multiple dialogs

                _pendingAction = () =>
                {
                    using var scope = new FileWatcherSuspender(_fileSystemWatcher);
                    
                    var opt = EditorUtility.DisplayDialog("File Changed",
                                                          $"The file '{e.Name}' has been changed outside of the editor. Do you want to reload?",
                                                          "Reload", "Cancel");
                    if (opt)
                        Reload();

                    lock (_pendingActionLock)
                        _pendingActionInProgress = false;
                };
            }
        }

        #endregion
        
        public void Update()
        {
            Action pendingAction = null;
            if (Monitor.TryEnter(_pendingActionLock))
            {
                try
                {
                    pendingAction = _pendingAction;
                    _pendingAction = null;
                    if (pendingAction != null)
                        _pendingActionInProgress = true;
                }
                finally
                {
                    Monitor.Exit(_pendingActionLock);
                }
            }
            
            pendingAction?.Invoke();
        }

        private class BuildSchemeContextualActionsFactory : IBuildSchemeContextualActionsFactory
        {
            public event Action<string> CopyCreateRequested;
            public event Action<string> InheritCreateRequested;
            public event Action<string> RemoveRequested;
            public event Action<string> PreBuildRequested;
            public event Action<string> BuildRequested;
            public event Action<string> SetAsPrimaryRequested;
            public event Action<string> UnsetPrimaryRequested;
            public Func<string, bool> IsPrimary { private get; set; }

            public IBuildSchemeContextualActions Create(Func<string> getSchemeName)
            {
                return new BuildSchemeContextualActions(getSchemeName, this);
            }

            private class BuildSchemeContextualActions : IBuildSchemeContextualActions
            {
                private readonly Func<string> _getSchemeName;
                private readonly BuildSchemeContextualActionsFactory _factory;

                public BuildSchemeContextualActions(Func<string> getSchemeName,
                    BuildSchemeContextualActionsFactory factory)
                {
                    _getSchemeName = getSchemeName;
                    _factory = factory;
                }

                public void CopyCreateRequested()
                {
                    _factory.CopyCreateRequested?.Invoke(_getSchemeName());
                }

                public void InheritCreateRequested()
                {
                    _factory.InheritCreateRequested?.Invoke(_getSchemeName());
                }

                public void RemoveRequested()
                {
                    _factory.RemoveRequested?.Invoke(_getSchemeName());
                }

                public void PreBuildRequested()
                {
                    _factory.PreBuildRequested?.Invoke(_getSchemeName());
                }

                public void BuildRequested()
                {
                    _factory.BuildRequested?.Invoke(_getSchemeName());
                }

                public void SetAsPrimaryRequested()
                {
                    _factory.SetAsPrimaryRequested?.Invoke(_getSchemeName());
                }

                public void UnsetPrimaryRequested()
                {
                    _factory.UnsetPrimaryRequested?.Invoke(_getSchemeName());
                }

                public bool IsActive => !string.IsNullOrEmpty(_getSchemeName());
                public bool IsPrimary => _factory.IsPrimary(_getSchemeName());
            }
        }
    }
}
