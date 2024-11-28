// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using BuildMagicEditor;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace BuildMagic.Window.Editor.Elements
{
    internal sealed class ConfigurationEntryView : BindableElement
    {
        private readonly PropertyField _valueField;

        public ConfigurationEntryView()
        {
            var visualTree = AssetLoader.LoadUxml("ConfigurationEntry");
            Assert.IsNotNull(visualTree);
            visualTree.CloneTree(this);
            
            _valueField = this.Q<PropertyField>("value-field");
            Assert.IsNotNull(_valueField);
            _valueField.bindingPath = "_value";
        }
        
        public ConfigurationType Type { get; private set; }
        public int Index { get; private set; }

        public IBuildConfiguration Configuration { get; private set; }
        public bool Collectable => Configuration is IProjectSettingApplier;

        public void Bind(ConfigurationType type, int index, IBuildConfiguration configuration)
        {
            Type = type;
            Index = index;
            Configuration = configuration;
            _valueField.label = configuration?.GetDisplayName() ?? configuration?.PropertyName ?? "Missing";
        }
        
        public void CollectProjectSetting()
        {
            Assert.IsTrue(Configuration is IProjectSettingApplier);
            ((IProjectSettingApplier) Configuration).ApplyProjectSetting();
        }
        
        public new class UxmlFactory : UxmlFactory<ConfigurationEntryView, UxmlTraits>
        {
        }
    }
}
