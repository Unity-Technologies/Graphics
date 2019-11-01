using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Edge : IComparable<Edge>
    {
        [SerializeField]
        JsonRef<MaterialSlot> m_OutputSlot;

        [SerializeField]
        JsonRef<MaterialSlot> m_InputSlot;

        public Edge() { }

        public Edge(MaterialSlot outputSlot, MaterialSlot inputSlot)
        {
            m_OutputSlot = outputSlot;
            m_InputSlot = inputSlot;
        }

        public MaterialSlot outputSlot
        {
            get => m_OutputSlot;
        }

        public MaterialSlot inputSlot
        {
            get => m_InputSlot;
        }

        protected bool Equals(Edge other)
        {
            return Equals(m_OutputSlot, other.m_OutputSlot) && Equals(m_InputSlot, other.m_InputSlot);
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
                return ((m_OutputSlot != null ? m_OutputSlot.GetHashCode() : 0) * 397) ^ (m_InputSlot != null ? m_InputSlot.GetHashCode() : 0);
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
