// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Linq;
using BuildMagicEditor;
using BuildMagic.Window.Editor.SubWindows;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    internal sealed class LeftPaneView : VisualElement, ILeftPaneView
    {
        private readonly ListView _listView;
        private readonly VisualTreeAsset _listEntryTemplate;

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
            toolbarMenu.menu.AppendAction("Remove the selected build scheme", _ => RemoveRequested?.Invoke(string.Empty),
                                          OnMenuActionStatus);
            toolbarMenu.menu.AppendAction("Switch to the selected build scheme", _ => PreBuildRequested?.Invoke(),
                                          OnMenuActionStatus);
            toolbarMenu.menu.AppendAction("Build with the selected scheme", _ => BuildRequested?.Invoke(),
                                          OnMenuActionStatus);
            toolbarMenu.menu.AppendSeparator();
            toolbarMenu.menu.AppendAction("Open diff window", _ => DiffWindow.Open());

            _listView = this.Q<ListView>();
            Assert.IsNotNull(_listView);
            _listView.bindingPath = "_schemes";
            _listView.makeItem = () =>
            {
                var item = new LeftPaneListEntryView();
                item.AddManipulator(new ContextualMenuManipulator(ev =>
                {
                    ev.menu.AppendAction("Create a build scheme copy from this", _ => CopyCreateRequested?.Invoke(item.Value));
                    ev.menu.AppendAction("Create a build scheme inherit this", _ => InheritCreateRequested?.Invoke(item.Value), item.Inheritable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    ev.menu.AppendAction("Remove this", _ => RemoveRequested?.Invoke(item.Value));
                    ev.menu.AppendAction("Switch to this", _ => PreBuildRequestedByName?.Invoke(item.Value));
                    ev.menu.AppendAction("Set as primary build scheme", _ => BuildMagicSettings.instance.PrimaryBuildScheme = item.Value,
                                            _ => item.Value == BuildMagicSettings.instance.PrimaryBuildScheme ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                }));
                return item;
            };
            _listView.bindItem = (element, index) =>
            {
                var item = _listView.itemsSource[index] as SerializedProperty;
                ((LeftPaneListEntryView)element).CustomBind(item);
            };
            _listView.unbindItem = (element, index) =>
            {
                ((LeftPaneListEntryView)element).CustomUnbind();
            };
            _listView.selectedIndicesChanged += indices =>
            {
                var tmpArray = indices.ToArray();
                OnSelectionChanged?.Invoke(tmpArray.Length > 0 ? tmpArray[0] : -1);
            };
        }

        public event Action<string> CopyCreateRequested;
        public event Action<string> InheritCreateRequested;
        public event Action<string> RemoveRequested;
        public event Action PreBuildRequested;
        public event Action<string> PreBuildRequestedByName;
        public event Action BuildRequested;
        public event Action<int> OnSelectionChanged;
        public event Action SaveRequested;

        public void UpdateSelectedIndex(int index)
        {
            _listView.selectedIndex = index;
        }

        private DropdownMenuAction.Status OnMenuActionStatus(DropdownMenuAction _)
        {
            var index = _listView.selectedIndex;
            return index >= 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }

        public new class UxmlFactory : UxmlFactory<LeftPaneView, UxmlTraits>
        {
        }
    }
}
