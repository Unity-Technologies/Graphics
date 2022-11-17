using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    internal static class HDBlockFields
    {
        [GenerateBlocks("High Definition Render Pipeline")]
        public struct VertexDescription
        {
            public static string name = "VertexDescription";

            public static BlockFieldDescriptor CustomVelocity = new BlockFieldDescriptor(VertexDescription.name, "CustomVelocity", "Velocity", "VERTEXDESCRIPTION_CUSTOMVELOCITY",
                new Vector3Control(new Vector3(0.0f, 0.0f, 0.0f)), ShaderStage.Vertex);

            // TODO: In the future we may have proper ShaderStage for tessellation, this will need to be revisit then
            public static BlockFieldDescriptor TessellationFactor = new BlockFieldDescriptor(VertexDescription.name, "TessellationFactor", "Tessellation Factor", "VERTEXDESCRIPTION_TESSELLATIONFACTOR",
                new FloatControl(1.0f), ShaderStage.Vertex);
            public static BlockFieldDescriptor TessellationDisplacement = new BlockFieldDescriptor(VertexDescription.name, "TessellationDisplacement", "Tessellation Displacement", "VERTEXDESCRIPTION_TESSELLATIONDISPLACEMENT",
                new Vector3Control(new Vector3(0.0f, 0.0f, 0.0f)), ShaderStage.Vertex);
            public static BlockFieldDescriptor UV0 = new BlockFieldDescriptor(VertexDescription.name, "uv0", "VERTEXDESCRIPTION_UV0",
                new Vector4Control(Vector4.zero), ShaderStage.Vertex);
            public static BlockFieldDescriptor UV1 = new BlockFieldDescriptor(VertexDescription.name, "uv1", "VERTEXDESCRIPTION_UV1",
                new Vector4Control(Vector4.zero), ShaderStage.Vertex);
            public static BlockFieldDescriptor UV2 = new BlockFieldDescriptor(VertexDescription.name, "uv2", "VERTEXDESCRIPTION_UV2",
                new Vector4Control(Vector4.zero), ShaderStage.Vertex);
            public static BlockFieldDescriptor UV3 = new BlockFieldDescriptor(VertexDescription.name, "uv3", "VERTEXDESCRIPTION_UV3",
                new Vector4Control(Vector4.zero), ShaderStage.Vertex);
        }

        [GenerateBlocks("High Definition Render Pipeline")]
        public struct SurfaceDescription
        {
            public static string name = "SurfaceDescription";

            // --------------------------------------------------
            // Unlit

            public static BlockFieldDescriptor Distortion = new BlockFieldDescriptor(SurfaceDescription.name, "Distortion", "SURFACEDESCRIPTION_DISTORTION",
                new Vector2Control(Vector2.zero), ShaderStage.Fragment); // TODO: Lit is Vector2(2.0f, -1.0f)
            public static BlockFieldDescriptor DistortionBlur = new BlockFieldDescriptor(SurfaceDescription.name, "DistortionBlur", "Distortion Blur", "SURFACEDESCRIPTION_DISTORTIONBLUR",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor ShadowTint = new BlockFieldDescriptor(SurfaceDescription.name, "ShadowTint", "Shadow Tint", "SURFACEDESCRIPTION_SHADOWTINT",
                new ColorRGBAControl(Color.black), ShaderStage.Fragment);

            // --------------------------------------------------
            // Lit

            public static BlockFieldDescriptor BentNormal = new BlockFieldDescriptor(SurfaceDescription.name, "BentNormal", "Bent Normal", "SURFACEDESCRIPTION_BENTNORMAL",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor TangentTS = new BlockFieldDescriptor(SurfaceDescription.name, "TangentTS", "Tangent (Tangent Space)", "SURFACEDESCRIPTION_TANGENTTS",
                new TangentControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor TangentOS = new BlockFieldDescriptor(SurfaceDescription.name, "TangentOS", "Tangent (Object Space)", "SURFACEDESCRIPTION_TANGENTOS",
                new TangentControl(CoordinateSpace.Object), ShaderStage.Fragment);
            public static BlockFieldDescriptor TangentWS = new BlockFieldDescriptor(SurfaceDescription.name, "TangentWS", "Tangent (World Space)", "SURFACEDESCRIPTION_TANGENTWS",
                new TangentControl(CoordinateSpace.World), ShaderStage.Fragment);

            public static BlockFieldDescriptor Anisotropy = new BlockFieldDescriptor(SurfaceDescription.name, "Anisotropy", "SURFACEDESCRIPTION_ANISOTROPY",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SubsurfaceMask = new BlockFieldDescriptor(SurfaceDescription.name, "SubsurfaceMask", "Subsurface Mask", "SURFACEDESCRIPTION_SUBSURFACEMASK",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor TransmissionMask = new BlockFieldDescriptor(SurfaceDescription.name, "TransmissionMask", "Transmission Mask", "SURFACEDESCRIPTION_TRANSMISSIONMASK",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Thickness = new BlockFieldDescriptor(SurfaceDescription.name, "Thickness", "SURFACEDESCRIPTION_THICKNESS",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static CustomSlotBlockFieldDescriptor DiffusionProfileHash = new CustomSlotBlockFieldDescriptor(SurfaceDescription.name, "DiffusionProfileHash", "Diffusion Profile", "SURFACEDESCRIPTION_DIFFUSIONPROFILEHASH",
                () => { return new DiffusionProfileInputMaterialSlot(0, "Diffusion Profile", "DiffusionProfileHash", ShaderStageCapability.Fragment); });
            public static BlockFieldDescriptor IridescenceMask = new BlockFieldDescriptor(SurfaceDescription.name, "IridescenceMask", "Iridescence Mask", "SURFACEDESCRIPTION_IRIDESCENCEMASK",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor IridescenceThickness = new BlockFieldDescriptor(SurfaceDescription.name, "IridescenceThickness", "Iridescence Thickness", "SURFACEDESCRIPTION_IRIDESCENCETHICKNESS",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularOcclusion = new BlockFieldDescriptor(SurfaceDescription.name, "SpecularOcclusion", "Specular Occlusion", "SURFACEDESCRIPTION_SPECULAROCCLUSION",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThresholdDepthPrepass = new BlockFieldDescriptor(SurfaceDescription.name, "AlphaClipThresholdDepthPrepass", "Alpha Clip Threshold Depth Prepass", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLDDEPTHPREPASS",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThresholdDepthPostpass = new BlockFieldDescriptor(SurfaceDescription.name, "AlphaClipThresholdDepthPostpass", "Alpha Clip Threshold Depth Postpass", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLDDEPTHPOSTPASS",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThresholdShadow = new BlockFieldDescriptor(SurfaceDescription.name, "AlphaClipThresholdShadow", "Alpha Clip Threshold Shadow", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLDSHADOW",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularAAScreenSpaceVariance = new BlockFieldDescriptor(SurfaceDescription.name, "SpecularAAScreenSpaceVariance", "Specular AA Screen Space Variance", "SURFACEDESCRIPTION_SPECULARAASCEENSPACEVARIANCE",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularAAThreshold = new BlockFieldDescriptor(SurfaceDescription.name, "SpecularAAThreshold", "Specular AA Threshold", "SURFACEDESCRIPTION_SPECULARAATHRESHOLD",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static CustomSlotBlockFieldDescriptor BakedGI = new CustomSlotBlockFieldDescriptor(SurfaceDescription.name, "BakedGI", "Baked GI", "SURFACEDESCRIPTION_BAKEDGI",
                () => { return new DefaultMaterialSlot(0, "Baked GI", "BakedGI", ShaderStageCapability.Fragment); });
            public static CustomSlotBlockFieldDescriptor BakedBackGI = new CustomSlotBlockFieldDescriptor(SurfaceDescription.name, "BakedBackGI", "Baked Back GI", "SURFACEDESCRIPTION_BAKEDBACKGI",
                () => { return new DefaultMaterialSlot(0, "Baked Back GI", "BakedBackGI", ShaderStageCapability.Fragment); });
            public static BlockFieldDescriptor DepthOffset = new BlockFieldDescriptor(SurfaceDescription.name, "DepthOffset", "Depth Offset", "SURFACEDESCRIPTION_DEPTHOFFSET",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractionIndex = new BlockFieldDescriptor(SurfaceDescription.name, "RefractionIndex", "Index Of Refraction", "SURFACEDESCRIPTION_REFRACTIONINDEX",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractionColor = new BlockFieldDescriptor(SurfaceDescription.name, "RefractionColor", "Transmittance Color", "SURFACEDESCRIPTION_REFRACTIONCOLOR",
                new ColorControl(Color.white, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractionDistance = new BlockFieldDescriptor(SurfaceDescription.name, "RefractionDistance", "Absorption Distance", "SURFACEDESCRIPTION_REFRACTIONDISTANCE",
                new FloatControl(1.0f), ShaderStage.Fragment);

            // --------------------------------------------------
            // Decal

            public static BlockFieldDescriptor NormalAlpha = new BlockFieldDescriptor(SurfaceDescription.name, "NormalAlpha", "Normal Alpha", "SURFACEDESCRIPTION_NORMALALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor MAOSAlpha = new BlockFieldDescriptor(SurfaceDescription.name, "MAOSAlpha", "MAOS Alpha", "SURFACEDESCRIPTION_MAOSALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment);

            // --------------------------------------------------
            // Eye

            public static BlockFieldDescriptor IrisNormalTS = new BlockFieldDescriptor(SurfaceDescription.name, "IrisNormalTS", "Iris Normal (Tangent Space)", "SURFACEDESCRIPTION_IRISNORMALTS",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor IrisNormalOS = new BlockFieldDescriptor(SurfaceDescription.name, "IrisNormalOS", "Iris Normal (Object Space)", "SURFACEDESCRIPTION_IRISNORMALOS",
                new NormalControl(CoordinateSpace.Object), ShaderStage.Fragment);
            public static BlockFieldDescriptor IrisNormalWS = new BlockFieldDescriptor(SurfaceDescription.name, "IrisNormalWS", "Iris Normal (World Space)", "SURFACEDESCRIPTION_IRISNORMALWS",
                new NormalControl(CoordinateSpace.World), ShaderStage.Fragment);
            public static BlockFieldDescriptor IOR = new BlockFieldDescriptor(SurfaceDescription.name, "IOR", "Eye IOR", "SURFACEDESCRIPTION_IOR",
                new FloatControl(1.4f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Mask = new BlockFieldDescriptor(SurfaceDescription.name, "Mask", "SURFACEDESCRIPTION_MASK",
                new Vector2Control(new Vector2(1.0f, 0.0f)), ShaderStage.Fragment);
            public static BlockFieldDescriptor IrisPlaneOffset = new BlockFieldDescriptor(SurfaceDescription.name, "IrisPlaneOffset", "Iris Plane Offset", "SURFACEDESCRIPTION_IRISPLANEOFFSET",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor IrisRadius = new BlockFieldDescriptor(SurfaceDescription.name, "IrisRadius", "Iris Radius", "SURFACEDESCRIPTION_IRISRADIUS",
                new FloatControl(0.22f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CausticIntensity = new BlockFieldDescriptor(SurfaceDescription.name, "CausticIntensity", "Iris Caustic Intensity Multiplier", "SURFACEDESCRIPTION_CAUSTICINTENSITY",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CausticBlend = new BlockFieldDescriptor(SurfaceDescription.name, "CausticBlend", "Iris Caustic Blend", "SURFACEDESCRIPTION_CAUSTICBLEND",
                new FloatControl(1.0f), ShaderStage.Fragment);

            // --------------------------------------------------
            // Hair

            public static BlockFieldDescriptor Transmittance = new BlockFieldDescriptor(SurfaceDescription.name, "Transmittance", "SURFACEDESCRIPTION_TRANSMITTANCE",
                new Vector3Control(0.3f * new Vector3(1.0f, 0.65f, 0.3f)), ShaderStage.Fragment);
            public static BlockFieldDescriptor RimTransmissionIntensity = new BlockFieldDescriptor(SurfaceDescription.name, "RimTransmissionIntensity", "Rim Transmission Intensity", "SURFACEDESCRIPTION_RIMTRANSMISSIONINTENSITY",
                new FloatControl(0.2f), ShaderStage.Fragment);
            public static BlockFieldDescriptor HairStrandDirection = new BlockFieldDescriptor(SurfaceDescription.name, "HairStrandDirection", "Hair Strand Direction", "SURFACEDESCRIPTION_HAIRSTRANDDIRECTION",
                new Vector3Control(new Vector3(0, -1, 0)), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularTint = new BlockFieldDescriptor(SurfaceDescription.name, "SpecularTint", "Specular Tint", "SURFACEDESCRIPTION_SPECULARTINT",
                new ColorControl(Color.white, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularShift = new BlockFieldDescriptor(SurfaceDescription.name, "SpecularShift", "Specular Shift", "SURFACEDESCRIPTION_SPECULARSHIFT",
                new FloatControl(0.1f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SecondarySpecularTint = new BlockFieldDescriptor(SurfaceDescription.name, "SecondarySpecularTint", "Secondary Specular Tint", "SURFACEDESCRIPTION_SECONDARYSPECULARTINT",
                new ColorControl(Color.grey, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor SecondarySmoothness = new BlockFieldDescriptor(SurfaceDescription.name, "SecondarySmoothness", "Secondary Smoothness", "SURFACEDESCRIPTION_SECONDARYSMOOTHNESS",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SecondarySpecularShift = new BlockFieldDescriptor(SurfaceDescription.name, "SecondarySpecularShift", "Secondary Specular Shift", "SURFACEDESCRIPTION_SECONDARYSPECULARSHIFT",
                new FloatControl(-0.1f), ShaderStage.Fragment);
            public static BlockFieldDescriptor RadialSmoothness = new BlockFieldDescriptor(SurfaceDescription.name, "RadialSmoothness", "Radial Smoothness", "SURFACEDESCRIPTION_RADIALSMOOTHNESS",
                new FloatControl(0.7f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CuticleAngle = new BlockFieldDescriptor(SurfaceDescription.name, "CuticleAngle", "Cuticle Angle", "SURFACEDESCRIPTION_CUTICLEANGLE",
                new FloatControl(3f), ShaderStage.Fragment);
            public static BlockFieldDescriptor StrandCountProbe = new BlockFieldDescriptor(SurfaceDescription.name, "StrandCountProbe", "Strand Count Probe", "SURFACEDESCRIPTION_STRANDCOUNTPROBE",
                new Vector4Control(Vector4.zero), ShaderStage.Fragment);
            public static BlockFieldDescriptor StrandShadowBias = new BlockFieldDescriptor(SurfaceDescription.name, "StrandShadowBias", "Strand Shadow Bias", "SURFACEDESCRIPTION_STRANDSHADOWBIAS",
                new FloatControl(0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AbsorptionCoefficient = new BlockFieldDescriptor(SurfaceDescription.name, "AbsorptionCoefficient", "Absorption", "SURFACEDESCRIPTION_ABSORPTIONCOEFFICIENT",
                new Vector3Control(new Vector3(.06f, 0.1f, 0.2f)), ShaderStage.Fragment);
            public static BlockFieldDescriptor Eumelanin = new BlockFieldDescriptor(SurfaceDescription.name, "Eumelanin", "Eumelanin", "SURFACEDESCRIPTION_EUMELANIN",
                new FloatControl(0.3f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Pheomelanin = new BlockFieldDescriptor(SurfaceDescription.name, "Pheomelanin", "Pheomelanin", "SURFACEDESCRIPTION_PHEOMELANIN",
                new FloatControl(0.3f), ShaderStage.Fragment);


            // --------------------------------------------------
            // StackLit

            public static BlockFieldDescriptor CoatNormalOS = new BlockFieldDescriptor(SurfaceDescription.name, "CoatNormalOS", "Coat Normal (Object Space)", "SURFACEDESCRIPTION_COATNORMALOS",
                new NormalControl(CoordinateSpace.Object), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatNormalTS = new BlockFieldDescriptor(SurfaceDescription.name, "CoatNormalTS", "Coat Normal (Tangent Space)", "SURFACEDESCRIPTION_COATNORMALTS",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatNormalWS = new BlockFieldDescriptor(SurfaceDescription.name, "CoatNormalWS", "Coat Normal (World Space)", "SURFACEDESCRIPTION_COATNORMALWS",
                new NormalControl(CoordinateSpace.World), ShaderStage.Fragment);
            public static BlockFieldDescriptor DielectricIor = new BlockFieldDescriptor(SurfaceDescription.name, "DielectricIor", "Dielectric IOR", "SURFACEDESCRIPTION_DIELECTRICIOR",
                new FloatControl(1.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SmoothnessB = new BlockFieldDescriptor(SurfaceDescription.name, "SmoothnessB", "Smoothness B", "SURFACEDESCRIPTION_SMOOTHNESSB",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor LobeMix = new BlockFieldDescriptor(SurfaceDescription.name, "LobeMix", "Lobe Mix", "SURFACEDESCRIPTION_LOBEMIX",
                new FloatControl(0.3f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AnisotropyB = new BlockFieldDescriptor(SurfaceDescription.name, "AnisotropyB", "Anisotropy B", "SURFACEDESCRIPTION_ANISOTROPYB",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SOFixupVisibilityRatioThreshold = new BlockFieldDescriptor(SurfaceDescription.name, "SOFixupVisibilityRatioThreshold", "SO Fixup Visibility Ratio Threshold", "SURFACEDESCRIPTION_SOFIXUPVISIBILITYRATIOTHRESHOLD",
                new FloatControl(0.2f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SOFixupStrengthFactor = new BlockFieldDescriptor(SurfaceDescription.name, "SOFixupStrengthFactor", "SO Fixup Strength Factor", "SURFACEDESCRIPTION_SOFIXUPSTRENGTHFACTOR",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SOFixupMaxAddedRoughness = new BlockFieldDescriptor(SurfaceDescription.name, "SOFixupMaxAddedRoughness", "SO Fixup Max Added Roughness", "SURFACEDESCRIPTION_SOFIXUPMAXADDEDROUGHNESS",
                new FloatControl(0.2f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatIor = new BlockFieldDescriptor(SurfaceDescription.name, "CoatIor", "Coat IOR", "SURFACEDESCRIPTION_COATIOR",
                new FloatControl(1.4f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatThickness = new BlockFieldDescriptor(SurfaceDescription.name, "CoatThickness", "Coat Thickness", "SURFACEDESCRIPTION_COATTHICKNESS",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatExtinction = new BlockFieldDescriptor(SurfaceDescription.name, "CoatExtinction", "Coat Extinction", "SURFACEDESCRIPTION_COATEXTINCTION",
                new ColorControl(Color.white, true), ShaderStage.Fragment);
            public static BlockFieldDescriptor Haziness = new BlockFieldDescriptor(SurfaceDescription.name, "Haziness", "SURFACEDESCRIPTION_HAZINESS",
                new FloatControl(0.2f), ShaderStage.Fragment);
            public static BlockFieldDescriptor HazeExtent = new BlockFieldDescriptor(SurfaceDescription.name, "HazeExtent", "Haze Extent", "SURFACEDESCRIPTION_HAZEEXTENT",
                new FloatControl(3.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor HazyGlossMaxDielectricF0 = new BlockFieldDescriptor(SurfaceDescription.name, "HazyGlossMaxDielectricF0", "Hazy Gloss Max Dielectric F0", "SURFACEDESCRIPTION_HAZYGLOSSMAXDIELECTRICF0",
                new FloatControl(0.25f), ShaderStage.Fragment);
            public static BlockFieldDescriptor IridescenceCoatFixupTIR = new BlockFieldDescriptor(SurfaceDescription.name, "IridescenceCoatFixupTIR", "Iridescence Coat Fixup TIR", "SURFACEDESCRIPTION_IRIDESCENCECOATFIXUPTIR",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor IridescenceCoatFixupTIRClamp = new BlockFieldDescriptor(SurfaceDescription.name, "IridescenceCoatFixupTIRClamp", "Iridescence Coat Fixup TIR Clamp", "SURFACEDESCRIPTION_IRIDESCENCECOATFIXUPTIRCLAMP",
                new FloatControl(0.0f), ShaderStage.Fragment);
        }
    }
}
