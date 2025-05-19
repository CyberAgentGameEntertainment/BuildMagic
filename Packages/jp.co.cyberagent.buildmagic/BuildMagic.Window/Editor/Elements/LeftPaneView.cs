// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagic.Window.Editor.SubWindows;
using BuildMagic.Window.Editor.Utilities;
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
        private IBuildSchemeContextualActionsFactory _contextualActionsFactory;

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

            toolbarMenu.menu.AppendAction("New Build Scheme...", _ => NewBuildSchemeRequested?.Invoke());
            IBuildSchemeContextualActions.PopulateMenu(() => ContextualActionsForSelectedScheme, toolbarMenu.menu,
                "Selected Build Scheme");
            toolbarMenu.menu.AppendSeparator();
            toolbarMenu.menu.AppendAction("Show Diff...", _ => DiffWindow.Open());
            toolbarMenu.menu.AppendAction("Enable \"Just before the build\" Phase (advanced)", _ =>
                {
                    var v = UserSettings.EnableInternalPrepareEditor;
                    v.Value = !v.Value;
                },
                _ => UserSettings.EnableInternalPrepareEditor.Value
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);

            _treeView = this.Q<LeftPaneTreeView>();
            Assert.IsNotNull(_treeView);
            _treeView.OnSelectionChanged += index => OnSelectionChanged(index);
        }

        public IBuildSchemeContextualActionsFactory ContextualActionsFactory
        {
            set
            {
                _contextualActionsFactory = value;
                _treeView.ContextualActionsFactory = value;
            }
        }

        public IBuildSchemeContextualActions ContextualActionsForSelectedScheme { private get; set; }

        public event Action NewBuildSchemeRequested;
        public event Action<int> OnSelectionChanged;
        public event Action SaveRequested;

        public void OnBoundSchemeListChanged(SerializedProperty schemesProp)
        {
            _treeView.BindSchemeList(schemesProp);
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
