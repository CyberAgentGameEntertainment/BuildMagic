// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEditor;

namespace BuildMagicEditor
{
    public static class BuildSchemeSerializer
    {
        public static string Serialize(IBuildScheme schemes)
        {
            return EditorJsonUtility.ToJson(schemes, true);
        }

        public static T Deserialize<T>(string json) where T : IBuildScheme
        {
            var obj = Activator.CreateInstance(typeof(T));
            EditorJsonUtility.FromJsonOverwrite(json, obj);
            return (T)obj;
        }
    }
}
