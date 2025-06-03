// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BuildMagic.Window.Editor.Elements
{
    public interface ISerializableDictionaryTabViewKeyProvider<T>
    {
        IEqualityComparer<T> EqualityComparer { get; }
        IReadOnlyList<T> AvailableValues { get; }
        T GetValue(SerializedProperty target);
        void SetValue(SerializedProperty target, T value);
        string GetDisplayName(T target);
        Texture2D GetIcon(T target);
        object GetUserData(SerializedProperty target);
    }
}
