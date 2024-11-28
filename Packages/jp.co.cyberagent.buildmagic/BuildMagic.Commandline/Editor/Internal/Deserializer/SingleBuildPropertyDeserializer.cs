// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor.Commandline.Internal
{
    /// <summary>
    ///     float build property deserializer.
    /// </summary>
    internal class SingleBuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(float);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return float.Parse(value);
        }
    }
}
