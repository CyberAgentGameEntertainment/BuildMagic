// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections;
using BuildMagicEditor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    internal sealed class RightPaneView : VisualElement, IRightPaneView
    {
        private readonly Button _addConfigurationButton;

        public RightPaneView()
        {
            var visualTree = AssetLoader.LoadUxml("RightPane");
            Assert.IsNotNull(visualTree);
            visualTree.CloneTree(this);
            
            _addConfigurationButton = this.Q<Button>("add-configuration-button");
            Assert.IsNotNull(_addConfigurationButton);
            _addConfigurationButton.clickable.clicked += () => AddRequested?.Invoke(_addConfigurationButton.layout);;

            var bindableElement = this.Q<BindableElement>("container");
            Assert.IsNotNull(bindableElement);
            bindableElement.bindingPath = "_selected._self";

            var nameLabel = this.Q<Label>("name-label");
            Assert.IsNotNull(nameLabel);
            nameLabel.bindingPath = "_name";

            var linkBase = this.Q<SchemeLinkLabel>("link-base");
            Assert.IsNotNull(linkBase);
            linkBase.bindingPath = "_baseSchemeName";
        }

        public event Action<Rect> AddRequested;
        public event Action<ConfigurationType, int, IBuildConfiguration> RemoveRequested;

        public void SetSelected(bool selected)
        {
            style.visibility = selected ? Visibility.Visible : Visibility.Hidden;
        }

        public void OnBind(SerializedObject modelObject)
        {
            var selected = modelObject.FindProperty("_selected");
            if (selected.managedReferenceId is ManagedReferenceUtility.RefIdUnknown
                or ManagedReferenceUtility.RefIdNull) return;

            foreach (var configurationListView in this.Query<ConfigurationListView>()
                         .Class("configuration-list-view-root").Build())
                configurationListView.Bind(selected,
                    (type, index, configuration) => RemoveRequested?.Invoke(type, index, configuration));
        }
        
        public new class UxmlFactory : UxmlFactory<RightPaneView, UxmlTraits>
        {
        }
    }
}
