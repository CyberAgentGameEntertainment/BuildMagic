using UnityEditor;

namespace BuildMagicEditor.BuiltIn
{
    /// <summary>
    ///     This task enables Game Mode for iOS apps by adding Game Mode related keys to Info.plist.
    ///
    ///     - Enable GCSupportsGameMode: Used for iOS 18.0 to 18.6
    ///       - https://developer.apple.com/documentation/bundleresources/information-property-list/gcsupportsgamemode
    /// 
    ///     - Enable LSSupportsGameMode: Used for iOS 18.6+
    ///       - https://developer.apple.com/documentation/bundleresources/information-property-list/lssupportsgamemode
    /// </summary>
    [GenerateBuildTaskAccessories("Enable iOS Game Mode",
        PropertyName = "BuildMagicEditor.BuiltIn.iOSEnableGameModeTask")]
    public sealed class iOSEnableGameModeTask : BuildTaskBase<IPostBuildContext>
    {
        private readonly bool _enableLSSupportsGameMode;
        private readonly bool _enableGCSupportsGameMode;

        public iOSEnableGameModeTask(bool enableLSSupportsGameMode, bool enableGCSupportsGameMode)
        {
            _enableLSSupportsGameMode = enableLSSupportsGameMode;
            _enableGCSupportsGameMode = enableGCSupportsGameMode;
        }

        public override void Run(IPostBuildContext context)
        {
            if (context.ActiveBuildTarget != BuildTarget.iOS)
            {
                return;
            }

#if UNITY_IOS
            RunInternal(context);
#endif
        }

#if UNITY_IOS
        private void RunInternal(IPostBuildContext context)
        {
            var projectRootPath = context.BuildReport.summary.outputPath;
            var infoPlistPath = System.IO.Path.Combine(projectRootPath, "Info.plist");
            if (!System.IO.File.Exists(infoPlistPath)) return;

            var plist = new UnityEditor.iOS.Xcode.PlistDocument();
            plist.ReadFromFile(infoPlistPath);

            var root = plist.root;
            
            root.SetBoolean("LSSupportsGameMode", _enableLSSupportsGameMode);
            root.SetBoolean("GCSupportsGameMode", _enableGCSupportsGameMode);

            plist.WriteToFile(infoPlistPath);
        }
#endif
    }
}
