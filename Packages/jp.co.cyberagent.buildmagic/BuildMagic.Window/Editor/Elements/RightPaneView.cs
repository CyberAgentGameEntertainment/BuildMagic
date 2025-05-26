// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Reflection;
using BuildMagic.Window.Editor.Foundation.TinyRx;
using BuildMagic.Window.Editor.Utilities;
using BuildMagicEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    internal sealed class RightPaneView : VisualElement, IRightPaneView
    {
        private readonly Button _addConfigurationButton;
        private readonly Foldout _internalPrepareConfigurationFoldout;

        public RightPaneView()
        {
            var visualTree = AssetLoader.LoadUxml("RightPane");
            Assert.IsNotNull(visualTree);
            visualTree.CloneTree(this);

            _addConfigurationButton = this.Q<Button>("add-configuration-button");
            Assert.IsNotNull(_addConfigurationButton);
            _addConfigurationButton.clickable.clicked += () => AddRequested?.Invoke(_addConfigurationButton.layout);

            var bindableElement = this.Q<BindableElement>("container");
            Assert.IsNotNull(bindableElement);
            bindableElement.bindingPath = "_selected._self";

            var nameLabel = this.Q<Label>("name-label");
            Assert.IsNotNull(nameLabel);
            nameLabel.bindingPath = "_name";

            var linkBase = this.Q<SchemeLinkLabel>("link-base");
            Assert.IsNotNull(linkBase);
            linkBase.bindingPath = "_baseSchemeName";

            _internalPrepareConfigurationFoldout = this.Q<Foldout>("internal-prepare-configuration-foldout");

            // interactable though disabled
            typeof(Clickable).GetProperty("acceptClicksIfDisabled", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(
                    typeof(Toggle).GetField("m_Clickable", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(
                        typeof(Foldout).GetProperty("toggle", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(
                            _internalPrepareConfigurationFoldout))
                    , true);

            UserSettings.EnableInternalPrepareEditor.Subscribe(enabled =>
            {
                _internalPrepareConfigurationFoldout.SetEnabled(enabled);
                // show if enabled
                if (enabled)
                    _internalPrepareConfigurationFoldout.style.display =
                        new StyleEnum<DisplayStyle>(StyleKeyword.Undefined);
            });
        }

        public event Action<Rect> AddRequested;
        public event Action<ConfigurationType, int, IBuildConfiguration> RemoveRequested;
        public event Action<ConfigurationType, string> PasteRequested;

        public void SetSelected(bool selected)
        {
            style.visibility = selected ? Visibility.Visible : Visibility.Hidden;
        }

        public void OnSelectedSchemeChanged(SerializedProperty selectedSchemeProp)
        {
            if (selectedSchemeProp.managedReferenceId is ManagedReferenceUtility.RefIdUnknown
                or ManagedReferenceUtility.RefIdNull) return;

            var hasAnyInternalPrepareConfigurations = false;
            foreach (var configurationListView in this.Query<ConfigurationListView>()
                         .Class("configuration-list-view-root").Build())
            {
                configurationListView.Bind(selectedSchemeProp,
                    (type, index, configuration) => RemoveRequested?.Invoke(type, index, configuration),
                    (type, json) => PasteRequested?.Invoke(type, json),
                    out var hasAny);
                if (configurationListView.Type == ConfigurationType.InternalPrepare)
                    hasAnyInternalPrepareConfigurations = hasAny;
            }

            // if the selected scheme has at least one internal prepare configuration at this time, just display the foldout (but not editable)
            if (!UserSettings.EnableInternalPrepareEditor.Value)
                _internalPrepareConfigurationFoldout.style.display = hasAnyInternalPrepareConfigurations
                    ? new StyleEnum<DisplayStyle>(StyleKeyword.Undefined)
                    : DisplayStyle.None;
        }

        public new class UxmlFactory : UxmlFactory<RightPaneView, UxmlTraits>
        {
        }
    }
}
