using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.HairMasterNode")]
    [FormerName("UnityEditor.Rendering.HighDefinition.HairMasterNode")]
    class HairMasterNode1 : AbstractMaterialNode, IMasterNode1
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
            KajiyaKay
        }

        public const int PositionSlotId = 0;
        public const int AlbedoSlotId = 1;
        public const int NormalSlotId = 2;
        public const int SpecularOcclusionSlotId = 3;
        public const int BentNormalSlotId = 4;
        public const int HairStrandDirectionSlotId = 5;
        public const int UnusedSlot6 = 6;
        public const int TransmittanceSlotId = 7;
        public const int RimTransmissionIntensitySlotId = 8;
        public const int SmoothnessSlotId = 9;
        public const int AmbientOcclusionSlotId = 10;
        public const int EmissionSlotId = 11;
        public const int AlphaSlotId = 12;
        public const int AlphaClipThresholdSlotId = 13;
        public const int AlphaClipThresholdDepthPrepassSlotId = 14;
        public const int AlphaClipThresholdDepthPostpassSlotId = 15;
        public const int SpecularAAScreenSpaceVarianceSlotId = 16;
        public const int SpecularAAThresholdSlotId = 17;
        public const int SpecularTintSlotId = 18;
        public const int SpecularShiftSlotId = 19;
        public const int SecondarySpecularTintSlotId = 20;
        public const int SecondarySmoothnessSlotId = 21;
        public const int SecondarySpecularShiftSlotId = 22;
        public const int AlphaClipThresholdShadowSlotId = 23;
        public const int LightingSlotId = 24;
        public const int BackLightingSlotId = 25;
        public const int DepthOffsetSlotId = 26;
        public const int VertexNormalSlotId = 27;
        public const int VertexTangentSlotId = 28;

        [Flags]
        public enum SlotMask
        {
            None = 0,
            Position = 1 << PositionSlotId,
            Albedo = 1 << AlbedoSlotId,
            Normal = 1 << NormalSlotId,
            SpecularOcclusion = 1 << SpecularOcclusionSlotId,
            BentNormal = 1 << BentNormalSlotId,
            HairStrandDirection = 1 << HairStrandDirectionSlotId,
            Slot6 = 1 << UnusedSlot6,
            Transmittance = 1 << TransmittanceSlotId,
            RimTransmissionIntensity = 1 << RimTransmissionIntensitySlotId,
            Smoothness = 1 << SmoothnessSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Emission = 1 << EmissionSlotId,
            Alpha = 1 << AlphaSlotId,
            AlphaClipThreshold = 1 << AlphaClipThresholdSlotId,
            AlphaClipThresholdDepthPrepass = 1 << AlphaClipThresholdDepthPrepassSlotId,
            AlphaClipThresholdDepthPostpass = 1 << AlphaClipThresholdDepthPostpassSlotId,
            SpecularTint = 1 << SpecularTintSlotId,
            SpecularShift = 1 << SpecularShiftSlotId,
            SecondarySpecularTint = 1 << SecondarySpecularTintSlotId,
            SecondarySmoothness = 1 << SecondarySmoothnessSlotId,
            SecondarySpecularShift = 1 << SecondarySpecularShiftSlotId,
            AlphaClipThresholdShadow = 1 << AlphaClipThresholdShadowSlotId,
            BakedGI = 1 << LightingSlotId,
            BakedBackGI = 1 << BackLightingSlotId,
            DepthOffset = 1 << DepthOffsetSlotId,
            VertexNormal = 1 << VertexNormalSlotId,
            VertexTangent = 1 << VertexTangentSlotId,
        }
        
        const SlotMask KajiyaKaySlotMask = SlotMask.Position | SlotMask.VertexNormal | SlotMask.VertexTangent | SlotMask.Albedo | SlotMask.Normal | SlotMask.SpecularOcclusion | SlotMask.BentNormal | SlotMask.HairStrandDirection | SlotMask.Slot6
                                            | SlotMask.Transmittance | SlotMask.RimTransmissionIntensity | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.Alpha | SlotMask.AlphaClipThreshold | SlotMask.AlphaClipThresholdDepthPrepass
                                                | SlotMask.AlphaClipThresholdDepthPostpass | SlotMask.SpecularTint | SlotMask.SpecularShift | SlotMask.SecondarySpecularTint | SlotMask.SecondarySmoothness | SlotMask.SecondarySpecularShift | SlotMask.AlphaClipThresholdShadow | SlotMask.BakedGI | SlotMask.DepthOffset;

        SlotMask GetActiveSlotMask()
        {
            return KajiyaKaySlotMask;
        }

        public bool MaterialTypeUsesSlotMask(SlotMask mask)
        {
            SlotMask activeMask = GetActiveSlotMask();
            return (activeMask & mask) != 0;
        }

        public SurfaceType m_SurfaceType;
        public AlphaMode m_AlphaMode;
        public bool m_BlendPreserveSpecular;
        public bool m_TransparencyFog;
        public bool m_AlphaTest;
        public bool m_AlphaTestDepthPrepass;
        public bool m_AlphaTestDepthPostpass;
        public bool m_TransparentWritesMotionVec;
        public bool m_AlphaTestShadow;
        public bool m_BackThenFrontRendering;
        public int m_SortPriority;
        public DoubleSidedMode m_DoubleSidedMode;
        public MaterialType m_MaterialType;
        public bool m_ReceiveDecals;
        public bool m_ReceivesSSR;
        public bool m_ReceivesSSRTransparent;
        public bool m_AddPrecomputedVelocity;
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
        public bool m_UseLightFacingNormal;
    }
}
