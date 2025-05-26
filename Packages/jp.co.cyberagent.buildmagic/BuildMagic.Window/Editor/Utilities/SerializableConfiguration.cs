// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Reflection;
using BuildMagicEditor;
using UnityEngine;

namespace BuildMagic.Window.Editor.Utilities
{
    [Serializable]
    internal class SerializableConfiguration
    {
        [SerializeField]
        private string _assemblyName;

        [SerializeField]
        private string _className;

        [SerializeField]
        private string _valueJson;
        
        private Type _cachedType;
        
        public SerializableConfiguration(IBuildConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");
            
            var type = configuration.GetType();
            _assemblyName = type.Assembly.FullName;
            _className = type.FullName;
            _cachedType = type;
            _valueJson = JsonUtility.ToJson(configuration, true);
        }

        public Type ConfigurationType => _cachedType;
        public string ValueJson => _valueJson;
        
        public static SerializableConfiguration FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON cannot be null or empty.", nameof(json));
            
            var obj = JsonUtility.FromJson<SerializableConfiguration>(json);
            if (obj == null)
                throw new InvalidOperationException("Failed to deserialize SerializableConfiguration from JSON.");
            
            var assembly = Assembly.Load(obj._assemblyName);
            if (assembly == null)
                throw new InvalidOperationException($"Assembly '{obj._assemblyName}' could not be loaded.");
            
            var type = assembly.GetType(obj._className);
            if (type == null)
                throw new InvalidOperationException($"Type '{obj._className}' could not be found in assembly '{obj._assemblyName}'.");
            
            obj._cachedType = type;
            return obj;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        public IBuildConfiguration CreateConfiguration()
        {
            if (_cachedType == null)
                throw new InvalidOperationException("Cached type is null. Cannot create configuration.");

            var configuration = Activator.CreateInstance(_cachedType);
            if (configuration == null)
                throw new InvalidOperationException($"Failed to create instance of type '{_cachedType.Name}'.");

            JsonUtility.FromJsonOverwrite(_valueJson, configuration);

            return (IBuildConfiguration)configuration;
        }

        public IBuildConfiguration OverwriteConfiguration(IBuildConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");

            if (configuration.GetType() != _cachedType)
                throw new
                    InvalidOperationException($"Configuration type '{configuration.GetType().Name}' does not match expected type '{_cachedType.Name}'.");

            JsonUtility.FromJsonOverwrite(_valueJson, configuration);
            return configuration;
        }
    }
}
