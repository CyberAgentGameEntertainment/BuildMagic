// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories("Save Linker Report",
        PropertyName = "BuildMagicEditor.BuiltIn.SaveLinkerReportTask.outputDirectory")]
    public class SaveLinkerReportTask : BuildTaskBase<IPostBuildContext>
    {
        private readonly string _outputDirectory;

        public SaveLinkerReportTask(string outputDirectory)
        {
            _outputDirectory = outputDirectory;
        }

        public override void Run(IPostBuildContext context)
        {
            string managedStrippedPath = context.ActiveBuildTarget switch
            {
                BuildTarget.Android => "Library/Bee/artifacts/Android/ManagedStripped",
                BuildTarget.iOS => "Library/Bee/artifacts/iOS/ManagedStripped",
                BuildTarget.WebGL => "Library/Bee/artifacts/WebGL/ManagedStripped",
                BuildTarget.StandaloneOSX => "Library/Bee/artifacts/MacStandalonePlayerBuildProgram/ManagedStripped",
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => "Library/Bee/artifacts/WinPlayerBuildProgram/ManagedStripped",
                BuildTarget.StandaloneLinux64 => "Library/Bee/artifacts/LinuxPlayerBuildProgram/ManagedStripped",
                _ => null
            };

            if (string.IsNullOrEmpty(managedStrippedPath))
            {
                Debug.LogWarning("SaveLinkerReportTask: Unsupported platform.");
                return;
            }

            var path = Path.Combine(managedStrippedPath, "UnityLinker_Diagnostics");

            CopyDirectory(path, _outputDirectory);
        }

        private static void CopyDirectory(string source, string dest)
        {
            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"SaveLinkerReportTask: Source directory does not exist: {source}");
                return;
            }

            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }

            foreach (var file in Directory.GetFiles(source))
            {
                var destFile = Path.Combine(dest, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var directory in Directory.GetDirectories(source))
            {
                var destDir = Path.Combine(dest, Path.GetFileName(directory));
                CopyDirectory(directory, destDir);
            }
        }
    }
}
