// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class UseSerializationWrapperAttribute : Attribute
    {
        public Type WrapperType { get; }

        public UseSerializationWrapperAttribute(Type wrapperType)
        {
            WrapperType = wrapperType;
        }
    }
}
