// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;

namespace BuildMagicEditor
{
    [Serializable]
    [SerializationWrapper(typeof(NamedBuildTarget))]
    public unsafe struct NamedBuildTargetSerializationWrapper : IEquatable<NamedBuildTargetSerializationWrapper>
    {
        private static delegate*<ref NamedBuildTarget, string, void> _constructorPtr;

        private static delegate*<ref NamedBuildTarget, string, void> ConstructorPtr
        {
            get
            {
                if ((IntPtr)_constructorPtr == IntPtr.Zero)
                {
                    var constructor = typeof(NamedBuildTarget).GetConstructor(
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        Type.DefaultBinder, new[] { typeof(string) }, Array.Empty<ParameterModifier>());

                    _constructorPtr =
                        (delegate*<ref NamedBuildTarget, string, void>)constructor.MethodHandle.GetFunctionPointer();
                }

                return _constructorPtr;
            }
        }

        [SerializeField] private string _name;

        public string Name => _name;

        public NamedBuildTargetSerializationWrapper(string name)
        {
            _name = name;
        }

        public static implicit operator NamedBuildTarget(NamedBuildTargetSerializationWrapper wrapper)
        {
            NamedBuildTarget instance = default;
            ConstructorPtr(ref instance, wrapper._name);

            return instance;
        }

        public static implicit operator NamedBuildTargetSerializationWrapper(NamedBuildTarget source)
        {
            return new NamedBuildTargetSerializationWrapper(source.TargetName);
        }

        public bool Equals(NamedBuildTargetSerializationWrapper other)
        {
            return _name == other._name;
        }

        public override bool Equals(object obj)
        {
            return obj is NamedBuildTargetSerializationWrapper other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _name != null ? _name.GetHashCode() : 0;
        }

        public static bool operator ==(NamedBuildTargetSerializationWrapper left,
            NamedBuildTargetSerializationWrapper right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NamedBuildTargetSerializationWrapper left,
            NamedBuildTargetSerializationWrapper right)
        {
            return !left.Equals(right);
        }
    }

    [CustomPropertyDrawer(typeof(NamedBuildTargetSerializationWrapper))]
    public class NamedBuildTargetSerializationWrapperDrawer : PropertyDrawer
    {
        private static List<string> _validNames;

        private static List<string> ValidNames =>
            _validNames ??= ((string[])typeof(NamedBuildTarget)
                .GetField("k_ValidNames", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null))?.ToList();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetName = property.FindPropertyRelative("_name");

            EditorGUI.PropertyField(position, targetName, label);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var targetName = property.FindPropertyRelative("_name");

            var popup = new DropdownField(property.displayName, ValidNames, ValidNames.IndexOf(targetName.stringValue));

            popup.RegisterValueChangedCallback(ev =>
            {
                targetName.stringValue = ev.newValue;
                targetName.serializedObject.ApplyModifiedProperties();
            });

            return popup;
        }
    }
}
