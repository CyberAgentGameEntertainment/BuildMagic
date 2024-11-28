// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BuildMagic.Window.Editor.Elements;
using BuildMagicEditor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using BuildPipeline = UnityEditor.BuildPipeline;

namespace BuildMagic.Window.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
    internal sealed class SerializableDictionaryDrawer : PropertyDrawer
    {
        static SerializableDictionaryDrawer()
        {
            SerializableDictionaryTabViewKeyProviderRegistry<NamedBuildTargetSerializationWrapper>.Provider =
                new SerializableDictionaryTabViewKeyProvider<NamedBuildTargetSerializationWrapper>(
                    EqualityComparer<NamedBuildTargetSerializationWrapper>.Default,
                    BuildPlatformWrapper.BuildPlatforms
                        .Select(p => (NamedBuildTargetSerializationWrapper)p.NamedBuildTarget).ToArray(),
                    prop => new NamedBuildTargetSerializationWrapper(prop.FindPropertyRelative("_name").stringValue),
                    (prop, value) => prop.FindPropertyRelative("_name").stringValue = value.Name,
                    value => value.Name,
                    value => BuildPlatformWrapper.BuildPlatforms.First(p => p.NamedBuildTarget.TargetName == value.Name)
                        .SmallIcon
                );

            SerializableDictionaryTabViewKeyProviderRegistry<BuildTarget>.EnumProvider =
                new SerializableDictionaryTabViewKeyProvider<int>(
                    EqualityComparer<int>.Default,
                    GetValidEnumValues<BuildTarget>(),
                    prop => prop.enumValueIndex,
                    (prop, value) => prop.enumValueIndex = value,
                    value => Enum.GetName(typeof(BuildTarget), value),
                    value => BuildPlatformWrapper.BuildPlatforms.FirstOrDefault(b =>
                        b.NamedBuildTarget.ToBuildTargetGroup() ==
                        BuildPipeline.GetBuildTargetGroup((BuildTarget)value)).SmallIcon
                );

            SerializableDictionaryTabViewKeyProviderRegistry<BuildTargetGroup>.EnumProvider =
                new SerializableDictionaryTabViewKeyProvider<int>(
                    EqualityComparer<int>.Default,
                    GetValidEnumValues<BuildTargetGroup>(),
                    prop => prop.enumValueIndex,
                    (prop, value) => prop.enumValueIndex = value,
                    value => Enum.GetName(typeof(BuildTargetGroup), value),
                    value => BuildPlatformWrapper.BuildPlatforms.FirstOrDefault(b =>
                        b.NamedBuildTarget.ToBuildTargetGroup() == (BuildTargetGroup)value).SmallIcon
                );
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return CreatePropertyGUI(property, fieldInfo);
        }

        public static VisualElement CreatePropertyGUI(SerializedProperty property, FieldInfo fieldInfo)
        {
            var keyType = fieldInfo.FieldType.GenericTypeArguments[0];
            var method = typeof(SerializableDictionaryDrawer)
                .GetMethod(nameof(CreatePropertyGUI), 1, BindingFlags.NonPublic | BindingFlags.Static,
                    Type.DefaultBinder, new[] { typeof(SerializedProperty) }, Array.Empty<ParameterModifier>())
                .MakeGenericMethod(keyType);

            return (VisualElement)method.Invoke(null, new object[] { property });
        }

        private static IReadOnlyList<int> GetValidEnumValues<T>()
        {
            return typeof(T)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.CustomAttributes.All(attr => attr.AttributeType != typeof(ObsoleteAttribute)))
                .Select(field => field.GetRawConstantValue()).Cast<int>().ToList();
        }

        private static VisualElement CreatePropertyGUI<TKey>(SerializedProperty property)
        {
            var provider = SerializableDictionaryTabViewKeyProviderRegistry<TKey>.Provider;

            if (typeof(TKey).IsEnum)
            {
                var enumProvider = SerializableDictionaryTabViewKeyProviderRegistry<TKey>.EnumProvider ??
                                   new SerializableDictionaryTabViewKeyProvider<int>(
                                       EqualityComparer<int>.Default,
                                       GetValidEnumValues<TKey>(),
                                       prop => prop.enumValueIndex,
                                       (prop, value) => prop.enumValueIndex = value,
                                       value => Enum.GetName(typeof(TKey), value),
                                       value => null
                                   );

                if (property.propertyType != SerializedPropertyType.Enum)
                {
                    var capturedEnumProvider = enumProvider;
                    // when self-nested SerializableDictionary`2 is serialized, the property type of enum key field turns to integer due to the Unity's bug.
                    // https://issuetracker.unity3d.com/issues/public-enum-located-in-generic-class-is-displayed-as-int-field-instead-of-drop-down-inside-inspector
                    enumProvider = new SerializableDictionaryTabViewKeyProvider<int>(
                        capturedEnumProvider.EqualityComparer,
                        capturedEnumProvider.AvailableValues,
                        prop => prop.intValue,
                        (prop, value) => prop.intValue = value,
                        value => capturedEnumProvider.GetDisplayName(value),
                        value => capturedEnumProvider.GetIcon(value)
                    );
                }

                return new SerializableDictionaryTabView<int>(property, enumProvider);
            }

            if (provider == null) return new PropertyField(property);

            return new SerializableDictionaryTabView<TKey>(property, provider);
        }
    }

    public static class SerializableDictionaryTabViewKeyProviderRegistry<T>
    {
        public static ISerializableDictionaryTabViewKeyProvider<T> Provider { get; set; }
        public static ISerializableDictionaryTabViewKeyProvider<int> EnumProvider { get; set; }
    }
}
