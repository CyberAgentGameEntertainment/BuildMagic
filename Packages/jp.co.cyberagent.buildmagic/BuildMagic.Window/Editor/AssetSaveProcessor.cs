// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEditor;

namespace BuildMagic.Window.Editor
{
    internal class AssetSaveProcessor : AssetModificationProcessor
    {
        public static event Action OnWillSaveAssetsEvent;
        
        private static string[] OnWillSaveAssets(string[] paths)
        {
            OnWillSaveAssetsEvent?.Invoke();
            return paths;
        }
    }
}
