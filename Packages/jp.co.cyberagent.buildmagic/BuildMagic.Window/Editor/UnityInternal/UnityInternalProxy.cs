// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.UnityInternal
{
    internal static class UnityInternalProxy
    {
        public static void ReplaceVisualTreeBindingsUpdater(EditorWindow window)
        {
            var panel = window.rootVisualElement.elementPanel;
            panel.SetUpdater(new CustomVisualTreeBindingsUpdater(), VisualTreeUpdatePhase.Bindings);
        }

        public static void ReplaceUIRLayoutUpdater(EditorWindow window)
        {
            var panel = window.rootVisualElement.elementPanel;
            panel.SetUpdater(new CustomUIRLayoutUpdater(), VisualTreeUpdatePhase.Layout);
        }
    }
}

#endif
