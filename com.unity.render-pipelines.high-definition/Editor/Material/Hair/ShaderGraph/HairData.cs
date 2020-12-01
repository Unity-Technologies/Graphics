using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HairData : HDTargetData
    {
        public enum MaterialType
        {
            KajiyaKay
        }

        [SerializeField]
        MaterialType m_MaterialType;
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
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
