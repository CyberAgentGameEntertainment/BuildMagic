// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BuildMagic.Window.Editor.Elements
{
    internal class SerializableDictionaryTabViewKeyProvider<T> : ISerializableDictionaryTabViewKeyProvider<T>
    {
        public SerializableDictionaryTabViewKeyProvider(
            IEqualityComparer<T> equalityComparer,
            IReadOnlyList<T> availableValues,
            Func<SerializedProperty, T> getValueDelegate,
            Action<SerializedProperty, T> setValueDelegate,
            Func<T, string> getDisplayNameDelegate,
            Func<T, Texture2D> getIconDelegate,
            Func<SerializedProperty, object> getUserDataDelegate)
        {
            EqualityComparer = equalityComparer;
            AvailableValues = availableValues;
            GetValueDelegate = getValueDelegate;
            SetValueDelegate = setValueDelegate;
            GetDisplayNameDelegate = getDisplayNameDelegate;
            GetIconDelegate = getIconDelegate;
            GetUserDataDelegate = getUserDataDelegate;
        }

        private Func<SerializedProperty, T> GetValueDelegate { get; }
        private Action<SerializedProperty, T> SetValueDelegate { get; }
        private Func<T, string> GetDisplayNameDelegate { get; }
        private Func<T, Texture2D> GetIconDelegate { get; }
        public Func<SerializedProperty, object> GetUserDataDelegate { get; }

        #region ISerializableDictionaryTabViewKeyProvider<T> Members

        public IEqualityComparer<T> EqualityComparer { get; }
        public IReadOnlyList<T> AvailableValues { get; }


        public T GetValue(SerializedProperty target)
        {
            return GetValueDelegate(target);
        }

        public void SetValue(SerializedProperty target, T value)
        {
            SetValueDelegate(target, value);
        }

        public string GetDisplayName(T target)
        {
            return GetDisplayNameDelegate(target);
        }

        public Texture2D GetIcon(T target)
        {
            return GetIconDelegate(target);
        }

        public object GetUserData(SerializedProperty target)
        {
            return GetUserDataDelegate(target);
        }

        #endregion
    }
}
