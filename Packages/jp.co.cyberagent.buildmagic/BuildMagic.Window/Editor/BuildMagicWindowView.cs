// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagic.Window.Editor.Elements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor
{
    internal sealed class BuildMagicWindowView : IDisposable
    {
        private readonly VisualElement _rootVisualElement;

        private readonly LeftPaneView _leftPaneView;
        private readonly RightPaneView _rightPaneView;

        public BuildMagicWindowView(VisualElement rootVisualElement)
        {
            _rootVisualElement = rootVisualElement;

            var visualTree = AssetLoader.LoadUxml("Window");
            visualTree.CloneTree(_rootVisualElement);

            _leftPaneView = _rootVisualElement.Q<LeftPaneView>();
            _rightPaneView = _rootVisualElement.Q<RightPaneView>();

            var styleSheet = AssetLoader.LoadUss("BuildMagicWindow");
            _rootVisualElement.styleSheets.Add(styleSheet);
            var modeStyleSheet = AssetLoader.LoadUss(EditorGUIUtility.isProSkin ? "BuildMagicWindow_Dark" : "BuildMagicWindow_Light");
            _rootVisualElement.styleSheets.Add(modeStyleSheet);
        }

        public ILeftPaneView LeftPaneView => _leftPaneView;
        public IRightPaneView RightPaneView => _rightPaneView;

        public void Dispose()
        {
        }

        public void Bind(SerializedObject so)
        {
            _rootVisualElement.Bind(so);
        }

        public void Unbind()
        {
            _rootVisualElement.Unbind();
        }

        public void UpdateSelectedIndex(int index)
        {
            _leftPaneView.UpdateSelectedIndex(index);
            _rightPaneView.SetSelected(index >= 0);
        }
    }
}
