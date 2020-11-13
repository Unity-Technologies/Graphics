using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HDLitMasterNode")]
    [FormerName("UnityEditor.Rendering.HighDefinition.HDLitMasterNode")]
    [FormerName("UnityEditor.ShaderGraph.HDLitMasterNode")]
    class HDLitMasterNode1 : AbstractMaterialNode, IMasterNode1
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

        public enum MaterialType
        {
            Standard,
            SubsurfaceScattering,
            Anisotropy,
            Iridescence,
            SpecularColor,
            Translucent
        }

        public const int PositionSlotId = 0;
        public const int AlbedoSlotId = 1;
        public const int NormalSlotId = 2;
        public const int BentNormalSlotId = 3;
        public const int TangentSlotId = 4;
        public const int SubsurfaceMaskSlotId = 5;
        public const int ThicknessSlotId = 6;
        public const int DiffusionProfileHashSlotId = 7;
        public const int IridescenceMaskSlotId = 8;
        public const int IridescenceThicknessSlotId = 9;
        public const int SpecularColorSlotId = 10;
        public const int CoatMaskSlotId = 11;
        public const int MetallicSlotId = 12;
        public const int EmissionSlotId = 13;
        public const int SmoothnessSlotId = 14;
        public const int AmbientOcclusionSlotId = 15;
        public const int AlphaSlotId = 16;
        public const int AlphaThresholdSlotId = 17;
        public const int AlphaThresholdDepthPrepassSlotId = 18;
        public const int AlphaThresholdDepthPostpassSlotId = 19;
        public const int AnisotropySlotId = 20;
        public const int SpecularAAScreenSpaceVarianceSlotId = 21;
        public const int SpecularAAThresholdSlotId = 22;
        public const int RefractionIndexSlotId = 23;
        public const int RefractionColorSlotId = 24;
        public const int RefractionDistanceSlotId = 25;
        public const int DistortionSlotId = 26;
        public const int DistortionBlurSlotId = 27;
        public const int SpecularOcclusionSlotId = 28;
        public const int AlphaThresholdShadowSlotId = 29;
        public const int LightingSlotId = 30;
        public const int BackLightingSlotId = 31;
        public const int DepthOffsetSlotId = 32;
        public const int VertexNormalSlotID = 33;
        public const int VertexTangentSlotID = 34;

        [Flags]
        public enum SlotMask
        {
            None = 0,
            Position = 1 << PositionSlotId,
            Albedo = 1 << AlbedoSlotId,
            Normal = 1 << NormalSlotId,
            BentNormal = 1 << BentNormalSlotId,
            Tangent = 1 << TangentSlotId,
            SubsurfaceMask = 1 << SubsurfaceMaskSlotId,
            Thickness = 1 << ThicknessSlotId,
            DiffusionProfile = 1 << DiffusionProfileHashSlotId,
            IridescenceMask = 1 << IridescenceMaskSlotId,
            IridescenceLayerThickness = 1 << IridescenceThicknessSlotId,
            Specular = 1 << SpecularColorSlotId,
            CoatMask = 1 << CoatMaskSlotId,
            Metallic = 1 << MetallicSlotId,
            Emission = 1 << EmissionSlotId,
            Smoothness = 1 << SmoothnessSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Alpha = 1 << AlphaSlotId,
            AlphaThreshold = 1 << AlphaThresholdSlotId,
            AlphaThresholdDepthPrepass = 1 << AlphaThresholdDepthPrepassSlotId,
            AlphaThresholdDepthPostpass = 1 << AlphaThresholdDepthPostpassSlotId,
            Anisotropy = 1 << AnisotropySlotId,
            SpecularOcclusion = 1 << SpecularOcclusionSlotId,
            AlphaThresholdShadow = 1 << AlphaThresholdShadowSlotId,
            Lighting = 1 << LightingSlotId,
            BackLighting = 1 << BackLightingSlotId,

            // We ran out of bits here. Luckily they always pass SlotMask tests. Upgrade these manally.
            // DepthOffset = 1 << DepthOffsetSlotId,
            // VertexNormal = 1 << VertexNormalSlotID,
            // VertexTangent = 1 << VertexTangentSlotID
        }

        const SlotMask StandardSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Metallic | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting;// | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask SubsurfaceScatteringSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.SubsurfaceMask | SlotMask.Thickness | SlotMask.DiffusionProfile | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting;// | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask AnisotropySlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.Tangent | SlotMask.Anisotropy | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Metallic | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting;// | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask IridescenceSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.IridescenceMask | SlotMask.IridescenceLayerThickness | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Metallic | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting;// | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask SpecularColorSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.Specular | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting;// | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask TranslucentSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.Normal | SlotMask.BentNormal | SlotMask.Thickness | SlotMask.DiffusionProfile | SlotMask.CoatMask | SlotMask.Emission | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.SpecularOcclusion | SlotMask.Alpha | SlotMask.AlphaThreshold | SlotMask.AlphaThresholdDepthPrepass | SlotMask.AlphaThresholdDepthPostpass | SlotMask.AlphaThresholdShadow | SlotMask.Lighting;// | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        
        SlotMask GetActiveSlotMask()
        {
            switch (m_MaterialType)
            {
                case MaterialType.Standard:
                    return StandardSlotMask;

                case MaterialType.SubsurfaceScattering:
                    return SubsurfaceScatteringSlotMask;

                case MaterialType.Anisotropy:
                    return AnisotropySlotMask;

                case MaterialType.Iridescence:
                    return IridescenceSlotMask;

                case MaterialType.SpecularColor:
                    return SpecularColorSlotMask;

                case MaterialType.Translucent:
                    return TranslucentSlotMask;

                default:
                    return SlotMask.None;
            }
        }

        public bool MaterialTypeUsesSlotMask(SlotMask mask)
        {
            SlotMask activeMask = GetActiveSlotMask();
            return (activeMask & mask) != 0;
        }

        public bool m_RayTracing;
        public SurfaceType m_SurfaceType;
        public AlphaMode m_AlphaMode;
        public HDRenderQueue.RenderQueueType m_RenderingPass;
        public bool m_BlendPreserveSpecular;
        public bool m_TransparencyFog;
        public ScreenSpaceRefraction.RefractionModel m_RefractionModel;
        public bool m_Distortion;
        public DistortionMode m_DistortionMode;
        public bool m_DistortionDepthTest;
        public bool m_AlphaTest;
        public bool m_AlphaTestDepthPrepass;
        public bool m_AlphaTestDepthPostpass;
        public bool m_TransparentWritesMotionVec;
        public bool m_AlphaTestShadow;
        public bool m_BackThenFrontRendering;
        public int m_SortPriority;
        public DoubleSidedMode m_DoubleSidedMode;
        public NormalDropOffSpace m_NormalDropOffSpace;
        public MaterialType m_MaterialType;
        public bool m_SSSTransmission;
        public bool m_ReceiveDecals;
        public bool m_ReceivesSSR;
        public bool m_ReceivesSSRTransparent;
        public bool m_AddPrecomputedVelocity;
        public bool m_EnergyConservingSpecular;
        public bool m_SpecularAA;
        public SpecularOcclusionMode m_SpecularOcclusionMode;
        public bool m_overrideBakedGI;
        public bool m_depthOffset;
        public bool m_ZWrite;
        public TransparentCullMode m_transparentCullMode;
        public CompareFunction m_ZTest;
        public bool m_SupportLodCrossFade;
        public bool m_DOTSInstancing;
        public bool m_AlphaToMask;
        public int m_MaterialNeedsUpdateHash;
        public string m_ShaderGUIOverride;
        public bool m_OverrideEnabled;
        public bool m_DrawBeforeRefraction;
    }
}
