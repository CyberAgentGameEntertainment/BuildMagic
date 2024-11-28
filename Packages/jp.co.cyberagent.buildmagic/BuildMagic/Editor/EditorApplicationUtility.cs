// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Reflection;
using UnityEditor;
using UnityEngine.Events;

namespace BuildMagicEditor
{
    [InitializeOnLoad]
    internal static class EditorApplicationUtility
    {
        private static readonly FieldInfo _fieldInfo =
            typeof(EditorApplication).GetField("projectWasLoaded",
                                               BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

        static EditorApplicationUtility()
        {
        }

        public static UnityAction projectWasLoaded
        {
            get => _fieldInfo.GetValue(null) as UnityAction;
            set
            {
                var functions = _fieldInfo.GetValue(null) as UnityAction;
                functions += value;
                _fieldInfo.SetValue(null, functions);
            }
        }
    }
}
