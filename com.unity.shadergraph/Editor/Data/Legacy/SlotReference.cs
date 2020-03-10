using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    struct SlotReference
    {
        [SerializeField]
        public int m_SlotId;

        [SerializeField]
        public string m_NodeGUIDSerialized;
    }
}
