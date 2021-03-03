using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDLitData : HDTargetData
    {
        public enum MaterialType
        {
            Standard,
            SubsurfaceScattering,
            Anisotropy,
            Iridescence,
            SpecularColor,
            Translucent
        }

        [SerializeField]
        bool m_RayTracing;
        public bool rayTracing
        {
            get => m_RayTracing;
            set => m_RayTracing = value;
        }

        [SerializeField]
        MaterialType m_MaterialType;
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        [SerializeField]
        ScreenSpaceRefraction.RefractionModel m_RefractionModel;
        public ScreenSpaceRefraction.RefractionModel refractionModel
        {
            get => m_RefractionModel;
            set => m_RefractionModel = value;
        }

        // TODO: Can this use m_Transmission from HDLightingData?
        // TODO: These have different defaults
        [SerializeField]
        bool m_SSSTransmission = true;
        public bool sssTransmission
        {
            get => m_SSSTransmission;
            set => m_SSSTransmission = value;
        }

        // TODO: This seems to have been replaced by a Port?
        // [SerializeField]
        // int m_DiffusionProfile;
        // public int diffusionProfile
        // {
        //     get => m_DiffusionProfile;
        //     set => m_DiffusionProfile = value;
        // }

        [SerializeField]
        bool m_EnergyConservingSpecular = true;
        public bool energyConservingSpecular
        {
            get => m_EnergyConservingSpecular;
            set => m_EnergyConservingSpecular = value;
        }

        [SerializeField]
        bool m_ClearCoat = false;
        public bool clearCoat
        {
            get => m_ClearCoat;
            set => m_ClearCoat = value;
        }
    }
}
