// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.UnityInternal
{
    internal sealed class CustomUIRLayoutUpdater
#if UNITY_6000_3_OR_NEWER
        : VisualTreeLayoutUpdater
#else
        : UIRLayoutUpdater
#endif
    {
        public override void Update()
        {
            var backup = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logEnabled = false;
            try
            {
                base.Update();
            }
            finally
            {
                Debug.unityLogger.logEnabled = backup;
            }
        }
    }
}
