// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BuildMagicEditor.BuiltIn
{
    /// <summary>
    ///     Pre-build player task to set scenes.
    /// </summary>
    [GenerateBuildTaskAccessories("EditorBuildSettings: Set Scenes",
        PropertyName = "EditorBuildSettings.scenes")]
    internal class EditorBuildSettingsSetScenesTask : BuildTaskBase<IPreBuildContext>
    {
        public EditorBuildSettingsSetScenesTask(SceneObject[] scenes)
        {
            _scenes = scenes;
        }

        public override void Run(IPreBuildContext preBuildContext)
        {
            EditorBuildSettings.scenes = _scenes
                .Select(s => new EditorBuildSettingsScene(s.Path, true))
                .ToArray();
        }

        private readonly SceneObject[] _scenes;

        [Serializable]
        public class SceneObject
        {
            [SerializeField] private string _path;
            public string Path => _path;
            
            internal static SceneObject FromPath(string path)
            {
                return new SceneObject { _path = path };
            }
        }

        [CustomPropertyDrawer(typeof(SceneObject))]
        public class SceneObjectDrawer : PropertyDrawer
        {
            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                var container = new VisualElement();

                var pathProperty = property.FindPropertyRelative("_path");

                var objectField = new ObjectField
                {
                    objectType = typeof(SceneAsset),
                    allowSceneObjects = false,
                    value = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathProperty.stringValue)
                };
                container.Add(objectField);

                objectField.RegisterValueChangedCallback(evt =>
                {
                    var sceneAsset = evt.newValue as SceneAsset;
                    pathProperty.stringValue =
                        sceneAsset != null ? AssetDatabase.GetAssetPath(sceneAsset) : string.Empty;
                    pathProperty.serializedObject.ApplyModifiedProperties();
                });

                return container;
            }
        }
    }

    /// <summary>
    ///     <see cref="EditorBuildSettingsSetScenesTaskConfiguration" /> の <see cref="IProjectSettingApplier" /> 実装
    /// </summary>
    internal partial class EditorBuildSettingsSetScenesTaskConfiguration : IProjectSettingApplier
    {
        public void ApplyProjectSetting()
        {
            Value = EditorBuildSettings.scenes
                .Select(scene => EditorBuildSettingsSetScenesTask.SceneObject.FromPath(scene.path))
                .Where(scene => scene != null)
                .ToArray();
        }
    }
}
