using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public class SerializableNode : ISerializationCallbackReceiver
    {
        public delegate void NeedsRepaint();

        private const int kPreviewWidth = 64;
        private const int kPreviewHeight = 64;

        [NonSerialized]
        private Guid m_Guid;

        [SerializeField]
        private string m_GuidSerialized;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private Rect m_Position;

        [NonSerialized]
        private List<SerializableSlot> m_Slots = new List<SerializableSlot>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableSlots = new List<SerializationHelper.JSONSerializedElement>();

        public SerializableGraph owner { get; set; }

        public Guid guid
        {
            get { return m_Guid; }
        }

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public virtual bool canDeleteNode
        {
            get { return true; }
        }

        protected virtual int previewWidth
        {
            get { return kPreviewWidth; }
        }

        protected virtual int previewHeight
        {
            get { return kPreviewHeight; }
        }

        public Rect position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        public IEnumerable<SerializableSlot> inputSlots
        {
            get { return m_Slots.Where(x => x.isInputSlot); }
        }

        public IEnumerable<SerializableSlot> outputSlots
        {
            get { return m_Slots.Where(x => x.isOutputSlot); }
        }

        public IEnumerable<SerializableSlot> slots
        {
            get { return m_Slots; }
        }

        public SerializableNode(SerializableGraph theOwner)
        {
            owner = theOwner;
            m_Guid = Guid.NewGuid();
        }

        public virtual float GetNodeUIHeight(float width)
        {
            return 0;
        }

        public virtual GUIModificationType NodeUI(Rect drawArea)
        {
            return GUIModificationType.None;
        }

        public virtual void OnBeforeSerialize()
        {
            m_GuidSerialized = m_Guid.ToString();
            m_SerializableSlots = SerializationHelper.Serialize(m_Slots);
        }

        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_GuidSerialized))
                m_Guid = new Guid(m_GuidSerialized);
            else
                m_Guid = Guid.NewGuid();

            m_Slots = SerializationHelper.Deserialize<SerializableSlot>(m_SerializableSlots, new object[] { this });
            m_SerializableSlots = null;
        }
    }
}
