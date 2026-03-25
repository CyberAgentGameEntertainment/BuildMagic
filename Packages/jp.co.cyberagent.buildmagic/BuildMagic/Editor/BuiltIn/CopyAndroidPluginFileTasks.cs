// --------------------------------------------------------------
// Copyright 2026 CyberAgent, Inc.
// --------------------------------------------------------------

using System.IO;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BuildMagicEditor.BuiltIn
{
    /// <summary>
    ///     Base class for tasks that copy a file to Assets/Plugins/Android/.
    /// </summary>
    public abstract class CopyAndroidPluginFileTaskBase : BuildTaskBase<IPreBuildContext>
    {
        private readonly Object _sourceFile;

        protected CopyAndroidPluginFileTaskBase(Object sourceFile)
        {
            _sourceFile = sourceFile;
        }

        protected abstract string DestinationFileName { get; }

        public override void Run(IPreBuildContext context)
        {
            const string destDir = "Assets/Plugins/Android";
            var destPath = Path.Combine(destDir, DestinationFileName);

            if (!_sourceFile)
            {
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                    AssetDatabase.Refresh();
                }

                return;
            }

            Directory.CreateDirectory(destDir);

            var sourcePath = AssetDatabase.GetAssetPath(_sourceFile);
            File.Copy(sourcePath, destPath, true);
            AssetDatabase.Refresh();
        }
    }

    [GenerateBuildTaskAccessories("Overwrite Custom AndroidManifest.xml",
        PropertyName = "BuildMagicEditor.BuiltIn.CopyAndroidFileTasks.AndroidManifestTask.sourceFile")]
    public sealed class CopyAndroidManifestTask : CopyAndroidPluginFileTaskBase
    {
        public CopyAndroidManifestTask(Object sourceFile) : base(sourceFile)
        {
        }

        protected override string DestinationFileName => "AndroidManifest.xml";
    }

    [GenerateBuildTaskAccessories("Overwrite Custom LauncherManifest.xml",
        PropertyName = "BuildMagicEditor.BuiltIn.CopyAndroidFileTasks.AndroidLauncherManifestTask.sourceFile")]
    public sealed class CopyAndroidLauncherManifestTask : CopyAndroidPluginFileTaskBase
    {
        public CopyAndroidLauncherManifestTask(Object sourceFile) : base(sourceFile)
        {
        }

        protected override string DestinationFileName => "LauncherManifest.xml";
    }

    [GenerateBuildTaskAccessories("Overwrite Custom mainTemplate.gradle",
        PropertyName = "BuildMagicEditor.BuiltIn.CopyAndroidFileTasks.AndroidMainTemplateGradleTask.sourceFile")]
    public sealed class CopyAndroidMainTemplateGradleTask : CopyAndroidPluginFileTaskBase
    {
        public CopyAndroidMainTemplateGradleTask(Object sourceFile) : base(sourceFile)
        {
        }

        protected override string DestinationFileName => "mainTemplate.gradle";
    }

    [GenerateBuildTaskAccessories("Overwrite Custom launcherTemplate.gradle",
        PropertyName = "BuildMagicEditor.BuiltIn.CopyAndroidFileTasks.AndroidLauncherTemplateGradleTask.sourceFile")]
    public sealed class CopyAndroidLauncherTemplateGradleTask : CopyAndroidPluginFileTaskBase
    {
        public CopyAndroidLauncherTemplateGradleTask(Object sourceFile) : base(sourceFile)
        {
        }

        protected override string DestinationFileName => "launcherTemplate.gradle";
    }

    [GenerateBuildTaskAccessories("Overwrite Custom baseProjectTemplate.gradle",
        PropertyName = "BuildMagicEditor.BuiltIn.CopyAndroidFileTasks.AndroidBaseProjectTemplateGradleTask.sourceFile")]
    public sealed class CopyAndroidBaseProjectTemplateGradleTask : CopyAndroidPluginFileTaskBase
    {
        public CopyAndroidBaseProjectTemplateGradleTask(Object sourceFile) : base(sourceFile)
        {
        }

        protected override string DestinationFileName => "baseProjectTemplate.gradle";
    }

    [GenerateBuildTaskAccessories("Overwrite Custom gradleTemplate.properties",
        PropertyName = "BuildMagicEditor.BuiltIn.CopyAndroidFileTasks.AndroidGradlePropertiesTask.sourceFile")]
    public sealed class CopyAndroidGradlePropertiesTask : CopyAndroidPluginFileTaskBase
    {
        public CopyAndroidGradlePropertiesTask(Object sourceFile) : base(sourceFile)
        {
        }

        protected override string DestinationFileName => "gradleTemplate.properties";
    }

    [GenerateBuildTaskAccessories("Overwrite Custom settingsTemplate.gradle",
        PropertyName = "BuildMagicEditor.BuiltIn.CopyAndroidFileTasks.AndroidSettingsTemplateGradleTask.sourceFile")]
    public sealed class CopyAndroidSettingsTemplateGradleTask : CopyAndroidPluginFileTaskBase
    {
        public CopyAndroidSettingsTemplateGradleTask(Object sourceFile) : base(sourceFile)
        {
        }

        protected override string DestinationFileName => "settingsTemplate.gradle";
    }

    [GenerateBuildTaskAccessories("Overwrite Custom proguard-user.txt",
        PropertyName = "BuildMagicEditor.BuiltIn.CopyAndroidFileTasks.AndroidProguardTask.sourceFile")]
    public sealed class CopyAndroidProguardTask : CopyAndroidPluginFileTaskBase
    {
        public CopyAndroidProguardTask(Object sourceFile) : base(sourceFile)
        {
        }

        protected override string DestinationFileName => "proguard-user.txt";
    }
}
