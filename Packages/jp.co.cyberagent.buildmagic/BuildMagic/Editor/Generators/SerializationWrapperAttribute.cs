// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor
{
    public class SerializationWrapperAttribute : Attribute
    {
        public SerializationWrapperAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        
        public Type TargetType { get; }
    }
}
