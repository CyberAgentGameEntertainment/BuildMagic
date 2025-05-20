// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using BuildMagicEditor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    internal sealed class LeftPaneListEntryView : BindableElement
    {
        private readonly VisualElement _autoMark;
        private readonly Label _label;

        public LeftPaneListEntryView()
        {
            var visualTree = AssetLoader.LoadUxml("LeftPaneListEntry");
            Assert.IsNotNull(visualTree);
            visualTree.CloneTree(this);

            _label = this.Q<Label>("name-label");
            Assert.IsNotNull(_label);
            _label.bindingPath = "_name";

            _autoMark = this.Q<VisualElement>("auto-mark");
            Assert.IsNotNull(_autoMark);
        }

        public string Value => _label.text;

        public void CustomBind(SerializedProperty property)
        {
            using var scope = new DebugLogDisabledScope();
            this.BindProperty(property);
            BuildMagicSettings.instance.PrimaryBuildSchemeChanged += OnAutoPreBuildTargetChanged;
        }

        public void CustomUnbind()
        {
            this.Unbind();
            BuildMagicSettings.instance.PrimaryBuildSchemeChanged -= OnAutoPreBuildTargetChanged;
        }

        private void OnAutoPreBuildTargetChanged(string target)
        {
            _autoMark.style.visibility = Value == target ? Visibility.Visible : Visibility.Hidden;
        }

        public new class UxmlFactory : UxmlFactory<LeftPaneListEntryView, UxmlTraits>
        {
        }
    }
}
