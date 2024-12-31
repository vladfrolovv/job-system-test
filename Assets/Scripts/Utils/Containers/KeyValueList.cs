#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

#endregion

namespace Utils.Containers
{
    public abstract class DrawableKeyValueList
    {
    }

    [Serializable]
    public class KeyValueList<TKey, TValue> :
        DrawableKeyValueList,
        IList<KeyValuePair<TKey, TValue>>,
        IDictionary<TKey, TValue>,
        IReadOnlyDictionary<TKey, TValue>,
        ISerializationCallbackReceiver
    {
        [SerializeField] protected TKey[] serializedKeys;
        [SerializeField] private TValue[] serializedValues;
        private readonly IEqualityComparer<TKey> _equalityComparer;
        protected readonly List<KeyValuePair<TKey, TValue>> Pairs = new();
        
        public KeyValueList()
        {
        }

        public KeyValueList([NotNull] IEqualityComparer<TKey> equalityComparer)
        {
            _equalityComparer = equalityComparer;
        }

        public KeyValueList([NotNull] KeyValueList<TKey, TValue> otherList)
        {
            foreach (KeyValuePair<TKey, TValue> pair in otherList)
            {
                Add(pair);
            }
        }

        public ICollection<TKey> Keys => Pairs.Select(pair => pair.Key).ToArray();
        public ICollection<TValue> Values => Pairs.Select(pair => pair.Value).ToArray();

        public void Add(TKey key, TValue value)
        {
            Pairs.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public bool ContainsKey(TKey key)
        {
            return Pairs.Any(pair => Equals(pair.Key, key));
        }

        public bool Remove(TKey key)
        {
            return Remove(Find(pair => Equals(pair.Key, key)));
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (ContainsKey(key))
            {
                value = FindFirst(key);
                return true;
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        ///     Finds first value by the specified key
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                foreach (KeyValuePair<TKey, TValue> pair in Pairs)
                {
                    if (Equals(pair.Key, key))
                    {
                        return pair.Value;
                    }
                }

                throw new ArgumentOutOfRangeException($"The key {key} is not found");
            }

            set
            {
                for (int i = 0; i < Pairs.Count; ++i)
                {
                    if (Equals(Pairs[i].Key, key))
                    {
                        Pairs[i] = new KeyValuePair<TKey, TValue>(key, value);
                        return;
                    }
                }

                throw new ArgumentOutOfRangeException($"The key {key} is not found");
            }
        }

        public int Count => Pairs.Count;

        public bool IsReadOnly => false;


        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Pairs.GetEnumerator();
        }


        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Pairs.Add(item);
        }


        public void Clear()
        {
            Pairs.Clear();
        }


        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return Pairs.Contains(item);
        }


        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Pairs.CopyTo(array, arrayIndex);
        }


        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Pairs.Remove(item);
        }


        public int IndexOf(KeyValuePair<TKey, TValue> item)
        {
            return GetIndexOf(item);
        }


        public void Insert(int index, KeyValuePair<TKey, TValue> item)
        {
            Pairs.Insert(index, item);
        }


        public void RemoveAt(int index)
        {
            Pairs.RemoveAt(index);
        }


        public KeyValuePair<TKey, TValue> this[int index]
        {
            get { return Pairs[index]; }
            set { Pairs[index] = value; }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) Pairs).GetEnumerator();
        }


        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;


        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (Pairs.Count == 0)
            {
                serializedKeys = null;
                serializedValues = null;
            }
            else
            {
                serializedKeys = new TKey[Pairs.Count];
                serializedValues = new TValue[Pairs.Count];

                int i = 0;
                foreach (KeyValuePair<TKey, TValue> pair in Pairs)
                {
                    serializedKeys[i] = pair.Key;
                    serializedValues[i] = pair.Value;
                    ++i;
                }
            }
        }


        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (serializedKeys != null &&
                serializedValues != null)
            {
                int keysCount = serializedKeys.Length;
                int valuesCount = serializedValues.Length;
                int length = keysCount;

                if (keysCount < valuesCount)
                {
                    Debug.Log($"Key for value '{serializedValues[length]}' is lost.");
                }
                else if (valuesCount < keysCount)
                {
                    length = valuesCount;
                    Debug.Log($"Value for key '{serializedKeys[length]}' is lost.");
                }

                Pairs.Clear();

                if (Pairs.Capacity < length)
                {
                    Pairs.Capacity = length;
                }

                for (int i = 0; i < length; ++i)
                {
                    Pairs.Add(new KeyValuePair<TKey, TValue>(serializedKeys[i], serializedValues[i]));
                }
            }

#if !UNITY_EDITOR
            {
                // Clear unused data if we are not in editor
                serializedKeys = null;
                serializedValues = null;
            }
#endif
        }


        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            Pairs.AddRange(collection);
        }


        public bool ContainsValue(TValue value)
        {
            foreach (KeyValuePair<TKey, TValue> pair in Pairs)
            {
                if (Equals(pair.Value, value))
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        ///     Removes all pairs with the specified key.
        /// </summary>
        public bool RemoveAll(TKey key)
        {
            bool removed = false;
            for (int i = 0; i < Pairs.Count; ++i)
            {
                if (Equals(Pairs[i].Key, key))
                {
                    Pairs.RemoveAt(i);
                    --i;
                    removed = true;
                }
            }

            return removed;
        }


        public void RemoveRange(ICollection<TKey> range)
        {
            for (int i = 0; i < Pairs.Count; ++i)
            {
                if (range.Contains(Pairs[i].Key))
                {
                    Pairs.RemoveAt(i);
                    --i;
                }
            }
        }


        /// <summary>
        ///     Finds first value by the specified key
        /// </summary>
        public TValue FindFirst(TKey key)
        {
            return this[key];
        }


        /// <summary>
        ///     Finds first value by the specified key otherwise returns default value
        /// </summary>
        public TValue FindFirstOrDefault(TKey key)
        {
            if (ContainsKey(key))
            {
                return FindFirst(key);
            }

            return default(TValue);
        }


        /// <summary>
        ///     Finds all values corresponding to the specified key
        /// </summary>
        public List<TValue> Find(TKey key)
        {
            var result = new List<TValue>();
            foreach (KeyValuePair<TKey, TValue> pair in Pairs)
            {
                if (Equals(pair.Key, key))
                {
                    result.Add(pair.Value);
                }
            }

            return result;
        }


        public KeyValuePair<TKey, TValue> Find(Predicate<KeyValuePair<TKey, TValue>> condition)
        {
            return Pairs.Find(condition);
        }


        public List<KeyValuePair<TKey, TValue>> FindAll(Predicate<KeyValuePair<TKey, TValue>> condition)
        {
            return Pairs.FindAll(condition);
        }


        public void Sort(Comparison<KeyValuePair<TKey, TValue>> comparison)
        {
            Pairs.Sort(comparison);
        }


        private int GetIndexOf(KeyValuePair<TKey, TValue> item)
        {
            int count = Pairs.Count;
            for (int index = 0; index < count; index += 1)
            {
                KeyValuePair<TKey, TValue> pairedItem = Pairs[index];
                if (pairedItem.Equals(item))
                {
                    return index;
                }
            }

            return -1;
        }


        private bool Equals(TKey first, TKey second)
        {
            return _equalityComparer?.Equals(first, second) ?? EqualityComparer<TKey>.Default.Equals(first, second);
        }
    }

    /// <summary>
    ///     Specifies that KeyValueList is allowed to have equal keys in different pairs
    /// </summary>
    public class NonUniqueKeysAttribute : Attribute
    {
    }

    public class ContainsAllEnumKeysAttribute : Attribute
    {
    }
}