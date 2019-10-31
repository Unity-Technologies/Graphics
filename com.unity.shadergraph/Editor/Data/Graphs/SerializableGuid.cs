using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class SerializableGuid : ISerializationCallbackReceiver
    {
        public SerializableGuid()
        {
            m_Guid = Guid.NewGuid();
        }

        public SerializableGuid(Guid guid)
        {
            m_Guid = guid;
        }

        [NonSerialized]
        private Guid m_Guid;

        [SerializeField]
        private string m_GuidSerialized;

        public Guid guid
        {
            get { return m_Guid; }
        }

        public string guidSerialized => m_GuidSerialized;

        public virtual void OnBeforeSerialize()
        {
            m_GuidSerialized = m_Guid.ToString();
        }

        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(guidSerialized))
                m_Guid = new Guid(guidSerialized);
            else
                m_Guid = Guid.NewGuid();
        }
    }
}
