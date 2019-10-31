using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    struct JsonRef<T> : ISerializationCallbackReceiver, IEquatable<JsonRef<T>>
        where T : JsonObject
    {
        [SerializeField]
        string m_Ref;

        [NonSerialized]
        T m_Target;

        public JsonRef(T target)
        {
            m_Target = target;
            m_Ref = null;
        }

        public static implicit operator JsonRef<T>(T target)
        {
            return new JsonRef<T>(target);
        }

        public static implicit operator T(JsonRef<T> jsonRef)
        {
            return jsonRef.target;
        }

        public T target => m_Target;

        public void OnBeforeSerialize()
        {
            m_Ref = m_Target == null ? null : SerializationContext.GetReference(target);
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_Ref) && target == null)
            {
                m_Target = (T)DeserializationContext.ResolveReference(m_Ref);
                m_Ref = null;
            }
        }

        public bool Equals(JsonRef<T> other)
        {
            return m_Target == other.m_Target;
        }

        public override bool Equals(object obj)
        {
            return obj is JsonRef<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Target.GetHashCode();
        }

        public static bool operator ==(JsonRef<T> jsonRef, T target)
        {
            return jsonRef.target == target;
        }

        public static bool operator !=(JsonRef<T> jsonRef, T target)
        {
            return !(jsonRef == target);
        }

        public static bool operator ==(T target, JsonRef<T> jsonRef)
        {
            return jsonRef.target == target;
        }

        public static bool operator !=(T target, JsonRef<T> jsonRef)
        {
            return !(jsonRef == target);
        }
    }

    static class JsonRefExtensions
    {
        public static JsonRef<T> ToJsonRef<T>(this T target)
            where T : JsonObject
        {
            return new JsonRef<T>(target);
        }

        public static JsonRefListEnumerable<T> SelectTarget<T>(this List<JsonRef<T>> list)
            where T : JsonObject
        {
            return new JsonRefListEnumerable<T>(list);
        }
    }
}
