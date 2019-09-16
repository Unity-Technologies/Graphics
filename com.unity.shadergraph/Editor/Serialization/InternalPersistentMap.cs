using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityEditor.ShaderGraph.Serialization
{
    [JsonDictionary(ItemIsReference = false, ItemTypeNameHandling = TypeNameHandling.All)]
    class InternalPersistentMap : IDictionary<string, IPersistent>
    {
        SortedDictionary<string, IPersistent> m_Dictionary = new SortedDictionary<string, IPersistent>();

        public SortedDictionary<string, IPersistent>.Enumerator GetEnumerator()
        {
            return m_Dictionary.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, IPersistent>> IEnumerable<KeyValuePair<string, IPersistent>>.GetEnumerator()
        {
            return m_Dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)m_Dictionary).GetEnumerator();
        }

        void ICollection<KeyValuePair<string, IPersistent>>.Add(KeyValuePair<string, IPersistent> item)
        {
            ((IDictionary<string, IPersistent>)m_Dictionary).Add(item);
        }

        public void Clear()
        {
            m_Dictionary.Clear();
        }

        bool ICollection<KeyValuePair<string, IPersistent>>.Contains(KeyValuePair<string, IPersistent> item)
        {
            return ((IDictionary<string, IPersistent>)m_Dictionary).Contains(item);
        }

        void ICollection<KeyValuePair<string, IPersistent>>.CopyTo(KeyValuePair<string, IPersistent>[] array, int arrayIndex)
        {
            ((IDictionary<string, IPersistent>)m_Dictionary).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, IPersistent>>.Remove(KeyValuePair<string, IPersistent> item)
        {
            return ((IDictionary<string, IPersistent>)m_Dictionary).Remove(item);
        }

        public int Count => m_Dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(string key, IPersistent value)
        {
            m_Dictionary.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return m_Dictionary.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return m_Dictionary.Remove(key);
        }

        public bool TryGetValue(string key, out IPersistent value)
        {
            return m_Dictionary.TryGetValue(key, out value);
        }

        public IPersistent this[string key]
        {
            get => m_Dictionary[key];
            set => m_Dictionary[key] = value;
        }

        public ICollection<string> Keys => m_Dictionary.Keys;

        public ICollection<IPersistent> Values => m_Dictionary.Values;
    }
}
