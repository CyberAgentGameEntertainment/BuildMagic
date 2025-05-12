// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuildMagicEditor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    internal class ConfigurationListView : BindableElement
    {
        private readonly Label _label;
        private readonly ListView _listView;
        private readonly VisualElement _arraySizeTracker;
        private ConfigurationListView _baseView;
        private Action<ConfigurationType, int, IBuildConfiguration> _requestRemove;

        public ConfigurationListView()
        {
            Add(_label = new Label());
            Add(_listView = new ListView
            {
                reorderable = true,
                showFoldoutHeader = false,
                reorderMode = ListViewReorderMode.Animated,
                showBoundCollectionSize = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            });
            Add(_arraySizeTracker = new VisualElement());
            _listView.AddToClassList("hide-size");
            _listView.AddToClassList("hide-empty");
            _listView.AddToClassList("configuration-list");
            _label.AddToClassList("configuration-list-derived-label");
        }

        public ConfigurationListView(ConfigurationType type) : this()
        {
            Type = type;
        }

        public ConfigurationType Type { get; private set; }

        public void Bind(SerializedProperty buildSchemeContainerProp,
            Action<ConfigurationType, int, IBuildConfiguration> requestRemove, out bool hasAny)
        {
            Bind(buildSchemeContainerProp, null, out hasAny, false, requestRemove);
        }

        private void Bind(SerializedProperty buildSchemeContainerProp, IConfigurationFilter filter, out bool hasAny,
            bool isDerived = false,
            Action<ConfigurationType, int, IBuildConfiguration> requestRemove = null)
        {
            _requestRemove = requestRemove;
            var listView = _listView;
            bindingPath = buildSchemeContainerProp.propertyPath;
            if (isDerived)
            {
                _label.style.display = new StyleEnum<DisplayStyle>(StyleKeyword.Undefined);
                _label.text =
                    $"Derived from {buildSchemeContainerProp.FindPropertyRelative("_self._name").stringValue}";
            }
            else
            {
                _label.style.display = new StyleEnum<DisplayStyle>(StyleKeyword.None);
            }

            var relativeBindingPath = Type switch
            {
                ConfigurationType.PreBuild => "_self._preBuildConfigurations",
                ConfigurationType.InternalPrepare => "_self._internalPrepareConfigurations",
                ConfigurationType.PostBuild => "_self._postBuildConfigurations",
                _ => null
            };
            listView.bindingPath = relativeBindingPath;
            listView.makeItem = () => MakeConfigurationEntryView(Type);
            listView.bindItem = (e, index) => BindConfiguration(e, index, listView.itemsSource, Type, filter);
            listView.unbindItem = (e, _) => UnbindConfiguration(e);
            listView.reorderable = !isDerived;
            listView.EnableInClassList("configuration-list-derived", isDerived);
            listView.Rebuild();

            hasAny = buildSchemeContainerProp.FindPropertyRelative(relativeBindingPath).arraySize > 0;
            RebuildBaseView(out var hasBaseAny);
            hasAny |= hasBaseAny;

            if (!isDerived)
            {
                // track array size to trigger filter rebinding
                var arrayProp = buildSchemeContainerProp.FindPropertyRelative(relativeBindingPath);
                var arraySizeProp = arrayProp.FindPropertyRelative("Array");
                arraySizeProp.Next(true);

                _arraySizeTracker.Unbind();
                _arraySizeTracker.TrackPropertyValue(arraySizeProp, _ => { RebuildBaseView(out var _); });
            }

            void RebuildBaseView(out bool hasAny)
            {
                var baseProp = buildSchemeContainerProp.FindPropertyRelative("_base");
                if (baseProp.managedReferenceId is ManagedReferenceUtility.RefIdNull
                    or ManagedReferenceUtility.RefIdUnknown)
                {
                    // root
                    _baseView?.RemoveFromHierarchy();
                    _baseView = null;
                    hasAny = false;
                }
                else
                {
                    if (_baseView == null)
                    {
                        _baseView = new ConfigurationListView(Type);
                        _baseView.SetEnabled(false);
                        Add(_baseView);
                    }

                    static IEnumerable<Type> GetTaskTypes(SerializedProperty prop)
                    {
                        for (var i = 0; i < prop.arraySize; i++)
                            if (prop.GetArrayElementAtIndex(i).managedReferenceValue is IBuildConfiguration
                                configuration)
                                yield return configuration.TaskType;
                    }

                    var configurationsProp = buildSchemeContainerProp.FindPropertyRelative(relativeBindingPath);
                    _baseView.Bind(baseProp, new ConfigurationFilter(GetTaskTypes(configurationsProp), filter),
                        out hasAny, true);
                }
            }
        }

        private VisualElement MakeConfigurationEntryView(ConfigurationType type)
        {
            var entry = new ConfigurationEntryView();
            entry.AddManipulator(new ContextualMenuManipulator(ev =>
            {
                ev.menu.AppendAction("Copy the configuration key",
                    _ => EditorGUIUtility.systemCopyBuffer = entry.Configuration.PropertyName,
                    entry.Configuration != null
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                ev.menu.AppendAction(
                    "Collect a project setting",
                    _ => entry.CollectProjectSetting(),
                    entry.Collectable ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                ev.menu.AppendAction(
                    "Remove the configuration",
                    _ => _requestRemove?.Invoke(type, entry.Index, entry.Configuration));
            }));
            return entry;
        }

        private static void BindConfiguration(VisualElement element, int index, IList sourceList,
            ConfigurationType type, IConfigurationFilter filter = null)
        {
            var entry = element as ConfigurationEntryView;
            Assert.IsNotNull(entry);
            var source = sourceList[index] as SerializedProperty;
            Assert.IsNotNull(source);
            var configuration = source.managedReferenceValue as IBuildConfiguration;

            if (!(filter?.IsVisible(configuration) ?? true))
            {
                entry.style.height = 1f; // HACK: avoid hidden items allocate empty space
                return;
            }

            entry.style.height = new StyleLength(StyleKeyword.Auto);

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

        public new class UxmlFactory : UxmlFactory<ConfigurationListView, UxmlTraits>
        {
        }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private readonly UxmlEnumAttributeDescription<ConfigurationType> _type = new()
            {
                name = "configuration-type"
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var derived = (ConfigurationListView)ve;
                derived.Type = _type.GetValueFromBag(bag, cc);
            }
        }

        private interface IConfigurationFilter
        {
            bool IsVisible(IBuildConfiguration configuration);
        }

        private class ConfigurationFilter : IConfigurationFilter
        {
            private readonly Type[] _excludedTaskTypes;
            private readonly IConfigurationFilter _parent;

            public ConfigurationFilter(IEnumerable<Type> excludedTypes, IConfigurationFilter parent = null)
            {
                _parent = parent;
                _excludedTaskTypes = excludedTypes.ToArray();
            }

            public bool IsVisible(IBuildConfiguration configuration)
            {
                if (configuration == null) return false;
                return (_parent?.IsVisible(configuration) ?? true) &&
                       !_excludedTaskTypes.Contains(configuration.TaskType);
            }
        }
    }
}
