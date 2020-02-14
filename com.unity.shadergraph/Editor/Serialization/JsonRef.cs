using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    struct JsonRef<T> : ISerializationCallbackReceiver
        where T : JsonObject
    {
        T m_Value;

        [SerializeField]
        string m_Id;

        public void OnBeforeSerialize()
        {
            if (MultiJsonInternal.isSerializing && m_Value != null)
            {
                m_Id = m_Value.id;

                if (MultiJsonInternal.serializedSet.Add(m_Id))
                {
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
                    m_Value = (T)MultiJsonInternal.instanceMap[m_Id];
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

        public static implicit operator JsonRef<T>(T jsonRef)
        {
            return new JsonRef<T> { m_Value = jsonRef };
        }
    }
}
