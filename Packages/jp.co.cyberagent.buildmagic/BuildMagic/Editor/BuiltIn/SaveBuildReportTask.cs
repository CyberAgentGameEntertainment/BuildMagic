// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories("Save Build Report",
        PropertyName = "BuildMagicEditor.BuiltIn.SaveBuildReportTask.outputPath")]
    public class SaveBuildReportTask : BuildTaskBase<IPostBuildContext>
    {
        private const string LastBuildReportPath = "Library/LastBuild.buildreport";

        private readonly string _outputPath;
        private readonly bool _useYaml;

        public SaveBuildReportTask(bool useYaml, string outputPath)
        {
            _useYaml = useYaml;
            _outputPath = outputPath;
        }

        public override void Run(IPostBuildContext context)
        {
            // context.BuildReport cannot be saved with InternalEditorUtility, so copy Library/LastBuild.buildreport

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_outputPath)));

            // LastBuild.buildreport is binary, so load it first if we want to convert it to YAML
            if (_useYaml)
            {
                var objects = InternalEditorUtility.LoadSerializedFileAndForget(LastBuildReportPath);
                InternalEditorUtility.SaveToSerializedFileAndForget(objects, _outputPath, true);
            }
            else
            {
                File.Copy(LastBuildReportPath, _outputPath, true);
            }

            // AssetDatabase.Refresh if output under Assets/

            var relativePath = Path.GetRelativePath(".", _outputPath);

            if (relativePath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase) &&
                relativePath.Length > "Assets".Length &&
                relativePath["Assets".Length] is '/' or '\\')
            {
                AssetDatabase.Refresh();
            }
        }
    }
}
