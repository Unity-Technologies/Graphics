using System;

namespace UnityEngine.Graphing
{
    [Serializable]
    public class SerializableSlot : ISlot
    {
        private const string kNotInit =  "Not Initilaized";

        [SerializeField]
        private int m_Id;

        [SerializeField]
        private string m_DisplayName = kNotInit;

        [SerializeField]
        private SlotType m_SlotType = SlotType.Input;

        [SerializeField]
        private int m_Priority = int.MaxValue;

        public SlotReference slotReference
        {
            get { return new SlotReference(owner.guid, m_Id); }
        }

        public INode owner { get; set; }

        public int id
        {
            get { return m_Id; }
        }

        public virtual string displayName
        {
            get { return m_DisplayName; }
            set { m_DisplayName = value; }
        }

        public int priority
        {
            get { return m_Priority; }
            set { m_Priority = value; }
        }

        public bool isInputSlot
        {
            get { return m_SlotType == SlotType.Input; }
        }

        public bool isOutputSlot
        {
            get { return m_SlotType == SlotType.Output; }
        }

        // used via reflection / serialization after deserialize
        // to reconstruct this slot.
        public SerializableSlot()
        {}

        public SerializableSlot(int id, string displayName, SlotType slotType, int priority)
        {
            m_Id = id;
            m_DisplayName = displayName;
            m_SlotType = slotType;
            m_Priority = priority;
        }

        public SerializableSlot(int id, string displayName, SlotType slotType)
        {
            m_Id = id;
            m_DisplayName = displayName;
            m_SlotType = slotType;
        }
    }
}
