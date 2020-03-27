using UnityEditor.ShaderGraph;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDPortMasks
    {
        public static class Vertex
        {
            public static int[] StackLitDefault = new int[]
            {
                StackLitMasterNode.PositionSlotId,
                StackLitMasterNode.VertexNormalSlotId,
                StackLitMasterNode.VertexTangentSlotId
            };

            public static int[] StackLitPosition = new int[]
            {
                StackLitMasterNode.PositionSlotId,
            };
        }

        public static class Pixel
        {
            public static int[] StackLitMETA = new int[]
            {
                StackLitMasterNode.BaseColorSlotId,
                StackLitMasterNode.NormalSlotId,
                StackLitMasterNode.BentNormalSlotId,
                StackLitMasterNode.TangentSlotId,
                StackLitMasterNode.SubsurfaceMaskSlotId,
                StackLitMasterNode.ThicknessSlotId,
                StackLitMasterNode.DiffusionProfileHashSlotId,
                StackLitMasterNode.IridescenceMaskSlotId,
                StackLitMasterNode.IridescenceThicknessSlotId,
                StackLitMasterNode.IridescenceCoatFixupTIRSlotId,
                StackLitMasterNode.IridescenceCoatFixupTIRClampSlotId,
                StackLitMasterNode.SpecularColorSlotId,
                StackLitMasterNode.DielectricIorSlotId,
                StackLitMasterNode.MetallicSlotId,
                StackLitMasterNode.EmissionSlotId,
                StackLitMasterNode.SmoothnessASlotId,
                StackLitMasterNode.SmoothnessBSlotId,
                StackLitMasterNode.AmbientOcclusionSlotId,
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId,
                StackLitMasterNode.AnisotropyASlotId,
                StackLitMasterNode.AnisotropyBSlotId,
                StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                StackLitMasterNode.SpecularAAThresholdSlotId,
                StackLitMasterNode.CoatSmoothnessSlotId,
                StackLitMasterNode.CoatIorSlotId,
                StackLitMasterNode.CoatThicknessSlotId,
                StackLitMasterNode.CoatExtinctionSlotId,
                StackLitMasterNode.CoatNormalSlotId,
                StackLitMasterNode.CoatMaskSlotId,
                StackLitMasterNode.LobeMixSlotId,
                StackLitMasterNode.HazinessSlotId,
                StackLitMasterNode.HazeExtentSlotId,
                StackLitMasterNode.HazyGlossMaxDielectricF0SlotId,
                StackLitMasterNode.SpecularOcclusionSlotId,
                StackLitMasterNode.SOFixupVisibilityRatioThresholdSlotId,
                StackLitMasterNode.SOFixupStrengthFactorSlotId,
                StackLitMasterNode.SOFixupMaxAddedRoughnessSlotId,
            };

            public static int[] StackLitAlphaDepth = new int[]
            {
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId,
                StackLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] StackLitDepthMotionVectors = new int[]
            {
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId,
                StackLitMasterNode.DepthOffsetSlotId,
                // StackLitMasterNode.coat
                StackLitMasterNode.CoatSmoothnessSlotId,
                StackLitMasterNode.CoatNormalSlotId,
                // !StackLitMasterNode.coat
                StackLitMasterNode.NormalSlotId,
                StackLitMasterNode.LobeMixSlotId,
                StackLitMasterNode.SmoothnessASlotId,
                StackLitMasterNode.SmoothnessBSlotId,
                // StackLitMasterNode.geometricSpecularAA
                StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                StackLitMasterNode.SpecularAAThresholdSlotId,
            };

            public static int[] StackLitDistortion = new int[]
            {
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId,
                StackLitMasterNode.DistortionSlotId,
                StackLitMasterNode.DistortionBlurSlotId,
                StackLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] StackLitForward = new int[]
            {
                StackLitMasterNode.BaseColorSlotId,
                StackLitMasterNode.NormalSlotId,
                StackLitMasterNode.BentNormalSlotId,
                StackLitMasterNode.TangentSlotId,
                StackLitMasterNode.SubsurfaceMaskSlotId,
                StackLitMasterNode.ThicknessSlotId,
                StackLitMasterNode.DiffusionProfileHashSlotId,
                StackLitMasterNode.IridescenceMaskSlotId,
                StackLitMasterNode.IridescenceThicknessSlotId,
                StackLitMasterNode.IridescenceCoatFixupTIRSlotId,
                StackLitMasterNode.IridescenceCoatFixupTIRClampSlotId,
                StackLitMasterNode.SpecularColorSlotId,
                StackLitMasterNode.DielectricIorSlotId,
                StackLitMasterNode.MetallicSlotId,
                StackLitMasterNode.EmissionSlotId,
                StackLitMasterNode.SmoothnessASlotId,
                StackLitMasterNode.SmoothnessBSlotId,
                StackLitMasterNode.AmbientOcclusionSlotId,
                StackLitMasterNode.AlphaSlotId,
                StackLitMasterNode.AlphaClipThresholdSlotId,
                StackLitMasterNode.AnisotropyASlotId,
                StackLitMasterNode.AnisotropyBSlotId,
                StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                StackLitMasterNode.SpecularAAThresholdSlotId,
                StackLitMasterNode.CoatSmoothnessSlotId,
                StackLitMasterNode.CoatIorSlotId,
                StackLitMasterNode.CoatThicknessSlotId,
                StackLitMasterNode.CoatExtinctionSlotId,
                StackLitMasterNode.CoatNormalSlotId,
                StackLitMasterNode.CoatMaskSlotId,
                StackLitMasterNode.LobeMixSlotId,
                StackLitMasterNode.HazinessSlotId,
                StackLitMasterNode.HazeExtentSlotId,
                StackLitMasterNode.HazyGlossMaxDielectricF0SlotId,
                StackLitMasterNode.SpecularOcclusionSlotId,
                StackLitMasterNode.SOFixupVisibilityRatioThresholdSlotId,
                StackLitMasterNode.SOFixupStrengthFactorSlotId,
                StackLitMasterNode.SOFixupMaxAddedRoughnessSlotId,
                StackLitMasterNode.LightingSlotId,
                StackLitMasterNode.BackLightingSlotId,
                StackLitMasterNode.DepthOffsetSlotId,
            };
        }
    }
}
