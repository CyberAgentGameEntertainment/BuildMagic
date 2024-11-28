// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using BuildMagicEditor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using TabView = BuildMagic.Window.Editor.Elements.TabView; // UnityEngine.UIElements.TabView is added in Unity 2023.2

namespace BuildMagic.Window.Editor.SubWindows
{
    internal sealed class DiffWindow : EditorWindow
    {
        private static readonly Vector2 MinSize = new(800, 300); // TODO: Temporary size

        private string _leftSchemeName;
        private string _rightSchemeName;
        private ConfigurationType _configurationType;
        private ListView _listView;

        private DiffRowModel[] _diffModels;

        private void CreateGUI()
        {
            _leftSchemeName = null;
            _rightSchemeName = null;
            _configurationType = ConfigurationType.None;

            var schemeNames = BuildSchemeLoader.LoadAll<BuildScheme>().Select(s => s.Name).ToArray();

            var visualTree = AssetLoader.LoadUxml("SubWindows/DiffWindow");
            visualTree.CloneTree(rootVisualElement);

            var styleSheet = AssetLoader.LoadUss("SubWindows/DiffWindow");
            rootVisualElement.styleSheets.Add(styleSheet);
            var themeStyleSheet =
                AssetLoader.LoadUss(EditorGUIUtility.isProSkin
                                        ? "SubWindows/DiffWindow_Dark"
                                        : "SubWindows/DiffWindow_Light");
            rootVisualElement.styleSheets.Add(themeStyleSheet);

            var tabView = rootVisualElement.Q<TabView>();
            tabView.Setup();
            tabView.OnTabSelected += index =>
            {
                _configurationType = index switch
                {
                    (int)ConfigurationType.PreBuild        => ConfigurationType.PreBuild,
                    (int)ConfigurationType.PostBuild       => ConfigurationType.PostBuild,
                    _                                      => throw new ArgumentOutOfRangeException()
                };
                RebuildDiff();
            };

            var leftDropdown = rootVisualElement.Q<DropdownField>("left-dropdown");
            leftDropdown.choices.AddRange(schemeNames);
            leftDropdown.RegisterValueChangedCallback(evt =>
            {
                _leftSchemeName = evt.newValue;
                RebuildDiff();
            });

            var rightDropdown = rootVisualElement.Q<DropdownField>("right-dropdown");
            rightDropdown.choices.AddRange(schemeNames);
            rightDropdown.RegisterValueChangedCallback(evt =>
            {
                _rightSchemeName = evt.newValue;
                RebuildDiff();
            });

            _listView = rootVisualElement.Q<ListView>("");
        }

        private void RebuildDiff()
        {
            if (string.IsNullOrWhiteSpace(_leftSchemeName) || string.IsNullOrWhiteSpace(_rightSchemeName) || _configurationType == ConfigurationType.None)
                return;
            
            var leftScheme = BuildSchemeLoader.Load<BuildScheme>(_leftSchemeName);
            var rightScheme = BuildSchemeLoader.Load<BuildScheme>(_rightSchemeName);

            var leftConfigurations = GetConfigurations(leftScheme, _configurationType).ToList();
            var rightConfigurations = GetConfigurations(rightScheme, _configurationType).ToList();
            
            var workList = new List<DiffRowModel>();
            foreach (var left in leftConfigurations)
            {
                var right = rightConfigurations.FirstOrDefault(r => r.GetType() == left.GetType());
                if (right != null)
                    rightConfigurations.Remove(right);
                var model = DiffRowModel.Create(left, right);
                workList.Add(model);
            }
            workList.AddRange(rightConfigurations.Select(right => DiffRowModel.Create(null, right)));

            _diffModels = workList.ToArray();

            _listView.ClearSelection();
            _listView.Clear();
            _listView.itemsSource = _diffModels;
            _listView.makeItem = () => new DiffRowView();
            _listView.bindItem = (e, index) => ((DiffRowView)e).Bind(_diffModels[index]);
            _listView.unbindItem = (e, _) => ((DiffRowView)e).Unbind(null);
            _listView.Rebuild();

            return;

            IEnumerable<IBuildConfiguration> GetConfigurations(IBuildScheme scheme, ConfigurationType type)
            {
                switch (type)
                {
                    case ConfigurationType.PreBuild:
                        return scheme.PreBuildConfigurations;
                    case ConfigurationType.PostBuild:
                        return scheme.PostBuildConfigurations;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal static void Open()
        {
            var window = CreateInstance<DiffWindow>();
            window.minSize = MinSize;
            window.titleContent = new GUIContent("Build Scheme Diff - Build Magic");
            window.Show();
        }
        
        [Serializable]
        private class BuildConfigurationWrapper
        {
            [SerializeReference]
            private IBuildConfiguration _configuration;

            public BuildConfigurationWrapper(IBuildConfiguration configuration)
            {
                _configuration = configuration;
            }
        }
        
        [CustomPropertyDrawer(typeof(BuildConfigurationWrapper))]
        public class BuildConfigurationWrapperDrawer : PropertyDrawer
        {
            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                var configurationProperty = property.FindPropertyRelative("_configuration");
                var valueProperty = configurationProperty.FindPropertyRelative("_value");

                return valueProperty == null ? new VisualElement() : CreateView(valueProperty);
            }

            private static VisualElement CreateView(SerializedProperty property)
            {
                if (!property.hasChildren || property.propertyType == SerializedPropertyType.String)
                    return new PropertyField(property, string.Empty);
                
                var container = new VisualElement();

                if (property.isArray)
                {
                    var arraySize = property.arraySize;
                    if (arraySize == 0)
                        return new Label("List is empty.");
                    
                    for (var i = 0; i < arraySize; i++)
                    {
                        var element = property.GetArrayElementAtIndex(i);
                        container.Add(new PropertyField(element, string.Empty));
                    }

                    return container;
                }
                
                var copy = property.Copy();
                var endProperty = copy.GetEndProperty();
                copy.NextVisible(true);
                do
                {
                    if (SerializedProperty.EqualContents(copy, endProperty))
                        break;
                    
                    container.Add(new PropertyField(copy));
                } while (copy.NextVisible(false));

                return container;
            }
        }

        private class DiffRowModel : ScriptableObject
        {
            [SerializeField]
            private BuildConfigurationWrapper _left;

            [SerializeField]
            private BuildConfigurationWrapper _right;

            [SerializeField]
            private string _name;

            public string Name => _name;

            public bool HasDiff { get; private set; }

            public static DiffRowModel Create(IBuildConfiguration left, IBuildConfiguration right)
            {
                var instance = CreateInstance<DiffRowModel>();
                instance._left = new BuildConfigurationWrapper(left);
                instance._right = new BuildConfigurationWrapper(right);
                instance._name = left?.GetDisplayName() ?? right.GetDisplayName();

                if (left == null || right == null)
                {
                    instance.HasDiff = true;
                    return instance;
                }

                var leftPropertyValue = left.GatherProperty().Value;
                var rightPropertyValue = right.GatherProperty().Value;
                instance.HasDiff = !leftPropertyValue.Equals(rightPropertyValue);
                return instance;
            }
        }

        private sealed class DiffRowView : BindableElement
        {
            private readonly Label _nameLabel;
            private readonly PropertyField _leftPropertyField;
            private readonly PropertyField _rightPropertyField;

            public DiffRowView()
            {
                var visualTree = AssetLoader.LoadUxml("SubWindows/DiffRow");
                visualTree.CloneTree(this);

                _nameLabel = this.Q<Label>("name");

                _leftPropertyField = this.Q<PropertyField>("left-property");
                _leftPropertyField.bindingPath = "_left";
                _leftPropertyField.SetEnabled(false);
                _leftPropertyField.RemoveFromClassList("unity-disabled");
                _rightPropertyField = this.Q<PropertyField>("right-property");
                _rightPropertyField.bindingPath = "_right";
                _rightPropertyField.SetEnabled(false);
                _rightPropertyField.RemoveFromClassList("unity-disabled");
            }

            public void Bind(DiffRowModel model)
            {
                _nameLabel.text = model.Name;
                
                if (model.HasDiff)
                    AddToClassList(ClassName.DiffRedStrong);

                this.Bind(new SerializedObject(model));
            }

            public void Unbind(DiffRowModel _)
            {
                _nameLabel.text = string.Empty;
                
                RemoveFromClassList(ClassName.DiffRedStrong);

                this.Unbind();
            }
            
            private static class ClassName
            {
                public const string DiffRed = "diff-red";
                public const string DiffRedStrong = "diff-red-strong";
                public const string DiffGreen = "diff-green";
                public const string DiffGreenStrong = "diff-green-strong";
                public const string DiffEmpty = "diff-empty";
            }

            public new class UxmlFactory : UxmlFactory<DiffRowView, UxmlTraits>
            {
            }
        }
    }
}
