// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagicEditor;
using UnityEngine;

namespace BuildMagic.Window.Editor.Elements
{
    internal interface IRightPaneView
    {
        event Action<Rect> AddRequested;
        event Action<ConfigurationType, int, IBuildConfiguration> RemoveRequested;
        event Action<ConfigurationType> PasteRequested;
        
        Func<ConfigurationType, bool> CanPaste { set; }
    }
}
