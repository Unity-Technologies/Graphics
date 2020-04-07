using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDUnlitData : HDTargetData
    {
        [SerializeField]
        bool m_EnableShadowMatte = false;
        public bool enableShadowMatte
        {
            get => m_EnableShadowMatte;
            set => m_EnableShadowMatte = value;
        }
    }
}
