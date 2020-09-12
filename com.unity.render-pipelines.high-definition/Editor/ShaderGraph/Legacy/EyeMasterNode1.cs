using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.Rendering.HighDefinition.EyeMasterNode")]
    class EyeMasterNode1 : AbstractMaterialNode, IMasterNode1
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
            Eye,
            EyeCinematic
        }

        public const int PositionSlotId = 0;
        public const int AlbedoSlotId = 1;
        public const int SpecularOcclusionSlotId = 2;
        public const int NormalSlotId = 3;
        public const int IrisNormalSlotId = 4;
        public const int SmoothnessSlotId = 5;
        public const int IORSlotId = 6;
        public const int AmbientOcclusionSlotId = 7;
        public const int MaskSlotId = 8;
        public const int DiffusionProfileHashSlotId = 9;
        public const int SubsurfaceMaskSlotId = 10;
        public const int EmissionSlotId = 11;
        public const int AlphaSlotId = 12;
        public const int AlphaClipThresholdSlotId = 13;
        public const int BentNormalSlotId = 14;
        public const int LightingSlotId = 15;
        public const int BackLightingSlotId = 16;
        public const int DepthOffsetSlotId = 17;
         public const int VertexNormalSlotID = 18;
        public const int VertexTangentSlotID = 19;

        [Flags]
        public enum SlotMask
        {
            None = 0,
            Position = 1 << PositionSlotId,
            VertexNormal = 1 << VertexNormalSlotID,
            VertexTangent = 1 << VertexTangentSlotID,
            Albedo = 1 << AlbedoSlotId,
            SpecularOcclusion = 1 << SpecularOcclusionSlotId,
            Normal = 1 << NormalSlotId,
            IrisNormal = 1 << IrisNormalSlotId,
            Smoothness = 1 << SmoothnessSlotId,
            IOR = 1 << IORSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Mask = 1 << MaskSlotId,
            DiffusionProfile = 1 << DiffusionProfileHashSlotId,
            SubsurfaceMask = 1 << SubsurfaceMaskSlotId,
            Emission = 1 << EmissionSlotId,
            Alpha = 1 << AlphaSlotId,
            AlphaClipThreshold = 1 << AlphaClipThresholdSlotId,
            BentNormal = 1 << BentNormalSlotId,
            BakedGI = 1 << LightingSlotId,
            BakedBackGI = 1 << BackLightingSlotId,
            DepthOffset = 1 << DepthOffsetSlotId,
        }

        const SlotMask EyeSlotMask = SlotMask.Position | SlotMask.VertexNormal | SlotMask.VertexTangent | SlotMask.Albedo | SlotMask.SpecularOcclusion | SlotMask.Normal | SlotMask.IrisNormal | SlotMask.Smoothness | SlotMask.IOR | SlotMask.Occlusion | SlotMask.Mask | SlotMask.DiffusionProfile | SlotMask.SubsurfaceMask | SlotMask.Emission | SlotMask.Alpha | SlotMask.AlphaClipThreshold | SlotMask.BentNormal | SlotMask.BakedGI | SlotMask.DepthOffset;
        const SlotMask EyeCinematicSlotMask = EyeSlotMask;

        SlotMask GetActiveSlotMask()
        {
            switch (m_MaterialType)
            {
                case MaterialType.Eye:
                    return EyeSlotMask;

                case MaterialType.EyeCinematic:
                    return EyeCinematicSlotMask;

                default:
                    return SlotMask.None;
            }
        }

        public bool MaterialTypeUsesSlotMask(SlotMask mask)
        {
            SlotMask activeMask = GetActiveSlotMask();
            return (activeMask & mask) != 0;
        }

        // public bool m_RayTracing;
        public SurfaceType m_SurfaceType;
        public AlphaMode m_AlphaMode;
        public bool m_BlendPreserveSpecular;
        public bool m_TransparencyFog;
        public bool m_AlphaTest;
        public bool m_AlphaTestDepthPrepass;
        public bool m_AlphaTestDepthPostpass;
        public int m_SortPriority;
        public DoubleSidedMode m_DoubleSidedMode;
        public MaterialType m_MaterialType;
        public bool m_ReceiveDecals;
        public bool m_ReceivesSSR;
        public bool m_ReceivesSSRTransparent;
        public bool m_AddPrecomputedVelocity;
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
        public bool m_SubsurfaceScattering;
    }
}
