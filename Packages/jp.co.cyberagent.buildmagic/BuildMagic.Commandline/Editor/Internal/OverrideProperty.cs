// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

namespace BuildMagicEditor.Commandline.Internal
{
    /// <summary>
    ///     Override property.
    /// </summary>
    internal class OverrideProperty
    {
        /// <summary>
        ///     Name of the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Value of the property.
        /// </summary>
        public string Value { get; }

        public OverrideProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
