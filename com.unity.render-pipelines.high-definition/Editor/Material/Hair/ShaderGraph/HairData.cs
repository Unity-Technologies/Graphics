using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HairData : HDTargetData
    {
        public enum MaterialType
        {
            KajiyaKay,
            Marschner
        }

        public enum ScatteringMode
        {
            Approximate,
            DensityVolume
        }

        [SerializeField]
        MaterialType m_MaterialType;
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        [SerializeField]
        ScatteringMode m_ScatteringMode;

        public ScatteringMode scatteringMode
        {
            get => m_ScatteringMode;
            set => m_ScatteringMode = value;
        }

        [SerializeField]
        bool m_UseLightFacingNormal = false;
        public bool useLightFacingNormal
        {
            get => m_UseLightFacingNormal;
            set => m_UseLightFacingNormal = value;
        }
    }
}
