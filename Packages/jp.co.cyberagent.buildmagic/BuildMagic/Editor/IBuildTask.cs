// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

namespace BuildMagicEditor
{
    /// <summary>
    ///     Interface for defining BuildMagic processes.
    /// </summary>
    public interface IBuildTask
    {
        /// <summary>
        ///     Execute the task.
        /// </summary>
        /// <param name="context">Context for executing the task.</param>
        void Run(IBuildContext context);
    }

    /// <summary>
    ///     Interface for defining BuildMagic processes with a specific context <see cref="TContext" />.
    /// </summary>
    public interface IBuildTask<TContext> : IBuildTask
        where TContext : IBuildContext
    {
        /// <summary>
        ///     Execute the task.
        /// </summary>
        /// <param name="context">Context for executing the task.</param>
        void Run(TContext context);
    }
}
