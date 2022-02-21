using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.StackLitMasterNode")]
    [FormerName("UnityEditor.Rendering.HighDefinition.StackLitMasterNode")]
    [FormerName("UnityEditor.ShaderGraph.StackLitMasterNode")]
    class StackLitMasterNode1 : AbstractMaterialNode, IMasterNode1
    {
        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum AlphaMode
        {
            Alpha,
            Premultiply,
            Additive,
        }

        public enum SpecularOcclusionBaseMode
        {
            Off = 0,
            DirectFromAO = 1, // TriACE
            ConeConeFromBentAO = 2,
            SPTDIntegrationOfBentAO = 3,
            Custom = 4,
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
        public const int SpecularOcclusionSlotId = 43; // for custom (external) SO replacing data based SO (which normally comes from some func of DataBasedSOMode(dataAO, optional bent normal))

        public SurfaceType m_SurfaceType;
        public AlphaMode m_AlphaMode;
        public bool m_BlendPreserveSpecular;
        public bool m_TransparencyFog;
        public bool m_Distortion;
        public DistortionMode m_DistortionMode;
        public bool m_DistortionDepthTest;
        public bool m_AlphaTest;
        public int m_SortPriority;
        public DoubleSidedMode m_DoubleSidedMode;
        public NormalDropOffSpace m_NormalDropOffSpace;
        public StackLit.BaseParametrization m_BaseParametrization;
        public bool m_EnergyConservingSpecular;
        public StackLit.DualSpecularLobeParametrization m_DualSpecularLobeParametrization;
        public bool m_Anisotropy;
        public bool m_Coat;
        public bool m_CoatNormal;
        public bool m_DualSpecularLobe;
        public bool m_CapHazinessWrtMetallic;
        public bool m_Iridescence;
        public bool m_SubsurfaceScattering;
        public bool m_Transmission;
        public bool m_ReceiveDecals;
        public bool m_ReceiveSSR;
        public bool m_ReceivesSSRTransparent;
        public bool m_AddPrecomputedVelocity;
        public bool m_GeometricSpecularAA;
        public SpecularOcclusionBaseMode m_ScreenSpaceSpecularOcclusionBaseMode;
        public SpecularOcclusionBaseMode m_DataBasedSpecularOcclusionBaseMode;
        public SpecularOcclusionAOConeSize m_ScreenSpaceSpecularOcclusionAOConeSize; // This is still provided to tweak the effect of SSAO on the SO.
        public SpecularOcclusionAOConeDir m_ScreenSpaceSpecularOcclusionAOConeDir;
        public SpecularOcclusionAOConeSize m_DataBasedSpecularOcclusionAOConeSize; // Only for SO methods using visibility cones (ie ConeCone and SPTD)
        public SpecularOcclusionConeFixupMethod m_SpecularOcclusionConeFixupMethod;
        public bool m_AnisotropyForAreaLights;
        public bool m_RecomputeStackPerLight;
        public bool m_HonorPerLightMinRoughness;
        public bool m_ShadeBaseUsingRefractedAngles;
        public bool m_Debug;
        public bool m_DevMode;
        public bool m_overrideBakedGI;
        public bool m_depthOffset;
        public bool m_ZWrite;
        public TransparentCullMode m_transparentCullMode;
        public CompareFunction m_ZTest;
        public bool m_SupportLodCrossFade;
        public bool m_DOTSInstancing;
        public int m_MaterialNeedsUpdateHash;
        public string m_ShaderGUIOverride;
        public bool m_OverrideEnabled;
    }
}
