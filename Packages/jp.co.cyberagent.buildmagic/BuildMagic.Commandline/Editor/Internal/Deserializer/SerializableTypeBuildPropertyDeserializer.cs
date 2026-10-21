// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEditor;

namespace BuildMagicEditor.Commandline.Internal
{
    /// <summary>
    ///     Serializable type build property deserializer.
    /// </summary>
    internal class SerializableTypeBuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return !valueType.IsAbstract && !valueType.IsPrimitive && valueType.IsSerializable;
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type valueType)
        {
            var obj = Activator.CreateInstance(valueType);
            EditorJsonUtility.FromJsonOverwrite(value, obj);
            return obj;
        }
    }
}
