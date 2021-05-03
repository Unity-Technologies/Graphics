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

        public ExposableProperty<ScreenSpaceRefraction.RefractionModel> refractionModelProp = new ExposableProperty<ScreenSpaceRefraction.RefractionModel>(default);
        public ScreenSpaceRefraction.RefractionModel refractionModel
        {
            get => refractionModelProp.value;
            set => refractionModelProp.value = value;
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

        // Allow to track if something is connected to emissive input
        // will determine if we generate a force forward pass or not
        [SerializeField]
        bool m_EmissionOverriden;
        public bool emissionOverriden
        {
            get => m_EmissionOverriden;
            set => m_EmissionOverriden = value;
        }

        public ExposableProperty<bool> forceForwardEmissiveProp = new ExposableProperty<bool>(false);
        public bool forceForwardEmissive
        {
            get => forceForwardEmissiveProp.value;
            set => forceForwardEmissiveProp.value = value;
        }

        // Kept for migration
        [SerializeField, Obsolete("Keep for migration")]
        ScreenSpaceRefraction.RefractionModel m_RefractionModel;
        [SerializeField, Obsolete("Keep for migration")]
        bool m_ForceForwardEmissive = false;

        internal void MigrateToExposableProperties()
        {
#pragma warning disable 618
            forceForwardEmissiveProp.IsExposed = true;
            refractionModelProp.IsExposed = m_RefractionModel != ScreenSpaceRefraction.RefractionModel.None;

            // Migrate Values
            refractionModel = m_RefractionModel;
            forceForwardEmissive = m_ForceForwardEmissive;
#pragma warning restore 618
        }
    }
}
