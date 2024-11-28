// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagic.Window.Editor
{
    public sealed class DebugLogDisabledScope : IDisposable
    {
        private readonly bool _backup;
        
        public DebugLogDisabledScope()
        {
            _backup = UnityEngine.Debug.unityLogger.logEnabled;
            UnityEngine.Debug.unityLogger.logEnabled = false;
        }
        
        public void Dispose()
        {
            UnityEngine.Debug.unityLogger.logEnabled = _backup;
        }
    }
}
