using System;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.Graphing
{
    // TODO: Upgrade
    [Serializable]
    class EdgeData
    {
        [SerializeField]
        JsonRef<MaterialSlot> m_OutputSlot;

        [SerializeField]
        JsonRef<MaterialSlot> m_InputSlot;

        public EdgeData(MaterialSlot outputSlot, MaterialSlot inputSlot)
        {
            m_OutputSlot = outputSlot;
            m_InputSlot = inputSlot;
        }

        public MaterialSlot outputSlot => m_OutputSlot;

        public MaterialSlot inputSlot => m_InputSlot;

        public bool Equals(EdgeData other)
        {
            return m_OutputSlot.Equals(other.m_OutputSlot) && m_InputSlot.Equals(other.m_InputSlot);
        }

        public override bool Equals(object obj)
        {
            return obj is EdgeData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_OutputSlot.GetHashCode() * 397) ^ m_InputSlot.GetHashCode();
            }
        }
    }
}
