// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using BuildMagicEditor.Commandline.Error;
using BuildMagicEditor.Commandline.Internal;
using NUnit.Framework;

namespace BuildMagicEditor.Commandline.Tests
{
    public class CommandLineParserTest
    {
        [Test]
        public void CommandlineParserTest_TryParse()
        {
            var args = $"{PreBuildCommandline} -batchmode -quit -override KEY1=VALUE1 -override KEY2=VALUE2"
                .Split(' ');

            var parser = new CommandLineParser(args);
            Assert.IsTrue(parser.TryParse("override", out var options));
            Assert.AreEqual(2, options.Length);
            Assert.AreEqual("KEY1=VALUE1", options[0]);
            Assert.AreEqual("KEY2=VALUE2", options[1]);

            // The key with option prefix are supported.
            Assert.IsTrue(parser.TryParse("-override", out options));
            Assert.AreEqual(2, options.Length);
            Assert.AreEqual("KEY1=VALUE1", options[0]);
            Assert.AreEqual("KEY2=VALUE2", options[1]);

            // A non-existent key is specified.
            Assert.IsFalse(parser.TryParse("none", out _));
        }

        [Test]
        public void CommandlineParserTest_TryParse_EdgeCase_OptionsWerePassedConsecutively()
        {
            // "-override -override" is not valid.
            var args =
                $"{PreBuildCommandline} -batchmode -quit -override -override KEY1=VALUE1 -override KEY2=VALUE2"
                    .Split(' ');

            var parser = new CommandLineParser(args);
            Assert.IsTrue(parser.TryParse("override", out var options));
            Assert.AreEqual(2, options.Length);
            Assert.AreEqual("KEY1=VALUE1", options[0]);
            Assert.AreEqual("KEY2=VALUE2", options[1]);
        }

        [Test]
        public void CommandlineParserTest_Parse()
        {
            var args = $"{PreBuildCommandline} -batchmode -quit -override KEY1=VALUE1 -override KEY2=VALUE2"
                .Split(' ');

            var parser = new CommandLineParser(args);
            var options = parser.Parse("override");
            Assert.AreEqual(2, options.Length);
            Assert.AreEqual("KEY1=VALUE1", options[0]);
            Assert.AreEqual("KEY2=VALUE2", options[1]);

            // The key with option prefix are supported.
            options = parser.Parse("-override");
            Assert.AreEqual(2, options.Length);
            Assert.AreEqual("KEY1=VALUE1", options[0]);
            Assert.AreEqual("KEY2=VALUE2", options[1]);

            void CheckThrowingException()
            {
                parser.Parse("none");
            }

            // If A non-existent key is specified, CommandLineArgumentException is thrown.
            Assert.That(CheckThrowingException, Throws.TypeOf<CommandLineArgumentException>());
        }

        private static readonly string PreBuildCommandline =
            "/Applications/Unity/Hub/Editor/2021.3.31f1/Unity.app/Contents/MacOS/Unity -projectPath /Path/To/Your/Project -executeMethod BuildMagicCLI.PreBuild";
    }
}
