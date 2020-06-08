using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.FabricMasterNode")]
    [FormerName("UnityEditor.Rendering.HighDefinition.FabricMasterNode")]
    [FormerName("UnityEditor.ShaderGraph.FabricMasterNode")]
    class FabricMasterNode1 : AbstractMaterialNode, IMasterNode1
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
            CottonWool,
            Silk
        }

        public const int PositionSlotId = 0;
        public const int AlbedoSlotId = 1;
        public const int SpecularOcclusionSlotId = 2;
        public const int NormalSlotId = 3;
        public const int SmoothnessSlotId = 4;
        public const int AmbientOcclusionSlotId = 5;
        public const int SpecularColorSlotId = 6;
        public const int DiffusionProfileHashSlotId = 7;
        public const int SubsurfaceMaskSlotId = 8;
        public const int ThicknessSlotId = 9;
        public const int TangentSlotId = 10;
        public const int AnisotropySlotId = 11;
        public const int EmissionSlotId = 12;
        public const int AlphaSlotId = 13;
        public const int AlphaClipThresholdSlotId = 14;
        public const int BentNormalSlotId = 15;
        public const int LightingSlotId = 16;
        public const int BackLightingSlotId = 17;
        public const int DepthOffsetSlotId = 18;
        public const int VertexNormalSlotId = 19;
        public const int VertexTangentSlotId = 20;

        [Flags]
        public enum SlotMask
        {
            None = 0,
            Position = 1 << PositionSlotId,
            Albedo = 1 << AlbedoSlotId,
            SpecularOcclusion = 1 << SpecularOcclusionSlotId,
            Normal = 1 << NormalSlotId,
            Smoothness = 1 << SmoothnessSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Specular = 1 << SpecularColorSlotId,
            DiffusionProfile = 1 << DiffusionProfileHashSlotId,
            SubsurfaceMask = 1 << SubsurfaceMaskSlotId,
            Thickness = 1 << ThicknessSlotId,
            Tangent = 1 << TangentSlotId,
            Anisotropy = 1 << AnisotropySlotId,
            Emission = 1 << EmissionSlotId,
            Alpha = 1 << AlphaSlotId,
            AlphaClipThreshold = 1 << AlphaClipThresholdSlotId,
            BentNormal = 1 << BentNormalSlotId,
            BakedGI = 1 << LightingSlotId,
            BakedBackGI = 1 << BackLightingSlotId,
            DepthOffset = 1 << DepthOffsetSlotId,
            VertexNormal = 1 << VertexNormalSlotId,
            VertexTangent = 1 << VertexTangentSlotId,
        }

        const SlotMask CottonWoolSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.SpecularOcclusion | SlotMask.Normal | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.Specular | SlotMask.DiffusionProfile | SlotMask.SubsurfaceMask | SlotMask.Thickness | SlotMask.Emission | SlotMask.Alpha | SlotMask.AlphaClipThreshold | SlotMask.BentNormal | SlotMask.BakedGI | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;
        const SlotMask SilkSlotMask = SlotMask.Position | SlotMask.Albedo | SlotMask.SpecularOcclusion | SlotMask.Normal | SlotMask.Smoothness | SlotMask.Occlusion | SlotMask.Specular | SlotMask.DiffusionProfile | SlotMask.SubsurfaceMask | SlotMask.Thickness | SlotMask.Tangent | SlotMask.Anisotropy | SlotMask.Emission | SlotMask.Alpha | SlotMask.AlphaClipThreshold | SlotMask.BentNormal | SlotMask.BakedGI | SlotMask.DepthOffset | SlotMask.VertexNormal | SlotMask.VertexTangent;

        // This could also be a simple array. For now, catch any mismatched data.
        SlotMask GetActiveSlotMask()
        {
            switch (m_MaterialType)
            {
                case MaterialType.CottonWool:
                    return CottonWoolSlotMask;

                case MaterialType.Silk:
                    return SilkSlotMask;

                default:
                    return SlotMask.None;
            }
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
        public int m_SortPriority;
        public DoubleSidedMode m_DoubleSidedMode;
        public MaterialType m_MaterialType;
        public bool m_ReceiveDecals;
        public bool m_ReceivesSSR;
        public bool m_ReceivesSSRTransparent;
        public bool m_AddPrecomputedVelocity;
        public bool m_EnergyConservingSpecular;
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
        public bool m_Transmission;
        public bool m_SubsurfaceScattering;
    }
}
