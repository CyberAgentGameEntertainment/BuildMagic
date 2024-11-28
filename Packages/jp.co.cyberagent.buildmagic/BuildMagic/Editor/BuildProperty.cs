// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor
{
    /// <summary>
    ///     Implementation of <see cref="IBuildProperty" />.
    /// </summary>
    public class BuildProperty<TValue> : IBuildProperty
    {
        private BuildProperty(string name, TValue value)
        {
            Name = name;
            Value = value;
        }

        /// <inheritdoc cref="IBuildProperty.Name" />
        public string Name { get; }

        /// <inheritdoc cref="IBuildProperty{TValue}.Value" />
        public TValue Value { get; }

        /// <inheritdoc cref="IBuildProperty.Value" />
        object IBuildProperty.Value => Value;

        /// <inheritdoc cref="IBuildProperty.ValueType" />
        public Type ValueType => typeof(TValue);

        /// <summary>
        ///     Create a new instance of <see cref="BuildProperty{TValue}" />.
        /// </summary>
        public static BuildProperty<TValue> Create(string name, TValue value)
        {
            return new BuildProperty<TValue>(name, value);
        }
    }

    public class BuildProperty : IBuildProperty
    {
        /// <inheritdoc cref="IBuildProperty.Name" />
        public string Name { get; }

        /// <inheritdoc cref="IBuildProperty.ValueType" />
        public Type ValueType { get; }

        /// <inheritdoc cref="IBuildProperty.Value" />
        public object Value { get; }

        private BuildProperty(string name, Type valueType, object value)
        {
            Name = name;
            ValueType = valueType;
            Value = value;
        }

        /// <summary>
        ///     Create a new instance of <see cref="IBuildProperty" />.
        /// </summary>
        public static IBuildProperty Create(string name, Type valueType, object value)
        {
            return new BuildProperty(name, valueType, value);
        }
    }
}
