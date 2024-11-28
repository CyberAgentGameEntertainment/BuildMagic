// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEditor;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories("Delete Assets",
        PropertyName = "BuildMagicEditor.BuiltIn.DeleteAssetTasks.paths")]
    public sealed class DeleteAssetsTask : BuildTaskBase<IPreBuildContext>
    {
        private readonly string[] _paths;

        public DeleteAssetsTask(string[] paths)
        {
            _paths = paths;
        }

        public override void Run(IPreBuildContext context)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in _paths.AsSpan()) AssetDatabase.DeleteAsset(path);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}
