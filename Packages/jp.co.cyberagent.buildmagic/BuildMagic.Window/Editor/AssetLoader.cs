// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEditor;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor
{
    internal static class AssetLoader
    {
        private const string AssetPath = "Packages/jp.co.cyberagent.buildmagic/BuildMagic.Window/Editor/Assets/";

        public static VisualTreeAsset LoadUxml(string name)
            => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{AssetPath}/Uxml/{name}.uxml");

        public static StyleSheet LoadUss(string name)
            => AssetDatabase.LoadAssetAtPath<StyleSheet>($"{AssetPath}/Uss/{name}.uss");
    }
}
