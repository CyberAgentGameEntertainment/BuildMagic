// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEditor;
using UnityEngine;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories("PlayerSettings.Android: Custom Signing (on internal prepare phase)",
        PropertyName = "BuildMagicEditor.BuiltIn.AndroidCustomSigningTask")]
    public sealed class AndroidCustomSigningTask : BuildTaskBase<IInternalPrepareContext>
    {
        private string KeystoreName { get; }
        private string KeystorePass { get; }
        private string KeyaliasName { get; }
        private string KeyaliasPass { get; }

        public AndroidCustomSigningTask(string keystoreName, string keystorePass, string keyaliasName,
            string keyaliasPass)
        {
            KeystoreName = keystoreName;
            KeystorePass = keystorePass;
            KeyaliasName = keyaliasName;
            KeyaliasPass = keyaliasPass;
        }

        public override void Run(IInternalPrepareContext context)
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = KeystoreName;
            PlayerSettings.Android.keystorePass = KeystorePass;
            PlayerSettings.Android.keyaliasName = KeyaliasName;
            PlayerSettings.Android.keyaliasPass = KeyaliasPass;
        }
    }
}
