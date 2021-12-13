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
            EyeCinematic,
            EyeCinematicWithCaustic
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

        [SerializeField]
        float m_IrisHeight = 0.5f;
        public float irisHeight
        {
            get => m_IrisHeight;
            set => m_IrisHeight = value;
        }

        [SerializeField]
        float m_IrisRadius = 0.1f;
        public float irisRadius
        {
            get => m_IrisRadius;
            set => m_IrisRadius = value;
        }

        [SerializeField]
        float m_CausticIntensity = 1.0f;
        public float causticIntensity
        {
            get => m_CausticIntensity;
            set => m_CausticIntensity = value;
        }

        [SerializeField]
        float m_CausticBlend = 1.0f;
        public float causticBlend
        {
            get => m_CausticBlend;
            set => m_CausticBlend = Mathf.Clamp01(value);
        }
    }
}
