// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

#if UNITY_6000_0_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories("Build Profiles: Switch Build Profile",
        PropertyName = "BuildProfile.SetActiveBuildProfile()")]
    public class SwitchBuildProfileTask : BuildTaskBase<IPreBuildContext>
    {
        private readonly IReadOnlyDictionary<BuildTarget, BuildProfile> _buildProfile;

        public SwitchBuildProfileTask(IReadOnlyDictionary<BuildTarget, BuildProfileWrapper> buildProfile)
        {
            _buildProfile = buildProfile.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.BuildProfile);
        }

        public override void Run(IPreBuildContext context)
        {
            if (_buildProfile.TryGetValue(context.ActiveBuildTarget, out var buildProfile) && buildProfile)
            {
                var buildTarget =
                    (BuildTarget)typeof(BuildProfile).GetField("m_BuildTarget",
                        BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(buildProfile);

                if (buildTarget == context.ActiveBuildTarget)
                {
                    BuildProfile.SetActiveBuildProfile(buildProfile);
                    return;
                }
            }

            Debug.LogWarning($"Build profile not found for target: {context.ActiveBuildTarget}");
        }

        [Serializable]
        public struct BuildProfileWrapper
        {
            [SerializeField] private BuildProfile buildProfile;

            public BuildProfile BuildProfile => buildProfile;
        }

        [CustomPropertyDrawer(typeof(BuildProfileWrapper))]
        private class BuildProfileWrapperDrawer : PropertyDrawer
        {
            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                var root = new VisualElement();
                var valueProperty = property.FindPropertyRelative("buildProfile");
                var objectField = new PropertyField(valueProperty, "Switch Build Profile");

                root.Bind(property.serializedObject);

                var buildProfileContainer = new VisualElement();

                buildProfileContainer.TrackPropertyValue(valueProperty,
                    valueProperty =>
                    {
                        UpdateBuildProfileGUI(buildProfileContainer, valueProperty.objectReferenceValue as BuildProfile,
                            property.propertyPath);
                    });

                root.Add(objectField);
                root.Add(buildProfileContainer);

                // ReSharper disable once HeapView.CanAvoidClosure
                root.RegisterCallback<AttachToPanelEvent>(evt =>
                {
                    UpdateBuildProfileGUI(buildProfileContainer, valueProperty.objectReferenceValue as BuildProfile,
                        property.propertyPath);
                });

                return root;
            }

            private static void UpdateBuildProfileGUI(VisualElement container, BuildProfile value, string viewDataKey)
            {
                container.Clear();
                if (!value) return;

                var buildTarget =
                    (BuildTarget)typeof(BuildProfile).GetField("m_BuildTarget",
                        BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value);
                if (container.FindAncestorUserData() is BuildTarget tabBuildTarget)
                    if (tabBuildTarget != buildTarget)
                    {
                        container.Add(new HelpBox(
                            $"The build target of the selected build profile ({buildTarget}) does not match the current tab's build target ({tabBuildTarget}).",
                            HelpBoxMessageType.Warning));
                        return;
                    }

                var editor = Editor.CreateEditor(value);
                if (!editor) return;

                var foldout = new Foldout
                {
                    viewDataKey = viewDataKey,
                    text = value.name
                };

                foldout.Add(editor.CreateInspectorGUI());
                container.Add(foldout);
            }
        }
    }
}
#endif
