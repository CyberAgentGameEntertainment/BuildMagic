// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagic.Window.Editor.UnityInternal;
using UnityEditor;
using UnityEngine;

namespace BuildMagic.Window.Editor
{
    /// <summary>
    ///     Build Magic window.
    /// </summary>
    public class BuildMagicWindow : EditorWindow, ISerializationCallbackReceiver
    {
        private static readonly Vector2 MinSize = new(400, 300); // TODO: Temporary size

        [SerializeField]
        private BuildMagicWindowModel _model;

        [SerializeField]
        private string _modelCache;

        private BuildMagicWindowPresenter _presenter;

        private void OnEnable()
        {
            minSize = MinSize;
        }

        private void OnDisable()
        {
            _presenter?.Dispose();
            _presenter = null;
            
            AssetSaveProcessor.OnWillSaveAssetsEvent -= OnWillSaveAssets;
        }

        private void CreateGUI()
        {
            UnityInternalProxy.ReplaceVisualTreeBindingsUpdater(this);
            UnityInternalProxy.ReplaceUIRLayoutUpdater(this);

            if (_model == null)
            {
                _model = CreateInstance<BuildMagicWindowModel>();
                _model.Initialize();
                if (_modelCache != null)
                    JsonUtility.FromJsonOverwrite(_modelCache, _model);
                _modelCache = null;
                _model.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor;
            }

            AssetSaveProcessor.OnWillSaveAssetsEvent += OnWillSaveAssets;

            _presenter = new BuildMagicWindowPresenter(_model, rootVisualElement);
            _presenter.Setup();
        }

        private void OnGUI()
        {
            hasUnsavedChanges = _model != null && EditorUtility.IsDirty(_model);
        }

        [MenuItem("Window/Build Magic")]
        private static void ShowWindow()
        {
            var window = GetWindow<BuildMagicWindow>();
            window.Show();
            
            window.saveChangesMessage = "There may be unsaved changes. Do you want to save them?";
        }

        public void OnBeforeSerialize()
        {
            _modelCache = JsonUtility.ToJson(_model);
        }

        public void OnAfterDeserialize()
        {
        }
        
        public override void SaveChanges()
        {
            _model.Save();
            base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            base.DiscardChanges();
        }

        public void SelectScheme(string schemeName)
        {
            _model?.Select(schemeName);
        }

        private void OnWillSaveAssets()
        {
            if (_model == null)
                return;

            if (EditorUtility.IsDirty(_model) == false)
                return;
            
            _model.Save();
            Repaint();
        }
    }
}
