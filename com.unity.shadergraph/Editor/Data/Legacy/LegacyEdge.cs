using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class LegacyEdge
    {
        [SerializeField]
        SlotReference m_OutputSlot = default;

        [SerializeField]
        SlotReference m_InputSlot = default;

        public SlotReference outputSlot => m_OutputSlot;

        public SlotReference inputSlot => m_InputSlot;
    }
}
