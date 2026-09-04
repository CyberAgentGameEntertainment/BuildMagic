// --------------------------------------------------------------
// Copyright 2026 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace BuildMagic.Window.Editor.Utilities
{
    internal static class AdvancedDropdownItemExtensions
    {
        public static IEnumerable<AdvancedDropdownItem> GetChildren(this AdvancedDropdownItem item)
        {
#if UNITY_6000_5_OR_NEWER
            return item.childList;
#else
            return item.children;
#endif
        }
    }
}
