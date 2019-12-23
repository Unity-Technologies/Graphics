using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    //
    // Unity can't serialize Dictionary so here's a custom wrapper that does. Note that you have to
    // extend it before it can be serialized as Unity won't serialized generic-based types either.
    //
    // Example:
    //   public sealed class MyDictionary : SerializedDictionary<KeyType, ValueType> {}
    //
    /// <summary>
    /// Serialized Dictionary
    /// </summary>
    /// <typeparam name="K">Key Type</typeparam>
    /// <typeparam name="V">Value Type</typeparam>
    [Serializable]
    public class SerializedDictionary<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<K> m_Keys = new List<K>();

        [SerializeField]
        List<V> m_Values = new List<V>();

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_Keys.Clear();
            m_Values.Clear();

            foreach (var kvp in this)
            {
                m_Keys.Add(kvp.Key);
                m_Values.Add(kvp.Value);
            }
        }

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            for (int i = 0; i < m_Keys.Count; i++)
                Add(m_Keys[i], m_Values[i]);

            m_Keys.Clear();
            m_Values.Clear();
        }
    }
}
