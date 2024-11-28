// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor.Commandline.Internal
{
    /// <summary>
    ///     Interface for deserializing build properties from string.
    /// </summary>
    internal interface IBuildPropertyDeserializer
    {
        /// <summary>
        ///     Determines if this deserializer can process the given value type.
        /// </summary>
        /// <param name="valueType">
        ///     The type of the value to process.
        /// </param>
        bool WillProcess(Type valueType);

        /// <summary>
        ///     Deserialize the given value into a build property.
        /// </summary>
        /// <param name="value">
        ///     The string value of the property.
        /// </param>
        /// <param name="valueType">
        ///     The type of the value to deserialize.
        /// </param>
        /// <returns>
        ///     Build property.
        /// </returns>
        object Deserialize(string value, Type valueType);
    }
}
