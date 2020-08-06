using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class FabricData : HDTargetData
    {
        public enum MaterialType
        {
            CottonWool,
            Silk
        }

        [SerializeField]
        MaterialType m_MaterialType;
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        [SerializeField]
        bool m_EnergyConservingSpecular = true;
        public bool energyConservingSpecular
        {
            get => m_EnergyConservingSpecular;
            set => m_EnergyConservingSpecular = value;
        }

        [SerializeField]
        bool m_Transmission = false;
        public bool transmission
        {
            get => m_Transmission;
            set => m_Transmission = value;
        }

        [SerializeField]
        bool m_SubsurfaceScattering = false;
        public bool subsurfaceScattering
        {
            get => m_SubsurfaceScattering;
            set => m_SubsurfaceScattering = value;
        }
    }
}
