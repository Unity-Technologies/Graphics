using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class SlotReference
    {
        [SerializeField]
        string m_NodeGUIDSerialized = default;

        [SerializeField]
        int m_SlotId = default;

        public string nodeGUID => m_NodeGUIDSerialized;

        public int slotId => m_SlotId;
    }
}
