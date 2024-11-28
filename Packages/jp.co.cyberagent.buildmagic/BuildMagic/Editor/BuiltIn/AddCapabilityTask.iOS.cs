// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

#if UNITY_IOS

using System;
using UnityEditor.iOS.Xcode;

namespace BuildMagicEditor.BuiltIn
{
    public sealed partial class iOSAddCapabilityTask
    {
        private void RunInternal(IPostBuildContext context)
        {
            var pbxProjectPath = PBXProject.GetPBXProjectPath(context.BuildReport.summary.outputPath);
            var pbxProject = new PBXProject();
            pbxProject.ReadFromFile(pbxProjectPath);

            var manager = new ProjectCapabilityManager(
                pbxProjectPath,
                _entitlementFilePath,
                targetGuid: pbxProject.GetUnityMainTargetGuid());

            foreach (var capability in _capabilities.Value) ProcessCapability(manager, capability);

            manager.WriteToFile();
        }

        private static void ProcessCapability(ProjectCapabilityManager manager, ICapability capability)
        {
            var generator = GenerateProcessor(capability);
            generator.Process(manager, capability);
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
                GameCenterCapability => new GameCenterCapabilityProcessor(),
                HealthKitCapability => new HealthKitCapabilityProcessor(),
                HomeKitCapability => new HomeKitCapabilityProcessor(),
                iCloudCapability => new iCloudCapabilityProcessor(),
                InAppPurchaseCapability => new InAppPurchaseCapabilityProcessor(),
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
            void Process(ProjectCapabilityManager manager, ICapability capability);
        }

        private abstract class CapabilityProcessorBase<T> : ICapabilityProcessor
            where T : ICapability
        {
            protected abstract void Process(ProjectCapabilityManager manager, T capability);

            public void Process(ProjectCapabilityManager manager, ICapability capability)
            {
                if (capability is T t) Process(manager, t);
            }
        }

        private class AppGroupsCapabilityProcessor : CapabilityProcessorBase<AppGroupsCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, AppGroupsCapability capability)
            {
                manager.AddAppGroups(capability.groups);
            }
        }

        private class ApplePayCapabilityProcessor : CapabilityProcessorBase<ApplePayCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, ApplePayCapability capability)
            {
                manager.AddApplePay(capability.merchants);
            }
        }

        private class AssociatedDomainsCapabilityProcessor : CapabilityProcessorBase<AssociatedDomainsCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, AssociatedDomainsCapability capability)
            {
                manager.AddAssociatedDomains(capability.domains);
            }
        }

        private class BackgroundModesCapabilityProcessor : CapabilityProcessorBase<BackgroundModesCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, BackgroundModesCapability capability)
            {
                manager.AddBackgroundModes(ToUnityType(capability.modes));
            }

            private static UnityEditor.iOS.Xcode.BackgroundModesOptions ToUnityType(BackgroundModesOptions options)
            {
                return (UnityEditor.iOS.Xcode.BackgroundModesOptions)options;
            }
        }

        private class DataProtectionCapabilityProcessor : CapabilityProcessorBase<DataProtectionCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, DataProtectionCapability capability)
            {
                manager.AddDataProtection();
            }
        }

        private class GameCenterCapabilityProcessor : CapabilityProcessorBase<GameCenterCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, GameCenterCapability capability)
            {
                manager.AddGameCenter();
            }
        }

        private class HealthKitCapabilityProcessor : CapabilityProcessorBase<HealthKitCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, HealthKitCapability capability)
            {
                manager.AddHealthKit();
            }
        }

        private class HomeKitCapabilityProcessor : CapabilityProcessorBase<HomeKitCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, HomeKitCapability capability)
            {
                manager.AddHomeKit();
            }
        }

        private class iCloudCapabilityProcessor : CapabilityProcessorBase<iCloudCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, iCloudCapability capability)
            {
                manager.AddiCloud(
                    capability.enableKeyValueStorage,
                    capability.enableiCloudDocument,
                    capability.enablecloudKit,
                    capability.addDefaultContainers,
                    capability.cusomContainers);
            }
        }

        private class InAppPurchaseCapabilityProcessor : CapabilityProcessorBase<InAppPurchaseCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, InAppPurchaseCapability capability)
            {
                manager.AddInAppPurchase();
            }
        }

        private class InterAppAudioCapabilityProcessor : CapabilityProcessorBase<InterAppAudioCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, InterAppAudioCapability capability)
            {
                manager.AddInterAppAudio();
            }
        }

        private class KeychainSharingCapabilityProcessor : CapabilityProcessorBase<KeychainSharingCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, KeychainSharingCapability capability)
            {
                manager.AddKeychainSharing(capability.accessGroups);
            }
        }

        private class MapsCapabilityProcessor : CapabilityProcessorBase<MapsCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, MapsCapability capability)
            {
                manager.AddMaps(ToUnityType(capability.options));
            }

            private static UnityEditor.iOS.Xcode.MapsOptions ToUnityType(MapsOptions options)
            {
                return (UnityEditor.iOS.Xcode.MapsOptions)options;
            }
        }

        private class PersonalVPNCapabilityProcessor : CapabilityProcessorBase<PersonalVPNCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, PersonalVPNCapability capability)
            {
                manager.AddPersonalVPN();
            }
        }

        private class PushNotificationCapabilityProcessor : CapabilityProcessorBase<PushNotificationCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, PushNotificationCapability capability)
            {
                manager.AddPushNotifications(capability.development);
            }
        }

        private class SigninWithAppleCapabilityProcessor : CapabilityProcessorBase<SigninWithAppleCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, SigninWithAppleCapability capability)
            {
                manager.AddSignInWithApple();
            }
        }

        private class SiriCapabilityProcessor : CapabilityProcessorBase<SiriCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, SiriCapability capability)
            {
                manager.AddSiri();
            }
        }

        private class WalletCapabilityProcessor : CapabilityProcessorBase<WalletCapability>
        {
            protected override void Process(ProjectCapabilityManager manager, WalletCapability capability)
            {
                manager.AddWallet(capability.passSubset);
            }
        }

        private class WirelessAccessoryConfigurationCapabilityProcessor :
            CapabilityProcessorBase<WirelessAccessoryConfigurationCapability>
        {
            protected override void Process(ProjectCapabilityManager manager,
                WirelessAccessoryConfigurationCapability capability)
            {
                manager.AddWirelessAccessoryConfiguration();
            }
        }
    }
}
#endif
