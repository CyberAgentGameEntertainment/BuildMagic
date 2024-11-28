// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor
{
    /// <summary>
    ///     An interface to associate <see cref="IBuildTask" /> and <see cref="IBuildProperty" />.
    /// </summary>
    public interface IBuildConfiguration
    {
        /// <summary>
        ///     The target type of the task.
        /// </summary>
        Type TaskType { get; }

        /// <summary>
        ///     Name of the property.
        /// </summary>
        string PropertyName { get; }

        /// <summary>
        ///     Gather the property for the task.
        /// </summary>
        IBuildProperty GatherProperty();
    }
}
