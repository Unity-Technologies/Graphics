using System;

namespace UnityEngine.Graphing
{
    [Serializable]
    public class SlotReference : ISerializationCallbackReceiver
    {
        [SerializeField]
        private int m_SlotId;

        [NonSerialized]
        private Guid m_NodeGUID;

        [SerializeField]
        private string m_NodeGUIDSerialized;

        public SlotReference(Guid nodeGuid, int slotId)
        {
            m_NodeGUID = nodeGuid;
            m_SlotId = slotId;
            m_NodeGUIDSerialized = string.Empty;
        }

        public Guid nodeGuid
        {
            get { return m_NodeGUID; }
        }

        public int slotId
        {
            get { return m_SlotId; }
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
