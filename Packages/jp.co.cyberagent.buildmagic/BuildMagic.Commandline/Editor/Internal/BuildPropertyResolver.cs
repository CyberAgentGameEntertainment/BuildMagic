// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using BuildMagicEditor.Commandline.Error;

namespace BuildMagicEditor.Commandline.Internal
{
    /// <summary>
    ///     Resolves build properties considering the command line arguments.
    /// </summary>
    internal class BuildPropertyResolver
    {
        private BuildPropertyResolver(
            IReadOnlyList<OverrideProperty> overrideProperties, IBuildPropertyDeserializer[] deserializers)
        {
            _overrideProperties = overrideProperties.ToDictionary(x => x.Name);
            _unresolvedOverrideProperties = overrideProperties.ToHashSet();
            _deserializers = deserializers;
        }

        public static BuildPropertyResolver CreateDefault(IReadOnlyList<OverrideProperty> overrideProperties)
        {
            return new BuildPropertyResolver(overrideProperties, CreateDefaultDeserializers());
        }

        /// <summary>
        ///     Resolve the property for the configuration.
        /// </summary>
        /// <param name="configuration">The configuration to resolve.</param>
        /// <returns>Resolved property.</returns>
        public IBuildProperty ResolveProperty(IBuildConfiguration configuration)
        {
            var buildProperty = configuration.GatherProperty();
            if (!_overrideProperties.TryGetValue(configuration.PropertyName, out var overrideProperty))
                // If no override property specified, return the property of IBuildConfiguration.
                return buildProperty;

            _unresolvedOverrideProperties.Remove(overrideProperty);

            try
            {
                foreach (var deserializer in _deserializers)
                    if (deserializer.WillProcess(buildProperty.ValueType))
                    {
                        // If the deserializer is found, deserialize the value.
                        var deserializedValue =
                            deserializer.Deserialize(overrideProperty.Value, buildProperty.ValueType);

                        return BuildProperty.Create(buildProperty.Name, buildProperty.ValueType, deserializedValue);
                    }

                throw new BuildPropertyResolveException($"No deserializer for {buildProperty.ValueType}.");
            }
            catch (Exception e)
            {
                throw new BuildPropertyResolveException("Failed to resolve the property.", e);
            }
        }

        public OverrideProperty[] GetUnresolvedOverrideProperties()
        {
            return _unresolvedOverrideProperties.ToArray();
        }

        private static IBuildPropertyDeserializer[] CreateDefaultDeserializers()
        {
            return new IBuildPropertyDeserializer[]
            {
                new IntBuildPropertyDeserializer(),
                new SingleBuildPropertyDeserializer(),
                new EnumBuildPropertyDeserializer(),
                new StringBuildPropertyDeserializer(),
                new SerializableTypeBuildPropertyDeserializer()
            };
        }

        private readonly Dictionary<string, OverrideProperty> _overrideProperties;
        private readonly IBuildPropertyDeserializer[] _deserializers;
        private readonly HashSet<OverrideProperty> _unresolvedOverrideProperties;
    }
}
