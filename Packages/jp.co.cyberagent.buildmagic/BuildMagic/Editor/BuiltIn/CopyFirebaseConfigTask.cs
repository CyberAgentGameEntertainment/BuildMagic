// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BuildMagicEditor.BuiltIn
{
    [GenerateBuildTaskAccessories("Copy Firebase Config",
        PropertyName = "BuildMagicEditor.BuiltIn.CopyFirebaseConfigTask")]
    public class CopyFirebaseConfigTask : BuildTaskBase<IPreBuildContext>
    {
        private readonly string _destinationDirectory;
        private readonly Object _googleServiceInfoPlist;
        private readonly Object _googleServicesJson;

        public CopyFirebaseConfigTask(string destinationDirectory, Object googleServiceInfoPlist,
            Object googleServicesJson)
        {
            _googleServicesJson = googleServicesJson;
            _googleServiceInfoPlist = googleServiceInfoPlist;
            _destinationDirectory = destinationDirectory;
        }

        public override void Run(IPreBuildContext context)
        {
            var dest = Path.Combine("Assets", _destinationDirectory);
            Directory.CreateDirectory(dest);

            if (_googleServiceInfoPlist)
            {
                var source = AssetDatabase.GetAssetPath(_googleServiceInfoPlist);
                {
                    var bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
                    // It may contain multiple bundle ids
                    if (!ContainsBundleIdGoogleServiceInfoPlist(source, bundleId))
                        throw new InvalidOperationException(
                            $"{source} does not contain the bundle id \"{bundleId}\" which is currently set to this project.");
                }
                File.Copy(AssetDatabase.GetAssetPath(_googleServiceInfoPlist),
                    Path.Combine(dest, "GoogleService-Info.plist"), true);
            }

            if (_googleServicesJson)
            {
                var source = AssetDatabase.GetAssetPath(_googleServicesJson);
                {
                    var bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
                    // It may contain multiple bundle ids
                    if (!ContainsBundleIdGoogleServicesJson(source, bundleId))
                        throw new InvalidOperationException(
                            $"{source} does not contain the bundle id \"{bundleId}\" which is currently set to this project.");
                }
                File.Copy(source, Path.Combine(dest, "google-services.json"), true);
            }

            AssetDatabase.Refresh();
        }

        private bool ContainsBundleIdGoogleServiceInfoPlist(string path, string bundleId)
        {
            var doc = new XmlDocument();
            using var reader = new StreamReader(path);
            doc.Load(reader);
            if (doc.SelectSingleNode("plist/dict") is not { } dictNode) return false;

            for (var i = 0; i < dictNode.ChildNodes.Count; i++)
            {
                var node = dictNode.ChildNodes[i];
                if (node.Name == "key" && node.InnerText == "BUNDLE_ID")
                {
                    var valueNode = dictNode.ChildNodes[i + 1];
                    if (valueNode.Name != "string")
                        continue;

                    if (valueNode.InnerText == bundleId) return true;
                }
            }

            return false;
        }

        private bool ContainsBundleIdGoogleServicesJson(string path, string bundleId)
        {
            var json = JsonUtility.FromJson<GoogleServicesJson>(File.ReadAllText(path));
            foreach (var client in json.client)
            {
                var packageName = client.client_info.android_client_info.package_name;
                if (packageName == bundleId) return true;
            }

            return false;
        }

        [Serializable]
        private class GoogleServicesJson
        {
            public Client[] client;

            [Serializable]
            public class Client
            {
                public ClientInfo client_info;
            }

            [Serializable]
            public class ClientInfo
            {
                public AndroidClientInfo android_client_info;
            }

            [Serializable]
            public class AndroidClientInfo
            {
                public string package_name;
            }
        }
    }
}
