// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor
{
    /// <summary>
    ///     Property for executing the task.
    /// </summary>
    public interface IBuildProperty
    {
        /// <summary>
        ///     The name of the property.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     The type of the value.
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        ///     The value of the property.
        /// </summary>
        object Value { get; }
    }
}
