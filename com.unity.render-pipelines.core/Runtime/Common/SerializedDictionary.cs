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
    public class SerializedDictionary<K, V> : SerializedDictionary<K, V, K, V>
    {
        public override K SerializeKey(K key) => key;
        public override V SerializeValue(V val) => val;
        public override K DeserializeKey(K key) => key;
        public override V DeserializeValue(V val) => val;
    }

    [Serializable]
    public abstract class SerializedDictionary<K, V, SK, SV> : Dictionary<K, V>, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<SK> m_Keys = new List<SK>();

        [SerializeField]
        List<SV> m_Values = new List<SV>();

        /// <summary>
        /// Serialize key K to SK
        /// </summary>
        public abstract SK SerializeKey(K key);
        /// <summary>
        /// Serialize value V to SV
        /// </summary>
        public abstract SV SerializeValue(V value);
        /// <summary>
        /// Deserialize key SK to K
        /// </summary>
        public abstract K DeserializeKey(SK serializedKey);
        /// <summary>
        /// Deserialize value SV to V
        /// </summary>
        public abstract V DeserializeValue(SV serializedValue);

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_Keys.Clear();
            m_Values.Clear();

            foreach (var kvp in this)
            {
                m_Keys.Add(SerializeKey(kvp.Key));
                m_Values.Add(SerializeValue(kvp.Value));
            }
        }

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            for (int i = 0; i < m_Keys.Count; i++)
                Add(DeserializeKey(m_Keys[i]), DeserializeValue(m_Values[i]));

            m_Keys.Clear();
            m_Values.Clear();
        }
    }
}
