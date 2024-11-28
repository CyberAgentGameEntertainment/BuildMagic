// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

namespace BuildMagicEditor
{
    public interface IBuildTaskBuilder
    {
        /// <summary>
        ///     Build the task.
        /// </summary>
        /// <param name="value">Build parameter.</param>
        IBuildTask Build(object value);
    }

    /// <summary>
    ///     Build a task with <c>TValue</c>.
    /// </summary>
    public interface IBuildTaskBuilder<TTask, TValue> : IBuildTaskBuilder
        where TTask : IBuildTask
    {
        /// <summary>
        ///     Build the task.
        /// </summary>
        /// <param name="value">Build parameter.</param>
        TTask Build(TValue value);
    }
}
