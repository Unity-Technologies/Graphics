using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    sealed class ContextData : ISerializationCallbackReceiver
    {
        [SerializeField]
        List<string> m_SerializableBlockGuids = new List<string>();

        [SerializeField]
        Vector2 m_Position;

        [NonSerialized]
        List<Guid> m_BlockGuids = new List<Guid>();

        public ContextData()
        {
        }

        public List<Guid> blockGuids => m_BlockGuids;

        public Vector2 position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public void OnBeforeSerialize()
        {
            m_SerializableBlockGuids = new List<string>();
            foreach(var blockGuid in blockGuids)
            {
                m_SerializableBlockGuids.Add(blockGuid.ToString());
            }
        }

        public void OnAfterDeserialize()
        {
            foreach(var blockGuid in m_SerializableBlockGuids)
            {
                blockGuids.Add(new Guid(blockGuid));
            }
            m_SerializableBlockGuids = null;
        }
    }
}
