// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using BuildMagicEditor.Commandline.Internal;
using NUnit.Framework;

namespace BuildMagicEditor.Commandline.Tests
{
    public class BuildMagicCLITest
    {
        [Test]
        public void Internal_ParseOverriderProperties()
        {
            var args = "-batchmode -quit -override KEY1=VALUE1 -override KEY2=VALUE2".Split(' ');
            var commandlineParser = new CommandLineParser(args);

            var overrideProperties = BuildMagicCLI.ParseOverrideProperties(commandlineParser);
            Assert.AreEqual(2, overrideProperties.Count);
            Assert.AreEqual("KEY1", overrideProperties[0].Name);
            Assert.AreEqual("VALUE1", overrideProperties[0].Value);

            Assert.AreEqual("KEY2", overrideProperties[1].Name);
            Assert.AreEqual("VALUE2", overrideProperties[1].Value);
        }

        [Test]
        public void Internal_ParseOverriderProperties_NoOverrideSpecified()
        {
            var args = "-batchmode -quit".Split(' ');
            var commandlineParser = new CommandLineParser(args);

            var overrideProperties = BuildMagicCLI.ParseOverrideProperties(commandlineParser);
            Assert.AreEqual(0, overrideProperties.Count);
        }

        [Test]
        public void Internal_ParseOverriderProperties_Aliases()
        {
            var args = "-batchmode -quit -override KEY1=VALUE1 -key2 VALUE2".Split(' ');
            var commandlineParser = new CommandLineParser(args);

            var aliases = new Dictionary<string, string>
            {
                { "key2", "KEY2" }
            };

            var overrideProperties = BuildMagicCLI.ParseOverrideProperties(commandlineParser, aliases);

            Assert.AreEqual(2, overrideProperties.Count);

            Assert.AreEqual("KEY1", overrideProperties[0].Name);
            Assert.AreEqual("VALUE1", overrideProperties[0].Value);

            Assert.AreEqual("KEY2", overrideProperties[1].Name);
            Assert.AreEqual("VALUE2", overrideProperties[1].Value);
        }

        [Test]
        public void Internal_ParseOverriderProperties_Aliases_WithoutOverride()
        {
            var args = "-batchmode -quit -override -key2 VALUE2".Split(' ');
            var commandlineParser = new CommandLineParser(args);

            var aliases = new Dictionary<string, string>
            {
                { "key2", "KEY2" }
            };

            var overrideProperties = BuildMagicCLI.ParseOverrideProperties(commandlineParser, aliases);

            Assert.AreEqual(1, overrideProperties.Count);

            Assert.AreEqual("KEY2", overrideProperties[0].Name);
            Assert.AreEqual("VALUE2", overrideProperties[0].Value);
        }
    }
}
