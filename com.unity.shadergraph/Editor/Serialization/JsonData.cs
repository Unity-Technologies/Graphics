using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    struct JsonData<T> : ISerializationCallbackReceiver
        where T : JsonObject
    {
        [NonSerialized]
        T m_Value;

        [SerializeField]
        string m_Id;

        public T value => m_Value;

        public void OnBeforeSerialize()
        {
            if (MultiJsonInternal.isSerializing && m_Value != null && MultiJsonInternal.serializedSet.Add(m_Id))
            {
                MultiJsonInternal.serializationQueue.Add(m_Value);
            }
        }

        public void OnAfterDeserialize()
        {
            if (MultiJsonInternal.isDeserializing)
            {
                try
                {
                    m_Value = (T)MultiJsonInternal.instanceMap[m_Id];
                }
                catch (Exception e)
                {
                    // TODO: Allow custom logging function
                    Debug.LogException(e);
                }
            }
        }

        public static implicit operator T(JsonData<T> jsonRef)
        {
            return jsonRef.m_Value;
        }

        public static implicit operator JsonData<T>(T value)
        {
            return new JsonData<T> { m_Value = value, m_Id = value.id };
        }

        public bool Equals(JsonData<T> other)
        {
            return EqualityComparer<T>.Default.Equals(m_Value, other.m_Value);
        }

        public override bool Equals(object obj)
        {
            return obj is JsonData<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(m_Value);
        }
    }
}
