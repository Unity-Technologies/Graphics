using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    internal static class HDBlockFields
    {
        [GenerateBlocks]
        public struct SurfaceDescription
        {
            public static string name = "SurfaceDescription";

            // --------------------------------------------------
            // Unlit

            public static BlockFieldDescriptor Distortion = new BlockFieldDescriptor(SurfaceDescription.name, "Distortion", "SURFACEDESCRIPTION_DISTORTION",
                new Vector2Control(Vector2.zero), ShaderStage.Fragment); // TODO: Lit is Vector2(2.0f, -1.0f)
            public static BlockFieldDescriptor DistortionBlur = new BlockFieldDescriptor(SurfaceDescription.name, "DistortionBlur", "SURFACEDESCRIPTION_DISTORTIONBLUR",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor ShadowTint = new BlockFieldDescriptor(SurfaceDescription.name, "ShadowTint", "SURFACEDESCRIPTION_SHADOWTINT",
                new ColorRGBAControl(Color.black), ShaderStage.Fragment);

            // --------------------------------------------------
            // Lit

            public static BlockFieldDescriptor BentNormal = new BlockFieldDescriptor(SurfaceDescription.name, "BentNormal", "SURFACEDESCRIPTION_BENTNORMAL",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor Tangent = new BlockFieldDescriptor(SurfaceDescription.name, "Tangent", "SURFACEDESCRIPTION_TANGENT",
                new TangentControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor Anisotropy = new BlockFieldDescriptor(SurfaceDescription.name, "Anisotropy", "SURFACEDESCRIPTION_ANISOTROPY", 
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SubsurfaceMask = new BlockFieldDescriptor(SurfaceDescription.name, "SubsurfaceMask", "SURFACEDESCRIPTION_SUBSURFACEMASK", 
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Thickness = new BlockFieldDescriptor(SurfaceDescription.name, "Thickness", "SURFACEDESCRIPTION_THICKNESS", 
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static CustomSlotBlockFieldDescriptor DiffusionProfileHash = new CustomSlotBlockFieldDescriptor(SurfaceDescription.name, "DiffusionProfileHash", "SURFACEDESCRIPTION_DIFFUSIONPROFILEHASH", 
                new DiffusionProfileInputMaterialSlot(0, "DiffusionProfile", "DiffusionProfileHash", ShaderStageCapability.Fragment));
            public static BlockFieldDescriptor IridescenceMask = new BlockFieldDescriptor(SurfaceDescription.name, "IridescenceMask", "SURFACEDESCRIPTION_IRIDESCENCEMASK", 
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor IridescenceThickness = new BlockFieldDescriptor(SurfaceDescription.name, "IridescenceThickness", "SURFACEDESCRIPTION_IRIDESCENCETHICKNESS", 
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatMask = new BlockFieldDescriptor(SurfaceDescription.name, "CoatMask", "SURFACEDESCRIPTION_COATMASK", 
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularOcclusion = new BlockFieldDescriptor(SurfaceDescription.name, "SpecularOcclusion", "SURFACEDESCRIPTION_SPECULAROCCLUSION", 
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThresholdDepthPrepass = new BlockFieldDescriptor(SurfaceDescription.name, "AlphaClipThresholdDepthPrepass", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLDDEPTHPREPASS", 
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThresholdDepthPostpass = new BlockFieldDescriptor(SurfaceDescription.name, "AlphaClipThresholdDepthPostpass", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLDDEPTHPOSTPASS", 
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThresholdShadow = new BlockFieldDescriptor(SurfaceDescription.name, "AlphaClipThresholdShadow", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLDSHADOW", 
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularAAScreenSpaceVariance = new BlockFieldDescriptor(SurfaceDescription.name, "SpecularAAScreenSpaceVariance", "SURFACEDESCRIPTION_SPECULARAASCEENSPACEVARIANCE", 
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularAAThreshold = new BlockFieldDescriptor(SurfaceDescription.name, "SpecularAAThreshold", "SURFACEDESCRIPTION_SPECULARAATHRESHOLD", 
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static CustomSlotBlockFieldDescriptor BakedGI = new CustomSlotBlockFieldDescriptor(SurfaceDescription.name, "BakedGI", "SURFACEDESCRIPTION_BAKEDGI", 
                new DefaultMaterialSlot(0, "BakedGI", "BakedGI", ShaderStageCapability.Fragment));
            public static CustomSlotBlockFieldDescriptor BakedBackGI = new CustomSlotBlockFieldDescriptor(SurfaceDescription.name, "BakedBackGI", "SURFACEDESCRIPTION_BAKEDBACKGI", 
                new DefaultMaterialSlot(0, "BakedBackGI", "BakedBackGI", ShaderStageCapability.Fragment));
            public static BlockFieldDescriptor DepthOffset = new BlockFieldDescriptor(SurfaceDescription.name, "DepthOffset", "SURFACEDESCRIPTION_DEPTHOFFSET", 
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractionIndex = new BlockFieldDescriptor(SurfaceDescription.name, "RefractionIndex", "SURFACEDESCRIPTION_REFRACTIONINDEX", 
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractionColor = new BlockFieldDescriptor(SurfaceDescription.name, "RefractionColor", "SURFACEDESCRIPTION_REFRACTIONCOLOR",
                new ColorControl(Color.white, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractionDistance = new BlockFieldDescriptor(SurfaceDescription.name, "RefractionDistance", "SURFACEDESCRIPTION_REFRACTIONDISTANCE", 
                new FloatControl(1.0f), ShaderStage.Fragment);
        }
    }
}
