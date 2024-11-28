// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEditor.PackageManager;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories("Delete Package Dependencies",
        PropertyName = "BuildMagicEditor.BuiltIn.DeletePackageDependenciesTask.packageNames")]
    public sealed class DeletePackageDependenciesTask : BuildTaskBase<IPreBuildContext>
    {
        private readonly string[] _packageNames;

        public DeletePackageDependenciesTask(string[] packageNames)
        {
            _packageNames = packageNames;
        }

        public override void Run(IPreBuildContext context)
        {
            foreach (var packageName in _packageNames.AsSpan()) Client.Remove(packageName);
        }
    }
}
