// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagic.Window.Editor.Elements
{
    internal interface ILeftPaneView
    {
        public IBuildSchemeContextualActionsFactory ContextualActionsFactory { set; }

        public IBuildSchemeContextualActions ContextualActionsForSelectedScheme { set; }

        event Action<int> OnSelectionChanged;
        
        event Action SaveRequested;
        event Action NewBuildSchemeRequested;
    }
}
