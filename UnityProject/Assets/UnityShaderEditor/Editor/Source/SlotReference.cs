using System;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public class SlotReference : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_SlotName;

        [NonSerialized]
        private Guid m_NodeGUID;

        [SerializeField]
        private string m_NodeGUIDSerialized;

        public SlotReference(Guid nodeGuid, string slotName)
        {
            m_NodeGUID = nodeGuid;
            m_SlotName = slotName;
        }

        public Guid nodeGuid
        {
            get { return m_NodeGUID; }
        }

        public string slotName
        {
            get { return m_SlotName; }
        }

        public void OnBeforeSerialize()
        {
            m_NodeGUIDSerialized = m_NodeGUID.ToString();
        }

        public void OnAfterDeserialize()
        {
            m_NodeGUID = new Guid(m_NodeGUIDSerialized);
        }
    }
}
