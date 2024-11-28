// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor
{
    public class BuildTaskBuilderAttribute : Attribute
    {
        public Type TaskType { get; }
        public Type ValueType { get; }

        public BuildTaskBuilderAttribute(Type taskType, Type valueType)
        {
            TaskType = taskType;
            ValueType = valueType;
        }
    }
}
