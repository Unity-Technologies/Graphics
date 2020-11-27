using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy
{
    [FormerName("UnityEditor.Experimental.Rendering.HDPipeline.DecalMasterNode")]
    [FormerName("UnityEditor.Rendering.HighDefinition.DecalMasterNode")]
    class DecalMasterNode1 : AbstractMaterialNode, IMasterNode1
    {
        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public const int PositionSlotId = 0;
        public const int AlbedoSlotId = 1;
        public const int BaseColorOpacitySlotId = 2;
        public const int NormalSlotId = 3;
        public const int NormaOpacitySlotId = 4;
        public const int MetallicSlotId = 5;
        public const int AmbientOcclusionSlotId = 6;
        public const int SmoothnessSlotId = 7;
        public const int MAOSOpacitySlotId = 8;
        public const int EmissionSlotId = 9;
        public const int VertexNormalSlotID = 10;
        public const int VertexTangentSlotID = 11;

        [Flags]
        public enum SlotMask
        {
            None = 0,
            Position = 1 << PositionSlotId,
            VertexNormal = 1 << VertexNormalSlotID,
            VertexTangent = 1 << VertexTangentSlotID,
            Albedo = 1 << AlbedoSlotId,
            AlphaAlbedo = 1 << BaseColorOpacitySlotId,
            Normal = 1 << NormalSlotId,
            AlphaNormal = 1 << NormaOpacitySlotId,
            Metallic = 1 << MetallicSlotId,
            Occlusion = 1 << AmbientOcclusionSlotId,
            Smoothness = 1 << SmoothnessSlotId,
            AlphaMAOS = 1 << MAOSOpacitySlotId,
            Emission = 1 << EmissionSlotId
        }

        const SlotMask decalParameter = SlotMask.Position | SlotMask.VertexNormal | SlotMask.VertexTangent | SlotMask.Albedo | SlotMask.AlphaAlbedo | SlotMask.Normal | SlotMask.AlphaNormal | SlotMask.Metallic | SlotMask.Occlusion | SlotMask.Smoothness | SlotMask.AlphaMAOS | SlotMask.Emission;

        SlotMask GetActiveSlotMask()
        {
            return decalParameter;
        }

        public bool MaterialTypeUsesSlotMask(SlotMask mask)
        {
            SlotMask activeMask = GetActiveSlotMask();
            return (activeMask & mask) != 0;
        }

        public SurfaceType m_SurfaceType;
        public bool m_AffectsMetal;
        public bool m_AffectsAO;
        public bool m_AffectsSmoothness;
        public bool m_AffectsAlbedo;
        public bool m_AffectsNormal;
        public bool m_AffectsEmission;
        public int m_DrawOrder;
        public bool m_DOTSInstancing;
        public string m_ShaderGUIOverride;
        public bool m_OverrideEnabled;
    }
}
