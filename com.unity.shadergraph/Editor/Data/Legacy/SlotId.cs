using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class SlotId
    {
        [SerializeField]
        int m_Id = default;

        public int value => m_Id;
    }
}
