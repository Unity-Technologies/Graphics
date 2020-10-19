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
            if (MultiJsonInternal.isSerializing && m_Value != null)
            {
                if (MultiJsonInternal.serializedSet.TryGetValue(m_Id, out JsonObject existingJsonObject))
                {
                    // ID has already been found by serialization, double check it is actually the same value (same reference)
                    if (m_Value != existingJsonObject)
                    {
                        Type existType = existingJsonObject.GetType();
                        Type valueType = m_Value.GetType();

                        Debug.LogError(
                            "Encountered two different objects with the same objectID, this may cause unexpected behavior. " +
                            "This should never occur, please report it as a bug! " +
                            "In the meantime, you can likely work around this issue by finding one of the duplicate objects and recreating or copy-pasting it. " +
                            "The object will be in one of the subgraphs used by this graph, usually a subgraph that has been copied from another subgraph. " +
                            "The first object is of type '" + existType + "'. " +
                            "The second object is of type '" + valueType + "'. " +
                            "Both objects have the objectID '" + m_Id + "', which will show up in the text of the subgraph files containing these objects.");
                    }
                }
                else
                {
                    // new ID encountered -- add it's value to the serialization queue
                    MultiJsonInternal.serializedSet.Add(m_Id, m_Value);
                    MultiJsonInternal.serializationQueue.Add(m_Value);
                }
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
                        m_Id = m_Value.objectId;
                    }
                    else
                    {
                        Debug.LogError($"Missing {typeof(T).FullName} {m_Id}");
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
