using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    internal static class HDBlockFields
    {
        static BlockFieldProviderInfo m_ProviderInfo = new BlockFieldProviderInfo("HDRP");
        // Note: the provider below is specific for the HDBlockFields.SurfaceDescription group below
        // and isn't meant to be constructed except by SG to enumerate the blockfields available and their signature.
        class Provider : BlockFieldProvider
        {
            Provider()
                : base(m_ProviderInfo, () =>
                   {
                       return BlockFields.GetPartialSignatureMapFromGenerateBlockGroup(m_ProviderInfo.uniqueNamespace, typeof(HDBlockFields.SurfaceDescription), HDBlockFields.SurfaceDescription.tagName, s_OldValidSurfaceTagName, s_OldValidSurfaceBlockFieldNames);
                   })
            {}
        }

        // These are valid blockfield names that we know dont collide with UnityEditor.ShaderGraph.BlockFields.
        // Both of these were used with tagnames prefix as serialized identifiers for blockfields, and didnt collide
        // with each other (post fixing the URP/HDRP collision of coatsmoothness and coatmask that is).
        // We save these to recognize previous shadergraphs with this weak serialization.
        // The new serialized blockfield descriptor string uses a unique namespace provided by the IBlockFieldProvider.
        //
        // Nothing should be added to this list, as no other blockfields were known prior to the "ProviderNamespace.Tag.Name"
        // format.
        static string s_OldValidSurfaceTagName = "SurfaceDescription";
        static HashSet<string> s_OldValidSurfaceBlockFieldNames = new HashSet<string>()
        {
            "Distortion",
            "DistortionBlur",
            "ShadowTint",
            "BentNormal",
            "TangentTS",
            "TangentOS",
            "TangentWS",
            "Anisotropy",
            "SubsurfaceMask",
            "Thickness",
            "DiffusionProfileHash",
            "IridescenceMask",
            "IridescenceThickness",
            "SpecularOcclusion",
            "AlphaClipThresholdDepthPrepass",
            "AlphaClipThresholdDepthPostpass",
            "AlphaClipThresholdShadow",
            "SpecularAAScreenSpaceVariance",
            "SpecularAAThreshold",
            "BakedGI",
            "BakedBackGI",
            "DepthOffset",
            "RefractionIndex",
            "RefractionColor",
            "RefractionDistance",
            "NormalAlpha",
            "MAOSAlpha",
            "IrisNormalTS",
            "IrisNormalOS",
            "IrisNormalWS",
            "IOR",
            "Mask",
            "Transmittance",
            "RimTransmissionIntensity",
            "HairStrandDirection",
            "SpecularTint",
            "SpecularShift",
            "SecondarySpecularTint",
            "SecondarySmoothness",
            "SecondarySpecularShift",
            "CoatNormalOS",
            "CoatNormalTS",
            "CoatNormalWS",
            "DielectricIor",
            "SmoothnessB",
            "LobeMix",
            "AnisotropyB",
            "SOFixupVisibilityRatioThreshold",
            "SOFixupStrengthFactor",
            "SOFixupMaxAddedRoughness",
            "CoatIor",
            "CoatThickness",
            "CoatExtinction",
            "Haziness",
            "HazeExtent",
            "HazyGlossMaxDielectricF0",
            "IridescenceCoatFixupTIR",
            "IridescenceCoatFixupTIRClamp",
        };


        [GenerateBlocks("High Definition Render Pipeline")]
        public struct SurfaceDescription
        {
            public static string tagName = "SurfaceDescription";

            // --------------------------------------------------
            // Unlit

            public static BlockFieldDescriptor Distortion = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Distortion", "SURFACEDESCRIPTION_DISTORTION",
                new Vector2Control(Vector2.zero), ShaderStage.Fragment); // TODO: Lit is Vector2(2.0f, -1.0f)
            public static BlockFieldDescriptor DistortionBlur = new BlockFieldDescriptor(m_ProviderInfo, tagName, "DistortionBlur", "Distortion Blur", "SURFACEDESCRIPTION_DISTORTIONBLUR",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor ShadowTint = new BlockFieldDescriptor(m_ProviderInfo, tagName, "ShadowTint", "Shadow Tint", "SURFACEDESCRIPTION_SHADOWTINT",
                new ColorRGBAControl(Color.black), ShaderStage.Fragment);

            // --------------------------------------------------
            // Lit

            public static BlockFieldDescriptor BentNormal = new BlockFieldDescriptor(m_ProviderInfo, tagName, "BentNormal", "Bent Normal", "SURFACEDESCRIPTION_BENTNORMAL",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor TangentTS = new BlockFieldDescriptor(m_ProviderInfo, tagName, "TangentTS", "Tangent (Tangent Space)", "SURFACEDESCRIPTION_TANGENTTS",
                new TangentControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor TangentOS = new BlockFieldDescriptor(m_ProviderInfo, tagName, "TangentOS", "Tangent (Object Space)", "SURFACEDESCRIPTION_TANGENTOS",
                new TangentControl(CoordinateSpace.Object), ShaderStage.Fragment);
            public static BlockFieldDescriptor TangentWS = new BlockFieldDescriptor(m_ProviderInfo, tagName, "TangentWS", "Tangent (World Space)", "SURFACEDESCRIPTION_TANGENTWS",
                new TangentControl(CoordinateSpace.World), ShaderStage.Fragment);

            public static BlockFieldDescriptor Anisotropy = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Anisotropy", "SURFACEDESCRIPTION_ANISOTROPY",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SubsurfaceMask = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SubsurfaceMask", "Subsurface Mask", "SURFACEDESCRIPTION_SUBSURFACEMASK",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Thickness = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Thickness", "SURFACEDESCRIPTION_THICKNESS",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static CustomSlotBlockFieldDescriptor DiffusionProfileHash = new CustomSlotBlockFieldDescriptor(m_ProviderInfo, tagName, "DiffusionProfileHash", "Diffusion Profile", "SURFACEDESCRIPTION_DIFFUSIONPROFILEHASH",
                () => { return new DiffusionProfileInputMaterialSlot(0, "Diffusion Profile", "DiffusionProfileHash", ShaderStageCapability.Fragment); });
            public static BlockFieldDescriptor IridescenceMask = new BlockFieldDescriptor(m_ProviderInfo, tagName, "IridescenceMask", "Iridescence Mask", "SURFACEDESCRIPTION_IRIDESCENCEMASK",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor IridescenceThickness = new BlockFieldDescriptor(m_ProviderInfo, tagName, "IridescenceThickness", "Iridescence Thickness", "SURFACEDESCRIPTION_IRIDESCENCETHICKNESS",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularOcclusion = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SpecularOcclusion", "Specular Occlusion", "SURFACEDESCRIPTION_SPECULAROCCLUSION",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThresholdDepthPrepass = new BlockFieldDescriptor(m_ProviderInfo, tagName, "AlphaClipThresholdDepthPrepass", "Alpha Clip Threshold Depth Prepass", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLDDEPTHPREPASS",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThresholdDepthPostpass = new BlockFieldDescriptor(m_ProviderInfo, tagName, "AlphaClipThresholdDepthPostpass", "Alpha Clip Threshold Depth Postpass", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLDDEPTHPOSTPASS",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThresholdShadow = new BlockFieldDescriptor(m_ProviderInfo, tagName, "AlphaClipThresholdShadow", "Alpha Clip Threshold Shadow", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLDSHADOW",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularAAScreenSpaceVariance = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SpecularAAScreenSpaceVariance", "Specular AA Screen Space Variance", "SURFACEDESCRIPTION_SPECULARAASCEENSPACEVARIANCE",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularAAThreshold = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SpecularAAThreshold", "Specular AA Threshold", "SURFACEDESCRIPTION_SPECULARAATHRESHOLD",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static CustomSlotBlockFieldDescriptor BakedGI = new CustomSlotBlockFieldDescriptor(m_ProviderInfo, tagName, "BakedGI", "Baked GI", "SURFACEDESCRIPTION_BAKEDGI",
                () => { return new DefaultMaterialSlot(0, "Baked GI", "BakedGI", ShaderStageCapability.Fragment); });
            public static CustomSlotBlockFieldDescriptor BakedBackGI = new CustomSlotBlockFieldDescriptor(m_ProviderInfo, tagName, "BakedBackGI", "Baked Back GI", "SURFACEDESCRIPTION_BAKEDBACKGI",
                () => { return new DefaultMaterialSlot(0, "Baked Back GI", "BakedBackGI", ShaderStageCapability.Fragment); });
            public static BlockFieldDescriptor DepthOffset = new BlockFieldDescriptor(m_ProviderInfo, tagName, "DepthOffset", "Depth Offset", "SURFACEDESCRIPTION_DEPTHOFFSET",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractionIndex = new BlockFieldDescriptor(m_ProviderInfo, tagName, "RefractionIndex", "Refraction Index", "SURFACEDESCRIPTION_REFRACTIONINDEX",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractionColor = new BlockFieldDescriptor(m_ProviderInfo, tagName, "RefractionColor", "Refraction Color", "SURFACEDESCRIPTION_REFRACTIONCOLOR",
                new ColorControl(Color.white, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor RefractionDistance = new BlockFieldDescriptor(m_ProviderInfo, tagName, "RefractionDistance", "Refraction Distance", "SURFACEDESCRIPTION_REFRACTIONDISTANCE",
                new FloatControl(1.0f), ShaderStage.Fragment);

            // --------------------------------------------------
            // Decal

            public static BlockFieldDescriptor NormalAlpha = new BlockFieldDescriptor(m_ProviderInfo, tagName, "NormalAlpha", "Normal Alpha", "SURFACEDESCRIPTION_NORMALALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor MAOSAlpha = new BlockFieldDescriptor(m_ProviderInfo, tagName, "MAOSAlpha", "MAOS Alpha", "SURFACEDESCRIPTION_MAOSALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment);

            // --------------------------------------------------
            // Eye

            public static BlockFieldDescriptor IrisNormalTS = new BlockFieldDescriptor(m_ProviderInfo, tagName, "IrisNormalTS", "Iris Normal (Tangent Space)", "SURFACEDESCRIPTION_IRISNORMALTS",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor IrisNormalOS = new BlockFieldDescriptor(m_ProviderInfo, tagName, "IrisNormalOS", "Iris Normal (Object Space)", "SURFACEDESCRIPTION_IRISNORMALOS",
                new NormalControl(CoordinateSpace.Object), ShaderStage.Fragment);
            public static BlockFieldDescriptor IrisNormalWS = new BlockFieldDescriptor(m_ProviderInfo, tagName, "IrisNormalWS", "Iris Normal (World Space)", "SURFACEDESCRIPTION_IRISNORMALWS",
                new NormalControl(CoordinateSpace.World), ShaderStage.Fragment);
            public static BlockFieldDescriptor IOR = new BlockFieldDescriptor(m_ProviderInfo, tagName, "IOR", "SURFACEDESCRIPTION_IOR",
                new FloatControl(1.4f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Mask = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Mask", "SURFACEDESCRIPTION_MASK",
                new Vector2Control(new Vector2(1.0f, 0.0f)), ShaderStage.Fragment);

            // --------------------------------------------------
            // Hair

            public static BlockFieldDescriptor Transmittance = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Transmittance", "SURFACEDESCRIPTION_TRANSMITTANCE",
                new Vector3Control(0.3f * new Vector3(1.0f, 0.65f, 0.3f)), ShaderStage.Fragment);
            public static BlockFieldDescriptor RimTransmissionIntensity = new BlockFieldDescriptor(m_ProviderInfo, tagName, "RimTransmissionIntensity", "Rim Transmission Intensity", "SURFACEDESCRIPTION_RIMTRANSMISSIONINTENSITY",
                new FloatControl(0.2f), ShaderStage.Fragment);
            public static BlockFieldDescriptor HairStrandDirection = new BlockFieldDescriptor(m_ProviderInfo, tagName, "HairStrandDirection", "Hair Strand Direction", "SURFACEDESCRIPTION_HAIRSTRANDDIRECTION",
                new Vector3Control(new Vector3(0, -1, 0)), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularTint = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SpecularTint", "Specular Tint", "SURFACEDESCRIPTION_SPECULARTINT",
                new ColorControl(Color.white, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor SpecularShift = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SpecularShift", "Specular Shift", "SURFACEDESCRIPTION_SPECULARSHIFT",
                new FloatControl(0.1f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SecondarySpecularTint = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SecondarySpecularTint", "Secondary Specular Tint", "SURFACEDESCRIPTION_SECONDARYSPECULARTINT",
                new ColorControl(Color.grey, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor SecondarySmoothness = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SecondarySmoothness", "Secondary Smoothness", "SURFACEDESCRIPTION_SECONDARYSMOOTHNESS",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SecondarySpecularShift = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SecondarySpecularShift", "Secondary Specular Shift", "SURFACEDESCRIPTION_SECONDARYSPECULARSHIFT",
                new FloatControl(-0.1f), ShaderStage.Fragment);

            // --------------------------------------------------
            // StackLit

            public static BlockFieldDescriptor CoatNormalOS = new BlockFieldDescriptor(m_ProviderInfo, tagName, "CoatNormalOS", "Coat Normal (Object Space)", "SURFACEDESCRIPTION_COATNORMALOS",
                new NormalControl(CoordinateSpace.Object), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatNormalTS = new BlockFieldDescriptor(m_ProviderInfo, tagName, "CoatNormalTS", "Coat Normal (Tangent Space)", "SURFACEDESCRIPTION_COATNORMALTS",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatNormalWS = new BlockFieldDescriptor(m_ProviderInfo, tagName, "CoatNormalWS", "Coat Normal (World Space)", "SURFACEDESCRIPTION_COATNORMALWS",
                new NormalControl(CoordinateSpace.World), ShaderStage.Fragment);
            public static BlockFieldDescriptor DielectricIor = new BlockFieldDescriptor(m_ProviderInfo, tagName, "DielectricIor", "Dielectric IOR", "SURFACEDESCRIPTION_DIELECTRICIOR",
                new FloatControl(1.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SmoothnessB = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SmoothnessB", "Smoothness B", "SURFACEDESCRIPTION_SMOOTHNESSB",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor LobeMix = new BlockFieldDescriptor(m_ProviderInfo, tagName, "LobeMix", "Lobe Mix", "SURFACEDESCRIPTION_LOBEMIX",
                new FloatControl(0.3f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AnisotropyB = new BlockFieldDescriptor(m_ProviderInfo, tagName, "AnisotropyB", "Anisotropy B", "SURFACEDESCRIPTION_ANISOTROPYB",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SOFixupVisibilityRatioThreshold = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SOFixupVisibilityRatioThreshold", "SO Fixup Visibility Ratio Threshold", "SURFACEDESCRIPTION_SOFIXUPVISIBILITYRATIOTHRESHOLD",
                new FloatControl(0.2f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SOFixupStrengthFactor = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SOFixupStrengthFactor", "SO Fixup Strength Factor", "SURFACEDESCRIPTION_SOFIXUPSTRENGTHFACTOR",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor SOFixupMaxAddedRoughness = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SOFixupMaxAddedRoughness", "SO Fixup Max Added Roughness", "SURFACEDESCRIPTION_SOFIXUPMAXADDEDROUGHNESS",
                new FloatControl(0.2f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatIor = new BlockFieldDescriptor(m_ProviderInfo, tagName, "CoatIor", "Coat IOR", "SURFACEDESCRIPTION_COATIOR",
                new FloatControl(1.4f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatThickness = new BlockFieldDescriptor(m_ProviderInfo, tagName, "CoatThickness", "Coat Thickness", "SURFACEDESCRIPTION_COATTHICKNESS",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatExtinction = new BlockFieldDescriptor(m_ProviderInfo, tagName, "CoatExtinction", "Coat Extinction", "SURFACEDESCRIPTION_COATEXTINCTION",
                new ColorControl(Color.white, true), ShaderStage.Fragment);
            public static BlockFieldDescriptor Haziness = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Haziness", "SURFACEDESCRIPTION_HAZINESS",
                new FloatControl(0.2f), ShaderStage.Fragment);
            public static BlockFieldDescriptor HazeExtent = new BlockFieldDescriptor(m_ProviderInfo, tagName, "HazeExtent", "Haze Extent", "SURFACEDESCRIPTION_HAZEEXTENT",
                new FloatControl(3.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor HazyGlossMaxDielectricF0 = new BlockFieldDescriptor(m_ProviderInfo, tagName, "HazyGlossMaxDielectricF0", "Hazy Gloss Max Dielectric F0", "SURFACEDESCRIPTION_HAZYGLOSSMAXDIELECTRICF0",
                new FloatControl(0.25f), ShaderStage.Fragment);
            public static BlockFieldDescriptor IridescenceCoatFixupTIR = new BlockFieldDescriptor(m_ProviderInfo, tagName, "IridescenceCoatFixupTIR", "Iridescence Coat Fixup TIR", "SURFACEDESCRIPTION_IRIDESCENCECOATFIXUPTIR",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor IridescenceCoatFixupTIRClamp = new BlockFieldDescriptor(m_ProviderInfo, tagName, "IridescenceCoatFixupTIRClamp", "Iridescence Coat Fixup TIR Clamp", "SURFACEDESCRIPTION_IRIDESCENCECOATFIXUPTIRCLAMP",
                new FloatControl(0.0f), ShaderStage.Fragment);
        }
    }
}
