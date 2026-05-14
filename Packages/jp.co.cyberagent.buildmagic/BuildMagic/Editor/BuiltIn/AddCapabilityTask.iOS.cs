// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

#if UNITY_IOS

using System;
using System.Reflection;
using UnityEditor.iOS.Xcode;

namespace BuildMagicEditor.BuiltIn
{
    public sealed partial class iOSAddCapabilityTask
    {
        private const string ExtendedVirtualAddressingCapabilityId =
            "com.apple.developer.kernel.extended-virtual-addressing";
        private const string ExtendedVirtualAddressingEntitlementKey =
            "com.apple.developer.kernel.extended-virtual-addressing";
        private const string IncreasedMemoryLimitCapabilityId =
            "com.apple.developer.kernel.increased-memory-limit";
        private const string IncreasedMemoryLimitEntitlementKey =
            "com.apple.developer.kernel.increased-memory-limit";

        private void RunInternal(IPostBuildContext context)
        {
            var buildPath = context.BuildReport.summary.outputPath;
            var pbxProjectPath = PBXProject.GetPBXProjectPath(buildPath);
            var pbxProject = new PBXProject();
            pbxProject.ReadFromFile(pbxProjectPath);
            var targetGuid = pbxProject.GetUnityMainTargetGuid();

            var manager = new iOSProjectCapabilityManager(
                pbxProjectPath,
                _entitlementFilePath,
                targetGuid);
            var capabilityProcessContext = new iOSCapabilityProcessContext(
                manager,
                targetGuid,
                _entitlementFilePath);

            foreach (var capability in _capabilities.Value)
                ProcessCapability(capabilityProcessContext, capability);

            capabilityProcessContext.WriteToFile();
        }

        private static void ProcessCapability(iOSCapabilityProcessContext context, ICapability capability)
        {
            var generator = GenerateProcessor(capability);
            generator.Process(context, capability);
        }

        private static ICapabilityProcessor GenerateProcessor(ICapability capability)
        {
            return capability switch
            {
                AppGroupsCapability => new AppGroupsCapabilityProcessor(),
                ApplePayCapability => new ApplePayCapabilityProcessor(),
                AssociatedDomainsCapability => new AssociatedDomainsCapabilityProcessor(),
                BackgroundModesCapability => new BackgroundModesCapabilityProcessor(),
                DataProtectionCapability => new DataProtectionCapabilityProcessor(),
                ExtendedVirtualAddressingCapability => new ExtendedVirtualAddressingCapabilityProcessor(),
                GameCenterCapability => new GameCenterCapabilityProcessor(),
                HealthKitCapability => new HealthKitCapabilityProcessor(),
                HomeKitCapability => new HomeKitCapabilityProcessor(),
                iCloudCapability => new iCloudCapabilityProcessor(),
                InAppPurchaseCapability => new InAppPurchaseCapabilityProcessor(),
                IncreasedMemoryLimitCapability => new IncreasedMemoryLimitCapabilityProcessor(),
                InterAppAudioCapability => new InterAppAudioCapabilityProcessor(),
                KeychainSharingCapability => new KeychainSharingCapabilityProcessor(),
                MapsCapability => new MapsCapabilityProcessor(),
                PersonalVPNCapability => new PersonalVPNCapabilityProcessor(),
                PushNotificationCapability => new PushNotificationCapabilityProcessor(),
                SigninWithAppleCapability => new SigninWithAppleCapabilityProcessor(),
                SiriCapability => new SiriCapabilityProcessor(),
                WalletCapability => new WalletCapabilityProcessor(),
                WirelessAccessoryConfigurationCapability => new WirelessAccessoryConfigurationCapabilityProcessor(),
                _ => throw new NotImplementedException()
            };
        }

        private interface ICapabilityProcessor
        {
            void Process(iOSCapabilityProcessContext context, ICapability capability);
        }

        private abstract class CapabilityProcessorBase<T> : ICapabilityProcessor
            where T : ICapability
        {
            protected abstract void Process(iOSCapabilityProcessContext context, T capability);

            public void Process(iOSCapabilityProcessContext context, ICapability capability)
            {
                if (capability is T t) Process(context, t);
            }
        }

        private sealed class iOSCapabilityProcessContext
        {
            private const string GetOrCreateEntitlementDocMethodName = "GetOrCreateEntitlementDoc";

            private static readonly MethodInfo GetOrCreateEntitlementDocMethod =
                typeof(ProjectCapabilityManager).GetMethod(
                    GetOrCreateEntitlementDocMethodName,
                    BindingFlags.Instance | BindingFlags.NonPublic);

            private readonly string _entitlementFilePath;
            private readonly iOSProjectCapabilityManager _manager;

            public iOSCapabilityProcessContext(
                iOSProjectCapabilityManager manager,
                string targetGuid,
                string entitlementFilePath)
            {
                _manager = manager;
                Project = manager.Project;
                TargetGuid = targetGuid;
                _entitlementFilePath = entitlementFilePath;
            }

            public ProjectCapabilityManager Manager => _manager;

            public PBXProject Project { get; }

            public string TargetGuid { get; }

            public string EntitlementFilePath
            {
                get
                {
                    EnsureEntitlementFilePath();
                    return _entitlementFilePath;
                }
            }

            public void SetEntitlementBoolean(string entitlementKey)
            {
                var entitlementDocument = GetOrCreateEntitlementDocument();
                entitlementDocument.root.SetBoolean(entitlementKey, true);
            }

            public void WriteToFile()
            {
                _manager.WriteToFile();
            }

            private void EnsureEntitlementFilePath()
            {
                if (string.IsNullOrEmpty(_entitlementFilePath))
                    throw new InvalidOperationException(
                        "An entitlement file path is required to add iOS memory capabilities.");
            }

            private PlistDocument GetOrCreateEntitlementDocument()
            {
                EnsureEntitlementFilePath();

                if (GetOrCreateEntitlementDocMethod == null)
                    throw new MissingMethodException(
                        typeof(ProjectCapabilityManager).FullName,
                        GetOrCreateEntitlementDocMethodName);

                return (PlistDocument)GetOrCreateEntitlementDocMethod.Invoke(_manager, null);
            }
        }

        private sealed class iOSProjectCapabilityManager : ProjectCapabilityManager
        {
            public iOSProjectCapabilityManager(
                string pbxProjectPath,
                string entitlementFilePath,
                string targetGuid)
                : base(pbxProjectPath, entitlementFilePath, targetGuid: targetGuid)
            {
            }

            public PBXProject Project => project;
        }

        private class AppGroupsCapabilityProcessor : CapabilityProcessorBase<AppGroupsCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, AppGroupsCapability capability)
            {
                context.Manager.AddAppGroups(capability.groups);
            }
        }

        private class ApplePayCapabilityProcessor : CapabilityProcessorBase<ApplePayCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, ApplePayCapability capability)
            {
                context.Manager.AddApplePay(capability.merchants);
            }
        }

        private class AssociatedDomainsCapabilityProcessor : CapabilityProcessorBase<AssociatedDomainsCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, AssociatedDomainsCapability capability)
            {
                context.Manager.AddAssociatedDomains(capability.domains);
            }
        }

        private class BackgroundModesCapabilityProcessor : CapabilityProcessorBase<BackgroundModesCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, BackgroundModesCapability capability)
            {
                context.Manager.AddBackgroundModes(ToUnityType(capability.modes));
            }

            private static UnityEditor.iOS.Xcode.BackgroundModesOptions ToUnityType(BackgroundModesOptions options)
            {
                return (UnityEditor.iOS.Xcode.BackgroundModesOptions)options;
            }
        }

        private class DataProtectionCapabilityProcessor : CapabilityProcessorBase<DataProtectionCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, DataProtectionCapability capability)
            {
                context.Manager.AddDataProtection();
            }
        }

        private class ExtendedVirtualAddressingCapabilityProcessor :
            CapabilityProcessorBase<ExtendedVirtualAddressingCapability>
        {
            protected override void Process(
                iOSCapabilityProcessContext context,
                ExtendedVirtualAddressingCapability capability)
            {
#if UNITY_6000_0_OR_NEWER
                context.Manager.AddExtendedVirtualAddressing();
#else
                context.SetEntitlementBoolean(ExtendedVirtualAddressingEntitlementKey);
                context.Project.AddCapability(
                    context.TargetGuid,
                    new PBXCapabilityType(ExtendedVirtualAddressingCapabilityId, true, null, false),
                    context.EntitlementFilePath,
                    false);
#endif
            }
        }

        private class GameCenterCapabilityProcessor : CapabilityProcessorBase<GameCenterCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, GameCenterCapability capability)
            {
                context.Manager.AddGameCenter();
            }
        }

        private class HealthKitCapabilityProcessor : CapabilityProcessorBase<HealthKitCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, HealthKitCapability capability)
            {
                context.Manager.AddHealthKit();
            }
        }

        private class HomeKitCapabilityProcessor : CapabilityProcessorBase<HomeKitCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, HomeKitCapability capability)
            {
                context.Manager.AddHomeKit();
            }
        }

        private class iCloudCapabilityProcessor : CapabilityProcessorBase<iCloudCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, iCloudCapability capability)
            {
                context.Manager.AddiCloud(
                    capability.enableKeyValueStorage,
                    capability.enableiCloudDocument,
                    capability.enablecloudKit,
                    capability.addDefaultContainers,
                    capability.cusomContainers);
            }
        }

        private class InAppPurchaseCapabilityProcessor : CapabilityProcessorBase<InAppPurchaseCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, InAppPurchaseCapability capability)
            {
                context.Manager.AddInAppPurchase();
            }
        }

        private class IncreasedMemoryLimitCapabilityProcessor : CapabilityProcessorBase<IncreasedMemoryLimitCapability>
        {
            protected override void Process(
                iOSCapabilityProcessContext context,
                IncreasedMemoryLimitCapability capability)
            {
#if UNITY_6000_0_OR_NEWER
                context.Manager.AddIncreasedMemoryLimit();
#else
                context.SetEntitlementBoolean(IncreasedMemoryLimitEntitlementKey);
                context.Project.AddCapability(
                    context.TargetGuid,
                    new PBXCapabilityType(IncreasedMemoryLimitCapabilityId, true, null, false),
                    context.EntitlementFilePath,
                    false);
#endif
            }
        }

        private class InterAppAudioCapabilityProcessor : CapabilityProcessorBase<InterAppAudioCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, InterAppAudioCapability capability)
            {
                context.Manager.AddInterAppAudio();
            }
        }

        private class KeychainSharingCapabilityProcessor : CapabilityProcessorBase<KeychainSharingCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, KeychainSharingCapability capability)
            {
                context.Manager.AddKeychainSharing(capability.accessGroups);
            }
        }

        private class MapsCapabilityProcessor : CapabilityProcessorBase<MapsCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, MapsCapability capability)
            {
                context.Manager.AddMaps(ToUnityType(capability.options));
            }

            private static UnityEditor.iOS.Xcode.MapsOptions ToUnityType(MapsOptions options)
            {
                return (UnityEditor.iOS.Xcode.MapsOptions)options;
            }
        }

        private class PersonalVPNCapabilityProcessor : CapabilityProcessorBase<PersonalVPNCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, PersonalVPNCapability capability)
            {
                context.Manager.AddPersonalVPN();
            }
        }

        private class PushNotificationCapabilityProcessor : CapabilityProcessorBase<PushNotificationCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, PushNotificationCapability capability)
            {
                context.Manager.AddPushNotifications(capability.development);
            }
        }

        private class SigninWithAppleCapabilityProcessor : CapabilityProcessorBase<SigninWithAppleCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, SigninWithAppleCapability capability)
            {
                context.Manager.AddSignInWithApple();
            }
        }

        private class SiriCapabilityProcessor : CapabilityProcessorBase<SiriCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, SiriCapability capability)
            {
                context.Manager.AddSiri();
            }
        }

        private class WalletCapabilityProcessor : CapabilityProcessorBase<WalletCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context, WalletCapability capability)
            {
                context.Manager.AddWallet(capability.passSubset);
            }
        }

        private class WirelessAccessoryConfigurationCapabilityProcessor :
            CapabilityProcessorBase<WirelessAccessoryConfigurationCapability>
        {
            protected override void Process(iOSCapabilityProcessContext context,
                WirelessAccessoryConfigurationCapability capability)
            {
                context.Manager.AddWirelessAccessoryConfiguration();
            }
        }
    }
}
#endif
