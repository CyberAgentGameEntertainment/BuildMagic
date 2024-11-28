// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagicEditor
{
    [Flags]
    public enum BuildTaskAccessories
    {
        None = 0,
        Configuration = 1 << 0,
        Parameters = 1 << 1,
        Builder = 1 << 2,
        All = -1
    }
}
