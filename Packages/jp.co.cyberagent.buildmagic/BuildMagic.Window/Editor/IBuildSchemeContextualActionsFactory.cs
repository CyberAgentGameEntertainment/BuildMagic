// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagic.Window.Editor
{
    internal interface IBuildSchemeContextualActionsFactory
    {
        public IBuildSchemeContextualActions Create(Func<string> getSchemeName);
    }
}
