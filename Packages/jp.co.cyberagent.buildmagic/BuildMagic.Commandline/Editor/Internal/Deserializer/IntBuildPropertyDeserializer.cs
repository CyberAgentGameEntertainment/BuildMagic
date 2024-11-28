// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor.Commandline.Internal
{
    /// <summary>
    ///     Int32 build property deserializer.
    /// </summary>
    internal class IntBuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(int);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return int.Parse(value);
        }
    }
}
