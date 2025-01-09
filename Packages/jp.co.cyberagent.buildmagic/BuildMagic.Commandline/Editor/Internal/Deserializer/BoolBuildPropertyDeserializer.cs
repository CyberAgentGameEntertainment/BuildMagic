// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor.Commandline.Internal
{
    internal class BoolBuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(bool);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return value.ToLowerInvariant() switch
            {
                "true" => true, 
                "false" => false,
                _ => throw new ArgumentException($"Invalid value: {value}", nameof(value))
            };
        }
    }
}
