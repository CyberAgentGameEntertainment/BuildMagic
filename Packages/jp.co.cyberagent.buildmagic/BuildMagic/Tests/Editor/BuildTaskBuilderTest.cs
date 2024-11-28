// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using BuildMagicEditor;
using NUnit.Framework;
using UnityEditor;

public class BuildTaskBuilderTest
{
    [Test]
    public void IBuildTaskBuilder_Check_Usage()
    {
        var builderProvider = BuildTaskBuilderProvider.CreateDefault();
        builderProvider.Register(new ApplicationIdentifierBuildTaskBuilder());

        var builder = builderProvider.GetBuilder(
            typeof(ApplicationIdentifierBuildTask), typeof(ApplicationIdentifierBuildTaskParameter));

        const string kIdentifier = "jp.co.cyberagent.buildmagic.sample";

        var parameter = new ApplicationIdentifierBuildTaskParameter(BuildTargetGroup.Android, kIdentifier);
        var task = builder.Build(parameter) as ApplicationIdentifierBuildTask;

        Assert.AreEqual(BuildTargetGroup.Android, task.BuildTargetGroup);
        Assert.AreEqual(kIdentifier, task.Identifier);

        using (new ApplicationIdentifierScope(BuildTargetGroup.Android))
        {
            task.Run(null);
            Assert.AreEqual(kIdentifier, PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
        }
    }

    private class ApplicationIdentifierBuildTask : IBuildTask
    {
        internal BuildTargetGroup BuildTargetGroup { get; }
        internal string Identifier { get; }

        internal ApplicationIdentifierBuildTask(BuildTargetGroup buildTargetGroup, string identifier)
        {
            BuildTargetGroup = buildTargetGroup;
            Identifier = identifier;
        }

        public void Run(IBuildContext context)
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup, Identifier);
        }
    }

    private class ApplicationIdentifierBuildTaskParameter
    {
        internal BuildTargetGroup BuildTargetGroup { get; }
        internal string Identifier { get; }

        internal ApplicationIdentifierBuildTaskParameter(BuildTargetGroup buildTargetGroup, string identifier)
        {
            BuildTargetGroup = buildTargetGroup;
            Identifier = identifier;
        }
    }

    private class ApplicationIdentifierBuildTaskBuilder
        : BuildTaskBuilderBase<ApplicationIdentifierBuildTask, ApplicationIdentifierBuildTaskParameter>
    {
        public override ApplicationIdentifierBuildTask Build(ApplicationIdentifierBuildTaskParameter value)
        {
            return new ApplicationIdentifierBuildTask(value.BuildTargetGroup, value.Identifier);
        }
    }

    private readonly struct ApplicationIdentifierScope : IDisposable
    {
        internal ApplicationIdentifierScope(BuildTargetGroup buildTargetGroup)
        {
            _buildTargetGroup = buildTargetGroup;
            _identifier = PlayerSettings.GetApplicationIdentifier(buildTargetGroup);
        }

        public void Dispose()
        {
            PlayerSettings.SetApplicationIdentifier(_buildTargetGroup, _identifier);
        }

        private readonly BuildTargetGroup _buildTargetGroup;
        private readonly string _identifier;
    }
}
