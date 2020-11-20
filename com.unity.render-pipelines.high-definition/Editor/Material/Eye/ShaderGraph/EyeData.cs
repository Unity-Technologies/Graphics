using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class EyeData : HDTargetData
    {
        public enum MaterialType
        {
            Eye,
            EyeCinematic
        }

        [SerializeField]
        MaterialType m_MaterialType;
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        [SerializeField]
        bool m_SubsurfaceScattering = false;
        public bool subsurfaceScattering
        {
            get => m_SubsurfaceScattering;
            set => m_SubsurfaceScattering = value;
        }

        [SerializeField]
        bool m_IrisNormal = false;
        public bool irisNormal
        {
            get => m_IrisNormal;
            set => m_IrisNormal = value;
        }
    }
}
