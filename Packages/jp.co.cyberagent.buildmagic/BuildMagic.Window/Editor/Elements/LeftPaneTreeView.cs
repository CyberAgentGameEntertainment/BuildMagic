// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using BuildMagicEditor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    internal class LeftPaneTreeView : TreeView
    {
        private SerializedProperty _currentSchemeListProp;
        private BuildScheme[] _currentSchemes;
        private (BuildScheme scheme, SerializedProperty prop)[] _currentSchemesById;
        private readonly VisualElement _currentTracker;

        public LeftPaneTreeView()
        {
            hierarchy.Add(_currentTracker = new VisualElement());

            makeItem = () =>
            {
                var item = new LeftPaneListEntryView();
                item.AddManipulator(new ContextualMenuManipulator(ev =>
                {
                    var actions = ContextualActionsFactory?.Create(() => item.Value);
                    IBuildSchemeContextualActions.PopulateMenu(() => actions, "", ev.menu);
                }));
                return item;
            };
            bindItem = (element, index) =>
            {
                var prop = _currentSchemesById[GetIdForIndex(index)].prop;
                ((LeftPaneListEntryView)element).CustomBind(prop);
            };
            unbindItem = (element, _) => { ((LeftPaneListEntryView)element).CustomUnbind(); };
            selectedIndicesChanged += indices =>
            {
                var tmpArray = indices.ToArray();
                var index = tmpArray.Length > 0 ? tmpArray[0] : -1;
                var scheme = index >= 0 ? _currentSchemesById[GetIdForIndex(index)].scheme : null;
                OnSelectionChanged?.Invoke(Array.IndexOf(_currentSchemes, scheme));
            };

            itemIndexChanged += (sourceId, parentId) =>
            {
                var pair = _currentSchemesById[sourceId];
                var baseName = "";
                if (parentId >= 0)
                {
                    var parentPair = _currentSchemesById[parentId];
                    baseName = parentPair.scheme.Name;
                    ExpandItem(parentId);
                }

                pair.prop.FindPropertyRelative("_baseSchemeName").stringValue = baseName;
                _currentSchemeListProp.serializedObject.ApplyModifiedProperties();
                RebuildTree(_currentSchemeListProp);
                SetSelectionById(Array.FindIndex(_currentSchemesById, newPair => newPair.scheme == pair.scheme));
            };
        }

        public IBuildSchemeContextualActionsFactory ContextualActionsFactory { private get; set; }

        public event Action<int> OnSelectionChanged;

        public void SelectIndex(int index)
        {
            var id = -1;
            if (_currentSchemes == null) return;
            if (index >= 0 && index < _currentSchemes.Length)
                id = Array.FindIndex(_currentSchemesById, pair => pair.scheme == _currentSchemes[index]);

            if (id >= 0)
                SetSelectionById(id);
            else
                ClearSelection();
        }

        public void BindSchemeList(SerializedProperty schemeListProp)
        {
            _currentSchemeListProp = schemeListProp;
            var sizeProp = schemeListProp.FindPropertyRelative("Array");
            sizeProp.Next(true);
            _currentTracker.Unbind();
            _currentTracker.TrackPropertyValue(sizeProp,
                _ => RebuildTree(schemeListProp)); // rebuild on resize
            RebuildTree(schemeListProp);
        }

        private void RebuildTree(SerializedProperty schemeListProp)
        {
            var prop = schemeListProp.FindPropertyRelative("Array");

            var end = prop.GetEndProperty();

            prop.NextVisible(true);

            List<(BuildScheme, SerializedProperty)> rootSchemes = new();
            Dictionary<string /* name */, HashSet<(BuildScheme, SerializedProperty)>> hierarchy = new();
            List<BuildScheme> schemes = new();
            do
            {
                if (SerializedProperty.EqualContents(prop, end)) break;
                if (prop.propertyType is SerializedPropertyType.ArraySize) continue;

                var scheme = prop.managedReferenceValue as BuildScheme;
                if (scheme == null) continue;
                var baseSchemeName = scheme.BaseSchemeName;
                if (string.IsNullOrEmpty(baseSchemeName))
                {
                    // root
                    rootSchemes.Add((scheme, prop.Copy()));
                }
                else
                {
                    if (!hierarchy.TryGetValue(baseSchemeName, out var children))
                        children = hierarchy[baseSchemeName] = new HashSet<(BuildScheme, SerializedProperty)>();

                    children.Add((scheme, prop.Copy()));
                }

                schemes.Add(scheme);
            } while (prop.NextVisible(false));

            _currentSchemes = schemes.ToArray();

            // create tree

            List<TreeViewItemData<BuildScheme>> rootItems = new();
            var schemesById =
                new (BuildScheme scheme, SerializedProperty prop)[schemes.Count];

            var id = 0;

            HashSet<BuildScheme> visited = new();

            TreeViewItemData<BuildScheme> SetupChild((BuildScheme scheme, SerializedProperty prop) pair, ref int id)
            {
                if (!visited.Add(pair.scheme)) throw new InvalidOperationException("Circular inheritance detected!");
                List<TreeViewItemData<BuildScheme>> childrenItems = new();
                var name = pair.scheme.Name;
                if (hierarchy.TryGetValue(name, out var children))
                {
                    foreach (var child in children)
                        childrenItems.Add(SetupChild(child, ref id));

                    hierarchy.Remove(name);
                }

                schemesById[id] = pair;

                return new TreeViewItemData<BuildScheme>(id++, pair.scheme, childrenItems);
            }

            foreach (var pair in rootSchemes)
                rootItems.Add(SetupChild(pair, ref id));

            foreach (var (missingBaseSchemeName, children) in hierarchy)
            foreach (var pair in children)
                rootItems.Add(SetupChild(pair, ref id));

            _currentSchemesById = schemesById;

            Clear();
            SetRootItems(rootItems);
            Rebuild();
        }

        public new class UxmlFactory : UxmlFactory<LeftPaneTreeView, UxmlTraits>
        {
        }

        public new class UxmlTraits : TreeView.UxmlTraits
        {
        }
    }
}
