using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition
{
    public static class HDRPShaderGraphFields
    {
#region Tags
        const string kMaterial = "Material";
        const string kSpecular = "Specular";
        const string kDoubleSided = "DoubleSided";
#endregion

#region Fields
        // Material
        public static FieldDescriptor Anisotropy =              new FieldDescriptor(kMaterial, "Anisotropy", "_MATERIAL_FEATURE_TRANSMISSION 1");
        public static FieldDescriptor Iridescence =             new FieldDescriptor(kMaterial, "Iridescence", "_MATERIAL_FEATURE_TRANSMISSION 1");
        public static FieldDescriptor SpecularColor =           new FieldDescriptor(kMaterial, "SpecularColor", "_MATERIAL_FEATURE_TRANSMISSION 1");
        public static FieldDescriptor Standard =                new FieldDescriptor(kMaterial, "Standard", "_MATERIAL_FEATURE_TRANSMISSION 1");
        public static FieldDescriptor SubsurfaceScattering =    new FieldDescriptor(kMaterial, "SubsurfaceScattering", "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING 1");
        public static FieldDescriptor Transmission =            new FieldDescriptor(kMaterial, "Transmission", "_MATERIAL_FEATURE_TRANSMISSION 1");
        public static FieldDescriptor Translucent =             new FieldDescriptor(kMaterial, "Translucent", "_MATERIAL_FEATURE_TRANSLUCENT 1");
        public static FieldDescriptor Coat =                    new FieldDescriptor(kMaterial, "Coat", "_MATERIAL_FEATURE_COAT");
        public static FieldDescriptor CoatNormal =              new FieldDescriptor(kMaterial, "CoatNormal", "_MATERIAL_FEATURE_COAT_NORMALMAP");
        public static FieldDescriptor Eye =                     new FieldDescriptor(kMaterial, "Eye", "_MATERIAL_FEATURE_EYE 1");
        public static FieldDescriptor EyeCinematic =            new FieldDescriptor(kMaterial, "EyeCinematic", "_MATERIAL_FEATURE_EYE_CINEMATIC 1");
        public static FieldDescriptor CottonWool =              new FieldDescriptor(kMaterial, "CottonWool", "_MATERIAL_FEATURE_COTTON_WOOL 1");
        public static FieldDescriptor Silk =                    new FieldDescriptor(kMaterial, "Silk", "_MATERIAL_FEATURE_SILK 1");
        public static FieldDescriptor KajiyaKay =               new FieldDescriptor(kMaterial, "KajiyaKay", "_MATERIAL_FEATURE_HAIR_KAJIYA_KAY 1");
        public static FieldDescriptor AffectsAlbedo =           new FieldDescriptor(kMaterial, "AffectsAlbedo", "_MATERIAL_AFFECTS_ALBEDO 1");
        public static FieldDescriptor AffectsNormal =           new FieldDescriptor(kMaterial, "AffectsNormal", "_MATERIAL_AFFECTS_NORMAL 1");
        public static FieldDescriptor AffectsEmission =         new FieldDescriptor(kMaterial, "AffectsEmission", "_MATERIAL_AFFECTS_EMISSION 1");
        public static FieldDescriptor AffectsMaskMap =          new FieldDescriptor(kMaterial, "AffectsMaskMap", "_MATERIAL_AFFECTS_MASKMAP 1");

        // Specular
        public static FieldDescriptor EnergyConservingSpecular = new FieldDescriptor(kSpecular, "EnergyConserving", "_ENERGY_CONSERVING_SPECULAR 1");
        public static FieldDescriptor SpecularAA =              new FieldDescriptor(kSpecular, "AA", "_ENABLE_GEOMETRIC_SPECULAR_AA 1");
        
        // Specular Occlusion
        public static FieldDescriptor SpecularOcclusionFromAO = new FieldDescriptor(string.Empty, "SpecularOcclusionFromAO", "_SPECULAR_OCCLUSION_FROM_AO 1");
        public static FieldDescriptor SpecularOcclusionFromAOBentNormal = new FieldDescriptor(string.Empty, "SpecularOcclusionFromAOBentNormal", "_SPECULAR_OCCLUSION_FROM_AO_BENT_NORMAL 1");
        public static FieldDescriptor SpecularOcclusionCustom = new FieldDescriptor(string.Empty, "SpecularOcclusionCustom", "_SPECULAR_OCCLUSION_CUSTOM 1");

        // Double Sided
        public static FieldDescriptor DoubleSided =             new FieldDescriptor(string.Empty, "DoubleSided", "");
        public static FieldDescriptor DoubleSidedFlip =         new FieldDescriptor(kDoubleSided, "Flip", "");
        public static FieldDescriptor DoubleSidedMirror =       new FieldDescriptor(kDoubleSided, "Mirror", "");

        // Refraction
        public static FieldDescriptor Refraction =              new FieldDescriptor(string.Empty, "Refraction", "_HAS_REFRACTION 1");
        public static FieldDescriptor RefractionBox =           new FieldDescriptor(string.Empty, "RefractionBox", "_REFRACTION_PLANE 1");
        public static FieldDescriptor RefractionSphere =        new FieldDescriptor(string.Empty, "RefractionSphere", "_REFRACTION_SPHERE 1");

        // Misc
        public static FieldDescriptor AlphaTestShadow =         new FieldDescriptor(string.Empty, "AlphaTestShadow", "_ALPHA_TEST_SHADOW 1");
        public static FieldDescriptor AlphaTestPrepass =        new FieldDescriptor(string.Empty, "AlphaTestPrepass", "_ALPHA_TEST_PREPASS 1");
        public static FieldDescriptor AlphaTestPostpass =       new FieldDescriptor(string.Empty, "AlphaTestPostpass", "_ALPHA_TEST_POSTPASS 1");
        public static FieldDescriptor AlphaFog =                new FieldDescriptor(string.Empty, "AlphaFog", "_ENABLE_FOG_ON_TRANSPARENT 1");
        public static FieldDescriptor BlendPreserveSpecular =   new FieldDescriptor(DefaultFields.kBlendMode, "PreserveSpecular", "_BLENDMODE_PRESERVE_SPECULAR_LIGHTING 1");
        public static FieldDescriptor DisableDecals =           new FieldDescriptor(string.Empty, "DisableDecals", "_DISABLE_DECALS 1");
        public static FieldDescriptor DisableSSR =              new FieldDescriptor(string.Empty, "DisableSSR", "_DISABLE_SSR 1");
        public static FieldDescriptor BentNormal =              new FieldDescriptor(string.Empty, "BentNormal", "_BENT_NORMAL 1");
        public static FieldDescriptor AmbientOcclusion =        new FieldDescriptor(string.Empty, "AmbientOcclusion", "_AMBIENT_OCCLUSION 1");
        public static FieldDescriptor CoatMask =                new FieldDescriptor(string.Empty, "CoatMask", "_COAT_MASK 1");
        public static FieldDescriptor CoatMaskZero =            new FieldDescriptor(string.Empty, "CoatMaskZero", "");
        public static FieldDescriptor CoatMaskOne =             new FieldDescriptor(string.Empty, "CoatMaskOne", "");
        public static FieldDescriptor Tangent =                 new FieldDescriptor(string.Empty, "Tangent", "_TANGENT 1");
        public static FieldDescriptor LightingGI =              new FieldDescriptor(string.Empty, "LightingGI", "_LIGHTING_GI 1");
        public static FieldDescriptor BackLightingGI =          new FieldDescriptor(string.Empty, "BackLightingGI", "_BACK_LIGHTING_GI 1");
        public static FieldDescriptor DepthOffset =             new FieldDescriptor(string.Empty, "DepthOffset", "_DEPTH_OFFSET 1");
        public static FieldDescriptor TransparentWritesMotionVec = new FieldDescriptor(string.Empty, "TransparentWritesMotionVec", "_WRITE_TRANSPARENT_MOTION_VECTOR 1");
        public static FieldDescriptor HairStrandDirection =     new FieldDescriptor(string.Empty, "DepthOffset", "_HAIR_STRAND_DIRECTION 1");
        public static FieldDescriptor Transmittance =           new FieldDescriptor(string.Empty, "Transmittance", "_TRANSMITTANCE 1");
        public static FieldDescriptor RimTransmissionIntensity = new FieldDescriptor(string.Empty, "RimTransmissionIntensity", "_RIM_TRANSMISSION_INTENSITY 1");
        public static FieldDescriptor UseLightFacingNormal =    new FieldDescriptor(string.Empty, "UseLightFacingNormal", "_USE_LIGHT_FACING_NORMAL 1");

        // TODO: ALEX - Move this...
        public static FieldDescriptor IsFrontFace =             new FieldDescriptor("FragInputs", "isFrontFace", "");
#endregion
    }
}
