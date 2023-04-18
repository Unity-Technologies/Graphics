using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDLitData : HDTargetData
    {
        [Obsolete("Use MaterialTypeMask instead.")]
        public enum MaterialType
        {
            Standard,
            SubsurfaceScattering,
            Anisotropy,
            Iridescence,
            SpecularColor,
            Translucent
        }

        [Flags]
        public enum MaterialTypeMask
        {
            Standard = 1 << MaterialId.LitStandard,
            SubsurfaceScattering = 1 << MaterialId.LitSSS,
            Anisotropy = 1 << MaterialId.LitAniso,
            Iridescence = 1 << MaterialId.LitIridescence,
            SpecularColor = 1 << MaterialId.LitSpecular,
            Translucent = 1 << MaterialId.LitTranslucent,
            ColoredTranslucent = 1 << MaterialId.LitColoredTranslucent,
        }

        [SerializeField]
        bool m_RayTracing;
        public bool rayTracing
        {
            get => m_RayTracing;
            set => m_RayTracing = value;
        }

        [SerializeField, Obsolete("use m_MaterialTypeMask instead.")]
        MaterialType m_MaterialType;

        [Obsolete("Use materialTypeMask instead.")]
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        public bool HasMaterialType(MaterialTypeMask materialType)
            => (m_MaterialTypeMask & materialType) != 0;

        [SerializeField]
        MaterialTypeMask m_MaterialTypeMask = MaterialTypeMask.Standard;
        public MaterialTypeMask materialTypeMask
        {
            get => m_MaterialTypeMask;
            set
            {
                if (value == 0)
                    m_MaterialTypeMask = MaterialTypeMask.Standard;
                else
                    m_MaterialTypeMask = value;
            }
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
