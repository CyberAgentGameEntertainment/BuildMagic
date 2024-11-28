// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagic.Window.Editor.Elements
{
    internal interface ILeftPaneView
    {
        event Action<string> CopyCreateRequested;
        event Action<string> InheritCreateRequested;
        event Action<string> RemoveRequested;
        event Action PreBuildRequested;
        event Action<string> PreBuildRequestedByName;
        event Action BuildRequested;

        event Action<int> OnSelectionChanged;
        
        event Action SaveRequested;
    }
}
