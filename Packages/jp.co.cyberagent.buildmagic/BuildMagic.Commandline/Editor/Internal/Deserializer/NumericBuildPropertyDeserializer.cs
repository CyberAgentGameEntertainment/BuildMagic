// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor.Commandline.Internal
{
    /// <summary>
    ///     System.Byte build property deserializer.
    /// </summary>
    internal class ByteBuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(System.Byte);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return System.Byte.Parse(value);
        }
    }

    /// <summary>
    ///     System.SByte build property deserializer.
    /// </summary>
    internal class SByteBuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(System.SByte);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return System.SByte.Parse(value);
        }
    }

    /// <summary>
    ///     System.UInt16 build property deserializer.
    /// </summary>
    internal class UInt16BuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(System.UInt16);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return System.UInt16.Parse(value);
        }
    }

    /// <summary>
    ///     System.Int16 build property deserializer.
    /// </summary>
    internal class Int16BuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(System.Int16);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return System.Int16.Parse(value);
        }
    }

    /// <summary>
    ///     System.UInt32 build property deserializer.
    /// </summary>
    internal class UInt32BuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(System.UInt32);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return System.UInt32.Parse(value);
        }
    }

    /// <summary>
    ///     System.Int32 build property deserializer.
    /// </summary>
    internal class Int32BuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(System.Int32);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return System.Int32.Parse(value);
        }
    }

    /// <summary>
    ///     System.UInt64 build property deserializer.
    /// </summary>
    internal class UInt64BuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(System.UInt64);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return System.UInt64.Parse(value);
        }
    }

    /// <summary>
    ///     System.Int64 build property deserializer.
    /// </summary>
    internal class Int64BuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(System.Int64);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return System.Int64.Parse(value);
        }
    }

    /// <summary>
    ///     System.Single build property deserializer.
    /// </summary>
    internal class SingleBuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(System.Single);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return System.Single.Parse(value);
        }
    }

    /// <summary>
    ///     System.Double build property deserializer.
    /// </summary>
    internal class DoubleBuildPropertyDeserializer : IBuildPropertyDeserializer
    {
        /// <inheritdoc cref="IBuildPropertyDeserializer.WillProcess" />
        public bool WillProcess(Type valueType)
        {
            return valueType == typeof(System.Double);
        }

        /// <inheritdoc cref="IBuildPropertyDeserializer.Deserialize" />
        public object Deserialize(string value, Type _)
        {
            return System.Double.Parse(value);
        }
    }

}
