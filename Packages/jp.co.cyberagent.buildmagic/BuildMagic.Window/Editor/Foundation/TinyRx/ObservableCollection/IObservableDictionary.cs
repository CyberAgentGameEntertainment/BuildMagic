﻿// --------------------------------------------------------------
// Copyright 2023 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace BuildMagic.Window.Editor.Foundation.TinyRx.ObservableCollection
{
    public interface IObservableDictionary<TKey, TValue> : IObservableDictionary<TKey, TValue, Empty>
    {
    }

    /// <summary>
    ///     The interface of the dictionary that can be observed changes of the items.
    /// </summary>
    /// <typeparam name="TKey">Type of keys.</typeparam>
    /// <typeparam name="TValue">Type of items.</typeparam>
    /// <typeparam name="TEmpty"></typeparam>
    public interface IObservableDictionary<TKey, TValue, TEmpty> : IDictionary<TKey, TValue>
    {
        /// <summary>
        ///     The observable that is called when a item was added.
        /// </summary>
        IObservable<DictionaryAddEvent<TKey, TValue>> ObservableAdd { get; }

        /// <summary>
        ///     The observable that is called when a item was removed.
        /// </summary>
        IObservable<DictionaryRemoveEvent<TKey, TValue>> ObservableRemove { get; }

        /// <summary>
        ///     The observable that is called when items was cleared.
        /// </summary>
        IObservable<TEmpty> ObservableClear { get; }

        /// <summary>
        ///     The observable that is called when a item was replaced.
        /// </summary>
        IObservable<DictionaryReplaceEvent<TKey, TValue>> ObservableReplace { get; }
    }
}
