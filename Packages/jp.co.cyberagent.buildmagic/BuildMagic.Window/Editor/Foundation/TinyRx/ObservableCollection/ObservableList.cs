// --------------------------------------------------------------
// Copyright 2023 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BuildMagic.Window.Editor.Foundation.TinyRx.ObservableCollection
{
    [Serializable]
    public class ObservableList<T> : ObservableListBase<T>
    {
        [SerializeField]
        private List<T> _internalList = new();

        protected override List<T> InternalList => _internalList;
    }
}
