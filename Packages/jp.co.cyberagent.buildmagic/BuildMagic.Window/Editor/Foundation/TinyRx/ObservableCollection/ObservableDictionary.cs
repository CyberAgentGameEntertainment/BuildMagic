﻿// --------------------------------------------------------------
// Copyright 2023 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEngine;

namespace BuildMagic.Window.Editor.Foundation.TinyRx.ObservableCollection
{
    [Serializable]
    public class ObservableDictionary<TKey, TValue> : ObservableDictionaryBase<TKey, TValue>
    {
        [SerializeField]
        private TValue[] _values;

        protected override TValue[] InternalValues
        {
            get => _values;
            set => _values = value;
        }
    }
}
