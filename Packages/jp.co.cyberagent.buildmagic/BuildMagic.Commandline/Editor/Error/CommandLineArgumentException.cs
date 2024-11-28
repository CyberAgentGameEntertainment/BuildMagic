// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor.Commandline.Error
{
    public class CommandLineArgumentException : Exception
    {
        public CommandLineArgumentException(string message) : base(message) { }
    }
}