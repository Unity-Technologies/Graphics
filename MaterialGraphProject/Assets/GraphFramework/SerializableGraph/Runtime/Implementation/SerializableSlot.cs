using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Graphing
{
    [Serializable]
    public class SerializableSlot : ISlot
    {
        private const string kNotInit =  "Not Initilaized";

        [SerializeField]
        private string m_Name = kNotInit;

        [SerializeField]
        private string m_DisplayName = kNotInit;

        [SerializeField]
        private SlotType m_SlotType;

        [SerializeField]
        private int m_Priority;
        
        public string name
        {
            get { return m_Name; }
        }

        public string displayName
        {
            get { return m_DisplayName; }
            set { m_DisplayName = value; }
        }

        public bool isInputSlot
        {
            get { return m_SlotType == SlotType.Input; }
        }

        public bool isOutputSlot
        {
            get { return m_SlotType == SlotType.Output; }
        }

        public int priority
        {
            get { return m_Priority; }
            set { m_Priority = value; } 
        }

        public SerializableSlot(string name, string displayName, SlotType slotType, int priority)
        {
            m_Name = name;
            m_DisplayName = displayName;
            m_SlotType = slotType;
            m_Priority = priority;
        }

        // used via reflection / serialization after deserialize
        // to reconstruct this slot.
        protected SerializableSlot()
        {}

        public virtual bool OnGUI()
        {
            return false;
        }
    }
}
