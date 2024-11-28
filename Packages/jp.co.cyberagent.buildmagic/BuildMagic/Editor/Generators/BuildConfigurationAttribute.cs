// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BuildConfigurationAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public string PropertyName { get; set; }

        public BuildConfigurationAttribute(string displayName)
        {
            DisplayName = displayName;
        }

        public BuildConfigurationAttribute()
        {
        }
    }
}
