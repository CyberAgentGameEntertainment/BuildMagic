// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using BuildMagicEditor.Commandline.Error;

namespace BuildMagicEditor.Commandline.Internal
{
    /// <summary>
    ///     Command line parser for BuildMagic CLI.
    /// </summary>
    internal class CommandLineParser
    {
        /// <summary>
        ///     The prefix of option string.
        /// </summary>
        private static readonly string OptionPrefix = "-";

        internal CommandLineParser(string[] args)
        {
            _args = args;
        }

        public static CommandLineParser Create()
        {
            return new CommandLineParser(Environment.GetCommandLineArgs());
        }

        /// <summary>
        ///     Parse options with specified key.
        /// </summary>
        /// <param name="key">The key for options.</param>
        /// <param name="options">The options.</param>
        /// <returns>
        ///     If options are found, returns true. Otherwise, returns false.
        /// </returns>
        public bool TryParse(string key, out string[] options)
        {
            if (!TryParseInternal(key, out options)) return false;
            return options.Length > 0;
        }

        private bool TryParseInternal(string key, out string[] options)
        {
            // If the key does not start with the option prefix, add it.
            if (!key.StartsWith(OptionPrefix)) key = $"{OptionPrefix}{key}";

            var ret = new List<string>();
            var hasFlag = false;
            for (var i = 0; i < _args.Length; i++)
            {
                if (_args[i] == key)
                {
                    hasFlag = true;
                    if (i + 1 >= _args.Length) continue;

                    // option prefix is not allowed as a value.
                    if (_args[i + 1].StartsWith(OptionPrefix)) continue;

                    ret.Add(_args[i + 1]);
                }
            }

            options = ret.ToArray();
            return hasFlag;
        }

        public string[] Parse(string key)
        {
            if (TryParse(key, out var options))
            {
                return options;
            }

            throw new CommandLineArgumentException($"{key} does not specified.");
        }

        public string ParseFirst(string key)
        {
            return Parse(key).First();
        }

        public bool HasFlag(string key, bool allowValue = false)
        {
            if (!TryParse(key, out var options)) return false;
            if (!allowValue && options.Length != 0)
                throw new CommandLineArgumentException(
                    $"{key} must be specified as a flag and does not accept any value.");
            return true;
        }

        private readonly string[] _args;
    }
}
