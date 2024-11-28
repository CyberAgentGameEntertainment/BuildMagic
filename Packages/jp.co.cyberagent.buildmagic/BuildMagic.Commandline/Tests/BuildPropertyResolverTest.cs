// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagicEditor.Commandline.Internal;
using NUnit.Framework;

namespace BuildMagicEditor.Commandline.Tests
{
    public class BuildPropertyResolverTest
    {
        [Test]
        public void Resolve_WithOverride_IntProperty()
        {
            var overrideProperty = new OverrideProperty("int", "345");
            var resolver = BuildPropertyResolver.CreateDefault(new[] { overrideProperty });

            var property = resolver.ResolveProperty(new SampleIntBuildConfiguration());
            Assert.AreEqual(int.Parse(overrideProperty.Value), (int)property.Value);
        }

        [Test]
        public void Resolve_WithoutOverride()
        {
            var resolver = BuildPropertyResolver.CreateDefault(Array.Empty<OverrideProperty>());

            var configuration = new SampleIntBuildConfiguration();
            var property = resolver.ResolveProperty(configuration);
            Assert.AreEqual((int)configuration.GatherProperty().Value, (int)property.Value);
        }

        [Test]
        public void Resolve_WithWrongPropertyName()
        {
            var overrideProperty = new OverrideProperty("wrongName", "345");
            var resolver = BuildPropertyResolver.CreateDefault(new[] { overrideProperty });

            var configuration = new SampleIntBuildConfiguration();
            var property = resolver.ResolveProperty(configuration);
            Assert.AreEqual((int)configuration.GatherProperty().Value, (int)property.Value);
        }

        private class SampleIntBuildConfiguration : IBuildConfiguration
        {
            public string PropertyName => GatherProperty().Name;

            public IBuildProperty GatherProperty()
            {
                return BuildProperty<int>.Create("int", 123);
            }

            public Type TaskType => null;
        }
    }
}
