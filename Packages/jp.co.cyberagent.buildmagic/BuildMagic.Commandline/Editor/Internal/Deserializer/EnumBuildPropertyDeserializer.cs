// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor.Commandline.Internal
{
    /// <summary>
    ///     Enum build property deserializer.
    /// </summary>
    internal class EnumBuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType.IsEnum;
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type valueType)
        {
            return Enum.Parse(valueType, value);
        }
    }
}
