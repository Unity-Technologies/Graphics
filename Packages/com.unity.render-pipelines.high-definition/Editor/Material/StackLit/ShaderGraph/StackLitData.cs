using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class StackLitData : HDTargetData
    {
        // In StackLit.hlsl engine side
        //public enum BaseParametrization
        //public enum DualSpecularLobeParametrization

        // Available options for computing Vs (specular occlusion) based on:
        //
        // baked diffuse visibility (aka "data based AO") orientation
        // (ie baked visibility cone (aka "bent visibility cone") orientation)
        // := { normal aligned (default bentnormal value), bent normal }
        // X
        // baked diffuse visibility solid angle inference algo from baked visibility scalar
        // (ie baked visibility cone aperture angle or solid angle)
        // := { uniform (solid angle measure), cos weighted (projected solid angle measure with cone oriented with normal),
        //      cos properly weighted wrt bentnormal (projected solid angle measure with cone oriented with bent normal) }
        // X
        // Vs (aka specular occlusion) calculation algo from baked diffuse values above and BSDF lobe properties
        // := {triACE - not tuned to account for bent normal, cone BSDF proxy intersection with bent cone, precise SPTD BSDF proxy lobe integration against the bent cone} }
        //
        // Note that in Lit SSAO is used with triACE as a clamp value to combine it with the calculations done with the baked AO,
        // by doing a min(VsFromTriACE+SSAO, VsFromBakedVisibility).
        // (See in particular Lit.hlsl:PostEvaluateBSDF(), MaterialEvaluation.hlsl:GetScreenSpaceAmbientOcclusionMultibounce(),
        // where the handed bsdfData.specularOcclusion is data based (baked texture).
        //
        // In StackLit, we allow control of the SSAO based SO and also the data based one.
        //
        // Of the algos described above, we can narrow to these combined options:
        // { Off, NoBentNormalTriACE, *ConeCone, *SPTD }, where * is any combination of using the normal or the bentnormal with any of 3 choices to interpret the AO
        // measure for the cone aperture.
        //
        // See also _DebugSpecularOcclusion.
        public enum SpecularOcclusionBaseMode
        {
            Off = 0,
            DirectFromAO = 1, // TriACE
            ConeConeFromBentAO = 2,
            SPTDIntegrationOfBentAO = 3,
            Custom = 4,
            // Custom user port input: For now, we will only have one input used for all lobes and only for data-based SO
            // (TODO: Normally would need a custom input per lobe.
            // Main rationale is that roughness can change IBL fetch direction and not only BSDF lobe width, and interface normal changes shading reference frame
            // hence it also changes the directional relation between the visibility cone and the BSDF lobe.)
        }

        public enum SpecularOcclusionBaseModeSimple
        {
            Off = 0,
            DirectFromAO = 1, // TriACE
            SPTDIntegrationOfBentAO = 3,
            Custom = 4,
        }

        public enum SpecularOcclusionAOConeSize
        {
            UniformAO,
            CosWeightedAO,
            CosWeightedBentCorrectAO
        }

        // This is in case SSAO-based SO method requires it (the SSAO we have doesn't provide a direction)
        public enum SpecularOcclusionAOConeDir
        {
            GeomNormal,
            BentNormal,
            ShadingNormal
        }

        // SO Bent cone fixup is only for methods using visibility cone and only for the data based SO:
        public enum SpecularOcclusionConeFixupMethod
        {
            Off,
            BoostBSDFRoughness,
            TiltDirectionToGeomNormal,
            BoostAndTilt,
        }

        // Features: material surface input parametrizations
        [SerializeField]
        StackLit.BaseParametrization m_BaseParametrization;
        public StackLit.BaseParametrization baseParametrization
        {
            get => m_BaseParametrization;
            set => m_BaseParametrization = value;
        }

        [SerializeField]
        StackLit.DualSpecularLobeParametrization m_DualSpecularLobeParametrization;
        public StackLit.DualSpecularLobeParametrization dualSpecularLobeParametrization
        {
            get => m_DualSpecularLobeParametrization;
            set => m_DualSpecularLobeParametrization = value;
        }

        // TODO: Change all to enable* ?
        // Features: "physical" material type enables
        [SerializeField]
        bool m_Anisotropy;
        public bool anisotropy
        {
            get => m_Anisotropy;
            set => m_Anisotropy = value;
        }

        [SerializeField]
        bool m_Coat;
        public bool coat
        {
            get => m_Coat;
            set => m_Coat = value;
        }

        [SerializeField]
        bool m_CoatNormal;
        public bool coatNormal
        {
            get => m_CoatNormal;
            set => m_CoatNormal = value;
        }

        [SerializeField]
        bool m_DualSpecularLobe;
        public bool dualSpecularLobe
        {
            get => m_DualSpecularLobe;
            set => m_DualSpecularLobe = value;
        }

        [SerializeField]
        bool m_CapHazinessWrtMetallic = true;
        public bool capHazinessWrtMetallic
        {
            get => m_CapHazinessWrtMetallic;
            set => m_CapHazinessWrtMetallic = value;
        }

        [SerializeField]
        bool m_Iridescence;
        public bool iridescence
        {
            get => m_Iridescence;
            set => m_Iridescence = value;
        }

        [SerializeField]
        SpecularOcclusionBaseMode m_ScreenSpaceSpecularOcclusionBaseMode = SpecularOcclusionBaseMode.DirectFromAO;
        public SpecularOcclusionBaseMode screenSpaceSpecularOcclusionBaseMode
        {
            get => m_ScreenSpaceSpecularOcclusionBaseMode;
            set => m_ScreenSpaceSpecularOcclusionBaseMode = value;
        }

        [SerializeField]
        SpecularOcclusionBaseMode m_DataBasedSpecularOcclusionBaseMode;
        public SpecularOcclusionBaseMode dataBasedSpecularOcclusionBaseMode
        {
            get => m_DataBasedSpecularOcclusionBaseMode;
            set => m_DataBasedSpecularOcclusionBaseMode = value;
        }

        [SerializeField]
        SpecularOcclusionAOConeSize m_ScreenSpaceSpecularOcclusionAOConeSize; // This is still provided to tweak the effect of SSAO on the SO.
        public SpecularOcclusionAOConeSize screenSpaceSpecularOcclusionAOConeSize
        {
            get => m_ScreenSpaceSpecularOcclusionAOConeSize;
            set => m_ScreenSpaceSpecularOcclusionAOConeSize = value;
        }

        // See SpecularOcclusionAOConeDir for why we need this only for SSAO-based SO:
        [SerializeField]
        SpecularOcclusionAOConeDir m_ScreenSpaceSpecularOcclusionAOConeDir;
        public SpecularOcclusionAOConeDir screenSpaceSpecularOcclusionAOConeDir
        {
            get => m_ScreenSpaceSpecularOcclusionAOConeDir;
            set => m_ScreenSpaceSpecularOcclusionAOConeDir = value;
        }

        [SerializeField]
        SpecularOcclusionAOConeSize m_DataBasedSpecularOcclusionAOConeSize = SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO; // Only for SO methods using visibility cones (ie ConeCone and SPTD)
        public SpecularOcclusionAOConeSize dataBasedSpecularOcclusionAOConeSize
        {
            get => m_DataBasedSpecularOcclusionAOConeSize;
            set => m_DataBasedSpecularOcclusionAOConeSize = value;
        }

        // TODO: This was commented out. Can it be removed?
        // TODO: this needs to be per lobe, less useful to have custom input.
        // [SerializeField]
        // bool m_SpecularOcclusionIsCustom; // allow custom input port for SO (replaces the data based one)
        // public bool specularOcclusionIsCustom
        // {
        //    get => m_SpecularOcclusionIsCustom;
        //    set => m_SpecularOcclusionIsCustom = value;
        // }

        // SO Bent cone fixup is only for methods using visibility cone and only for the data based SO:
        [SerializeField]
        SpecularOcclusionConeFixupMethod m_SpecularOcclusionConeFixupMethod;
        public SpecularOcclusionConeFixupMethod specularOcclusionConeFixupMethod
        {
            get => m_SpecularOcclusionConeFixupMethod;
            set => m_SpecularOcclusionConeFixupMethod = value;
        }

        // Features: Advanced options
        //
        [SerializeField]
        bool m_AnisotropyForAreaLights = true;
        public bool anisotropyForAreaLights
        {
            get => m_AnisotropyForAreaLights;
            set => m_AnisotropyForAreaLights = value;
        }

        [SerializeField]
        bool m_RecomputeStackPerLight;
        public bool recomputeStackPerLight
        {
            get => m_RecomputeStackPerLight;
            set => m_RecomputeStackPerLight = value;
        }

        [SerializeField]
        bool m_HonorPerLightMinRoughness;
        public bool honorPerLightMinRoughness
        {
            get => m_HonorPerLightMinRoughness;
            set => m_HonorPerLightMinRoughness = value;
        }

        [SerializeField]
        bool m_ShadeBaseUsingRefractedAngles;
        public bool shadeBaseUsingRefractedAngles
        {
            get => m_ShadeBaseUsingRefractedAngles;
            set => m_ShadeBaseUsingRefractedAngles = value;
        }

        [SerializeField]
        bool m_Debug;
        public bool debug
        {
            get => m_Debug;
            set => m_Debug = value;
        }

        [SerializeField]
        bool m_DevMode;
        public bool devMode
        {
            get => m_DevMode;
            set => m_DevMode = value;
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
