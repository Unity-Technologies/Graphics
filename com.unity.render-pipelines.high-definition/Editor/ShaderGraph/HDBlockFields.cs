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

            // --------------------------------------------------
            // Decal

            public static BlockFieldDescriptor NormalAlpha = new BlockFieldDescriptor(SurfaceDescription.name, "NormalAlpha", "SURFACEDESCRIPTION_NORMALALPHA", 
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor MAOSAlpha = new BlockFieldDescriptor(SurfaceDescription.name, "MAOSAlpha", "SURFACEDESCRIPTION_MAOSALPHA", 
                new FloatControl(1.0f), ShaderStage.Fragment);

            // --------------------------------------------------
            // Eye

            public static BlockFieldDescriptor IrisNormal = new BlockFieldDescriptor(SurfaceDescription.name, "IrisNormal", "SURFACEDESCRIPTION_IRISNORMAL",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor IOR = new BlockFieldDescriptor(SurfaceDescription.name, "IOR", "SURFACEDESCRIPTION_IOR", 
                new FloatControl(1.4f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Mask = new BlockFieldDescriptor(SurfaceDescription.name, "Mask", "SURFACEDESCRIPTION_MASK", 
                new Vector2Control(new Vector2(1.0f, 0.0f)), ShaderStage.Fragment);

            // --------------------------------------------------
            // Hair

            public static BlockFieldDescriptor Transmittance = new BlockFieldDescriptor(SurfaceDescription.name, "Transmittance", "SURFACEDESCRIPTION_TRANSMITTANCE", 
                new Vector3Control(0.3f * new Vector3(1.0f, 0.65f, 0.3f)), ShaderStage.Fragment);
            public static BlockFieldDescriptor RimTransmissionIntensity = new BlockFieldDescriptor(SurfaceDescription.name, "RimTransmissionIntensity", "SURFACEDESCRIPTION_RIMTRANSMISSIONINTENSITY", 
                new FloatControl(0.2f), ShaderStage.Fragment);
            public static BlockFieldDescriptor HairStrandDirection = new BlockFieldDescriptor(SurfaceDescription.name, "HairStrandDirection", "SURFACEDESCRIPTION_HAIRSTRANDDIRECTION", 
                new Vector3Control(new Vector3(0, -1, 0)), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularTint = new BlockFieldDescriptor(SurfaceDescription.name, "SpecularTint", "SURFACEDESCRIPTION_SPECULARTINT",
                new ColorControl(Color.white, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularShift = new BlockFieldDescriptor(SurfaceDescription.name, "SpecularShift", "SURFACEDESCRIPTION_SPECULARSHIFT", 
                new FloatControl(0.1f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SecondarySpecularTint = new BlockFieldDescriptor(SurfaceDescription.name, "SecondarySpecularTint", "SURFACEDESCRIPTION_SECONDARYSPECULARTINT",
                new ColorControl(Color.grey, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor SecondarySmoothness = new BlockFieldDescriptor(SurfaceDescription.name, "SecondarySmoothness", "SURFACEDESCRIPTION_SECONDARYSMOOTHNESS", 
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SecondarySpecularShift = new BlockFieldDescriptor(SurfaceDescription.name, "SecondarySpecularShift", "SURFACEDESCRIPTION_SECONDARYSPECULARSHIFT", 
                new FloatControl(-0.1f), ShaderStage.Fragment);
        }
    }
}
