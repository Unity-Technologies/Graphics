using UnityEditor.ShaderGraph;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDPortMasks
    {
        public static class Vertex
        {
            public static int[] UnlitDefault = new int[]
            {
                UnlitMasterNode.PositionSlotId,
                UnlitMasterNode.VertNormalSlotId,
                UnlitMasterNode.VertTangentSlotId,
            };

            public static int[] PBRDefault = new int[]
            {
                PBRMasterNode.PositionSlotId,
                PBRMasterNode.VertNormalSlotId,
                PBRMasterNode.VertTangentSlotId,
            };

            public static int[] HDUnlitDefault = new int[]
            {
                HDUnlitMasterNode.PositionSlotId,
                HDUnlitMasterNode.VertexNormalSlotId,
                HDUnlitMasterNode.VertexTangentSlotId,
            };

            public static int[] HDLitDefault = new int[]
            {
                HDLitMasterNode.PositionSlotId,
                HDLitMasterNode.VertexNormalSlotID,
                HDLitMasterNode.VertexTangentSlotID,
            };

            public static int[] EyeDefault = new int[]
            {
                EyeMasterNode.PositionSlotId,
                EyeMasterNode.VertexNormalSlotID,
                EyeMasterNode.VertexTangentSlotID,
            };

            public static int[] FabricDefault = new int[]
            {
                FabricMasterNode.PositionSlotId,
                FabricMasterNode.VertexNormalSlotId,
                FabricMasterNode.VertexTangentSlotId,
            };

            public static int[] HairDefault = new int[]
            {
                HairMasterNode.PositionSlotId,
                HairMasterNode.VertexNormalSlotId,
                HairMasterNode.VertexTangentSlotId,
            };

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
            public static int[] UnlitDefault = new int[]
            {
                UnlitMasterNode.ColorSlotId,
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId,
            };

            public static int[] UnlitOnlyAlpha = new int[]
            {
                UnlitMasterNode.AlphaSlotId,
                UnlitMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBRDefault = new int[]
            {
                PBRMasterNode.AlbedoSlotId,
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.MetallicSlotId,
                PBRMasterNode.SpecularSlotId,
                PBRMasterNode.EmissionSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.OcclusionSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBROnlyAlpha = new int[]
            {
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] PBRDepthMotionVectors = new int[]
            {
                PBRMasterNode.NormalSlotId,
                PBRMasterNode.SmoothnessSlotId,
                PBRMasterNode.AlphaSlotId,
                PBRMasterNode.AlphaThresholdSlotId,
            };

            public static int[] HDUnlitDefault = new int[]
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId,
            };

            public static int[] HDUnlitOnlyAlpha = new int[]
            {
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
            };

            public static int[] HDUnlitDistortion = new int[]
            {
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.DistortionSlotId,
                HDUnlitMasterNode.DistortionBlurSlotId,
            };

            public static int[] HDUnlitForward = new int[]
            {
                HDUnlitMasterNode.ColorSlotId,
                HDUnlitMasterNode.AlphaSlotId,
                HDUnlitMasterNode.AlphaThresholdSlotId,
                HDUnlitMasterNode.EmissionSlotId,
                HDUnlitMasterNode.ShadowTintSlotId,
            };

            public static int[] HDLitDefault = new int[]
            {
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
                HDLitMasterNode.LightingSlotId,
                HDLitMasterNode.BackLightingSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] HDLitMeta = new int[]
            {
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
            };

            public static int[] HDLitShadowCaster = new int[]
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AlphaThresholdShadowSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] HDLitSceneSelection = new int[]
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] HDLitDepthMotionVectors = new int[]
            {
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] HDLitDistortion = new int[]
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.DistortionSlotId,
                HDLitMasterNode.DistortionBlurSlotId,
            };

            public static int[] HDLitTransparentDepthPrepass = new int[]
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdDepthPrepassSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] HDLitTransparentBackface = new int[]
            {
                HDLitMasterNode.AlbedoSlotId,
                HDLitMasterNode.NormalSlotId,
                HDLitMasterNode.BentNormalSlotId,
                HDLitMasterNode.TangentSlotId,
                HDLitMasterNode.SubsurfaceMaskSlotId,
                HDLitMasterNode.ThicknessSlotId,
                HDLitMasterNode.DiffusionProfileHashSlotId,
                HDLitMasterNode.IridescenceMaskSlotId,
                HDLitMasterNode.IridescenceThicknessSlotId,
                HDLitMasterNode.SpecularColorSlotId,
                HDLitMasterNode.CoatMaskSlotId,
                HDLitMasterNode.MetallicSlotId,
                HDLitMasterNode.EmissionSlotId,
                HDLitMasterNode.SmoothnessSlotId,
                HDLitMasterNode.AmbientOcclusionSlotId,
                HDLitMasterNode.SpecularOcclusionSlotId,
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdSlotId,
                HDLitMasterNode.AnisotropySlotId,
                HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HDLitMasterNode.SpecularAAThresholdSlotId,
                HDLitMasterNode.RefractionIndexSlotId,
                HDLitMasterNode.RefractionColorSlotId,
                HDLitMasterNode.RefractionDistanceSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] HDLitTransparentDepthPostpass = new int[]
            {
                HDLitMasterNode.AlphaSlotId,
                HDLitMasterNode.AlphaThresholdDepthPrepassSlotId,
                HDLitMasterNode.DepthOffsetSlotId,
            };

            public static int[] EyeMETA = new int[]
            {
                EyeMasterNode.AlbedoSlotId,
                EyeMasterNode.SpecularOcclusionSlotId,
                EyeMasterNode.NormalSlotId,
                EyeMasterNode.IrisNormalSlotId,
                EyeMasterNode.SmoothnessSlotId,
                EyeMasterNode.IORSlotId,
                EyeMasterNode.AmbientOcclusionSlotId,
                EyeMasterNode.MaskSlotId,
                EyeMasterNode.DiffusionProfileHashSlotId,
                EyeMasterNode.SubsurfaceMaskSlotId,
                EyeMasterNode.EmissionSlotId,
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
            };

            public static int[] EyeAlphaDepth = new int[]
            {
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            };

            public static int[] EyeDepthMotionVectors = new int[]
            {
                EyeMasterNode.NormalSlotId,
                EyeMasterNode.SmoothnessSlotId,
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            };

            public static int[] EyeForward = new int[]
            {
                EyeMasterNode.AlbedoSlotId,
                EyeMasterNode.SpecularOcclusionSlotId,
                EyeMasterNode.NormalSlotId,
                EyeMasterNode.IrisNormalSlotId,
                EyeMasterNode.SmoothnessSlotId,
                EyeMasterNode.IORSlotId,
                EyeMasterNode.AmbientOcclusionSlotId,
                EyeMasterNode.MaskSlotId,
                EyeMasterNode.DiffusionProfileHashSlotId,
                EyeMasterNode.SubsurfaceMaskSlotId,
                EyeMasterNode.EmissionSlotId,
                EyeMasterNode.AlphaSlotId,
                EyeMasterNode.AlphaClipThresholdSlotId,
                EyeMasterNode.LightingSlotId,
                EyeMasterNode.BackLightingSlotId,
                EyeMasterNode.DepthOffsetSlotId,
            };

            public static int[] FabricMETA = new int[]
            {
                FabricMasterNode.AlbedoSlotId,
                FabricMasterNode.SpecularOcclusionSlotId,
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AmbientOcclusionSlotId,
                FabricMasterNode.SpecularColorSlotId,
                FabricMasterNode.DiffusionProfileHashSlotId,
                FabricMasterNode.SubsurfaceMaskSlotId,
                FabricMasterNode.ThicknessSlotId,
                FabricMasterNode.TangentSlotId,
                FabricMasterNode.AnisotropySlotId,
                FabricMasterNode.EmissionSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
            };

            public static int[] FabricAlphaDepth = new int[]
            {
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            };

            public static int[] FabricDepthMotionVectors = new int[]
            {
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            };

            public static int[] FabricForward = new int[]
            {
                FabricMasterNode.AlbedoSlotId,
                FabricMasterNode.SpecularOcclusionSlotId,
                FabricMasterNode.NormalSlotId,
                FabricMasterNode.BentNormalSlotId,
                FabricMasterNode.SmoothnessSlotId,
                FabricMasterNode.AmbientOcclusionSlotId,
                FabricMasterNode.SpecularColorSlotId,
                FabricMasterNode.DiffusionProfileHashSlotId,
                FabricMasterNode.SubsurfaceMaskSlotId,
                FabricMasterNode.ThicknessSlotId,
                FabricMasterNode.TangentSlotId,
                FabricMasterNode.AnisotropySlotId,
                FabricMasterNode.EmissionSlotId,
                FabricMasterNode.AlphaSlotId,
                FabricMasterNode.AlphaClipThresholdSlotId,
                FabricMasterNode.LightingSlotId,
                FabricMasterNode.BackLightingSlotId,
                FabricMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairMETA = new int[]
            {
                HairMasterNode.AlbedoSlotId,
                HairMasterNode.NormalSlotId,
                HairMasterNode.SpecularOcclusionSlotId,
                HairMasterNode.BentNormalSlotId,
                HairMasterNode.HairStrandDirectionSlotId,
                HairMasterNode.TransmittanceSlotId,
                HairMasterNode.RimTransmissionIntensitySlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AmbientOcclusionSlotId,
                HairMasterNode.EmissionSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HairMasterNode.SpecularAAThresholdSlotId,
                HairMasterNode.SpecularTintSlotId,
                HairMasterNode.SpecularShiftSlotId,
                HairMasterNode.SecondarySpecularTintSlotId,
                HairMasterNode.SecondarySmoothnessSlotId,
                HairMasterNode.SecondarySpecularShiftSlotId,
            };

            public static int[] HairShadowCaster = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.AlphaClipThresholdShadowSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairAlphaDepth = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairDepthMotionVectors = new int[]
            {
                HairMasterNode.NormalSlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairTransparentDepthPrepass = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdDepthPrepassSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairTransparentBackface = new int[]
            {
                HairMasterNode.AlbedoSlotId,
                HairMasterNode.NormalSlotId,
                HairMasterNode.SpecularOcclusionSlotId,
                HairMasterNode.BentNormalSlotId,
                HairMasterNode.HairStrandDirectionSlotId,
                HairMasterNode.TransmittanceSlotId,
                HairMasterNode.RimTransmissionIntensitySlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AmbientOcclusionSlotId,
                HairMasterNode.EmissionSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HairMasterNode.SpecularAAThresholdSlotId,
                HairMasterNode.SpecularTintSlotId,
                HairMasterNode.SpecularShiftSlotId,
                HairMasterNode.SecondarySpecularTintSlotId,
                HairMasterNode.SecondarySmoothnessSlotId,
                HairMasterNode.SecondarySpecularShiftSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairForward = new int[]
            {
                HairMasterNode.AlbedoSlotId,
                HairMasterNode.NormalSlotId,
                HairMasterNode.SpecularOcclusionSlotId,
                HairMasterNode.BentNormalSlotId,
                HairMasterNode.HairStrandDirectionSlotId,
                HairMasterNode.TransmittanceSlotId,
                HairMasterNode.RimTransmissionIntensitySlotId,
                HairMasterNode.SmoothnessSlotId,
                HairMasterNode.AmbientOcclusionSlotId,
                HairMasterNode.EmissionSlotId,
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdSlotId,
                HairMasterNode.SpecularAAScreenSpaceVarianceSlotId,
                HairMasterNode.SpecularAAThresholdSlotId,
                HairMasterNode.SpecularTintSlotId,
                HairMasterNode.SpecularShiftSlotId,
                HairMasterNode.SecondarySpecularTintSlotId,
                HairMasterNode.SecondarySmoothnessSlotId,
                HairMasterNode.SecondarySpecularShiftSlotId,
                HairMasterNode.LightingSlotId,
                HairMasterNode.BackLightingSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

            public static int[] HairTransparentDepthPostpass = new int[]
            {
                HairMasterNode.AlphaSlotId,
                HairMasterNode.AlphaClipThresholdDepthPostpassSlotId,
                HairMasterNode.DepthOffsetSlotId,
            };

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

            public static int[] DecalDefault = new int[]
            {
                DecalMasterNode.AlbedoSlotId,
                DecalMasterNode.BaseColorOpacitySlotId,
                DecalMasterNode.NormalSlotId,
                DecalMasterNode.NormaOpacitySlotId,
                DecalMasterNode.MetallicSlotId,
                DecalMasterNode.AmbientOcclusionSlotId,
                DecalMasterNode.SmoothnessSlotId,
                DecalMasterNode.MAOSOpacitySlotId,
            };

            public static int[] DecalEmissive = new int[]
            {
                DecalMasterNode.EmissionSlotId
            };

            public static int[] DecalMeshEmissive = new int[]
            {
                DecalMasterNode.AlbedoSlotId,
                DecalMasterNode.BaseColorOpacitySlotId,
                DecalMasterNode.NormalSlotId,
                DecalMasterNode.NormaOpacitySlotId,
                DecalMasterNode.MetallicSlotId,
                DecalMasterNode.AmbientOcclusionSlotId,
                DecalMasterNode.SmoothnessSlotId,
                DecalMasterNode.MAOSOpacitySlotId,
                DecalMasterNode.EmissionSlotId,
            };
        }
    }
}
