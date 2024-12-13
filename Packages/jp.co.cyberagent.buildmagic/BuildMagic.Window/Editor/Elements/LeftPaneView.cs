// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagic.Window.Editor.SubWindows;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    internal sealed class LeftPaneView : VisualElement, ILeftPaneView
    {
        private readonly VisualTreeAsset _listEntryTemplate;
        private readonly LeftPaneTreeView _treeView;

        public LeftPaneView()
        {
            var visualTree = AssetLoader.LoadUxml("LeftPane");
            Assert.IsNotNull(visualTree);
            visualTree.CloneTree(this);

            var saveButton = this.Q<ToolbarButton>();
            Assert.IsNotNull(saveButton);
            saveButton.clickable.clicked += () => SaveRequested?.Invoke();

            var toolbarMenu = this.Q<ToolbarMenu>();
            Assert.IsNotNull(toolbarMenu);

            toolbarMenu.menu.AppendAction("Create a build scheme", _ => CopyCreateRequested?.Invoke(string.Empty));
            toolbarMenu.menu.AppendSeparator();
            toolbarMenu.menu.AppendAction("Remove the selected build scheme",
                _ => RemoveRequested?.Invoke(string.Empty),
                OnMenuActionStatus);
            toolbarMenu.menu.AppendAction("Switch to the selected build scheme", _ => PreBuildRequested?.Invoke(),
                OnMenuActionStatus);
            toolbarMenu.menu.AppendAction("Build with the selected scheme", _ => BuildRequested?.Invoke(),
                OnMenuActionStatus);
            toolbarMenu.menu.AppendSeparator();
            toolbarMenu.menu.AppendAction("Open diff window", _ => DiffWindow.Open());

            _treeView = this.Q<LeftPaneTreeView>();
            Assert.IsNotNull(_treeView);
            _treeView.CopyCreateRequested += value => CopyCreateRequested(value);
            _treeView.InheritCreateRequested += value => InheritCreateRequested(value);
            _treeView.RemoveRequested += value => RemoveRequested(value);
            _treeView.PreBuildRequestedByName += value => PreBuildRequestedByName(value);
            _treeView.OnSelectionChanged += index => OnSelectionChanged(index);
        }

        public event Action<string> CopyCreateRequested;
        public event Action<string> InheritCreateRequested;
        public event Action<string> RemoveRequested;
        public event Action<string> PreBuildRequestedByName;
        public event Action<int> OnSelectionChanged;
        public event Action PreBuildRequested;
        public event Action BuildRequested;
        public event Action SaveRequested;

        public void OnBind(SerializedObject model)
        {
            _treeView.BindSchemeList(model.FindProperty("_schemes"));
        }

        public void UpdateSelectedIndex(int index)
        {
            _treeView.SelectIndex(index);
        }

        private DropdownMenuAction.Status OnMenuActionStatus(DropdownMenuAction _)
        {
            var index = _treeView.selectedIndex;
            return index >= 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }

        public new class UxmlFactory : UxmlFactory<LeftPaneView, UxmlTraits>
        {
        }
    }
}
