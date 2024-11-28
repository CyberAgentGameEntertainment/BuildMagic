// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEditor;
using UnityEngine;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories("Add iOS Capability",
        PropertyName = "BuildMagicEditor.BuiltIn.iOSAddCapabilityTask.capabilities")]
    public sealed partial class iOSAddCapabilityTask : BuildTaskBase<IPostBuildContext>
    {
        private readonly string _entitlementFilePath;
        private readonly iOSCapabilityList _capabilities;

        [Serializable]
        public class iOSCapabilityList
        {
            [SerializeReference] private ICapability[] _value;

            internal ICapability[] Value => _value;
        }

        public iOSAddCapabilityTask(string entitlementFilePath, iOSCapabilityList capabilities)
        {
            _entitlementFilePath = entitlementFilePath;
            _capabilities = capabilities;
        }

        public override void Run(IPostBuildContext context)
        {
            if (context.ActiveBuildTarget != BuildTarget.iOS) return;

#if UNITY_IOS
            RunInternal(context);
#endif
        }
    }
}
