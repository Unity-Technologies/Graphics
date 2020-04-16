using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    struct JsonRef<T> : ISerializationCallbackReceiver
        where T : JsonObject
    {
        [NonSerialized]
        T m_Value;

        [SerializeField]
        string m_Id;

        public T value => m_Value;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (MultiJsonInternal.isDeserializing)
            {
                try
                {
                    m_Value = (T)MultiJsonInternal.valueMap[m_Id];
                    m_Id = m_Value.objectId;
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
            return jsonRef.m_Value;
        }

        public static implicit operator JsonRef<T>(T value)
        {
            return new JsonRef<T> { m_Value = value, m_Id = value?.objectId };
        }

        public bool Equals(JsonRef<T> other)
        {
            return EqualityComparer<T>.Default.Equals(m_Value, other.m_Value);
        }

        public bool Equals(T other)
        {
            return EqualityComparer<T>.Default.Equals(m_Value, other);
        }

        public override bool Equals(object obj)
        {
            return obj is JsonRef<T> other && Equals(other) || obj is T otherValue && Equals(otherValue);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(m_Value);
        }

        public static bool operator ==(JsonRef<T> left, JsonRef<T> right)
        {
            return left.value == right.value;
        }

        public static bool operator !=(JsonRef<T> left, JsonRef<T> right)
        {
            return left.value != right.value;
        }

        public static bool operator ==(JsonRef<T> left, T right)
        {
            return left.value == right;
        }

        public static bool operator !=(JsonRef<T> left, T right)
        {
            return left.value != right;
        }

        public static bool operator ==(T left, JsonRef<T> right)
        {
            return left == right.value;
        }

        public static bool operator !=(T left, JsonRef<T> right)
        {
            return left != right.value;
        }
    }
}
