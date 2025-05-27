// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

#if UNITY_6000_0_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories("Build Profiles: Switch Build Profile",
        PropertyName = "BuildProfile.SetActiveBuildProfile()")]
    public class SwitchBuildProfileTask : BuildTaskBase<IPreBuildContext>
    {
        private readonly BuildProfile _buildProfile;

        public SwitchBuildProfileTask(BuildProfileWrapper buildProfile)
        {
            _buildProfile = buildProfile.BuildProfile;
        }

        public override void Run(IPreBuildContext context)
        {
            BuildProfile.SetActiveBuildProfile(_buildProfile);
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
                        UpdateBuildProfileGUI(buildProfileContainer, valueProperty.objectReferenceValue,
                            property.propertyPath);
                    });

                UpdateBuildProfileGUI(buildProfileContainer, valueProperty.objectReferenceValue, property.propertyPath);

                root.Add(objectField);
                root.Add(buildProfileContainer);
                return root;
            }

            private static void UpdateBuildProfileGUI(VisualElement container, Object value, string viewDataKey)
            {
                container.Clear();
                if (!value) return;

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
