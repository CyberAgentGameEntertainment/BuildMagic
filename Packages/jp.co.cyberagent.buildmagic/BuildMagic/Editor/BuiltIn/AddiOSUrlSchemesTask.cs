using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.iOS.Xcode;

namespace BuildMagicEditor.BuiltIn
{
    /// <summary>
    ///     This task adds custom URL schemes to the iOS Info.plist file during the build process.
    ///     URL schemes allow the app to be opened from other applications or web browsers using custom URLs (e.g.,
    ///     "myapp://path").
    ///     This is commonly used for deep linking, OAuth callbacks, inter-app communication, and handling custom protocol
    ///     redirects.
    ///     The task configures CFBundleURLTypes in Info.plist with the specified URL name, type role, and URL schemes.
    /// </summary>
    [GenerateBuildTaskAccessories("Add iOS URL Schemes",
        PropertyName = "BuildMagicEditor.BuiltIn.AddiOSUrlSchemesTask.urlSchemes")]
    public sealed class AddiOSUrlSchemesTask : BuildTaskBase<IPostBuildContext>
    {
        /// <summary>
        ///     CFBundleTypeRole
        ///     <see cref="https://developer.apple.com/documentation/bundleresources/information-property-list/cfbundleurltypes/cfbundletyperole" />
        /// </summary>
        public enum TypeRole
        {
            None,
            Editor,
            Viewer,
            Shell,
            QLGenerator
        }

        private readonly TypeRole _typeRole;
        private readonly string _urlIconFile;
        private readonly string _urlName;
        private readonly string[] _urlSchemes;

        public AddiOSUrlSchemesTask(TypeRole typeRole, string urlIconFile, string urlName, string[] urlSchemes)
        {
            _typeRole = typeRole;
            _urlIconFile = urlIconFile;
            _urlName = urlName;
            _urlSchemes = urlSchemes;
        }

        public override void Run(IPostBuildContext context)
        {
            if (context.ActiveBuildTarget != BuildTarget.iOS) return;

#if UNITY_IOS
            RunInternal(context);
#endif
        }

#if UNITY_IOS
        private void RunInternal(IPostBuildContext context)
        {
            var projectRootPath = context.BuildReport.summary.outputPath;
            var infoPlistPath = Path.Combine(projectRootPath, "Info.plist");
            if (!File.Exists(infoPlistPath)) return;

            var plist = new PlistDocument();
            plist.ReadFromFile(infoPlistPath);

            var root = plist.root;
            var bundleUrlTypes = root.CreateArray("CFBundleURLTypes");
            var dict = bundleUrlTypes.AddDict();

            dict.SetString("CFBundleTypeRole", _typeRole.ToString());
            if (!string.IsNullOrEmpty(_urlIconFile))
                dict.SetString("CFBundleURLIconFile", Path.GetFileName(_urlIconFile));

            dict.SetString("CFBundleURLName", _urlName);
            var urlSchemesArray = ForceCreateElementArray(dict, "CFBundleURLSchemes");
            foreach (var urlScheme in _urlSchemes.Distinct())
            {
                if (string.IsNullOrEmpty(urlScheme)) continue;

                urlSchemesArray.AddString(urlScheme);
            }

            plist.WriteToFile(infoPlistPath);
        }

        /// <summary>
        ///     Creates a new PlistElementArray with the specified key in the given PlistElementDict.
        ///     If the key already exists, it will be removed before creating the new array.
        /// </summary>
        private PlistElementArray ForceCreateElementArray(PlistElementDict element, string key)
        {
            if (element.values.ContainsKey(key))
                if (!element.values.Remove(key))
                    throw new InvalidOperationException("Failed to remove existing key: " + key);

            return element.CreateArray(key);
        }
#endif
    }
}
