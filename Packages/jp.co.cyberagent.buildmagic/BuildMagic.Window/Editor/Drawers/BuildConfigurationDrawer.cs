// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using BuildMagic.Window.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace BuildMagicEditor.BuiltIn
{
    [CustomPropertyDrawer(typeof(IBuildConfiguration))]
    public class BuildConfigurationDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var valueProp = property.FindPropertyRelative("_value");

            var root = new VisualElement();
            var visualTree = AssetLoader.LoadUxml("BuildConfiguration");
            Assert.IsNotNull(visualTree);
            visualTree.CloneTree(root);

            var valueField = root.Q<PropertyField>("value-field");
            Assert.IsNotNull(valueField);
            valueField.bindingPath = "_value";
            valueField.label = preferredLabel;

            // PropertyField does not show labels for properties with no properties
            // We do that explicitly
            if (valueProp is null or
                {
                    propertyType: SerializedPropertyType.Generic,
                    hasVisibleChildren: false
                })
                valueField.parent.Add(new Label(preferredLabel));
            else
                valueField.BindProperty(valueProp);

            return root;
        }
    }
}
