// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    internal sealed partial class TwoPaneSplitView : UnityEngine.UIElements.TwoPaneSplitView
    {
#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<TwoPaneSplitView, UxmlTraits>
        {
        }
#endif
    }
}
