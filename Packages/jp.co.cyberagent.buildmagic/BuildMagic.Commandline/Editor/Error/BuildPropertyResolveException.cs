// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor.Commandline.Error
{
    /// <summary>
    ///     Exception thrown when a build property cannot be resolved.
    /// </summary>
    public class BuildPropertyResolveException : Exception
    {
        public BuildPropertyResolveException(string message) : base(message)
        {
        }

        public BuildPropertyResolveException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
