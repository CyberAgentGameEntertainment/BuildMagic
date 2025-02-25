// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using BuildMagic.Window.Editor.Foundation.TinyRx;
using BuildMagic.Window.Editor.Foundation.TinyRx.ObservableProperty;
using UnityEditor;

namespace BuildMagic.Window.Editor.Utilities
{
    public static class UserSettings
    {
        static UserSettings()
        {
            EnableInternalPrepareEditor = RegisterBool("BuildMagic.EnableInternalPrepareEditor", false);
        }

        public static ObservableProperty<bool> EnableInternalPrepareEditor { get; }

        private static ObservableProperty<bool> RegisterBool(string key, bool defaultValue)
        {
            var result = new ObservableProperty<bool>(EditorPrefs.GetBool(key, defaultValue));
            result.Subscribe(value => EditorPrefs.SetBool(key, value));
            return result;
        }
    }
}
