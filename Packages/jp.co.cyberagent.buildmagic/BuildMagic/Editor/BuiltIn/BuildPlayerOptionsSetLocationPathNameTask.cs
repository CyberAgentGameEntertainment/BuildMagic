// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

namespace BuildMagicEditor.BuiltIn
{
    /// <summary>
    ///     Prepare build player task to set location path name of build player options.
    /// </summary>
    [GenerateBuildTaskAccessories("BuildPlayerOptions: Set Location Path Name",
        PropertyName = "BuildPlayerOptions.assetBundleManifestPath")]
    internal class BuildPlayerOptionsSetLocationPathNameTask : BuildTaskBase<IInternalPrepareContext>
    {
        public BuildPlayerOptionsSetLocationPathNameTask(string locationPathName)
        {
            _locationPathName = locationPathName;
        }

        public override void Run(IInternalPrepareContext playerContext)
        {
            playerContext.OptionsBuilder.SetLocationPathName(_locationPathName);
        }

        private readonly string _locationPathName;
    }
}
