// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class GenerateBuildTaskAccessoriesAttribute : Attribute
    {
        public GenerateBuildTaskAccessoriesAttribute()
        {
        }
        
        public GenerateBuildTaskAccessoriesAttribute(string displayName)
        {
            DisplayName = displayName;
        }
        
        public string DisplayName { get; }
        public string PropertyName { get; set; }
        public BuildTaskAccessories Targets { get; set; } = BuildTaskAccessories.All;
    }
}
