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

        public enum GeometryType
        {
            Cards,
            Strands
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
        GeometryType m_GeometryType;

        public GeometryType geometryType
        {
            get => m_GeometryType;
            set => m_GeometryType = value;
        }

        [SerializeField]
        bool m_UseRoughenedAzimuthalScattering = false;

        public bool useRoughenedAzimuthalScattering
        {
            get => m_UseRoughenedAzimuthalScattering;
            set => m_UseRoughenedAzimuthalScattering = value;
        }
    }
}
