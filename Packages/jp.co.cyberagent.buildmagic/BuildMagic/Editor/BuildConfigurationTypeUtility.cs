// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuildMagicEditor
{
    /// <summary>
    ///     An utility to map static data for build configurations.
    /// </summary>
    internal static class BuildConfigurationTypeUtility
    {
        private static Dictionary<string, Type> _propertyNamesAndConfigurationTypes;

        public static string GetPropertyName<T>() where T : IBuildConfiguration => GetPropertyName(typeof(T));

        public static string GetPropertyName(Type type)
        {
            return type
                .GetCustomAttributesData()
                .FirstOrDefault(a => a.AttributeType == typeof(BuildConfigurationAttribute))?.NamedArguments?
                .FirstOrDefault(a => a.MemberName == nameof(BuildConfigurationAttribute.PropertyName)).TypedValue
                .Value as string ?? type.AssemblyQualifiedName;
        }

        public static string GetDisplayName(Type type)
        {
            var attributeData = type.CustomAttributes
                .FirstOrDefault(attr => attr.AttributeType == typeof(BuildConfigurationAttribute));

            if (attributeData == null)
                return null;

            if (attributeData.ConstructorArguments.Count == 1)
                return attributeData.ConstructorArguments[0].Value as string;

            return attributeData.NamedArguments?.Where(argument => argument.MemberName == nameof(BuildConfigurationAttribute.DisplayName))
                .Select(argument => argument.TypedValue.Value as string).FirstOrDefault();
        }

        public static string GetDisplayName(this IBuildConfiguration configuration)
        {
            return GetDisplayName(configuration.GetType());
        }

        public static Type GetConfigurationType(string propertyName)
        {
            if (_propertyNamesAndConfigurationTypes == null)
            {
                _propertyNamesAndConfigurationTypes = new();

                foreach (var type in TypeCache.GetTypesDerivedFrom<IBuildConfiguration>())
                {
                    var name = GetPropertyName(type);
                    if (_propertyNamesAndConfigurationTypes.TryGetValue(name, out var existing))
                    {
                        Debug.LogWarning($"Duplicate property name \"{name}\" found in {existing} and {type}.");
                    }
                    else
                    {
                        _propertyNamesAndConfigurationTypes[name] = type;
                    }
                }
            }

            return _propertyNamesAndConfigurationTypes.GetValueOrDefault(propertyName);
        }

        public static bool TryGetConfigurationType(string propertyName, out Type result)
        {
            result = GetConfigurationType(propertyName);
            return result != default;
        }
    }
}
