using System;
using Newtonsoft.Json;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SerializableGuid
    {
        public SerializableGuid()
        {
            m_Guid = Guid.NewGuid();
        }

        public SerializableGuid(Guid guid)
        {
            m_Guid = guid;
        }

        [SerializeField]
        [JsonUpgrade("m_GuidSerialized")]
        Guid m_Guid;

        public Guid guid
        {
            get { return m_Guid; }
        }
    }
}
