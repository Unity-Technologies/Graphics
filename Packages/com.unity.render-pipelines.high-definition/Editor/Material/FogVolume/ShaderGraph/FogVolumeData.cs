using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class FogVolumeData : HDTargetData
    {
        [SerializeField]
        LocalVolumetricFogBlendingMode m_BlendMode = LocalVolumetricFogBlendingMode.Additive;
        public LocalVolumetricFogBlendingMode blendMode
        {
            get => m_BlendMode;
            set => m_BlendMode = value;
        }
    }
}
