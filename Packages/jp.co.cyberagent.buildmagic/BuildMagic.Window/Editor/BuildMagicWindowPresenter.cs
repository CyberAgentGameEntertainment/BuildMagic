// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagic.Window.Editor.Foundation.TinyRx;
using BuildMagic.Window.Editor.SubWindows;
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

        public BuildMagicWindowPresenter(BuildMagicWindowModel model, VisualElement rootVisualElement)
        {
            _model = model;
            _view = new BuildMagicWindowView(rootVisualElement);
        }

        public void Dispose()
        {
            CleanupViewEventHandlers();
            Unbind();
            _view.Dispose();
        }

        public void Setup()
        {
            SetupViewEventHandlers();
            Bind();
        }

        private void SetupViewEventHandlers()
        {
            Undo.undoRedoEvent += OnUndoRedo;

            _view.LeftPaneView.CopyCreateRequested += CopyCreate;
            _view.LeftPaneView.InheritCreateRequested += InheritCreate;
            _view.LeftPaneView.RemoveRequested += Remove;
            _view.LeftPaneView.PreBuildRequested += PreBuild;
            _view.LeftPaneView.PreBuildRequestedByName += PreBuildByName;
            _view.LeftPaneView.BuildRequested += Build;
            _view.LeftPaneView.OnSelectionChanged += OnSelectionChanged;
            _view.LeftPaneView.SaveRequested += Save;

            _view.RightPaneView.AddRequested += OnAddRequested;
            _view.RightPaneView.RemoveRequested += RemoveConfiguration;
        }

        private void CleanupViewEventHandlers()
        {
            _view.RightPaneView.RemoveRequested -= RemoveConfiguration;
            _view.RightPaneView.AddRequested -= OnAddRequested;

            _view.LeftPaneView.SaveRequested -= Save;
            _view.LeftPaneView.OnSelectionChanged -= OnSelectionChanged;
            _view.LeftPaneView.BuildRequested -= Build;
            _view.LeftPaneView.PreBuildRequestedByName -= PreBuildByName;
            _view.LeftPaneView.PreBuildRequested -= PreBuild;
            _view.LeftPaneView.RemoveRequested -= Remove;
            _view.LeftPaneView.InheritCreateRequested -= InheritCreate;
            _view.LeftPaneView.CopyCreateRequested -= CopyCreate;

            Undo.undoRedoEvent -= OnUndoRedo;
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

        #region EventHandlers

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
            _model.Remove(targetSettingName);
        }

        private void PreBuild()
        {
            _model.PreBuild();
        }
        
        private void PreBuildByName(string targetSettingName)
        {
            _model.PreBuildByName(targetSettingName);
        }

        private void Build()
        {
            var buildPath = EditorUtility.SaveFilePanel("Build Application Path", "", "", "");
            if (string.IsNullOrWhiteSpace(buildPath))
                return;
            _model.Build(buildPath);
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

        private void Save()
        {
            _model.Save();
        }

        private void OnUndoRedo(in UndoRedoInfo info)
        {
            _model.UndoRedo(info);
            if (info.undoName.StartsWith("Modified Selected.") && info.undoName.Contains("Configurations in"))
                using (new DebugLogDisabledScope())
                    _view.Bind(new SerializedObject(_model)); // HACK: Undo/Redo sorting of list with SerializeReference breaks view, so rebind and force update
        }

        #endregion
    }
}
