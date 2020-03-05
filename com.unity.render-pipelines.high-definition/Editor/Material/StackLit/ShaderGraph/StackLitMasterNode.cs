using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Rendering.HighDefinition.Drawing;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

//TODOTODO:
// clamp in shader code the ranged() properties
// or let inputs (eg mask?) follow invalid values ? Lit does that (let them free running).
namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    [Title("Master", "StackLit (HDRP)")]
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.StackLitMasterNode")]
    [FormerName("UnityEditor.ShaderGraph.StackLitMasterNode")]
    class StackLitMasterNode : AbstractMaterialNode, IMasterNode, IHasSettings, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent
    {
        public const string PositionSlotName = "Vertex Position";
        public const string PositionSlotDisplayName = "Vertex Position";

        public const string BaseColorSlotName = "BaseColor";

        public const string NormalSlotName = "Normal";
        public const string BentNormalSlotName = "BentNormal";
        public const string TangentSlotName = "Tangent";

        public const string SubsurfaceMaskSlotName = "SubsurfaceMask";
        public const string ThicknessSlotName = "Thickness";
        public const string DiffusionProfileHashSlotName = "DiffusionProfileHash";
        public const string DiffusionProfileHashSlotDisplayName = "Diffusion Profile";

        public const string IridescenceMaskSlotName = "IridescenceMask";
        public const string IridescenceThicknessSlotName = "IridescenceThickness";
        public const string IridescenceThicknessSlotDisplayName = "Iridescence Layer Thickness";
        public const string IridescenceCoatFixupTIRSlotName = "IridescenceCoatFixupTIR";
        public const string IridescenceCoatFixupTIRClampSlotName = "IridescenceCoatFixupTIRClamp";

        public const string SpecularColorSlotName = "SpecularColor";
        public const string MetallicSlotName = "Metallic";
        public const string DielectricIorSlotName = "DielectricIor";

        public const string EmissionSlotName = "Emission";
        public const string SmoothnessASlotName = "SmoothnessA";
        public const string SmoothnessBSlotName = "SmoothnessB";
        public const string AmbientOcclusionSlotName = "AmbientOcclusion";
        public const string AlphaSlotName = "Alpha";
        public const string AlphaClipThresholdSlotName = "AlphaClipThreshold";
        public const string AnisotropyASlotName = "AnisotropyA";
        public const string AnisotropyBSlotName = "AnisotropyB";
        public const string SpecularAAScreenSpaceVarianceSlotName = "SpecularAAScreenSpaceVariance";
        public const string SpecularAAThresholdSlotName = "SpecularAAThreshold";
        public const string DistortionSlotName = "Distortion";
        public const string DistortionSlotDisplayName = "Distortion Vector";
        public const string DistortionBlurSlotName = "DistortionBlur";

        public const string CoatSmoothnessSlotName = "CoatSmoothness";
        public const string CoatIorSlotName = "CoatIor";
        public const string CoatThicknessSlotName = "CoatThickness";
        public const string CoatExtinctionSlotName = "CoatExtinction";
        public const string CoatNormalSlotName = "CoatNormal";
        public const string CoatMaskSlotName = "CoatMask";

        public const string LobeMixSlotName = "LobeMix";
        public const string HazinessSlotName = "Haziness";
        public const string HazeExtentSlotName = "HazeExtent";
        public const string HazyGlossMaxDielectricF0SlotName = "HazyGlossMaxDielectricF0"; // only valid if above option enabled and we have a basecolor + metallic input parametrization

        public const string BakedGISlotName = "BakedGI";
        public const string BakedBackGISlotName = "BakedBackGI";

        // TODO: we would ideally need one value per lobe
        public const string SpecularOcclusionSlotName = "SpecularOcclusion";

        public const string SOFixupVisibilityRatioThresholdSlotName = "SOConeFixupVisibilityThreshold";
        public const string SOFixupStrengthFactorSlotName = "SOConeFixupStrength";
        public const string SOFixupMaxAddedRoughnessSlotName = "SOConeFixupMaxAddedRoughness";

        public const string DepthOffsetSlotName = "DepthOffset";

        public const string VertexNormalSlotName = "Vertex Normal";
        public const string VertexTangentSlotName = "Vertex Tangent";

        public const int PositionSlotId = 0;
        public const int BaseColorSlotId = 1;
        public const int NormalSlotId = 2;
        public const int BentNormalSlotId = 3;
        public const int TangentSlotId = 4;
        public const int SubsurfaceMaskSlotId = 5;
        public const int ThicknessSlotId = 6;
        public const int DiffusionProfileHashSlotId = 7;
        public const int IridescenceMaskSlotId = 8;
        public const int IridescenceThicknessSlotId = 9;
        public const int SpecularColorSlotId = 10;
        public const int DielectricIorSlotId = 11;
        public const int MetallicSlotId = 12;
        public const int EmissionSlotId = 13;
        public const int SmoothnessASlotId = 14;
        public const int SmoothnessBSlotId = 15;
        public const int AmbientOcclusionSlotId = 16;
        public const int AlphaSlotId = 17;
        public const int AlphaClipThresholdSlotId = 18;
        public const int AnisotropyASlotId = 19;
        public const int AnisotropyBSlotId = 20;
        public const int SpecularAAScreenSpaceVarianceSlotId = 21;
        public const int SpecularAAThresholdSlotId = 22;
        public const int DistortionSlotId = 23;
        public const int DistortionBlurSlotId = 24;

        public const int CoatSmoothnessSlotId = 25;
        public const int CoatIorSlotId = 26;
        public const int CoatThicknessSlotId = 27;
        public const int CoatExtinctionSlotId = 28;
        public const int CoatNormalSlotId = 29;

        public const int LobeMixSlotId = 30;
        public const int HazinessSlotId = 31;
        public const int HazeExtentSlotId = 32;
        public const int HazyGlossMaxDielectricF0SlotId = 33;

        public const int LightingSlotId = 34;
        public const int BackLightingSlotId = 35;

        public const int SOFixupVisibilityRatioThresholdSlotId = 36;
        public const int SOFixupStrengthFactorSlotId = 37;
        public const int SOFixupMaxAddedRoughnessSlotId = 38;

        public const int CoatMaskSlotId = 39;
        public const int IridescenceCoatFixupTIRSlotId = 40;
        public const int IridescenceCoatFixupTIRClampSlotId = 41;

        public const int DepthOffsetSlotId = 42;

        public const int VertexNormalSlotId = 44;
        public const int VertexTangentSlotId = 45;

        // TODO: we would ideally need one value per lobe
        public const int SpecularOcclusionSlotId = 43; // for custom (external) SO replacing data based SO (which normally comes from some func of DataBasedSOMode(dataAO, optional bent normal))

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

        // Don't support Multiply
        public enum AlphaModeLit
        {
            Alpha,
            Premultiply,
            Additive,
        }


        // Common surface config:
        //
        [SerializeField]
        SurfaceType m_SurfaceType;

        public SurfaceType surfaceType
        {
            get { return m_SurfaceType; }
            set
            {
                if (m_SurfaceType == value)
                    return;

                m_SurfaceType = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        AlphaMode m_AlphaMode;

        public AlphaMode alphaMode
        {
            get { return m_AlphaMode; }
            set
            {
                if (m_AlphaMode == value)
                    return;

                m_AlphaMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_BlendPreserveSpecular = true;

        public ToggleData blendPreserveSpecular
        {
            get { return new ToggleData(m_BlendPreserveSpecular); }
            set
            {
                if (m_BlendPreserveSpecular == value.isOn)
                    return;
                m_BlendPreserveSpecular = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_TransparencyFog = true;

        public ToggleData transparencyFog
        {
            get { return new ToggleData(m_TransparencyFog); }
            set
            {
                if (m_TransparencyFog == value.isOn)
                    return;
                m_TransparencyFog = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_Distortion;

        public ToggleData distortion
        {
            get { return new ToggleData(m_Distortion); }
            set
            {
                if (m_Distortion == value.isOn)
                    return;
                m_Distortion = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        DistortionMode m_DistortionMode;

        public DistortionMode distortionMode
        {
            get { return m_DistortionMode; }
            set
            {
                if (m_DistortionMode == value)
                    return;

                m_DistortionMode = value;
                UpdateNodeAfterDeserialization(); // TODOTODO: no need, ModificationScope.Graph is enough?
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_DistortionDepthTest = true;

        public ToggleData distortionDepthTest
        {
            get { return new ToggleData(m_DistortionDepthTest); }
            set
            {
                if (m_DistortionDepthTest == value.isOn)
                    return;
                m_DistortionDepthTest = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_AlphaTest;

        public ToggleData alphaTest
        {
            get { return new ToggleData(m_AlphaTest); }
            set
            {
                if (m_AlphaTest == value.isOn)
                    return;
                m_AlphaTest = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        int m_SortPriority;

        public int sortPriority
        {
            get { return m_SortPriority; }
            set
            {
                if (m_SortPriority == value)
                    return;
                m_SortPriority = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        DoubleSidedMode m_DoubleSidedMode;

        public DoubleSidedMode doubleSidedMode
        {
            get { return m_DoubleSidedMode; }
            set
            {
                if (m_DoubleSidedMode == value)
                    return;

                m_DoubleSidedMode = value;
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace;
        public NormalDropOffSpace normalDropOffSpace
        {
            get { return m_NormalDropOffSpace; }
            set
            {
                if (m_NormalDropOffSpace == value)
                    return;

                m_NormalDropOffSpace = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        // Features: material surface input parametrizations
        //
        [SerializeField]
        StackLit.BaseParametrization m_BaseParametrization;

        public StackLit.BaseParametrization baseParametrization
        {
            get { return m_BaseParametrization; }
            set
            {
                if (m_BaseParametrization == value)
                    return;

                m_BaseParametrization = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_EnergyConservingSpecular = true;

        public ToggleData energyConservingSpecular
        {
            get { return new ToggleData(m_EnergyConservingSpecular); }
            set
            {
                if (m_EnergyConservingSpecular == value.isOn)
                    return;
                m_EnergyConservingSpecular = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        StackLit.DualSpecularLobeParametrization m_DualSpecularLobeParametrization;

        public StackLit.DualSpecularLobeParametrization dualSpecularLobeParametrization
        {
            get { return m_DualSpecularLobeParametrization; }
            set
            {
                if (m_DualSpecularLobeParametrization == value)
                    return;

                m_DualSpecularLobeParametrization = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        // TODOTODO Change all to enable* ?

        // Features: "physical" material type enables
        //
        [SerializeField]
        bool m_Anisotropy;

        public ToggleData anisotropy
        {
            get { return new ToggleData(m_Anisotropy); }
            set
            {
                if (m_Anisotropy == value.isOn)
                    return;
                m_Anisotropy = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_Coat;

        public ToggleData coat
        {
            get { return new ToggleData(m_Coat); }
            set
            {
                if (m_Coat == value.isOn)
                    return;
                m_Coat = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_CoatNormal;

        public ToggleData coatNormal
        {
            get { return new ToggleData(m_CoatNormal); }
            set
            {
                if (m_CoatNormal == value.isOn)
                    return;
                m_CoatNormal = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_DualSpecularLobe;

        public ToggleData dualSpecularLobe
        {
            get { return new ToggleData(m_DualSpecularLobe); }
            set
            {
                if (m_DualSpecularLobe == value.isOn)
                    return;
                m_DualSpecularLobe = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_CapHazinessWrtMetallic = true;

        public ToggleData capHazinessWrtMetallic
        {
            get { return new ToggleData(m_CapHazinessWrtMetallic); }
            set
            {
                if (m_CapHazinessWrtMetallic == value.isOn)
                    return;
                m_CapHazinessWrtMetallic = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_Iridescence;

        public ToggleData iridescence
        {
            get { return new ToggleData(m_Iridescence); }
            set
            {
                if (m_Iridescence == value.isOn)
                    return;
                m_Iridescence = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_SubsurfaceScattering;

        public ToggleData subsurfaceScattering
        {
            get { return new ToggleData(m_SubsurfaceScattering); }
            set
            {
                if (m_SubsurfaceScattering == value.isOn)
                    return;
                m_SubsurfaceScattering = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_Transmission;

        public ToggleData transmission
        {
            get { return new ToggleData(m_Transmission); }
            set
            {
                if (m_Transmission == value.isOn)
                    return;
                m_Transmission = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        // Features: other options
        //
        [SerializeField]
        bool m_ReceiveDecals = true;

        public ToggleData receiveDecals
        {
            get { return new ToggleData(m_ReceiveDecals); }
            set
            {
                if (m_ReceiveDecals == value.isOn)
                    return;
                m_ReceiveDecals = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_ReceiveSSR = true;

        public ToggleData receiveSSR
        {
            get { return new ToggleData(m_ReceiveSSR); }
            set
            {
                if (m_ReceiveSSR == value.isOn)
                    return;
                m_ReceiveSSR = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_AddPrecomputedVelocity = false;

        public ToggleData addPrecomputedVelocity
        {
            get { return new ToggleData(m_AddPrecomputedVelocity); }
            set
            {
                if (m_AddPrecomputedVelocity == value.isOn)
                    return;
                m_AddPrecomputedVelocity = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_GeometricSpecularAA;

        public ToggleData geometricSpecularAA
        {
            get { return new ToggleData(m_GeometricSpecularAA); }
            set
            {
                if (m_GeometricSpecularAA == value.isOn)
                    return;
                m_GeometricSpecularAA = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        //[SerializeField]
        //bool m_SpecularOcclusion; // Main enable
        //
        //public ToggleData specularOcclusion
        //{
        //    get { return new ToggleData(m_SpecularOcclusion); }
        //    set
        //    {
        //        if (m_SpecularOcclusion == value.isOn)
        //            return;
        //        m_SpecularOcclusion = value.isOn;
        //        UpdateNodeAfterDeserialization();
        //        Dirty(ModificationScope.Topological);
        //    }
        //}

        [SerializeField]
        SpecularOcclusionBaseMode m_ScreenSpaceSpecularOcclusionBaseMode = SpecularOcclusionBaseMode.DirectFromAO;

        public SpecularOcclusionBaseMode screenSpaceSpecularOcclusionBaseMode
        {
            get { return m_ScreenSpaceSpecularOcclusionBaseMode; }
            set
            {
                if (m_ScreenSpaceSpecularOcclusionBaseMode == value)
                    return;

                m_ScreenSpaceSpecularOcclusionBaseMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        SpecularOcclusionBaseMode m_DataBasedSpecularOcclusionBaseMode;

        public SpecularOcclusionBaseMode dataBasedSpecularOcclusionBaseMode
        {
            get { return m_DataBasedSpecularOcclusionBaseMode; }
            set
            {
                if (m_DataBasedSpecularOcclusionBaseMode == value)
                    return;

                m_DataBasedSpecularOcclusionBaseMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        SpecularOcclusionAOConeSize m_ScreenSpaceSpecularOcclusionAOConeSize; // This is still provided to tweak the effect of SSAO on the SO.

        public SpecularOcclusionAOConeSize screenSpaceSpecularOcclusionAOConeSize
        {
            get { return m_ScreenSpaceSpecularOcclusionAOConeSize; }
            set
            {
                if (m_ScreenSpaceSpecularOcclusionAOConeSize == value)
                    return;

                m_ScreenSpaceSpecularOcclusionAOConeSize = value;
                Dirty(ModificationScope.Graph);
            }
        }

        // See SpecularOcclusionAOConeDir for why we need this only for SSAO-based SO:
        [SerializeField]
        SpecularOcclusionAOConeDir m_ScreenSpaceSpecularOcclusionAOConeDir;

        public SpecularOcclusionAOConeDir screenSpaceSpecularOcclusionAOConeDir
        {
            get { return m_ScreenSpaceSpecularOcclusionAOConeDir; }
            set
            {
                if (m_ScreenSpaceSpecularOcclusionAOConeDir == value)
                    return;

                m_ScreenSpaceSpecularOcclusionAOConeDir = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        SpecularOcclusionAOConeSize m_DataBasedSpecularOcclusionAOConeSize = SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO; // Only for SO methods using visibility cones (ie ConeCone and SPTD)

        public SpecularOcclusionAOConeSize dataBasedSpecularOcclusionAOConeSize
        {
            get { return m_DataBasedSpecularOcclusionAOConeSize; }
            set
            {
                if (m_DataBasedSpecularOcclusionAOConeSize == value)
                    return;

                m_DataBasedSpecularOcclusionAOConeSize = value;
                Dirty(ModificationScope.Graph);
            }
        }

        // TODO: this needs to be per lobe, less useful to have custom input.
        //[SerializeField]
        //bool m_SpecularOcclusionIsCustom; // allow custom input port for SO (replaces the data based one)
        //
        //public ToggleData specularOcclusionIsCustom
        //{
        //    get { return new ToggleData(m_SpecularOcclusionIsCustom); }
        //    set
        //    {
        //        if (m_SpecularOcclusionIsCustom == value.isOn)
        //            return;
        //        m_SpecularOcclusionIsCustom = value.isOn;
        //        UpdateNodeAfterDeserialization();
        //        Dirty(ModificationScope.Topological);
        //    }
        //}

        // SO Bent cone fixup is only for methods using visibility cone and only for the data based SO:
        [SerializeField]
        SpecularOcclusionConeFixupMethod m_SpecularOcclusionConeFixupMethod;

        public SpecularOcclusionConeFixupMethod specularOcclusionConeFixupMethod
        {
            get { return m_SpecularOcclusionConeFixupMethod; }
            set
            {
                if (m_SpecularOcclusionConeFixupMethod == value)
                    return;

                m_SpecularOcclusionConeFixupMethod = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        // Features: Advanced options
        //
        [SerializeField]
        bool m_AnisotropyForAreaLights = true;

        public ToggleData anisotropyForAreaLights
        {
            get { return new ToggleData(m_AnisotropyForAreaLights); }
            set
            {
                if (m_AnisotropyForAreaLights == value.isOn)
                    return;
                m_AnisotropyForAreaLights = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_RecomputeStackPerLight;

        public ToggleData recomputeStackPerLight
        {
            get { return new ToggleData(m_RecomputeStackPerLight); }
            set
            {
                if (m_RecomputeStackPerLight == value.isOn)
                    return;
                m_RecomputeStackPerLight = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_HonorPerLightMinRoughness;

        public ToggleData honorPerLightMinRoughness
        {
            get { return new ToggleData(m_HonorPerLightMinRoughness); }
            set
            {
                if (m_HonorPerLightMinRoughness == value.isOn)
                    return;
                m_HonorPerLightMinRoughness = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_ShadeBaseUsingRefractedAngles;

        public ToggleData shadeBaseUsingRefractedAngles
        {
            get { return new ToggleData(m_ShadeBaseUsingRefractedAngles); }
            set
            {
                if (m_ShadeBaseUsingRefractedAngles == value.isOn)
                    return;
                m_ShadeBaseUsingRefractedAngles = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_Debug;

        public ToggleData debug
        {
            get { return new ToggleData(m_Debug); }
            set
            {
                if (m_Debug == value.isOn)
                    return;
                m_Debug = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_DevMode;

        public ToggleData devMode
        {
            get { return new ToggleData(m_DevMode); }
            set
            {
                if (m_DevMode == value.isOn)
                    return;
                m_DevMode = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_overrideBakedGI;

        public ToggleData overrideBakedGI
        {
            get { return new ToggleData(m_overrideBakedGI); }
            set
            {
                if (m_overrideBakedGI == value.isOn)
                    return;
                m_overrideBakedGI = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_depthOffset;

        public ToggleData depthOffset
        {
            get { return new ToggleData(m_depthOffset); }
            set
            {
                if (m_depthOffset == value.isOn)
                    return;
                m_depthOffset = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        bool m_ZWrite;

        public ToggleData zWrite
        {
            get { return new ToggleData(m_ZWrite); }
            set
            {
                if (m_ZWrite == value.isOn)
                    return;
                m_ZWrite = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Topological);
            }
        }

        [SerializeField]
        TransparentCullMode m_transparentCullMode = TransparentCullMode.Back;
        public TransparentCullMode transparentCullMode
        {
            get => m_transparentCullMode;
            set
            {
                if (m_transparentCullMode == value)
                    return;

                m_transparentCullMode = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        CompareFunction m_ZTest = CompareFunction.LessEqual;
        public CompareFunction zTest
        {
            get => m_ZTest;
            set
            {
                if (m_ZTest == value)
                    return;

                m_ZTest = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        bool m_SupportLodCrossFade;

        public ToggleData supportLodCrossFade
        {
            get { return new ToggleData(m_SupportLodCrossFade); }
            set
            {
                if (m_SupportLodCrossFade == value.isOn)
                    return;
                m_SupportLodCrossFade = value.isOn;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Node);
            }
        }
        [SerializeField]
        bool m_DOTSInstancing = false;

        public ToggleData dotsInstancing
        {
            get { return new ToggleData(m_DOTSInstancing); }
            set
            {
                if (m_DOTSInstancing == value.isOn)
                    return;

                m_DOTSInstancing = value.isOn;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        int m_MaterialNeedsUpdateHash = 0;

        int ComputeMaterialNeedsUpdateHash()
        {
            int hash = 0;

            hash |= (alphaTest.isOn ? 0 : 1) << 0;
            hash |= (receiveSSR.isOn ? 0 : 1) << 2;
            hash |= (RequiresSplitLighting() ? 0 : 1) << 3;

            return hash;
        }

        public StackLitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return null; }
        }

        public bool HasDistortion()
        {
            return (surfaceType == SurfaceType.Transparent && distortion.isOn);
        }

        public static bool SpecularOcclusionModeUsesVisibilityCone(SpecularOcclusionBaseMode soMethod)
        {
            return (soMethod == SpecularOcclusionBaseMode.ConeConeFromBentAO
                || soMethod == SpecularOcclusionBaseMode.SPTDIntegrationOfBentAO);
        }

        public bool SpecularOcclusionUsesBentNormal()
        {
            return (SpecularOcclusionModeUsesVisibilityCone(dataBasedSpecularOcclusionBaseMode)
                    || (SpecularOcclusionModeUsesVisibilityCone(screenSpaceSpecularOcclusionBaseMode)
                        && screenSpaceSpecularOcclusionAOConeDir == SpecularOcclusionAOConeDir.BentNormal));
        }

        public bool DataBasedSpecularOcclusionIsCustom()
        {
            return dataBasedSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.Custom;
        }

        public static bool SpecularOcclusionConeFixupMethodModifiesRoughness(SpecularOcclusionConeFixupMethod soConeFixupMethod)
        {
            return (soConeFixupMethod == SpecularOcclusionConeFixupMethod.BoostBSDFRoughness
                || soConeFixupMethod == SpecularOcclusionConeFixupMethod.BoostAndTilt);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            base.UpdateNodeAfterDeserialization();
            name = "StackLit Master";

            List<int> validSlots = new List<int>();

            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotDisplayName, PositionSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            validSlots.Add(PositionSlotId);

            AddSlot(new NormalMaterialSlot(VertexNormalSlotId, VertexNormalSlotName, VertexNormalSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            validSlots.Add(VertexNormalSlotId);

            AddSlot(new TangentMaterialSlot(VertexTangentSlotId, VertexTangentSlotName, VertexTangentSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            validSlots.Add(VertexTangentSlotId);

            RemoveSlot(NormalSlotId);                
            var coordSpace = CoordinateSpace.Tangent;
            switch (m_NormalDropOffSpace)
            {
                case NormalDropOffSpace.Tangent:
                    coordSpace = CoordinateSpace.Tangent;
                    break;
                case NormalDropOffSpace.World:
                    coordSpace = CoordinateSpace.World;
                    break;
                case NormalDropOffSpace.Object:
                    coordSpace = CoordinateSpace.Object;
                    break;
            }
            AddSlot(new NormalMaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, coordSpace, ShaderStageCapability.Fragment));
            validSlots.Add(NormalSlotId);

            AddSlot(new NormalMaterialSlot(BentNormalSlotId, BentNormalSlotName, BentNormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
            validSlots.Add(BentNormalSlotId);

            AddSlot(new TangentMaterialSlot(TangentSlotId, TangentSlotName, TangentSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
            validSlots.Add(TangentSlotId);

            AddSlot(new ColorRGBMaterialSlot(BaseColorSlotId, BaseColorSlotName, BaseColorSlotName, SlotType.Input, Color.grey.gamma, ColorMode.Default, ShaderStageCapability.Fragment));
            validSlots.Add(BaseColorSlotId);

            if (baseParametrization == StackLit.BaseParametrization.BaseMetallic)
            {
                AddSlot(new Vector1MaterialSlot(MetallicSlotId, MetallicSlotName, MetallicSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(MetallicSlotId);
                AddSlot(new Vector1MaterialSlot(DielectricIorSlotId, DielectricIorSlotName, DielectricIorSlotName, SlotType.Input, 1.5f, ShaderStageCapability.Fragment));
                validSlots.Add(DielectricIorSlotId);
            }
            else if (baseParametrization == StackLit.BaseParametrization.SpecularColor)
            {
                AddSlot(new ColorRGBMaterialSlot(SpecularColorSlotId, SpecularColorSlotName, SpecularColorSlotName, SlotType.Input, Color.white, ColorMode.Default, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularColorSlotId);
            }

            AddSlot(new Vector1MaterialSlot(SmoothnessASlotId, SmoothnessASlotName, SmoothnessASlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
            validSlots.Add(SmoothnessASlotId);

            if (anisotropy.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AnisotropyASlotId, AnisotropyASlotName, AnisotropyASlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(AnisotropyASlotId);
            }

            AddSlot(new Vector1MaterialSlot(AmbientOcclusionSlotId, AmbientOcclusionSlotName, AmbientOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            validSlots.Add(AmbientOcclusionSlotId);

            // TODO: we would ideally need one value per lobe
            if (DataBasedSpecularOcclusionIsCustom())
            {
                AddSlot(new Vector1MaterialSlot(SpecularOcclusionSlotId, SpecularOcclusionSlotName, SpecularOcclusionSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularOcclusionSlotId);
            }

            if (SpecularOcclusionUsesBentNormal() && specularOcclusionConeFixupMethod != SpecularOcclusionConeFixupMethod.Off)
            {
                AddSlot(new Vector1MaterialSlot(SOFixupVisibilityRatioThresholdSlotId, SOFixupVisibilityRatioThresholdSlotName, SOFixupVisibilityRatioThresholdSlotName, SlotType.Input, 0.2f, ShaderStageCapability.Fragment));
                validSlots.Add(SOFixupVisibilityRatioThresholdSlotId);
                AddSlot(new Vector1MaterialSlot(SOFixupStrengthFactorSlotId, SOFixupStrengthFactorSlotName, SOFixupStrengthFactorSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SOFixupStrengthFactorSlotId);

                if (SpecularOcclusionConeFixupMethodModifiesRoughness(specularOcclusionConeFixupMethod))
                {
                    AddSlot(new Vector1MaterialSlot(SOFixupMaxAddedRoughnessSlotId, SOFixupMaxAddedRoughnessSlotName, SOFixupMaxAddedRoughnessSlotName, SlotType.Input, 0.2f, ShaderStageCapability.Fragment));
                    validSlots.Add(SOFixupMaxAddedRoughnessSlotId);
                }
            }

            if (coat.isOn)
            {
                AddSlot(new Vector1MaterialSlot(CoatSmoothnessSlotId, CoatSmoothnessSlotName, CoatSmoothnessSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(CoatSmoothnessSlotId);
                AddSlot(new Vector1MaterialSlot(CoatIorSlotId, CoatIorSlotName, CoatIorSlotName, SlotType.Input, 1.4f, ShaderStageCapability.Fragment));
                validSlots.Add(CoatIorSlotId);
                AddSlot(new Vector1MaterialSlot(CoatThicknessSlotId, CoatThicknessSlotName, CoatThicknessSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(CoatThicknessSlotId);
                AddSlot(new ColorRGBMaterialSlot(CoatExtinctionSlotId, CoatExtinctionSlotName, CoatExtinctionSlotName, SlotType.Input, Color.white, ColorMode.HDR, ShaderStageCapability.Fragment));
                validSlots.Add(CoatExtinctionSlotId);

                if (coatNormal.isOn)
                {
                    AddSlot(new NormalMaterialSlot(CoatNormalSlotId, CoatNormalSlotName, CoatNormalSlotName, CoordinateSpace.Tangent, ShaderStageCapability.Fragment));
                    validSlots.Add(CoatNormalSlotId);
                }

                AddSlot(new Vector1MaterialSlot(CoatMaskSlotId, CoatMaskSlotName, CoatMaskSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(CoatMaskSlotId);
            }

            if (dualSpecularLobe.isOn)
            {
                if (dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.Direct)
                {
                    AddSlot(new Vector1MaterialSlot(SmoothnessBSlotId, SmoothnessBSlotName, SmoothnessBSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                    validSlots.Add(SmoothnessBSlotId);
                    AddSlot(new Vector1MaterialSlot(LobeMixSlotId, LobeMixSlotName, LobeMixSlotName, SlotType.Input, 0.3f, ShaderStageCapability.Fragment));
                    validSlots.Add(LobeMixSlotId);
                }
                else if (dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss)
                {
                    AddSlot(new Vector1MaterialSlot(HazinessSlotId, HazinessSlotName, HazinessSlotName, SlotType.Input, 0.2f, ShaderStageCapability.Fragment));
                    validSlots.Add(HazinessSlotId);
                    AddSlot(new Vector1MaterialSlot(HazeExtentSlotId, HazeExtentSlotName, HazeExtentSlotName, SlotType.Input, 3.0f, ShaderStageCapability.Fragment));
                    validSlots.Add(HazeExtentSlotId);

                    if (capHazinessWrtMetallic.isOn && baseParametrization == StackLit.BaseParametrization.BaseMetallic) // the later should be an assert really
                    {
                        AddSlot(new Vector1MaterialSlot(HazyGlossMaxDielectricF0SlotId, HazyGlossMaxDielectricF0SlotName, HazyGlossMaxDielectricF0SlotName, SlotType.Input, 0.25f, ShaderStageCapability.Fragment));
                        validSlots.Add(HazyGlossMaxDielectricF0SlotId);
                    }
                }

                if (anisotropy.isOn)
                {
                    AddSlot(new Vector1MaterialSlot(AnisotropyBSlotId, AnisotropyBSlotName, AnisotropyBSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                    validSlots.Add(AnisotropyBSlotId);
                }
            }

            if (iridescence.isOn)
            {
                AddSlot(new Vector1MaterialSlot(IridescenceMaskSlotId, IridescenceMaskSlotName, IridescenceMaskSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(IridescenceMaskSlotId);
                AddSlot(new Vector1MaterialSlot(IridescenceThicknessSlotId, IridescenceThicknessSlotDisplayName, IridescenceThicknessSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(IridescenceThicknessSlotId);
                if (coat.isOn)
                {
                    AddSlot(new Vector1MaterialSlot(IridescenceCoatFixupTIRSlotId, IridescenceCoatFixupTIRSlotName, IridescenceCoatFixupTIRSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                    validSlots.Add(IridescenceCoatFixupTIRSlotId);
                    AddSlot(new Vector1MaterialSlot(IridescenceCoatFixupTIRClampSlotId, IridescenceCoatFixupTIRClampSlotName, IridescenceCoatFixupTIRClampSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                    validSlots.Add(IridescenceCoatFixupTIRClampSlotId);
                }
            }

            if (subsurfaceScattering.isOn)
            {
                AddSlot(new Vector1MaterialSlot(SubsurfaceMaskSlotId, SubsurfaceMaskSlotName, SubsurfaceMaskSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(SubsurfaceMaskSlotId);
            }

            if (transmission.isOn)
            {
                AddSlot(new Vector1MaterialSlot(ThicknessSlotId, ThicknessSlotName, ThicknessSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(ThicknessSlotId);
            }

            if (subsurfaceScattering.isOn || transmission.isOn)
            {
                AddSlot(new DiffusionProfileInputMaterialSlot(DiffusionProfileHashSlotId, DiffusionProfileHashSlotDisplayName, DiffusionProfileHashSlotName, ShaderStageCapability.Fragment));
                validSlots.Add(DiffusionProfileHashSlotId);
            }

            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
            validSlots.Add(AlphaSlotId);

            if (alphaTest.isOn)
            {
                AddSlot(new Vector1MaterialSlot(AlphaClipThresholdSlotId, AlphaClipThresholdSlotName, AlphaClipThresholdSlotName, SlotType.Input, 0.5f, ShaderStageCapability.Fragment));
                validSlots.Add(AlphaClipThresholdSlotId);
            }

            AddSlot(new ColorRGBMaterialSlot(EmissionSlotId, EmissionSlotName, EmissionSlotName, SlotType.Input, Color.black, ColorMode.HDR, ShaderStageCapability.Fragment));
            validSlots.Add(EmissionSlotId);

            if (HasDistortion())
            {
                AddSlot(new Vector2MaterialSlot(DistortionSlotId, DistortionSlotDisplayName, DistortionSlotName, SlotType.Input, new Vector2(2.0f, -1.0f), ShaderStageCapability.Fragment));
                validSlots.Add(DistortionSlotId);

                AddSlot(new Vector1MaterialSlot(DistortionBlurSlotId, DistortionBlurSlotName, DistortionBlurSlotName, SlotType.Input, 1.0f, ShaderStageCapability.Fragment));
                validSlots.Add(DistortionBlurSlotId);
            }

            if (geometricSpecularAA.isOn)
            {
                AddSlot(new Vector1MaterialSlot(SpecularAAScreenSpaceVarianceSlotId, SpecularAAScreenSpaceVarianceSlotName, SpecularAAScreenSpaceVarianceSlotName, SlotType.Input, 0.1f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularAAScreenSpaceVarianceSlotId);

                AddSlot(new Vector1MaterialSlot(SpecularAAThresholdSlotId, SpecularAAThresholdSlotName, SpecularAAThresholdSlotName, SlotType.Input, 0.2f, ShaderStageCapability.Fragment));
                validSlots.Add(SpecularAAThresholdSlotId);
            }

            if (overrideBakedGI.isOn)
            {
                AddSlot(new DefaultMaterialSlot(LightingSlotId, BakedGISlotName, BakedGISlotName, ShaderStageCapability.Fragment));
                validSlots.Add(LightingSlotId);
                AddSlot(new DefaultMaterialSlot(BackLightingSlotId, BakedBackGISlotName, BakedBackGISlotName, ShaderStageCapability.Fragment));
                validSlots.Add(BackLightingSlotId);
            }

            if (depthOffset.isOn)
            {
                AddSlot(new Vector1MaterialSlot(DepthOffsetSlotId, DepthOffsetSlotName, DepthOffsetSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                validSlots.Add(DepthOffsetSlotId);
            }

            RemoveSlotsNameNotMatching(validSlots, true);
        }

        public VisualElement CreateSettingsElement()
        {
            return new StackLitSettingsView(this);
        }

        public string renderQueueTag
        {
            get
            {
                var renderingPass = surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
                int queue = HDRenderQueue.ChangeType(renderingPass, sortPriority, alphaTest.isOn);
                return HDRenderQueue.GetShaderTagValue(queue);
            }
        }

        public string renderTypeTag => HDRenderTypeTags.HDLitShader.ToString();

        // Reference for GetConditionalFields
        // -------------------------------------------
        //
        // Properties (enables etc):
        //
        //  ok+MFD -> material feature define: means we need a predicate, because we will transform it into a #define that match the material feature, shader_feature-defined, that the rest of the shader code uses.
        //
        //  ok+MFD masterNode.baseParametrization    --> even though we can just always transfer present fields (check with $SurfaceDescription.*) like specularcolor and metallic,
        //                                               we need to translate this into the _MATERIAL_FEATURE_SPECULAR_COLOR define.
        //
        //  ok masterNode.energyConservingSpecular
        //
        //  ~~~~ ok+MFD: these are almost all material features:
        //  masterNode.anisotropy
        //  masterNode.coat
        //  masterNode.coatNormal
        //  masterNode.dualSpecularLobe
        //  masterNode.dualSpecularLobeParametrization
        //  masterNode.capHazinessWrtMetallic           -> not a material feature define, as such, we will create a combined predicate for the HazyGlossMaxDielectricF0 slot dependency
        //                                                 instead of adding a #define in the template...
        //  masterNode.iridescence
        //  masterNode.subsurfaceScattering
        //  masterNode.transmission
        //
        //  ~~~~ ...ok+MFD: these are all material features
        //
        //  ok masterNode.receiveDecals
        //  ok masterNode.receiveSSR
        //  ok masterNode.geometricSpecularAA    --> check, a way to combine predicates and/or exclude passes: TODOTODO What about WRITE_NORMAL_BUFFER passes ? (ie smoothness)
        //  ok masterNode.specularOcclusion      --> no use for it though! see comments.
        //
        //  ~~~~ ok+D: these require translation to defines also...
        //
        //  masterNode.anisotropyForAreaLights
        //  masterNode.recomputeStackPerLight
        //  masterNode.shadeBaseUsingRefractedAngles
        //  masterNode.debug

        // Inputs: Most inputs don't need a specific predicate in addition to the "present field predicate", ie the $SurfaceDescription.*,
        //         but in some special cases we check connectivity to avoid processing the default value for nothing...
        //         (see specular occlusion with _MASKMAP and _BENTNORMALMAP in LitData, or _TANGENTMAP, _BENTNORMALMAP, etc. which act a bit like that
        //         although they also avoid sampling in that case, but default tiny texture map sampling isn't a big hit since they are all cached once
        //         a default "unityTexWhite" is sampled, it is cached for everyone defaulting to white...)
        //
        // ok+ means there's a specific additional predicate
        //
        // ok masterNode.BaseColorSlotId
        // ok masterNode.NormalSlotId
        //
        // ok+ masterNode.BentNormalSlotId     --> Dependency of the predicate on IsSlotConnected avoids processing even if the slots
        // ok+ masterNode.TangentSlotId            are always there so any pass that declares its use in PixelShaderSlots will have the field in SurfaceDescription,
        //                                         but it's not necessarily useful (if slot isnt connected, waste processing on potentially static expressions if
        //                                         shader compiler cant optimize...and even then, useless to have static override value for those.)
        //
        //                                         TODOTODO: Note you could have the same argument for NormalSlot (which we dont exclude with a predicate).
        //                                         Also and anyways, the compiler is smart enough not to do the TS to WS matrix multiply on a (0,0,1) vector.
        //
        // ok+ masterNode.CoatNormalSlotId       -> we already have a "material feature" coat normal map so can use that instead, although using that former, we assume the coat normal slot
        //                                         will be there, but it's ok, we can #ifdef the code on the material feature define, and use the $SurfaceDescription.CoatNormal predicate
        //                                         for the actual assignment,
        //                                         although for that one we could again
        //                                         use the "connected" condition like for tangent and bentnormal
        //
        // The following are all ok, no need beyond present field predicate, ie $SurfaceDescription.*,
        // except special cases where noted
        //
        // ok masterNode.SubsurfaceMaskSlotId
        // ok masterNode.ThicknessSlotId
        // ok masterNode.DiffusionProfileHashSlotId
        // ok masterNode.IridescenceMaskSlotId
        // ok masterNode.IridescenceThicknessSlotId
        // ok masterNode.SpecularColorSlotId
        // ok masterNode.DielectricIorSlotId
        // ok masterNode.MetallicSlotId
        // ok masterNode.EmissionSlotId
        // ok masterNode.SmoothnessASlotId
        // ok masterNode.SmoothnessBSlotId
        // ok+ masterNode.AmbientOcclusionSlotId    -> defined a specific predicate, but not used, see StackLitData.
        // ok masterNode.AlphaSlotId
        // ok masterNode.AlphaClipThresholdSlotId
        // ok masterNode.AnisotropyASlotId
        // ok masterNode.AnisotropyBSlotId
        // ok masterNode.SpecularAAScreenSpaceVarianceSlotId
        // ok masterNode.SpecularAAThresholdSlotId
        // ok masterNode.CoatSmoothnessSlotId
        // ok masterNode.CoatIorSlotId
        // ok masterNode.CoatThicknessSlotId
        // ok masterNode.CoatExtinctionSlotId
        // ok masterNode.LobeMixSlotId
        // ok masterNode.HazinessSlotId
        // ok masterNode.HazeExtentSlotId
        // ok masterNode.HazyGlossMaxDielectricF0SlotId     -> No need for a predicate, the needed predicate is the combined (capHazinessWrtMetallic + HazyGlossMaxDielectricF0)
        //                                                     "leaking case": if the 2 are true, but we're not in metallic mode, the capHazinessWrtMetallic property is wrong,
        //                                                     that means the master node is really misconfigured, spew an error, should never happen...
        //                                                     If it happens, it's because we forgot UpdateNodeAfterDeserialization() call when modifying the capHazinessWrtMetallic or baseParametrization
        //                                                     properties, maybe through debug etc.
        //
        // ok masterNode.DistortionSlotId            -> Warning: peculiarly, instead of using $SurfaceDescription.Distortion and DistortionBlur,
        // ok masterNode.DistortionBlurSlotId           we do an #if (SHADERPASS == SHADERPASS_DISTORTION) in the template, instead of
        //                                              relying on other passed NOT to include the DistortionSlotId in their PixelShaderSlots!!

        // Other to deal with, and
        // Common between Lit and StackLit:
        //
        // doubleSidedMode, alphaTest, receiveDecals,
        // surfaceType, alphaMode, blendPreserveSpecular, transparencyFog,
        // distortion, distortionMode, distortionDepthTest,
        // sortPriority (int)
        // geometricSpecularAA, energyConservingSpecular, specularOcclusion

        public ConditionalField[] GetConditionalFields(PassDescriptor pass)
        {
            var ambientOcclusionSlot = FindSlot<Vector1MaterialSlot>(AmbientOcclusionSlotId);

            return new ConditionalField[]
            {
                // Features
                new ConditionalField(Fields.GraphVertex,                    IsSlotConnected(PositionSlotId) || 
                                                                                IsSlotConnected(VertexNormalSlotId) || 
                                                                                IsSlotConnected(VertexTangentSlotId)),
                new ConditionalField(Fields.GraphPixel,                     true),

                // Surface Type
                new ConditionalField(Fields.SurfaceOpaque,                  surfaceType == SurfaceType.Opaque),
                new ConditionalField(Fields.SurfaceTransparent,             surfaceType != SurfaceType.Opaque),

                // Structs
                new ConditionalField(HDStructFields.FragInputs.IsFrontFace,doubleSidedMode != DoubleSidedMode.Disabled &&
                                                                                !pass.Equals(HDPasses.StackLit.MotionVectors)),

                // Material
                new ConditionalField(HDFields.Anisotropy,                   anisotropy.isOn),
                new ConditionalField(HDFields.Coat,                         coat.isOn),
                new ConditionalField(HDFields.CoatMask,                     coat.isOn && pass.pixelPorts.Contains(CoatMaskSlotId) &&
                                                                                (IsSlotConnected(CoatMaskSlotId) || 
                                                                                (FindSlot<Vector1MaterialSlot>(CoatMaskSlotId).value != 0.0f &&
                                                                                FindSlot<Vector1MaterialSlot>(CoatMaskSlotId).value != 1.0f))),
                new ConditionalField(HDFields.CoatMaskZero,                 coat.isOn && pass.pixelPorts.Contains(CoatMaskSlotId) &&
                                                                                FindSlot<Vector1MaterialSlot>(CoatMaskSlotId).value == 0.0f),
                new ConditionalField(HDFields.CoatMaskOne,                  coat.isOn && pass.pixelPorts.Contains(CoatMaskSlotId) &&
                                                                                FindSlot<Vector1MaterialSlot>(CoatMaskSlotId).value == 1.0f),
                new ConditionalField(HDFields.CoatNormal,                   coatNormal.isOn && pass.pixelPorts.Contains(CoatNormalSlotId)),
                new ConditionalField(HDFields.Iridescence,                  iridescence.isOn),
                new ConditionalField(HDFields.SubsurfaceScattering,         subsurfaceScattering.isOn && surfaceType != SurfaceType.Transparent),
                new ConditionalField(HDFields.Transmission,                 transmission.isOn),
                new ConditionalField(HDFields.DualSpecularLobe,             dualSpecularLobe.isOn),

                // Normal Drop Off Space
                new ConditionalField(Fields.NormalDropOffOS,                normalDropOffSpace == NormalDropOffSpace.Object),
                new ConditionalField(Fields.NormalDropOffTS,                normalDropOffSpace == NormalDropOffSpace.Tangent),
                new ConditionalField(Fields.NormalDropOffWS,                normalDropOffSpace == NormalDropOffSpace.World),

                // Distortion
                new ConditionalField(HDFields.DistortionDepthTest,          distortionDepthTest.isOn),
                new ConditionalField(HDFields.DistortionAdd,                distortionMode == DistortionMode.Add),
                new ConditionalField(HDFields.DistortionMultiply,           distortionMode == DistortionMode.Multiply),
                new ConditionalField(HDFields.DistortionReplace,            distortionMode == DistortionMode.Replace),
                new ConditionalField(HDFields.TransparentDistortion,        surfaceType != SurfaceType.Opaque && distortion.isOn),

                // Base Parametrization
                // Even though we can just always transfer the present (check with $SurfaceDescription.*) fields like specularcolor
                // and metallic, we still need to know the baseParametrization in the template to translate into the
                // _MATERIAL_FEATURE_SPECULAR_COLOR define:
                new ConditionalField(HDFields.BaseParamSpecularColor,       baseParametrization == StackLit.BaseParametrization.SpecularColor),

                // Dual Specular Lobe Parametrization
                new ConditionalField(HDFields.HazyGloss,                    dualSpecularLobe.isOn && 
                                                                                dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss),

                // Misc
                new ConditionalField(Fields.AlphaTest,                      alphaTest.isOn && pass.pixelPorts.Contains(AlphaClipThresholdSlotId)),
                new ConditionalField(HDFields.AlphaFog,                     surfaceType != SurfaceType.Opaque && transparencyFog.isOn),
                new ConditionalField(HDFields.BlendPreserveSpecular,        surfaceType != SurfaceType.Opaque && blendPreserveSpecular.isOn),
                new ConditionalField(HDFields.EnergyConservingSpecular,     energyConservingSpecular.isOn),
                new ConditionalField(HDFields.DisableDecals,                !receiveDecals.isOn),
                new ConditionalField(HDFields.DisableSSR,                   !receiveSSR.isOn),
                new ConditionalField(Fields.VelocityPrecomputed,            addPrecomputedVelocity.isOn),
                new ConditionalField(HDFields.BentNormal,                   IsSlotConnected(BentNormalSlotId) && 
                                                                                pass.pixelPorts.Contains(BentNormalSlotId)),
                new ConditionalField(HDFields.AmbientOcclusion,             pass.pixelPorts.Contains(AmbientOcclusionSlotId) &&
                                                                                (IsSlotConnected(AmbientOcclusionSlotId) ||
                                                                                ambientOcclusionSlot.value != ambientOcclusionSlot.defaultValue)),
                new ConditionalField(HDFields.Tangent,                      IsSlotConnected(TangentSlotId) && 
                                                                                pass.pixelPorts.Contains(TangentSlotId)),
                new ConditionalField(HDFields.LightingGI,                   IsSlotConnected(LightingSlotId) && 
                                                                                pass.pixelPorts.Contains(LightingSlotId)),
                new ConditionalField(HDFields.BackLightingGI,               IsSlotConnected(BackLightingSlotId) && 
                                                                                pass.pixelPorts.Contains(BackLightingSlotId)),
                new ConditionalField(HDFields.DepthOffset,                  depthOffset.isOn && pass.pixelPorts.Contains(DepthOffsetSlotId)),
                // Option for baseParametrization == Metallic && DualSpecularLobeParametrization == HazyGloss:
                // Again we assume masternode has HazyGlossMaxDielectricF0 which should always be the case
                // if capHazinessWrtMetallic.isOn.
                new ConditionalField(HDFields.CapHazinessIfNotMetallic,     dualSpecularLobe.isOn && 
                                                                                dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss &&
                                                                                capHazinessWrtMetallic.isOn && baseParametrization == StackLit.BaseParametrization.BaseMetallic
                                                                                && pass.pixelPorts.Contains(HazyGlossMaxDielectricF0SlotId)),
                // Note here we combine an "enable"-like predicate and the $SurfaceDescription.(slotname) predicate
                // into a single $GeometricSpecularAA pedicate.
                //
                // ($SurfaceDescription.* predicates are useful to make sure the field is present in the struct in the template.
                // The field will be present if both the master node and pass have the slotid, see this set intersection we make
                // in GenerateSurfaceDescriptionStruct(), with HDSubShaderUtilities.FindMaterialSlotsOnNode().)
                //
                // Normally, since the feature enable adds the required slots, only the $SurfaceDescription.* would be required,
                // but some passes might not need it and not declare the PixelShaderSlot, or, inversely, the pass might not
                // declare it as a way to avoid it.
                //
                // IE this has also the side effect to disable geometricSpecularAA - even if "on" - for passes that don't explicitly
                // advertise these slots(eg for a general feature, with separate "enable" and "field present" predicates, the
                // template could take a default value and process it anyway if a feature is "on").
                //
                // (Note we can achieve the same results in the template on just single predicates by making defines out of them,
                // and using #if defined() && etc)
                new ConditionalField(HDFields.GeometricSpecularAA,          geometricSpecularAA.isOn &&
                                                                                pass.pixelPorts.Contains(SpecularAAThresholdSlotId) &&
                                                                                pass.pixelPorts.Contains(SpecularAAScreenSpaceVarianceSlotId)),
                new ConditionalField(HDFields.SpecularAA,                   geometricSpecularAA.isOn &&
                                                                                pass.pixelPorts.Contains(SpecularAAThresholdSlotId) &&
                                                                                pass.pixelPorts.Contains(SpecularAAScreenSpaceVarianceSlotId)),
                new ConditionalField(HDFields.SpecularOcclusion,            screenSpaceSpecularOcclusionBaseMode != SpecularOcclusionBaseMode.Off ||
                                                                                dataBasedSpecularOcclusionBaseMode != SpecularOcclusionBaseMode.Off),

                // Advanced
                new ConditionalField(HDFields.AnisotropyForAreaLights,      anisotropyForAreaLights.isOn),
                new ConditionalField(HDFields.RecomputeStackPerLight,       recomputeStackPerLight.isOn),
                new ConditionalField(HDFields.HonorPerLightMinRoughness,    honorPerLightMinRoughness.isOn),
                new ConditionalField(HDFields.ShadeBaseUsingRefractedAngles, shadeBaseUsingRefractedAngles.isOn),
                new ConditionalField(HDFields.StackLitDebug,                debug.isOn),

                // Screen Space Specular Occlusion Base Mode
                new ConditionalField(HDFields.SSSpecularOcclusionBaseModeOff, screenSpaceSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.Off),
                new ConditionalField(HDFields.SSSpecularOcclusionBaseModeDirectFromAO, screenSpaceSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.DirectFromAO),
                new ConditionalField(HDFields.SSSpecularOcclusionBaseModeConeConeFromBentAO, screenSpaceSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.ConeConeFromBentAO),
                new ConditionalField(HDFields.SSSpecularOcclusionBaseModeSPTDIntegrationOfBentAO, screenSpaceSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.SPTDIntegrationOfBentAO),
                new ConditionalField(HDFields.SSSpecularOcclusionBaseModeCustom, screenSpaceSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.Custom),

                // Screen Space Specular Occlusion AO Cone Size
                new ConditionalField(HDFields.SSSpecularOcclusionAOConeSizeUniformAO, SpecularOcclusionModeUsesVisibilityCone(screenSpaceSpecularOcclusionBaseMode) &&
                                                                                screenSpaceSpecularOcclusionAOConeSize == SpecularOcclusionAOConeSize.UniformAO),
                new ConditionalField(HDFields.SSSpecularOcclusionAOConeSizeCosWeightedAO, SpecularOcclusionModeUsesVisibilityCone(screenSpaceSpecularOcclusionBaseMode) &&
                                                                                screenSpaceSpecularOcclusionAOConeSize == SpecularOcclusionAOConeSize.CosWeightedAO),
                new ConditionalField(HDFields.SSSpecularOcclusionAOConeSizeCosWeightedBentCorrectAO, SpecularOcclusionModeUsesVisibilityCone(screenSpaceSpecularOcclusionBaseMode) &&
                                                                                screenSpaceSpecularOcclusionAOConeSize == SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO),

                // Screen Space Specular Occlusion AO Cone Dir
                new ConditionalField(HDFields.SSSpecularOcclusionAOConeDirGeomNormal, SpecularOcclusionModeUsesVisibilityCone(screenSpaceSpecularOcclusionBaseMode) &&
                                                                                screenSpaceSpecularOcclusionAOConeDir == SpecularOcclusionAOConeDir.GeomNormal),
                new ConditionalField(HDFields.SSSpecularOcclusionAOConeDirBentNormal, SpecularOcclusionModeUsesVisibilityCone(screenSpaceSpecularOcclusionBaseMode) &&
                                                                                screenSpaceSpecularOcclusionAOConeDir == SpecularOcclusionAOConeDir.BentNormal),
                new ConditionalField(HDFields.SSSpecularOcclusionAOConeDirShadingNormal, SpecularOcclusionModeUsesVisibilityCone(screenSpaceSpecularOcclusionBaseMode) &&
                                                                                screenSpaceSpecularOcclusionAOConeDir == SpecularOcclusionAOConeDir.ShadingNormal),

                // Data Based Specular Occlusion Base Mode
                new ConditionalField(HDFields.DataBasedSpecularOcclusionBaseModeOff, dataBasedSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.Off),
                new ConditionalField(HDFields.DataBasedSpecularOcclusionBaseModeDirectFromAO, dataBasedSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.DirectFromAO),
                new ConditionalField(HDFields.DataBasedSpecularOcclusionBaseModeConeConeFromBentAO, dataBasedSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.ConeConeFromBentAO),
                new ConditionalField(HDFields.DataBasedSpecularOcclusionBaseModeSPTDIntegrationOfBentAO, dataBasedSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.SPTDIntegrationOfBentAO),
                new ConditionalField(HDFields.DataBasedSpecularOcclusionBaseModeCustom, dataBasedSpecularOcclusionBaseMode == SpecularOcclusionBaseMode.Custom),

                // Data Based Specular Occlusion AO Cone Size
                new ConditionalField(HDFields.DataBasedSpecularOcclusionAOConeSizeUniformAO, SpecularOcclusionModeUsesVisibilityCone(dataBasedSpecularOcclusionBaseMode) &&
                                                                                dataBasedSpecularOcclusionAOConeSize == SpecularOcclusionAOConeSize.UniformAO),
                new ConditionalField(HDFields.DataBasedSpecularOcclusionAOConeSizeCosWeightedAO, SpecularOcclusionModeUsesVisibilityCone(dataBasedSpecularOcclusionBaseMode) &&
                                                                                dataBasedSpecularOcclusionAOConeSize == SpecularOcclusionAOConeSize.CosWeightedAO),
                new ConditionalField(HDFields.DataBasedSpecularOcclusionAOConeSizeCosWeightedBentCorrectAO, SpecularOcclusionModeUsesVisibilityCone(dataBasedSpecularOcclusionBaseMode) &&
                                                                                dataBasedSpecularOcclusionAOConeSize == SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO),

                // Specular Occlusion Cone Fixup Method
                new ConditionalField(HDFields.SpecularOcclusionConeFixupMethodOff, SpecularOcclusionUsesBentNormal() &&
                                                                                specularOcclusionConeFixupMethod == SpecularOcclusionConeFixupMethod.Off),
                new ConditionalField(HDFields.SpecularOcclusionConeFixupMethodBoostBSDFRoughness, SpecularOcclusionUsesBentNormal() &&
                                                                                specularOcclusionConeFixupMethod == SpecularOcclusionConeFixupMethod.BoostBSDFRoughness),
                new ConditionalField(HDFields.SpecularOcclusionConeFixupMethodTiltDirectionToGeomNormal, SpecularOcclusionUsesBentNormal() &&
                                                                                specularOcclusionConeFixupMethod == SpecularOcclusionConeFixupMethod.TiltDirectionToGeomNormal),
                new ConditionalField(HDFields.SpecularOcclusionConeFixupMethodBoostAndTilt, SpecularOcclusionUsesBentNormal() &&
                                                                                specularOcclusionConeFixupMethod == SpecularOcclusionConeFixupMethod.BoostAndTilt),
            };
        }

        public void ProcessPreviewMaterial(Material material)
        {
            // Fixup the material settings:
            material.SetFloat(kSurfaceType, (int)(SurfaceType)surfaceType);
            material.SetFloat(kDoubleSidedNormalMode, (int)doubleSidedMode);
            material.SetFloat(kDoubleSidedEnable, doubleSidedMode != DoubleSidedMode.Disabled ? 1.0f : 0.0f);
            material.SetFloat(kAlphaCutoffEnabled, alphaTest.isOn ? 1 : 0);
            material.SetFloat(kBlendMode, (int)HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode));
            material.SetFloat(kEnableFogOnTransparent, transparencyFog.isOn ? 1.0f : 0.0f);
            material.SetFloat(kZTestTransparent, (int)zTest);
            material.SetFloat(kTransparentCullMode, (int)transparentCullMode);
            material.SetFloat(kZWrite, zWrite.isOn ? 1.0f : 0.0f);
            // No sorting priority for shader graph preview
            var renderingPass = surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            material.renderQueue = (int)HDRenderQueue.ChangeType(renderingPass, offset: 0, alphaTest: alphaTest.isOn);

            StackLitGUI.SetupMaterialKeywordsAndPass(material);
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetSlots(slots);

            List<MaterialSlot> validSlots = new List<MaterialSlot>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].stageCapability != ShaderStageCapability.All && slots[i].stageCapability != stageCapability)
                    continue;

                validSlots.Add(slots[i]);
            }
            return validSlots.OfType<IMayRequireNormal>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresNormal(stageCapability));
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetSlots(slots);

            List<MaterialSlot> validSlots = new List<MaterialSlot>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].stageCapability != ShaderStageCapability.All && slots[i].stageCapability != stageCapability)
                    continue;

                validSlots.Add(slots[i]);
            }
            return validSlots.OfType<IMayRequireTangent>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresTangent(stageCapability));
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetSlots(slots);

            List<MaterialSlot> validSlots = new List<MaterialSlot>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].stageCapability != ShaderStageCapability.All && slots[i].stageCapability != stageCapability)
                    continue;

                validSlots.Add(slots[i]);
            }
            return validSlots.OfType<IMayRequirePosition>().Aggregate(NeededCoordinateSpace.None, (mask, node) => mask | node.RequiresPosition(stageCapability));
        }

        public bool RequiresSplitLighting()
        {
            return subsurfaceScattering.isOn;
        }

        public override object saveContext
        {
            get
            {
                int hash = ComputeMaterialNeedsUpdateHash();

                bool needsUpdate = hash != m_MaterialNeedsUpdateHash;

                if (needsUpdate)
                    m_MaterialNeedsUpdateHash = hash;

                return new HDSaveContext{ updateMaterials = needsUpdate };
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            if (debug.isOn)
            {
                // We have useful debug options in StackLit, so add them always, and let the UI editor (non shadergraph) handle displaying them
                // since this is also the editor that controls the keyword switching for the debug mode.
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugEnvLobeMask", // xyz is environments lights lobe 0 1 2 Enable, w is Enable VLayering
                    displayName = "_DebugEnvLobeMask",
                    value = new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                });
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugLobeMask", // xyz is analytical dirac lights lobe 0 1 2 Enable", false),
                    displayName = "_DebugLobeMask",
                    value = new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                });
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugAniso", // x is Hack Enable, w is factor
                    displayName = "_DebugAniso",
                    value = new Vector4(1.0f, 0.0f, 0.0f, 1000.0f)
                });
                // _DebugSpecularOcclusion:
                //
                // eg (2,2,1,2) :
                // .x = SO method {0 = fromAO, 1 = conecone, 2 = SPTD},
                // .y = bentao algo {0 = uniform, cos, bent cos},
                // .z = use upper visible hemisphere clipping,
                // .w = The last component of _DebugSpecularOcclusion controls debug visualization:
                //      -1 colors the object according to the SO algorithm used,
                //      and values from 1 to 4 controls what the lighting debug display mode will show when set to show "indirect specular occlusion":
                //      Since there's not one value in our case,
                //      0 will show the object all red to indicate to choose one, 1-4 corresponds to showing
                //      1 = coat SO, 2 = base lobe A SO, 3 = base lobe B SO, 4 = shows the result of sampling the SSAO texture (screenSpaceAmbientOcclusion).
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugSpecularOcclusion",
                    displayName = "_DebugSpecularOcclusion",
                    value = new Vector4(2.0f, 2.0f, 1.0f, 2.0f)
                });
            }

            // Trunk currently relies on checking material property "_EmissionColor" to allow emissive GI. If it doesn't find that property, or it is black, GI is forced off.
            // ShaderGraph doesn't use this property, so currently it inserts a dummy color (white). This dummy color may be removed entirely once the following PR has been merged in trunk: Pull request #74105
            // The user will then need to explicitly disable emissive GI if it is not needed.
            // To be able to automatically disable emission based on the ShaderGraph config when emission is black,
            // we will need a more general way to communicate this to the engine (not directly tied to a material property).
            collector.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = "_EmissionColor",
                hidden = true,
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f)
            });

            //See SG-ADDITIONALVELOCITY-NOTE
            if (addPrecomputedVelocity.isOn)
            {
                collector.AddShaderProperty(new BooleanShaderProperty
                {
                    value = true,
                    hidden = true,
                    overrideReferenceName = kAddPrecomputedVelocity,
                });
            }

            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddStencilShaderProperties(collector, RequiresSplitLighting(), receiveSSR.isOn);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                surfaceType,
                HDSubShaderUtilities.ConvertAlphaModeToBlendMode(alphaMode),
                sortPriority,
                zWrite.isOn,
                transparentCullMode,
                zTest,
                false,
                transparencyFog.isOn
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, alphaTest.isOn, false);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, doubleSidedMode);

            base.CollectShaderProperties(collector, generationMode);
        }
    }
}
