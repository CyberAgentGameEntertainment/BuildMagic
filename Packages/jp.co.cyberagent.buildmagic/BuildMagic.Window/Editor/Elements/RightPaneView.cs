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
            bindableElement.bindingPath = "_selected";

            var nameLabel = this.Q<Label>("name-label");
            Assert.IsNotNull(nameLabel);
            nameLabel.bindingPath = "_name";

            var linkBase = this.Q<SchemeLinkLabel>("link-base");
            Assert.IsNotNull(linkBase);
            linkBase.bindingPath = "_baseSchemeName";

            foreach (var listView in this.Query<ListView>("pre-build-configuration-list").Build())
            {
                listView.bindingPath = "_preBuildConfigurations";
                listView.makeItem = () => MakeConfigurationEntryView(ConfigurationType.PreBuild);
                listView.bindItem = (e, index)
                    => BindConfiguration(e, index, listView.itemsSource, ConfigurationType.PreBuild);
                listView.unbindItem = (e, _)
                    => UnbindConfiguration(e);
            }

            foreach (var listView in this.Query<ListView>("internal-prepare-configuration-list").Build())
            {
                listView.bindingPath = "_internalPrepareConfigurations";
                listView.makeItem = () => MakeConfigurationEntryView(ConfigurationType.InternalPrepare);
                listView.bindItem = (e, index)
                    => BindConfiguration(e, index, listView.itemsSource, ConfigurationType.InternalPrepare);
                listView.unbindItem = (e, _)
                    => UnbindConfiguration(e);
#if !BUILDMAGIC_DEVELOPER
                listView.style.display = DisplayStyle.None;
#endif
            }

            foreach (var listView in this.Query<ListView>("post-build-configuration-list").Build())
            {
                listView.bindingPath = "_postBuildConfigurations";
                listView.makeItem = () => MakeConfigurationEntryView(ConfigurationType.PostBuild);
                listView.bindItem = (e, index)
                    => BindConfiguration(e, index, listView.itemsSource, ConfigurationType.PostBuild);
                listView.unbindItem = (e, _)
                    => UnbindConfiguration(e);
            }

            foreach (var derived in this.Query<BindableElement>(className: "derived").Build())
            {
                derived.bindingPath = "_selectedBase";
                derived.SetEnabled(false);
            }
        }

        public event Action<Rect> AddRequested;
        public event Action<ConfigurationType, int, IBuildConfiguration> RemoveRequested;

        public void SetSelected(bool selected)
        {
            style.visibility = selected ? Visibility.Visible : Visibility.Hidden;
        }
        
        private VisualElement MakeConfigurationEntryView(ConfigurationType type)
        {
            var entry = new ConfigurationEntryView();
            entry.AddManipulator(new ContextualMenuManipulator(ev =>
            {
                ev.menu.AppendAction("Copy the configuration key",
                                     _ => EditorGUIUtility.systemCopyBuffer = entry.Configuration.PropertyName,
                                     entry.Configuration != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                ev.menu.AppendAction(
                                     "Collect a project setting",
                                     _ => entry.CollectProjectSetting(),
                                        entry.Collectable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                ev.menu.AppendAction(
                                     "Remove the configuration",
                                     _ => RemoveRequested?.Invoke(type, entry.Index, entry.Configuration));
            }));
            return entry;
        }
        
        private static void BindConfiguration(VisualElement element, int index, IList sourceList, ConfigurationType type)
        {
            var entry = element as ConfigurationEntryView;
            Assert.IsNotNull(entry);
            var source = sourceList[index] as SerializedProperty;
            Assert.IsNotNull(source);
            var configuration = source.managedReferenceValue as IBuildConfiguration;
            
            using var scope = new DebugLogDisabledScope();
            entry.Bind(type, index, configuration);
            entry.BindProperty(source);
        }
        
        private static void UnbindConfiguration(VisualElement element)
        {
            var entry = element as ConfigurationEntryView;
            Assert.IsNotNull(entry);
            entry.Bind(ConfigurationType.None, -1, null);
            entry.Unbind();
        }
        
        public new class UxmlFactory : UxmlFactory<RightPaneView, UxmlTraits>
        {
        }
    }
}
