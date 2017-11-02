using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphing
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

        [SerializeField]
        private bool m_Hidden;

        public SlotReference slotReference
        {
            get { return new SlotReference(owner.guid, m_Id); }
        }

        public INode owner { get; set; }

        public bool hidden
        {
            get { return m_Hidden; }
            set { m_Hidden = value; }
        }

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

        public SlotType slotType
        {
            get { return m_SlotType; }
        }

        // used via reflection / serialization after deserialize
        // to reconstruct this slot.
        public SerializableSlot()
        {}

        public SerializableSlot(int id, string displayName, SlotType slotType, int priority, bool hidden = false)
        {
            m_Id = id;
            m_DisplayName = displayName;
            m_SlotType = slotType;
            m_Priority = priority;
            m_Hidden = hidden;
        }

        public SerializableSlot(int id, string displayName, SlotType slotType, bool hidden = false)
        {
            m_Id = id;
            m_DisplayName = displayName;
            m_SlotType = slotType;
            m_Hidden = hidden;
        }

        protected bool Equals(SerializableSlot other)
        {
            return m_Id == other.m_Id && owner.guid.Equals(other.owner.guid);
        }

        public bool Equals(ISlot other)
        {
            return Equals(other as object);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SerializableSlot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Id * 397) ^ (owner != null ? owner.GetHashCode() : 0);
            }
        }

        public bool isConnected
        {
            get
            {
                // node and graph respectivly
                if (owner == null || owner.owner == null)
                    return false;

                var graph = owner.owner;
                var edges = graph.GetEdges(slotReference);
                return edges.Any();
            }
        }
    }
}
