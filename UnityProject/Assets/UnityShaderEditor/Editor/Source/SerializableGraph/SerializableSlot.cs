using System;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public class SerializableSlot
    {
        private const string kNotInit =  "Not Initilaized";

        [SerializeField]
        private string m_Name = kNotInit;

        [SerializeField]
        private string m_DisplayName = kNotInit;

        [SerializeField]
        private SlotType m_SlotType;
        
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

        public SerializableSlot(string name, string displayName, SlotType slotType)
        {
            m_Name = name;
            m_DisplayName = displayName;
            m_SlotType = slotType;
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
