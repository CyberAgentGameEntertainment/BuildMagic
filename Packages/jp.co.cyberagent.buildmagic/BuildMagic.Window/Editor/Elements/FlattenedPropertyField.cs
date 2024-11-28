// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    /// <summary>
    ///     A property field variant that flattens generic properties into individual fields.
    /// </summary>
    internal class FlattenedPropertyField : VisualElement
    {
        public FlattenedPropertyField()
        {
        }

        public FlattenedPropertyField(SerializedProperty property)
        {
            BindProperty(property);
        }

        public void BindProperty(SerializedProperty property)
        {
            Clear();

            if (property == null) return;

            if (property.propertyType != SerializedPropertyType.Generic || property.isArray)
            {
                var field = new PropertyField();
                field.BindProperty(property);
                Add(field);
                return;
            }

            var prop = property.Copy();

            var initialDepth = prop.depth;
            var end = prop.GetEndProperty();

            if (prop.Next(true) && prop.depth > initialDepth)
                while (!SerializedProperty.EqualContents(prop, end))
                {
                    var field = new PropertyField();
                    field.BindProperty(prop.Copy());
                    Add(field);
                    if (!prop.Next(false)) break;
                }
        }
    }
}
