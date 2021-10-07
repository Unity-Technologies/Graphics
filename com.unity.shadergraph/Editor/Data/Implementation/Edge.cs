using System;
using UnityEngine;

namespace UnityEditor.Graphing
{
    [Serializable]
    class Edge : IEdge, IComparable<Edge>
    {
        [SerializeField]
        private SlotReference m_OutputSlot;
        [SerializeField]
        private SlotReference m_InputSlot;

        public Edge()
        { }

        public Edge(SlotReference outputSlot, SlotReference inputSlot)
        {
            m_OutputSlot = outputSlot;
            m_InputSlot = inputSlot;
        }

        public SlotReference outputSlot
        {
            get { return m_OutputSlot; }
        }

        public SlotReference inputSlot
        {
            get { return m_InputSlot; }
        }

        protected bool Equals(Edge other)
        {
            return Equals(m_OutputSlot, other.m_OutputSlot) && Equals(m_InputSlot, other.m_InputSlot);
        }

        public bool Equals(IEdge other)
        {
            return Equals(other as object);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Edge)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // Can't make fields readonly due to Unity serialization
                return (m_OutputSlot.GetHashCode() * 397) ^ m_InputSlot.GetHashCode();
            }
        }

        public int CompareTo(Edge other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var outputSlotComparison = m_OutputSlot.CompareTo(other.m_OutputSlot);
            if (outputSlotComparison != 0) return outputSlotComparison;
            return m_InputSlot.CompareTo(other.m_InputSlot);
        }
    }
}
