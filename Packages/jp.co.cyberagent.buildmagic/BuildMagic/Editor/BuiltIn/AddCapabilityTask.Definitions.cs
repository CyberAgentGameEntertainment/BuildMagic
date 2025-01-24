// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BuildMagicEditor.BuiltIn
{
    public sealed partial class iOSAddCapabilityTask
    {
        [Flags]
        [Serializable]
        public enum BackgroundModesOptions
        {
            None = 0,
            AudioAirplayPiP = 1,
            LocationUpdates = 2,
            VoiceOverIP = 4,
            NewsstandDownloads = 8,
            ExternalAccessoryCommunication = 16, // 0x00000010
            UsesBluetoothLEAccessory = 32, // 0x00000020
            ActsAsABluetoothLEAccessory = 64, // 0x00000040
            BackgroundFetch = 128, // 0x00000080
            RemoteNotifications = 256, // 0x00000100
            Processing = 512 // 0x00000200
        }

        public interface ICapability
        {
        }

        [Flags]
        [Serializable]
        public enum MapsOptions
        {
            None = 0,
            Airplane = 1,
            Bike = 2,
            Bus = 4,
            Car = 8,
            Ferry = 16, // 0x00000010
            Pedestrian = 32, // 0x00000020
            RideSharing = 64, // 0x00000040
            StreetCar = 128, // 0x00000080
            Subway = 256, // 0x00000100
            Taxi = 512, // 0x00000200
            Train = 1024, // 0x00000400
            Other = 2048 // 0x00000800
        }

        [Serializable]
        public class AppGroupsCapability : ICapability
        {
            public string[] groups;
        }

        [Serializable]
        public class ApplePayCapability : ICapability
        {
            public string[] merchants;
        }

        [Serializable]
        public class AssociatedDomainsCapability : ICapability
        {
            public string[] domains;
        }

        [Serializable]
        public class BackgroundModesCapability : ICapability
        {
            public BackgroundModesOptions modes;
        }

        [Serializable]
        public class DataProtectionCapability : ICapability
        {
        }

        [Serializable]
        public class GameCenterCapability : ICapability
        {
        }

        [Serializable]
        public class HealthKitCapability : ICapability
        {
        }

        [Serializable]
        public class HomeKitCapability : ICapability
        {
        }

        [Serializable]
        public class iCloudCapability : ICapability
        {
            public bool enableKeyValueStorage;
            public bool enableiCloudDocument;
            public bool enablecloudKit;
            public bool addDefaultContainers;
            public string[] cusomContainers;
        }

        [Serializable]
        public class InAppPurchaseCapability : ICapability
        {
        }

        [Serializable]
        public class InterAppAudioCapability : ICapability
        {
        }

        [Serializable]
        public class KeychainSharingCapability : ICapability
        {
            public string[] accessGroups;
        }

        [Serializable]
        public class MapsCapability : ICapability
        {
            public MapsOptions options;
        }

        [Serializable]
        public class PersonalVPNCapability : ICapability
        {
        }

        [Serializable]
        public class PushNotificationCapability : ICapability
        {
            public bool development;
        }

        [Serializable]
        public class SigninWithAppleCapability : ICapability
        {
        }

        [Serializable]
        public class SiriCapability : ICapability
        {
        }

        [Serializable]
        public class WalletCapability : ICapability
        {
            public string[] passSubset;
        }

        [Serializable]
        public class WirelessAccessoryConfigurationCapability : ICapability
        {
        }

        [CustomPropertyDrawer(typeof(iOSCapabilityList))]
        public class iOSCapabilityListDrawer : PropertyDrawer
        {
            private SerializedProperty _boundProperty;
            private ReorderableList _reorderableList;
            private readonly Type[] _capabilityTypes;

            public iOSCapabilityListDrawer()
            {
                _capabilityTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type =>
                        typeof(ICapability).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    .ToArray();
            }

            private void Initialize(SerializedProperty property)
            {
                if (_boundProperty != property)
                {
                    _boundProperty = property;
                    _reorderableList = null;
                }

                if (_reorderableList == null)
                {
                    var arrayProperty = property.FindPropertyRelative("_value");

                    _reorderableList =
                        new ReorderableList(property.serializedObject, arrayProperty, true, true, true, true);

                    _reorderableList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Capabilities"); };

                    _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = arrayProperty.GetArrayElementAtIndex(index);

                        if (element != null)
                        {
                            rect.y += 2;

                            // get current class instance
                            var currentInstance = element.managedReferenceValue as ICapability;

                            // popup to pick a class
                            var selectedIndex = Array.IndexOf(_capabilityTypes, currentInstance?.GetType());
                            selectedIndex =
                                EditorGUI.Popup(
                                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                                    selectedIndex, _capabilityTypes.Select(t => t.Name).ToArray());

                            // create a new instance based on the selected class
                            if (selectedIndex >= 0 && selectedIndex < _capabilityTypes.Length)
                            {
                                var selectedType = _capabilityTypes[selectedIndex];
                                if (currentInstance == null || currentInstance.GetType() != selectedType)
                                    element.managedReferenceValue = Activator.CreateInstance(selectedType);
                            }

                            // draw the properties
                            if (element.managedReferenceValue != null)
                            {
                                var propertyRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 2,
                                    rect.width, EditorGUI.GetPropertyHeight(element, true));
                                EditorGUI.PropertyField(propertyRect, element, new GUIContent("Value"), true);
                            }
                        }
                    };

                    _reorderableList.elementHeightCallback = index =>
                    {
                        if (arrayProperty.arraySize > index)
                        {
                            var element = arrayProperty.GetArrayElementAtIndex(index);
                            return EditorGUI.GetPropertyHeight(element, true) + EditorGUIUtility.singleLineHeight + 6;
                        }

                        return EditorGUIUtility.singleLineHeight + 6;
                    };

                    _reorderableList.onAddCallback = list =>
                    {
                        arrayProperty.arraySize++;
                        list.index = arrayProperty.arraySize - 1;
                    };

                    _reorderableList.onRemoveCallback = list =>
                    {
                        ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    };
                }
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);

                Initialize(property);

                if (_reorderableList != null)
                    _reorderableList.DoList(
                        new Rect(position.x + 30, position.y, position.width - 30, position.height));

                EditorGUI.EndProperty();
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                Initialize(property);

                return _reorderableList != null
                    ? _reorderableList.GetHeight()
                    : base.GetPropertyHeight(property, label);
            }
        }
    }
}
