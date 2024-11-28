// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace BuildMagicEditor
{
    [Serializable]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SerializableDictionary<TKey, TValue> : IEnumerable<SerializableDictionary<TKey, TValue>.KeyValuePair>
    {
        #region Serialized Fields

        [SerializeField] private KeyValuePair[] pairs = Array.Empty<KeyValuePair>();

        #endregion

        public ref TValue this[TKey key]
        {
            get
            {
                var index = Array.FindIndex(pairs, p => EqualityComparer<TKey>.Default.Equals(p.Key, key));
                if (index == -1)
                {
                    index = pairs.Length;
                    Array.Resize(ref pairs, pairs.Length + 1);
                    pairs[index].key = key;
                }

                return ref pairs[index].value;
            }
        }

        #region IEnumerable<SerializableDictionary<TKey,TValue>.KeyValuePair> Members

        public IEnumerator<KeyValuePair> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair>)pairs).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public Dictionary<TKey, TValue> ToDictionary()
        {
            var dic = new Dictionary<TKey, TValue>();
            foreach (var pair in pairs) dic[pair.Key] = pair.Value;

            return dic;
        }

        #region Nested type: KeyValuePair

        [Serializable]
        public struct KeyValuePair
        {
            #region Serialized Fields

            [SerializeField] internal TKey key;
            [SerializeField] internal TValue value;

            #endregion

            public TKey Key => key;
            public TValue Value => value;

            public void Deconstruct(out TKey key, out TValue value)
            {
                key = this.key;
                value = this.value;
            }
        }

        #endregion
    }
}
