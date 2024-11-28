// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using BuildMagicEditor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BuildMagic.Window.Editor.SubWindows
{
    internal sealed class SelectConfigurationDropdown : AdvancedDropdown
    {
        private static Dictionary<Type, IBuildConfiguration> _configurationCache;
        
        private readonly Context _context;

        public SelectConfigurationDropdown(Context context, AdvancedDropdownState state) : base(state)
        {
            _context = context;
            minimumSize = new Vector2(500, 300);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            _configurationCache ??= new Dictionary<Type, IBuildConfiguration>();
            
            var root = new AdvancedDropdownItem("Configuration Type");

            var preBuildParent = new AdvancedDropdownItem("Pre Build");
            root.AddChild(preBuildParent);
#if BUILDMAGIC_DEVELOPER
            var internalPrepareParent = new AdvancedDropdownItem("Internal Prepare");
            root.AddChild(internalPrepareParent);
#endif
            var postBuildParent = new AdvancedDropdownItem("Post Build");
            root.AddChild(postBuildParent);

            var targetTypes = TypeCache.GetTypesDerivedFrom(typeof(IBuildConfiguration))
                                       .Where(t => t.Assembly.FullName.Contains("Tests") == false)
                                       .Where(t => t.IsInterface == false)
                                       .Where(t => t.IsAbstract == false)
                                       .Where(t => t.IsGenericType == false)
                                       .Where(t => _context.existConfigurations.Contains(t) == false)
                                       .OrderBy(t => t.FullName)
                                       .ToArray();

            var preBuildContextType = typeof(IPreBuildContext);
#if BUILDMAGIC_DEVELOPER
            var internalPrepareContextType = typeof(IInternalPrepareContext);
#endif
            var postBuildContextType = typeof(IPostBuildContext);

            foreach (var type in targetTypes)
            {
                var baseType = type.BaseType;
                var taskType = baseType!.GenericTypeArguments[0];
                var taskBaseType = taskType!.BaseType;
                var contextType = taskBaseType!.GenericTypeArguments[0];

                if (preBuildContextType.IsAssignableFrom(contextType))
                    AddNestedItem(preBuildParent, ConfigurationType.PreBuild, type);
#if BUILDMAGIC_DEVELOPER
                else if (internalPrepareContextType.IsAssignableFrom(contextType))
                    AddNestedItem(internalPrepareParent, ConfigurationType.InternalPrepare, type);
#endif
                else if (postBuildContextType.IsAssignableFrom(contextType))
                    AddNestedItem(postBuildParent, ConfigurationType.PostBuild, type);
            }
            
            var sortChildrenMethod = typeof(AdvancedDropdownItem).GetMethod("SortChildren", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Comparison<AdvancedDropdownItem> comparison = Compare;
            var parameters = new object[] {comparison, true};

            foreach (var parent in new[]
                     {
                         preBuildParent,
#if BUILDMAGIC_DEVELOPER
                         internalPrepareParent,
#endif
                         postBuildParent
                     })
            {
                sortChildrenMethod!.Invoke(parent, parameters);
                if (parent.children.Any())
                    continue;
                parent.AddChild(new AdvancedDropdownItem("Configurations are not found.") {enabled = false});
            }
            
            return root;
            
            void AddNestedItem(AdvancedDropdownItem parent, ConfigurationType configurationType, Type type)
            {
                if (_configurationCache.TryGetValue(type, out var instance) == false)
                {
                    instance = Activator.CreateInstance(type) as IBuildConfiguration;
                    _configurationCache[type] = instance;
                }
                var propertyName = instance!.PropertyName;
                
                var split = propertyName.Split('.', StringSplitOptions.RemoveEmptyEntries);
                
                var currentParent = parent;
                for (var i = 0; i < split.Length - 1; i++)
                {
                    var name = split[i];
                    var child = currentParent.children.FirstOrDefault(c => c.name == name);
                    if (child == null)
                    {
                        child = new AdvancedDropdownItem(name);
                        currentParent.AddChild(child);
                    }
                    currentParent = child;
                }
                
                currentParent.AddChild(new ConfigurationItem(configurationType, type));
            }
            
            int Compare(AdvancedDropdownItem a, AdvancedDropdownItem b)
            {
                if (a.children.Any() && !b.children.Any())
                    return -1;
                if (!a.children.Any() && b.children.Any())
                    return 1;
                
                return string.CompareOrdinal(a.name, b.name);
            }
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (item is ConfigurationItem configurationItem)
                _context.callback?.Invoke(configurationItem.ConfigurationType, configurationItem.Type);
        }

        public readonly struct Context
        {
            public readonly ISet<Type> existConfigurations;
            public readonly Action<ConfigurationType, Type> callback;
            
            public Context(ISet<Type> existConfigurations, Action<ConfigurationType, Type> callback)
            {
                this.existConfigurations = existConfigurations;
                this.callback = callback;
            }
        }

        private sealed class ConfigurationItem : AdvancedDropdownItem
        {
            public ConfigurationItem(ConfigurationType configurationType, Type type) : base(
                BuildConfigurationTypeUtility.GetDisplayName(type) ?? type.Name)
            {
                ConfigurationType = configurationType;
                Type = type;
            }

            public ConfigurationType ConfigurationType { get; }
            
            public Type Type { get; }
        }
    }
}
