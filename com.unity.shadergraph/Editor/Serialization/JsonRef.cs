using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    struct JsonRef<T> : ISerializationCallbackReceiver
        where T : JsonObject
    {
        T m_Target;

        [SerializeField]
        string m_Id;

        public T target => m_Target;

        public void OnBeforeSerialize()
        {
            if (MultiJsonInternal.isSerializing && m_Target != null)
            {
                m_Id = m_Target.id;

                if (MultiJsonInternal.serializedSet.Add(m_Id))
                {
                    MultiJsonInternal.serializationQueue.Add(m_Target);
                }
            }
        }

        public void OnAfterDeserialize()
        {
            if (MultiJsonInternal.isDeserializing)
            {
                try
                {
                    m_Target = (T)MultiJsonInternal.instanceMap[m_Id];
                }
                catch (Exception e)
                {
                    // TODO: Allow custom logging function
                    Debug.LogException(e);
                }
            }
        }

        public static implicit operator T(JsonRef<T> jsonRef)
        {
            return jsonRef.m_Target;
        }

        public static implicit operator JsonRef<T>(T value)
        {
            return new JsonRef<T> { m_Target = value };
        }

        public bool Equals(JsonRef<T> other)
        {
            return EqualityComparer<T>.Default.Equals(m_Target, other.m_Target);
        }

        public override bool Equals(object obj)
        {
            return obj is JsonRef<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(m_Target);
        }
    }
}
