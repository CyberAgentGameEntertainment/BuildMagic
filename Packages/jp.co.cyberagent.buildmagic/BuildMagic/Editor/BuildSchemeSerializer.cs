// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEngine;

namespace BuildMagicEditor
{
    public static class BuildSchemeSerializer
    {
        public static string Serialize(IBuildScheme schemes)
        {
            return JsonUtility.ToJson(schemes, true);
        }

        public static T Deserialize<T>(string json) where T : IBuildScheme
            => JsonUtility.FromJson<T>(json);
    }
}
