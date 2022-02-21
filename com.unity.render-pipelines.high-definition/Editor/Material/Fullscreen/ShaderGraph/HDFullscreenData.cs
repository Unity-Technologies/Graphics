using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDFullscreenData : HDTargetData
    {
        [SerializeField]
        bool m_ShowOnlyHDStencilBits = true;
        public bool showOnlyHDStencilBits
        {
            get => m_ShowOnlyHDStencilBits;
            set => m_ShowOnlyHDStencilBits = value;
        }
    }
}
