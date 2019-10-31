using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    class JsonList<T> : List<T>, ISerializationCallbackReceiver
        where T : JsonObject
    {
        [SerializeField]
        List<string> m_Refs = new List<string>();

        public void OnBeforeSerialize()
        {
            if (m_Refs == null)
            {
                m_Refs = new List<string>(Count);
            }
            else
            {
                m_Refs.Clear();
                m_Refs.Capacity = Count;
            }

            foreach (var item in this)
            {
                m_Refs.Add(SerializationContext.GetReference(item));
            }
        }

        public void OnAfterDeserialize()
        {
            if (m_Refs != null)
            {
                Clear();
                Capacity = m_Refs.Count;
                foreach (var id in m_Refs)
                {
                    Add((T)DeserializationContext.ResolveReference(id));
                }

                m_Refs = null;
            }
        }
    }
}
