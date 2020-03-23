using UnityEditor.ShaderGraph;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDBlockMasks
    {
        public static class Vertex
        {
            public static BlockFieldDescriptor[] Default = new BlockFieldDescriptor[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
            };
        }

        public static class Pixel
        {
            public static BlockFieldDescriptor[] OnlyAlpha = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
            };

            public static BlockFieldDescriptor[] Distortion = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.Distortion,
                HDBlockFields.SurfaceDescription.DistortionBlur,
            };
            
            public static BlockFieldDescriptor[] UnlitDefault = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                BlockFields.SurfaceDescription.Emission,
            };

            public static BlockFieldDescriptor[] UnlitForward = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                BlockFields.SurfaceDescription.Emission,
                HDBlockFields.SurfaceDescription.ShadowTint,
            };
        }

        // public static class Vertex
        // {
            // public static BlockFieldDescriptor[] UnlitDefault = new BlockFieldDescriptor[]
            // {
            //     UnlitMasterNode.PositionSlotId,
            //     UnlitMasterNode.VertNormalSlotId,
            //     UnlitMasterNode.VertTangentSlotId,
            // };

            // public static BlockFieldDescriptor[] PBRDefault = new BlockFieldDescriptor[]
            // {
            //     PBRMasterNode.PositionSlotId,
            //     PBRMasterNode.VertNormalSlotId,
            //     PBRMasterNode.VertTangentSlotId,
            // };

            // public static BlockFieldDescriptor[] HDUnlitDefault = new BlockFieldDescriptor[]
            // {
            //     HDUnlitMasterNode.PositionSlotId,
            //     HDUnlitMasterNode.VertexNormalSlotId,
            //     HDUnlitMasterNode.VertexTangentSlotId,
            // };

            // public static BlockFieldDescriptor[] HDLitDefault = new BlockFieldDescriptor[]
            // {
            //     HDLitMasterNode.PositionSlotId,
            //     HDLitMasterNode.VertexNormalSlotID,
            //     HDLitMasterNode.VertexTangentSlotID,
            // };

            // public static BlockFieldDescriptor[] EyeDefault = new BlockFieldDescriptor[]
            // {
            //     EyeMasterNode.PositionSlotId,
            //     EyeMasterNode.VertexNormalSlotID,
            //     EyeMasterNode.VertexTangentSlotID,
            // };

            // public static BlockFieldDescriptor[] FabricDefault = new BlockFieldDescriptor[]
            // {
            //     FabricMasterNode.PositionSlotId,
            //     FabricMasterNode.VertexNormalSlotId,
            //     FabricMasterNode.VertexTangentSlotId,
            // };

            // public static BlockFieldDescriptor[] HairDefault = new BlockFieldDescriptor[]
            // {
            //     HairMasterNode.PositionSlotId,
            //     HairMasterNode.VertexNormalSlotId,
            //     HairMasterNode.VertexTangentSlotId,
            // };

            // public static BlockFieldDescriptor[] StackLitDefault = new BlockFieldDescriptor[]
            // {
            //     StackLitMasterNode.PositionSlotId,
            //     StackLitMasterNode.VertexNormalSlotId,
            //     StackLitMasterNode.VertexTangentSlotId
            // };

            // public static BlockFieldDescriptor[] StackLitPosition = new BlockFieldDescriptor[]
            // {
            //     StackLitMasterNode.PositionSlotId,
            // };
        // }

        // public static class Pixel
        // {
        //     public static BlockFieldDescriptor[] UnlitDefault = new BlockFieldDescriptor[]
        //     {
        //         BlockFields.SurfaceDescription.BaseColor,
        //         BlockFields.SurfaceDescription.Alpha,
        //         BlockFields.SurfaceDescription.AlphaClipThreshold,
        //         BlockFields.SurfaceDescription.Emission,
        //     };

        //     public static BlockFieldDescriptor[] UnlitForward = new BlockFieldDescriptor[]
        //     {
        //         BlockFields.SurfaceDescription.BaseColor,
        //         BlockFields.SurfaceDescription.Alpha,
        //         BlockFields.SurfaceDescription.AlphaClipThreshold,
        //         BlockFields.SurfaceDescription.Emission,
        //         HDBlockFields.SurfaceDescription.ShadowTint,
        //     };

        //     public static BlockFieldDescriptor[] OnlyAlpha = new BlockFieldDescriptor[]
        //     {
        //         BlockFields.SurfaceDescription.Alpha,
        //         BlockFields.SurfaceDescription.AlphaClipThreshold,
        //     };

        //     public static BlockFieldDescriptor[] Distortion = new BlockFieldDescriptor[]
        //     {
        //         BlockFields.SurfaceDescription.Alpha,
        //         BlockFields.SurfaceDescription.AlphaClipThreshold,
        //         HDBlockFields.SurfaceDescription.Distortion,
        //         HDBlockFields.SurfaceDescription.DistortionBlur,
        //     };

        //     public static BlockFieldDescriptor[] UnlitDefault = new BlockFieldDescriptor[]
        //     {
        //         UnlitMasterNode.ColorSlotId,
        //         UnlitMasterNode.AlphaSlotId,
        //         UnlitMasterNode.AlphaThresholdSlotId,
        //     };

        //     public static BlockFieldDescriptor[] UnlitOnlyAlpha = new BlockFieldDescriptor[]
        //     {
        //         UnlitMasterNode.AlphaSlotId,
        //         UnlitMasterNode.AlphaThresholdSlotId,
        //     };

        //     public static BlockFieldDescriptor[] PBRDefault = new BlockFieldDescriptor[]
        //     {
        //         PBRMasterNode.AlbedoSlotId,
        //         PBRMasterNode.NormalSlotId,
        //         PBRMasterNode.MetallicSlotId,
        //         PBRMasterNode.SpecularSlotId,
        //         PBRMasterNode.EmissionSlotId,
        //         PBRMasterNode.SmoothnessSlotId,
        //         PBRMasterNode.OcclusionSlotId,
        //         PBRMasterNode.AlphaSlotId,
        //         PBRMasterNode.AlphaThresholdSlotId,
        //     };

        //     public static BlockFieldDescriptor[] PBROnlyAlpha = new BlockFieldDescriptor[]
        //     {
        //         PBRMasterNode.AlphaSlotId,
        //         PBRMasterNode.AlphaThresholdSlotId,
        //     };

        //     public static BlockFieldDescriptor[] PBRDepthMotionVectors = new BlockFieldDescriptor[]
        //     {
        //         PBRMasterNode.NormalSlotId,
        //         PBRMasterNode.SmoothnessSlotId,
        //         PBRMasterNode.AlphaSlotId,
        //         PBRMasterNode.AlphaThresholdSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDUnlitDefault = new BlockFieldDescriptor[]
        //     {
        //         HDUnlitMasterNode.ColorSlotId,
        //         HDUnlitMasterNode.AlphaSlotId,
        //         HDUnlitMasterNode.AlphaThresholdSlotId,
        //         HDUnlitMasterNode.EmissionSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDUnlitOnlyAlpha = new BlockFieldDescriptor[]
        //     {
        //         HDUnlitMasterNode.AlphaSlotId,
        //         HDUnlitMasterNode.AlphaThresholdSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDUnlitDistortion = new BlockFieldDescriptor[]
        //     {
        //         HDUnlitMasterNode.AlphaSlotId,
        //         HDUnlitMasterNode.AlphaThresholdSlotId,
        //         HDUnlitMasterNode.DistortionSlotId,
        //         HDUnlitMasterNode.DistortionBlurSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDUnlitForward = new BlockFieldDescriptor[]
        //     {
        //         HDUnlitMasterNode.ColorSlotId,
        //         HDUnlitMasterNode.AlphaSlotId,
        //         HDUnlitMasterNode.AlphaThresholdSlotId,
        //         HDUnlitMasterNode.EmissionSlotId,
        //         HDUnlitMasterNode.ShadowTintSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDLitDefault = new BlockFieldDescriptor[]
        //     {
        //         HDLitMasterNode.AlbedoSlotId,
        //         HDLitMasterNode.NormalSlotId,
        //         HDLitMasterNode.BentNormalSlotId,
        //         HDLitMasterNode.TangentSlotId,
        //         HDLitMasterNode.SubsurfaceMaskSlotId,
        //         HDLitMasterNode.ThicknessSlotId,
        //         HDLitMasterNode.DiffusionProfileHashSlotId,
        //         HDLitMasterNode.IridescenceMaskSlotId,
        //         HDLitMasterNode.IridescenceThicknessSlotId,
        //         HDLitMasterNode.SpecularColorSlotId,
        //         HDLitMasterNode.CoatMaskSlotId,
        //         HDLitMasterNode.MetallicSlotId,
        //         HDLitMasterNode.EmissionSlotId,
        //         HDLitMasterNode.SmoothnessSlotId,
        //         HDLitMasterNode.AmbientOcclusionSlotId,
        //         HDLitMasterNode.SpecularOcclusionSlotId,
        //         HDLitMasterNode.AlphaSlotId,
        //         HDLitMasterNode.AlphaThresholdSlotId,
        //         HDLitMasterNode.AnisotropySlotId,
        //         HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
        //         HDLitMasterNode.SpecularAAThresholdSlotId,
        //         HDLitMasterNode.RefractionIndexSlotId,
        //         HDLitMasterNode.RefractionColorSlotId,
        //         HDLitMasterNode.RefractionDistanceSlotId,
        //         HDLitMasterNode.LightingSlotId,
        //         HDLitMasterNode.BackLightingSlotId,
        //         HDLitMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDLitMeta = new BlockFieldDescriptor[]
        //     {
        //         HDLitMasterNode.AlbedoSlotId,
        //         HDLitMasterNode.NormalSlotId,
        //         HDLitMasterNode.BentNormalSlotId,
        //         HDLitMasterNode.TangentSlotId,
        //         HDLitMasterNode.SubsurfaceMaskSlotId,
        //         HDLitMasterNode.ThicknessSlotId,
        //         HDLitMasterNode.DiffusionProfileHashSlotId,
        //         HDLitMasterNode.IridescenceMaskSlotId,
        //         HDLitMasterNode.IridescenceThicknessSlotId,
        //         HDLitMasterNode.SpecularColorSlotId,
        //         HDLitMasterNode.CoatMaskSlotId,
        //         HDLitMasterNode.MetallicSlotId,
        //         HDLitMasterNode.EmissionSlotId,
        //         HDLitMasterNode.SmoothnessSlotId,
        //         HDLitMasterNode.AmbientOcclusionSlotId,
        //         HDLitMasterNode.SpecularOcclusionSlotId,
        //         HDLitMasterNode.AlphaSlotId,
        //         HDLitMasterNode.AlphaThresholdSlotId,
        //         HDLitMasterNode.AnisotropySlotId,
        //         HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
        //         HDLitMasterNode.SpecularAAThresholdSlotId,
        //         HDLitMasterNode.RefractionIndexSlotId,
        //         HDLitMasterNode.RefractionColorSlotId,
        //         HDLitMasterNode.RefractionDistanceSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDLitShadowCaster = new BlockFieldDescriptor[]
        //     {
        //         HDLitMasterNode.AlphaSlotId,
        //         HDLitMasterNode.AlphaThresholdSlotId,
        //         HDLitMasterNode.AlphaThresholdShadowSlotId,
        //         HDLitMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDLitSceneSelection = new BlockFieldDescriptor[]
        //     {
        //         HDLitMasterNode.AlphaSlotId,
        //         HDLitMasterNode.AlphaThresholdSlotId,
        //         HDLitMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDLitDepthMotionVectors = new BlockFieldDescriptor[]
        //     {
        //         HDLitMasterNode.NormalSlotId,
        //         HDLitMasterNode.SmoothnessSlotId,
        //         HDLitMasterNode.AlphaSlotId,
        //         HDLitMasterNode.AlphaThresholdSlotId,
        //         HDLitMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDLitDistortion = new BlockFieldDescriptor[]
        //     {
        //         HDLitMasterNode.AlphaSlotId,
        //         HDLitMasterNode.AlphaThresholdSlotId,
        //         HDLitMasterNode.DistortionSlotId,
        //         HDLitMasterNode.DistortionBlurSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDLitTransparentDepthPrepass = new BlockFieldDescriptor[]
        //     {
        //         HDLitMasterNode.AlphaSlotId,
        //         HDLitMasterNode.AlphaThresholdDepthPrepassSlotId,
        //         HDLitMasterNode.DepthOffsetSlotId,
        //         HDLitMasterNode.NormalSlotId,
        //         HDLitMasterNode.SmoothnessSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDLitTransparentBackface = new BlockFieldDescriptor[]
        //     {
        //         HDLitMasterNode.AlbedoSlotId,
        //         HDLitMasterNode.NormalSlotId,
        //         HDLitMasterNode.BentNormalSlotId,
        //         HDLitMasterNode.TangentSlotId,
        //         HDLitMasterNode.SubsurfaceMaskSlotId,
        //         HDLitMasterNode.ThicknessSlotId,
        //         HDLitMasterNode.DiffusionProfileHashSlotId,
        //         HDLitMasterNode.IridescenceMaskSlotId,
        //         HDLitMasterNode.IridescenceThicknessSlotId,
        //         HDLitMasterNode.SpecularColorSlotId,
        //         HDLitMasterNode.CoatMaskSlotId,
        //         HDLitMasterNode.MetallicSlotId,
        //         HDLitMasterNode.EmissionSlotId,
        //         HDLitMasterNode.SmoothnessSlotId,
        //         HDLitMasterNode.AmbientOcclusionSlotId,
        //         HDLitMasterNode.SpecularOcclusionSlotId,
        //         HDLitMasterNode.AlphaSlotId,
        //         HDLitMasterNode.AlphaThresholdSlotId,
        //         HDLitMasterNode.AnisotropySlotId,
        //         HDLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
        //         HDLitMasterNode.SpecularAAThresholdSlotId,
        //         HDLitMasterNode.RefractionIndexSlotId,
        //         HDLitMasterNode.RefractionColorSlotId,
        //         HDLitMasterNode.RefractionDistanceSlotId,
        //         HDLitMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HDLitTransparentDepthPostpass = new BlockFieldDescriptor[]
        //     {
        //         HDLitMasterNode.AlphaSlotId,
        //         HDLitMasterNode.AlphaThresholdDepthPrepassSlotId,
        //         HDLitMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] EyeMETA = new BlockFieldDescriptor[]
        //     {
        //         EyeMasterNode.AlbedoSlotId,
        //         EyeMasterNode.SpecularOcclusionSlotId,
        //         EyeMasterNode.NormalSlotId,
        //         EyeMasterNode.IrisNormalSlotId,
        //         EyeMasterNode.SmoothnessSlotId,
        //         EyeMasterNode.IORSlotId,
        //         EyeMasterNode.AmbientOcclusionSlotId,
        //         EyeMasterNode.MaskSlotId,
        //         EyeMasterNode.DiffusionProfileHashSlotId,
        //         EyeMasterNode.SubsurfaceMaskSlotId,
        //         EyeMasterNode.EmissionSlotId,
        //         EyeMasterNode.AlphaSlotId,
        //         EyeMasterNode.AlphaClipThresholdSlotId,
        //     };

        //     public static BlockFieldDescriptor[] EyeAlphaDepth = new BlockFieldDescriptor[]
        //     {
        //         EyeMasterNode.AlphaSlotId,
        //         EyeMasterNode.AlphaClipThresholdSlotId,
        //         EyeMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] EyeDepthMotionVectors = new BlockFieldDescriptor[]
        //     {
        //         EyeMasterNode.NormalSlotId,
        //         EyeMasterNode.SmoothnessSlotId,
        //         EyeMasterNode.AlphaSlotId,
        //         EyeMasterNode.AlphaClipThresholdSlotId,
        //         EyeMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] EyeForward = new BlockFieldDescriptor[]
        //     {
        //         EyeMasterNode.AlbedoSlotId,
        //         EyeMasterNode.SpecularOcclusionSlotId,
        //         EyeMasterNode.NormalSlotId,
        //         EyeMasterNode.IrisNormalSlotId,
        //         EyeMasterNode.SmoothnessSlotId,
        //         EyeMasterNode.IORSlotId,
        //         EyeMasterNode.AmbientOcclusionSlotId,
        //         EyeMasterNode.MaskSlotId,
        //         EyeMasterNode.DiffusionProfileHashSlotId,
        //         EyeMasterNode.SubsurfaceMaskSlotId,
        //         EyeMasterNode.EmissionSlotId,
        //         EyeMasterNode.AlphaSlotId,
        //         EyeMasterNode.AlphaClipThresholdSlotId,
        //         EyeMasterNode.LightingSlotId,
        //         EyeMasterNode.BackLightingSlotId,
        //         EyeMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] FabricMETA = new BlockFieldDescriptor[]
        //     {
        //         FabricMasterNode.AlbedoSlotId,
        //         FabricMasterNode.SpecularOcclusionSlotId,
        //         FabricMasterNode.NormalSlotId,
        //         FabricMasterNode.SmoothnessSlotId,
        //         FabricMasterNode.AmbientOcclusionSlotId,
        //         FabricMasterNode.SpecularColorSlotId,
        //         FabricMasterNode.DiffusionProfileHashSlotId,
        //         FabricMasterNode.SubsurfaceMaskSlotId,
        //         FabricMasterNode.ThicknessSlotId,
        //         FabricMasterNode.TangentSlotId,
        //         FabricMasterNode.AnisotropySlotId,
        //         FabricMasterNode.EmissionSlotId,
        //         FabricMasterNode.AlphaSlotId,
        //         FabricMasterNode.AlphaClipThresholdSlotId,
        //     };

        //     public static BlockFieldDescriptor[] FabricAlphaDepth = new BlockFieldDescriptor[]
        //     {
        //         FabricMasterNode.AlphaSlotId,
        //         FabricMasterNode.AlphaClipThresholdSlotId,
        //         FabricMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] FabricDepthMotionVectors = new BlockFieldDescriptor[]
        //     {
        //         FabricMasterNode.NormalSlotId,
        //         FabricMasterNode.SmoothnessSlotId,
        //         FabricMasterNode.AlphaSlotId,
        //         FabricMasterNode.AlphaClipThresholdSlotId,
        //         FabricMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] FabricForward = new BlockFieldDescriptor[]
        //     {
        //         FabricMasterNode.AlbedoSlotId,
        //         FabricMasterNode.SpecularOcclusionSlotId,
        //         FabricMasterNode.NormalSlotId,
        //         FabricMasterNode.BentNormalSlotId,
        //         FabricMasterNode.SmoothnessSlotId,
        //         FabricMasterNode.AmbientOcclusionSlotId,
        //         FabricMasterNode.SpecularColorSlotId,
        //         FabricMasterNode.DiffusionProfileHashSlotId,
        //         FabricMasterNode.SubsurfaceMaskSlotId,
        //         FabricMasterNode.ThicknessSlotId,
        //         FabricMasterNode.TangentSlotId,
        //         FabricMasterNode.AnisotropySlotId,
        //         FabricMasterNode.EmissionSlotId,
        //         FabricMasterNode.AlphaSlotId,
        //         FabricMasterNode.AlphaClipThresholdSlotId,
        //         FabricMasterNode.LightingSlotId,
        //         FabricMasterNode.BackLightingSlotId,
        //         FabricMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HairMETA = new BlockFieldDescriptor[]
        //     {
        //         HairMasterNode.AlbedoSlotId,
        //         HairMasterNode.NormalSlotId,
        //         HairMasterNode.SpecularOcclusionSlotId,
        //         HairMasterNode.BentNormalSlotId,
        //         HairMasterNode.HairStrandDirectionSlotId,
        //         HairMasterNode.TransmittanceSlotId,
        //         HairMasterNode.RimTransmissionIntensitySlotId,
        //         HairMasterNode.SmoothnessSlotId,
        //         HairMasterNode.AmbientOcclusionSlotId,
        //         HairMasterNode.EmissionSlotId,
        //         HairMasterNode.AlphaSlotId,
        //         HairMasterNode.AlphaClipThresholdSlotId,
        //         HairMasterNode.SpecularAAScreenSpaceVarianceSlotId,
        //         HairMasterNode.SpecularAAThresholdSlotId,
        //         HairMasterNode.SpecularTintSlotId,
        //         HairMasterNode.SpecularShiftSlotId,
        //         HairMasterNode.SecondarySpecularTintSlotId,
        //         HairMasterNode.SecondarySmoothnessSlotId,
        //         HairMasterNode.SecondarySpecularShiftSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HairShadowCaster = new BlockFieldDescriptor[]
        //     {
        //         HairMasterNode.AlphaSlotId,
        //         HairMasterNode.AlphaClipThresholdSlotId,
        //         HairMasterNode.AlphaClipThresholdShadowSlotId,
        //         HairMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HairAlphaDepth = new BlockFieldDescriptor[]
        //     {
        //         HairMasterNode.AlphaSlotId,
        //         HairMasterNode.AlphaClipThresholdSlotId,
        //         HairMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HairDepthMotionVectors = new BlockFieldDescriptor[]
        //     {
        //         HairMasterNode.NormalSlotId,
        //         HairMasterNode.SmoothnessSlotId,
        //         HairMasterNode.AlphaSlotId,
        //         HairMasterNode.AlphaClipThresholdSlotId,
        //         HairMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HairTransparentDepthPrepass = new BlockFieldDescriptor[]
        //     {
        //         HairMasterNode.AlphaSlotId,
        //         HairMasterNode.AlphaClipThresholdDepthPrepassSlotId,
        //         HairMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HairTransparentBackface = new BlockFieldDescriptor[]
        //     {
        //         HairMasterNode.AlbedoSlotId,
        //         HairMasterNode.NormalSlotId,
        //         HairMasterNode.SpecularOcclusionSlotId,
        //         HairMasterNode.BentNormalSlotId,
        //         HairMasterNode.HairStrandDirectionSlotId,
        //         HairMasterNode.TransmittanceSlotId,
        //         HairMasterNode.RimTransmissionIntensitySlotId,
        //         HairMasterNode.SmoothnessSlotId,
        //         HairMasterNode.AmbientOcclusionSlotId,
        //         HairMasterNode.EmissionSlotId,
        //         HairMasterNode.AlphaSlotId,
        //         HairMasterNode.AlphaClipThresholdSlotId,
        //         HairMasterNode.SpecularAAScreenSpaceVarianceSlotId,
        //         HairMasterNode.SpecularAAThresholdSlotId,
        //         HairMasterNode.SpecularTintSlotId,
        //         HairMasterNode.SpecularShiftSlotId,
        //         HairMasterNode.SecondarySpecularTintSlotId,
        //         HairMasterNode.SecondarySmoothnessSlotId,
        //         HairMasterNode.SecondarySpecularShiftSlotId,
        //         HairMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HairForward = new BlockFieldDescriptor[]
        //     {
        //         HairMasterNode.AlbedoSlotId,
        //         HairMasterNode.NormalSlotId,
        //         HairMasterNode.SpecularOcclusionSlotId,
        //         HairMasterNode.BentNormalSlotId,
        //         HairMasterNode.HairStrandDirectionSlotId,
        //         HairMasterNode.TransmittanceSlotId,
        //         HairMasterNode.RimTransmissionIntensitySlotId,
        //         HairMasterNode.SmoothnessSlotId,
        //         HairMasterNode.AmbientOcclusionSlotId,
        //         HairMasterNode.EmissionSlotId,
        //         HairMasterNode.AlphaSlotId,
        //         HairMasterNode.AlphaClipThresholdSlotId,
        //         HairMasterNode.SpecularAAScreenSpaceVarianceSlotId,
        //         HairMasterNode.SpecularAAThresholdSlotId,
        //         HairMasterNode.SpecularTintSlotId,
        //         HairMasterNode.SpecularShiftSlotId,
        //         HairMasterNode.SecondarySpecularTintSlotId,
        //         HairMasterNode.SecondarySmoothnessSlotId,
        //         HairMasterNode.SecondarySpecularShiftSlotId,
        //         HairMasterNode.LightingSlotId,
        //         HairMasterNode.BackLightingSlotId,
        //         HairMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] HairTransparentDepthPostpass = new BlockFieldDescriptor[]
        //     {
        //         HairMasterNode.AlphaSlotId,
        //         HairMasterNode.AlphaClipThresholdDepthPostpassSlotId,
        //         HairMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] StackLitMETA = new BlockFieldDescriptor[]
        //     {
        //         StackLitMasterNode.BaseColorSlotId,
        //         StackLitMasterNode.NormalSlotId,
        //         StackLitMasterNode.BentNormalSlotId,
        //         StackLitMasterNode.TangentSlotId,
        //         StackLitMasterNode.SubsurfaceMaskSlotId,
        //         StackLitMasterNode.ThicknessSlotId,
        //         StackLitMasterNode.DiffusionProfileHashSlotId,
        //         StackLitMasterNode.IridescenceMaskSlotId,
        //         StackLitMasterNode.IridescenceThicknessSlotId,
        //         StackLitMasterNode.IridescenceCoatFixupTIRSlotId,
        //         StackLitMasterNode.IridescenceCoatFixupTIRClampSlotId,
        //         StackLitMasterNode.SpecularColorSlotId,
        //         StackLitMasterNode.DielectricIorSlotId,
        //         StackLitMasterNode.MetallicSlotId,
        //         StackLitMasterNode.EmissionSlotId,
        //         StackLitMasterNode.SmoothnessASlotId,
        //         StackLitMasterNode.SmoothnessBSlotId,
        //         StackLitMasterNode.AmbientOcclusionSlotId,
        //         StackLitMasterNode.AlphaSlotId,
        //         StackLitMasterNode.AlphaClipThresholdSlotId,
        //         StackLitMasterNode.AnisotropyASlotId,
        //         StackLitMasterNode.AnisotropyBSlotId,
        //         StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
        //         StackLitMasterNode.SpecularAAThresholdSlotId,
        //         StackLitMasterNode.CoatSmoothnessSlotId,
        //         StackLitMasterNode.CoatIorSlotId,
        //         StackLitMasterNode.CoatThicknessSlotId,
        //         StackLitMasterNode.CoatExtinctionSlotId,
        //         StackLitMasterNode.CoatNormalSlotId,
        //         StackLitMasterNode.CoatMaskSlotId,
        //         StackLitMasterNode.LobeMixSlotId,
        //         StackLitMasterNode.HazinessSlotId,
        //         StackLitMasterNode.HazeExtentSlotId,
        //         StackLitMasterNode.HazyGlossMaxDielectricF0SlotId,
        //         StackLitMasterNode.SpecularOcclusionSlotId,
        //         StackLitMasterNode.SOFixupVisibilityRatioThresholdSlotId,
        //         StackLitMasterNode.SOFixupStrengthFactorSlotId,
        //         StackLitMasterNode.SOFixupMaxAddedRoughnessSlotId,
        //     };

        //     public static BlockFieldDescriptor[] StackLitAlphaDepth = new BlockFieldDescriptor[]
        //     {
        //         StackLitMasterNode.AlphaSlotId,
        //         StackLitMasterNode.AlphaClipThresholdSlotId,
        //         StackLitMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] StackLitDepthMotionVectors = new BlockFieldDescriptor[]
        //     {
        //         StackLitMasterNode.AlphaSlotId,
        //         StackLitMasterNode.AlphaClipThresholdSlotId,
        //         StackLitMasterNode.DepthOffsetSlotId,
        //         // StackLitMasterNode.coat
        //         StackLitMasterNode.CoatSmoothnessSlotId,
        //         StackLitMasterNode.CoatNormalSlotId,
        //         // !StackLitMasterNode.coat
        //         StackLitMasterNode.NormalSlotId,
        //         StackLitMasterNode.LobeMixSlotId,
        //         StackLitMasterNode.SmoothnessASlotId,
        //         StackLitMasterNode.SmoothnessBSlotId,
        //         // StackLitMasterNode.geometricSpecularAA
        //         StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
        //         StackLitMasterNode.SpecularAAThresholdSlotId,
        //     };

        //     public static BlockFieldDescriptor[] StackLitDistortion = new BlockFieldDescriptor[]
        //     {
        //         StackLitMasterNode.AlphaSlotId,
        //         StackLitMasterNode.AlphaClipThresholdSlotId,
        //         StackLitMasterNode.DistortionSlotId,
        //         StackLitMasterNode.DistortionBlurSlotId,
        //         StackLitMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] StackLitForward = new BlockFieldDescriptor[]
        //     {
        //         StackLitMasterNode.BaseColorSlotId,
        //         StackLitMasterNode.NormalSlotId,
        //         StackLitMasterNode.BentNormalSlotId,
        //         StackLitMasterNode.TangentSlotId,
        //         StackLitMasterNode.SubsurfaceMaskSlotId,
        //         StackLitMasterNode.ThicknessSlotId,
        //         StackLitMasterNode.DiffusionProfileHashSlotId,
        //         StackLitMasterNode.IridescenceMaskSlotId,
        //         StackLitMasterNode.IridescenceThicknessSlotId,
        //         StackLitMasterNode.IridescenceCoatFixupTIRSlotId,
        //         StackLitMasterNode.IridescenceCoatFixupTIRClampSlotId,
        //         StackLitMasterNode.SpecularColorSlotId,
        //         StackLitMasterNode.DielectricIorSlotId,
        //         StackLitMasterNode.MetallicSlotId,
        //         StackLitMasterNode.EmissionSlotId,
        //         StackLitMasterNode.SmoothnessASlotId,
        //         StackLitMasterNode.SmoothnessBSlotId,
        //         StackLitMasterNode.AmbientOcclusionSlotId,
        //         StackLitMasterNode.AlphaSlotId,
        //         StackLitMasterNode.AlphaClipThresholdSlotId,
        //         StackLitMasterNode.AnisotropyASlotId,
        //         StackLitMasterNode.AnisotropyBSlotId,
        //         StackLitMasterNode.SpecularAAScreenSpaceVarianceSlotId,
        //         StackLitMasterNode.SpecularAAThresholdSlotId,
        //         StackLitMasterNode.CoatSmoothnessSlotId,
        //         StackLitMasterNode.CoatIorSlotId,
        //         StackLitMasterNode.CoatThicknessSlotId,
        //         StackLitMasterNode.CoatExtinctionSlotId,
        //         StackLitMasterNode.CoatNormalSlotId,
        //         StackLitMasterNode.CoatMaskSlotId,
        //         StackLitMasterNode.LobeMixSlotId,
        //         StackLitMasterNode.HazinessSlotId,
        //         StackLitMasterNode.HazeExtentSlotId,
        //         StackLitMasterNode.HazyGlossMaxDielectricF0SlotId,
        //         StackLitMasterNode.SpecularOcclusionSlotId,
        //         StackLitMasterNode.SOFixupVisibilityRatioThresholdSlotId,
        //         StackLitMasterNode.SOFixupStrengthFactorSlotId,
        //         StackLitMasterNode.SOFixupMaxAddedRoughnessSlotId,
        //         StackLitMasterNode.LightingSlotId,
        //         StackLitMasterNode.BackLightingSlotId,
        //         StackLitMasterNode.DepthOffsetSlotId,
        //     };

        //     public static BlockFieldDescriptor[] DecalDefault = new BlockFieldDescriptor[]
        //     {
        //         DecalMasterNode.AlbedoSlotId,
        //         DecalMasterNode.BaseColorOpacitySlotId,
        //         DecalMasterNode.NormalSlotId,
        //         DecalMasterNode.NormaOpacitySlotId,
        //         DecalMasterNode.MetallicSlotId,
        //         DecalMasterNode.AmbientOcclusionSlotId,
        //         DecalMasterNode.SmoothnessSlotId,
        //         DecalMasterNode.MAOSOpacitySlotId,
        //     };

        //     public static BlockFieldDescriptor[] DecalEmissive = new BlockFieldDescriptor[]
        //     {
        //         DecalMasterNode.EmissionSlotId
        //     };

        //     public static BlockFieldDescriptor[] DecalMeshEmissive = new BlockFieldDescriptor[]
        //     {
        //         DecalMasterNode.AlbedoSlotId,
        //         DecalMasterNode.BaseColorOpacitySlotId,
        //         DecalMasterNode.NormalSlotId,
        //         DecalMasterNode.NormaOpacitySlotId,
        //         DecalMasterNode.MetallicSlotId,
        //         DecalMasterNode.AmbientOcclusionSlotId,
        //         DecalMasterNode.SmoothnessSlotId,
        //         DecalMasterNode.MAOSOpacitySlotId,
        //         DecalMasterNode.EmissionSlotId,
        //     };
        // }
    }
}
