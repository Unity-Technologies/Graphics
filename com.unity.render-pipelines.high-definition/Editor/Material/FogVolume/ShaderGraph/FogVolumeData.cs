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

        [SerializeField]
        Color m_SingleScatteringAlbedo = Color.white;
        public Color singleScatteringAlbedo
        {
            get => m_SingleScatteringAlbedo;
            set => m_SingleScatteringAlbedo = value;
        }

        [SerializeField]
        float m_FogDistance = 10;
        public float fogDistance
        {
            get => m_FogDistance;
            set => m_FogDistance = value;
        }

    }
}
