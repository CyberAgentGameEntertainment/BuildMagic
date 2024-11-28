// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor
{
    /// <summary>
    ///     A base class for <see cref="IBuildTask{TContext}" />.
    /// </summary>
    public abstract class BuildTaskBase<TContext> : IBuildTask<TContext> where TContext : IBuildContext
    {
        /// <inheritdoc cref="IBuildTask.Run" />
        public void Run(IBuildContext context)
        {
            if (context is TContext contextT)
                Run(contextT);
            else
                throw new InvalidCastException($"Invalid context type: {context.GetType()}");
        }

        /// <inheritdoc cref="IBuildTask{TContext}.Run(TContext)" />
        public abstract void Run(TContext context);
    }
}

