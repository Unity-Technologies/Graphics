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
                    if (MultiJsonInternal.valueMap.TryGetValue(m_Id, out var value))
                    {
                        m_Value = value.CastTo<T>();

                        // cast may fail for unknown types, but we can still grab the id from the original UnknownType
                        m_Id = value.objectId;
                    }
                    else
                    {
                    }
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
            return new JsonData<T> { m_Value = value, m_Id = value.objectId };
        }

        public bool Equals(JsonData<T> other)
        {
            return EqualityComparer<T>.Default.Equals(m_Value, other.m_Value);
        }

        public bool Equals(T other)
        {
            return EqualityComparer<T>.Default.Equals(m_Value, other);
        }

        public override bool Equals(object obj)
        {
            return obj is JsonData<T> other && Equals(other) || obj is T otherValue && Equals(otherValue);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(m_Value);
        }

        public static bool operator ==(JsonData<T> left, JsonData<T> right)
        {
            return left.value == right.value;
        }

        public static bool operator !=(JsonData<T> left, JsonData<T> right)
        {
            return left.value != right.value;
        }

        public static bool operator ==(JsonData<T> left, T right)
        {
            return left.value == right;
        }

        public static bool operator !=(JsonData<T> left, T right)
        {
            return left.value != right;
        }

        public static bool operator ==(T left, JsonData<T> right)
        {
            return left == right.value;
        }

        public static bool operator !=(T left, JsonData<T> right)
        {
            return left != right.value;
        }

        public static bool operator ==(JsonData<T> left, JsonRef<T> right)
        {
            return left.value == right.value;
        }

        public static bool operator !=(JsonData<T> left, JsonRef<T> right)
        {
            return left.value != right.value;
        }

        public static bool operator ==(JsonRef<T> left, JsonData<T> right)
        {
            return left.value == right.value;
        }

        public static bool operator !=(JsonRef<T> left, JsonData<T> right)
        {
            return left.value != right.value;
        }
    }
}
