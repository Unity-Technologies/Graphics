using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [FormerName("UnityEditor.Graphing.Edge")]
    class ShaderEdge : IEquatable<ShaderEdge>
    {
        [SerializeField]
        private SlotReference m_OutputSlot;
        [SerializeField]
        private SlotReference m_InputSlot;

        public ShaderEdge()
        {}

        public ShaderEdge(SlotReference outputSlot, SlotReference inputSlot)
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

        public bool Equals(ShaderEdge other)
        {
            return Equals(m_OutputSlot, other.m_OutputSlot) && Equals(m_InputSlot, other.m_InputSlot);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ShaderEdge)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // Can't make fields readonly due to Unity serialization
                return (m_OutputSlot.GetHashCode() * 397) ^ m_InputSlot.GetHashCode();
            }
        }
    }
}
