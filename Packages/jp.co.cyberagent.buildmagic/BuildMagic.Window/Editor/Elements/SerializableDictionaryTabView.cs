// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BuildMagic.Window.Editor.Drawers;
using BuildMagicEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    public sealed unsafe class SerializableDictionaryTabView<T> : VisualElement
    {
        private static delegate*<SerializedProperty, bool> _isSerializedPropertyValid;
        private static delegate*<SerializedProperty, out Type, FieldInfo> _getFieldInfoFromProperty;
        private readonly ISerializableDictionaryTabViewKeyProvider<T> _keyProvider;
        private readonly SerializedProperty _pairsProperty;
        private readonly VisualElement _tabContainer;
        private readonly List<TabItem> _tabs = new();
        private TabItem _activeTab;

        internal SerializableDictionaryTabView(SerializedProperty property,
            ISerializableDictionaryTabViewKeyProvider<T> keyProvider)
        {
            _keyProvider = keyProvider;

            var visualTree = AssetLoader.LoadUxml("Drawers/SerializableDictionaryTabView");
            Assert.IsNotNull(visualTree);
            visualTree.CloneTree(this);

            var styleSheet = AssetLoader.LoadUss("Drawers/SerializableDictionaryTabView");
            styleSheets.Add(styleSheet);

            _tabContainer = this.Q<VisualElement>("platform-tab-container");

            contentContainer = this.Q<VisualElement>("main-container");

            _pairsProperty = property.FindPropertyRelative("pairs");

            RegisterCallback<AttachToPanelEvent, SerializableDictionaryTabView<T>>(static (_, view) =>
            {
                view.Rebuild();
                Undo.undoRedoPerformed += view.Rebuild;
            }, this);

            RegisterCallback<DetachFromPanelEvent, SerializableDictionaryTabView<T>>(
                static (_, view) => { Undo.undoRedoPerformed -= view.Rebuild; }, this);
        }

        private static delegate*<SerializedProperty, bool> IsSerializedPropertyValid
        {
            get
            {
                // there seems to be no public method to check if a SerializedProperty is valid
                if ((IntPtr)_isSerializedPropertyValid == IntPtr.Zero)
                    _isSerializedPropertyValid =
                        (delegate*<SerializedProperty, bool>)typeof(SerializedProperty)
                            .GetMethod("get_isValid", BindingFlags.Instance | BindingFlags.NonPublic)
                            .MethodHandle.GetFunctionPointer();

                return _isSerializedPropertyValid;
            }
        }

        private static delegate*<SerializedProperty, out Type, FieldInfo> GetFieldInfoFromProperty
        {
            get
            {
                if ((IntPtr)_getFieldInfoFromProperty == IntPtr.Zero)
                    _getFieldInfoFromProperty =
                        (delegate*<SerializedProperty, out Type, FieldInfo>)typeof(UnityEditor.Editor).Assembly
                            .GetType("UnityEditor.ScriptAttributeUtility")
                            .GetMethod("GetFieldInfoFromProperty", BindingFlags.NonPublic | BindingFlags.Static)
                            .MethodHandle.GetFunctionPointer();

                return _getFieldInfoFromProperty;
            }
        }

        private TabItem ActiveTab
        {
            get => _activeTab;
            set
            {
                _activeTab = value;
                foreach (var t in _tabs)
                    t.TabButton.IsActive = t == _activeTab;

                contentContainer.Clear();

                if (_activeTab != null)
                {
                    var valueProp = _pairsProperty
                        .GetArrayElementAtIndex(_activeTab.Index)?
                        .FindPropertyRelative("value");

                    var fieldInfo = GetFieldInfoFromProperty(valueProp, out var fieldType);
                    if (fieldType is { IsConstructedGenericType: true } &&
                        fieldType.GetGenericTypeDefinition() == typeof(SerializableDictionary<,>))
                        // when self-nested SerializableDictionary`2 is serialized, the property type of enum key field turns to integer due to the Unity's bug.
                        // and it is not processed by our SerializableDictionaryDrawer, so we need to handle it here. 
                        // https://issuetracker.unity3d.com/issues/public-enum-located-in-generic-class-is-displayed-as-int-field-instead-of-drop-down-inside-inspector
                        contentContainer.Add(SerializableDictionaryDrawer.CreatePropertyGUI(valueProp, fieldInfo));
                    else
                        contentContainer.Add(new FlattenedPropertyField(valueProp));
                }
            }
        }

        public override VisualElement contentContainer { get; }

        private void Rebuild()
        {
            ActiveTab = null;
            _tabContainer.Clear();
            _tabs.Clear();

            var pairs = _pairsProperty;

            pairs.serializedObject.Update();

            // after undo/redo, the cached property is sometimes disposed
            if (!IsSerializedPropertyValid(pairs)) return;

            for (var i = 0; i < pairs.arraySize; i++)
            {
                var elementProp = pairs.GetArrayElementAtIndex(i);
                AddTabForElement(elementProp, i);
            }

            ActiveTab = _tabs.FirstOrDefault();

            var addPlatformButton = this.Q<Button>("add-platform-button");
            addPlatformButton.clicked += () =>
            {
                var existingValues = Enumerable.Range(0, pairs.arraySize).Select(index =>
                        _keyProvider.GetValue(pairs.GetArrayElementAtIndex(index).FindPropertyRelative("key")))
                    .ToArray();

                var menu = new GenericMenu();
                foreach (var value in _keyProvider.AvailableValues)
                    if (existingValues.Contains(value))
                        menu.AddDisabledItem(new GUIContent(_keyProvider.GetDisplayName(value)), false);
                    else
                        menu.AddItem(new GUIContent(_keyProvider.GetDisplayName(value)), false,
                            () => AddTab(value));

                menu.ShowAsContext();
            };
        }

        private int IndexOf(IReadOnlyList<T> items, T value)
        {
            for (var i = 0; i < items.Count; i++)
                if (_keyProvider.EqualityComparer.Equals(items[i], value))
                    return i;

            return -1;
        }

        private void AddTab(T target)
        {
            var valueIndex = IndexOf(_keyProvider.AvailableValues, target);

            var indexToInsert = _tabs.Count;
            for (var i = 0; i < _tabs.Count; i++)
            {
                var tabValue = _keyProvider.GetValue(_tabs[i].KeyProperty);
                var tabIndex = IndexOf(_keyProvider.AvailableValues, tabValue);
                if (valueIndex < tabIndex)
                {
                    indexToInsert = i;
                    break;
                }
            }

            _pairsProperty.InsertArrayElementAtIndex(indexToInsert);
            var elementProp = _pairsProperty.GetArrayElementAtIndex(indexToInsert);
            _keyProvider.SetValue(elementProp.FindPropertyRelative("key"), target);
            elementProp.serializedObject.ApplyModifiedProperties();

            AddTabForElement(elementProp, indexToInsert);
        }

        private void AddTabForElement(SerializedProperty elementProp, int index)
        {
            var targetValue = _keyProvider.GetValue(elementProp.FindPropertyRelative("key"));
            var button = new SerializableDictionaryTabViewButton();

            var name = _keyProvider.GetDisplayName(targetValue);
            var icon = _keyProvider.GetIcon(targetValue);

            button.tooltip = name;

            if (icon != null) button.Icon = icon;
            else button.text = name;

            var tab = new TabItem(this, button);
            _tabs.Insert(index, tab);
            _tabContainer.Insert(index, button);
            ActiveTab = tab;

            button.clicked += () => { ActiveTab = tab; };
            button.OnRemoveTab += () => { RemoveTab(tab); };
        }

        private void RemoveTab(TabItem tab)
        {
            var prevActiveIndex = ActiveTab.Index;
            var index = tab.Index;
            _tabs.Remove(tab);
            _tabContainer.Remove(tab.TabButton);
            _pairsProperty.DeleteArrayElementAtIndex(index);
            _pairsProperty.serializedObject.ApplyModifiedProperties();
            ActiveTab = _tabs.Count >= 1 ? _tabs[Math.Clamp(prevActiveIndex, 0, _tabs.Count - 1)] : null;
        }

        #region Nested type: SerializableDictionaryTabViewButton

        private class SerializableDictionaryTabViewButton : Button
        {
            private const string InactiveClass = "inactive";

            public SerializableDictionaryTabViewButton() : this(true)
            {
            }

            public SerializableDictionaryTabViewButton(bool isActive)
            {
                var visualTree = AssetLoader.LoadUxml("Drawers/SerializableDictionaryTabViewButton");
                visualTree.CloneTree(this);
                AddToClassList("patformgrouping-tab-button");
                IsActive = isActive;

                IconImage = this.Q<VisualElement>("platform-tab-button-image");
                IconImage.style.backgroundSize =
                    new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));

                var removeButton = this.Q<Button>("platform-tab-button-remove");
                removeButton.clicked += () => OnRemoveTab?.Invoke();
                typeof(Clickable).GetProperty("acceptClicksIfDisabled", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(clickable, true);
            }

            private VisualElement IconImage { get; }

            public bool IsActive
            {
                get => !ClassListContains(InactiveClass);
                set
                {
                    if (value)
                        RemoveFromClassList(InactiveClass);
                    else
                        AddToClassList(InactiveClass);
                }
            }

            public Texture2D Icon
            {
                get => IconImage.style.backgroundImage.value.texture;
                set => IconImage.style.backgroundImage = new StyleBackground(value);
            }

            public event Action OnRemoveTab;
        }

        #endregion

        #region Nested type: TabItem

        private record TabItem
        {
            public TabItem(SerializableDictionaryTabView<T> host, SerializableDictionaryTabViewButton tabButton)
            {
                Host = host;
                TabButton = tabButton;
            }

            public SerializableDictionaryTabView<T> Host { get; }
            public SerializableDictionaryTabViewButton TabButton { get; }

            public int Index => Host._tabs.IndexOf(this);
            public SerializedProperty Property => Host._pairsProperty.GetArrayElementAtIndex(Index);
            public SerializedProperty KeyProperty => Property.FindPropertyRelative("key");
        }

        #endregion
    }
}
