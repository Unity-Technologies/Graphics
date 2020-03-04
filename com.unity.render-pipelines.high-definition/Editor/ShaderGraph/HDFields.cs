using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    static class HDFields
    {
#region Tags
        const string kMaterial = "Material";
        const string kDots = "Dots";
        const string kSpecular = "Specular";
        const string kDoubleSided = "DoubleSided";
        const string kDistortion = "Distortion";
        const string kBaseParametrization = "BaseParametrization";
        const string kDualSpecularLobeParametrization = "DualSpecularLobeParametrization";
        const string kSSSpecularOcclusionBaseMode = "ScreenSpaceSpecularOcclusionBaseMode";
        const string kSSSpecularOcclusionAOConeSize = "ScreenSpaceSpecularOcclusionAOConeSize";
        const string kSSSpecularOcclusionAOConeDir = "ScreenSpaceSpecularOcclusionAOConeDir";
        const string kDataBasedSpecularOcclusionBaseMode = "DataBasedSpecularOcclusionBaseMode";
        const string kDataBasedSpecularOcclusionAOConeSize = "DataBasedSpecularOcclusionAOConeSize";
        const string kSpecularOcclusionConeFixupMethod = "SpecularOcclusionConeFixupMethod";
        const string kShaderPass = "ShaderPass";
        const string kSubShader = "SubShader";
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
        public static FieldDescriptor DualSpecularLobe =        new FieldDescriptor(kMaterial, "DualSpecularLobe", "_MATERIAL_FEATURE_DUAL_SPECULAR_LOBE");
        public static FieldDescriptor Eye =                     new FieldDescriptor(kMaterial, "Eye", "_MATERIAL_FEATURE_EYE 1");
        public static FieldDescriptor EyeCinematic =            new FieldDescriptor(kMaterial, "EyeCinematic", "_MATERIAL_FEATURE_EYE_CINEMATIC 1");
        public static FieldDescriptor CottonWool =              new FieldDescriptor(kMaterial, "CottonWool", "_MATERIAL_FEATURE_COTTON_WOOL 1");
        public static FieldDescriptor Silk =                    new FieldDescriptor(kMaterial, "Silk", "_MATERIAL_FEATURE_SILK 1");
        public static FieldDescriptor KajiyaKay =               new FieldDescriptor(kMaterial, "KajiyaKay", "_MATERIAL_FEATURE_HAIR_KAJIYA_KAY 1");
        public static FieldDescriptor AffectsAlbedo =           new FieldDescriptor(kMaterial, "AffectsAlbedo", "_MATERIAL_AFFECTS_ALBEDO 1");
        public static FieldDescriptor AffectsNormal =           new FieldDescriptor(kMaterial, "AffectsNormal", "_MATERIAL_AFFECTS_NORMAL 1");
        public static FieldDescriptor AffectsEmission =         new FieldDescriptor(kMaterial, "AffectsEmission", "_MATERIAL_AFFECTS_EMISSION 1");
        public static FieldDescriptor AffectsMetal =            new FieldDescriptor(kMaterial, "AffectsMetal", "");
        public static FieldDescriptor AffectsAO =               new FieldDescriptor(kMaterial, "AffectsAO", "");
        public static FieldDescriptor AffectsSmoothness =       new FieldDescriptor(kMaterial, "AffectsSmoothness", "");
        public static FieldDescriptor AffectsMaskMap =          new FieldDescriptor(kMaterial, "AffectsMaskMap", "_MATERIAL_AFFECTS_MASKMAP 1");
        public static FieldDescriptor DecalDefault =            new FieldDescriptor(kMaterial, "DecalDefault", "");

        // Dots
        public static FieldDescriptor DotsInstancing =          new FieldDescriptor(kDots, "Instancing", "");
        public static FieldDescriptor DotsProperties =          new FieldDescriptor(kDots, "Properties", "");

        // Specular
        public static FieldDescriptor EnergyConservingSpecular = new FieldDescriptor(kSpecular, "EnergyConserving", "_ENERGY_CONSERVING_SPECULAR 1");
        public static FieldDescriptor SpecularAA =              new FieldDescriptor(kSpecular, "AA", "_ENABLE_GEOMETRIC_SPECULAR_AA 1");
        public static FieldDescriptor GeometricSpecularAA =     new FieldDescriptor(kSpecular, "GeometricAA", "_ENABLE_GEOMETRIC_SPECULAR_AA 1");

        // Specular Occlusion
        public static FieldDescriptor SpecularOcclusion =       new FieldDescriptor(string.Empty, "SpecularOcclusion", "_ENABLESPECULAROCCLUSION");
        public static FieldDescriptor SpecularOcclusionFromAO = new FieldDescriptor(string.Empty, "SpecularOcclusionFromAO", "_SPECULAR_OCCLUSION_FROM_AO 1");
        public static FieldDescriptor SpecularOcclusionFromAOBentNormal = new FieldDescriptor(string.Empty, "SpecularOcclusionFromAOBentNormal", "_SPECULAR_OCCLUSION_FROM_AO_BENT_NORMAL 1");
        public static FieldDescriptor SpecularOcclusionCustom = new FieldDescriptor(string.Empty, "SpecularOcclusionCustom", "_SPECULAR_OCCLUSION_CUSTOM 1");

        // Double Sided
        public static FieldDescriptor DoubleSided =             new FieldDescriptor(string.Empty, "DoubleSided", "");
        public static FieldDescriptor DoubleSidedFlip =         new FieldDescriptor(kDoubleSided, "Flip", "");
        public static FieldDescriptor DoubleSidedMirror =       new FieldDescriptor(kDoubleSided, "Mirror", "");

        // Distortion
        public static FieldDescriptor DistortionDepthTest =     new FieldDescriptor(kDistortion, "DepthTest", "");
        public static FieldDescriptor DistortionAdd =           new FieldDescriptor(kDistortion, "Add", "");
        public static FieldDescriptor DistortionMultiply =      new FieldDescriptor(kDistortion, "Multiply", "");
        public static FieldDescriptor DistortionReplace =       new FieldDescriptor(kDistortion, "Replace", "");
        public static FieldDescriptor TransparentDistortion =    new FieldDescriptor(kDistortion, "TransparentDistortion", "");

        // Refraction
        public static FieldDescriptor Refraction =              new FieldDescriptor(string.Empty, "Refraction", "_HAS_REFRACTION 1");
        public static FieldDescriptor RefractionBox =           new FieldDescriptor(string.Empty, "RefractionBox", "_REFRACTION_PLANE 1");
        public static FieldDescriptor RefractionSphere =        new FieldDescriptor(string.Empty, "RefractionSphere", "_REFRACTION_SPHERE 1");

        // Base Parametrization
        public static FieldDescriptor BaseParamSpecularColor =  new FieldDescriptor(kBaseParametrization, "SpecularColor", "_MATERIAL_FEATURE_SPECULAR_COLOR");

        // Dual Specular Lobe Parametrization
        public static FieldDescriptor HazyGloss =               new FieldDescriptor(kDualSpecularLobeParametrization, "HazyGloss", "_MATERIAL_FEATURE_HAZY_GLOSS");

        // Misc
        public static FieldDescriptor AlphaTestShadow =         new FieldDescriptor(string.Empty, "AlphaTestShadow", "_ALPHA_TEST_SHADOW 1");
        public static FieldDescriptor AlphaTestPrepass =        new FieldDescriptor(string.Empty, "AlphaTestPrepass", "_ALPHA_TEST_PREPASS 1");
        public static FieldDescriptor AlphaTestPostpass =       new FieldDescriptor(string.Empty, "AlphaTestPostpass", "_ALPHA_TEST_POSTPASS 1");
        public static FieldDescriptor AlphaFog =                new FieldDescriptor(string.Empty, "AlphaFog", "_ENABLE_FOG_ON_TRANSPARENT 1");
        public static FieldDescriptor BlendPreserveSpecular =   new FieldDescriptor(Fields.kBlendMode, "PreserveSpecular", "_BLENDMODE_PRESERVE_SPECULAR_LIGHTING 1");
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
        public static FieldDescriptor CapHazinessIfNotMetallic = new FieldDescriptor(string.Empty, "CapHazinessIfNotMetallic", "");
        public static FieldDescriptor TransparentBackFace =     new FieldDescriptor(string.Empty, "TransparentBackFace", string.Empty);
        public static FieldDescriptor TransparentDepthPrePass = new FieldDescriptor(string.Empty, "TransparentDepthPrePass", string.Empty);
        public static FieldDescriptor TransparentDepthPostPass = new FieldDescriptor(string.Empty, "TransparentDepthPostPass", string.Empty);
        public static FieldDescriptor EnableShadowMatte =        new FieldDescriptor(string.Empty, "EnableShadowMatte", "_ENABLE_SHADOW_MATTE");

        // Advanced
        public static FieldDescriptor AnisotropyForAreaLights = new FieldDescriptor(string.Empty, "AnisotropyForAreaLights", "_ANISOTROPY_FOR_AREA_LIGHTS");
        public static FieldDescriptor RecomputeStackPerLight =  new FieldDescriptor(string.Empty, "RecomputeStackPerLight", "_VLAYERED_RECOMPUTE_PERLIGHT");
        public static FieldDescriptor HonorPerLightMinRoughness = new FieldDescriptor(string.Empty, "HonorPerLightMinRoughness", "_STACK_LIT_HONORS_LIGHT_MIN_ROUGHNESS");
        public static FieldDescriptor ShadeBaseUsingRefractedAngles = new FieldDescriptor(string.Empty, "ShadeBaseUsingRefractedAngles", "_VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE");
        public static FieldDescriptor StackLitDebug =           new FieldDescriptor(string.Empty, "StackLitDebug", "_STACKLIT_DEBUG");

        // Screen Space Specular Occlusion Base Mode
        public static FieldDescriptor SSSpecularOcclusionBaseModeOff = new FieldDescriptor(kSSSpecularOcclusionBaseMode, "Off", "_SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_DISABLED");
        public static FieldDescriptor SSSpecularOcclusionBaseModeDirectFromAO = new FieldDescriptor(kSSSpecularOcclusionBaseMode, "DirectFromAO", "_SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_FROM_AO");
        public static FieldDescriptor SSSpecularOcclusionBaseModeConeConeFromBentAO = new FieldDescriptor(kSSSpecularOcclusionBaseMode, "ConeConeFromBentAO", "_SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_CONECONE");
        public static FieldDescriptor SSSpecularOcclusionBaseModeSPTDIntegrationOfBentAO = new FieldDescriptor(kSSSpecularOcclusionBaseMode, "SPTDIntegrationOfBentAO", "_SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_SPTD");
        public static FieldDescriptor SSSpecularOcclusionBaseModeCustom = new FieldDescriptor(kSSSpecularOcclusionBaseMode, "Custom", "_SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_FROM_AO");

        // Screen Space Specular Occlusion AO Cone Size
        public static FieldDescriptor SSSpecularOcclusionAOConeSizeUniformAO = new FieldDescriptor(kSSSpecularOcclusionAOConeSize, "UniformAO", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_UNIFORM");
        public static FieldDescriptor SSSpecularOcclusionAOConeSizeCosWeightedAO = new FieldDescriptor(kSSSpecularOcclusionAOConeSize, "CosWeightedAO", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS");
        public static FieldDescriptor SSSpecularOcclusionAOConeSizeCosWeightedBentCorrectAO = new FieldDescriptor(kSSSpecularOcclusionAOConeSize, "CosWeightedBentCorrectAO", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION");

        // Screen Space Specular Occlusion AO Cone Dir
        public static FieldDescriptor SSSpecularOcclusionAOConeDirGeomNormal = new FieldDescriptor(kSSSpecularOcclusionAOConeDir, "GeomNormal", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR BENT_VISIBILITY_DIR_GEOM_NORMAL");
        public static FieldDescriptor SSSpecularOcclusionAOConeDirBentNormal = new FieldDescriptor(kSSSpecularOcclusionAOConeDir, "BentNormal", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR BENT_VISIBILITY_DIR_BENT_NORMAL");
        public static FieldDescriptor SSSpecularOcclusionAOConeDirShadingNormal = new FieldDescriptor(kSSSpecularOcclusionAOConeDir, "ShadingNormal", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR BENT_VISIBILITY_DIR_SHADING_NORMAL");

        // Data Bases Specular Occlusion Base Mode
        public static FieldDescriptor DataBasedSpecularOcclusionBaseModeOff = new FieldDescriptor(kDataBasedSpecularOcclusionBaseMode, "Off", "_DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_DISABLED");
        public static FieldDescriptor DataBasedSpecularOcclusionBaseModeDirectFromAO = new FieldDescriptor(kDataBasedSpecularOcclusionBaseMode, "DirectFromAO", "_DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_FROM_AO");
        public static FieldDescriptor DataBasedSpecularOcclusionBaseModeConeConeFromBentAO = new FieldDescriptor(kDataBasedSpecularOcclusionBaseMode, "ConeConeFromBentAO", "_DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_CONECONE");
        public static FieldDescriptor DataBasedSpecularOcclusionBaseModeSPTDIntegrationOfBentAO = new FieldDescriptor(kDataBasedSpecularOcclusionBaseMode, "SPTDIntegrationOfBentAO", "_DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_SPTD");
        public static FieldDescriptor DataBasedSpecularOcclusionBaseModeCustom = new FieldDescriptor(kDataBasedSpecularOcclusionBaseMode, "Custom", "_DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_CUSTOM_EXT_INPUT");

        // Data Based Specular Occlusion AO Cone Size
        public static FieldDescriptor DataBasedSpecularOcclusionAOConeSizeUniformAO = new FieldDescriptor(kDataBasedSpecularOcclusionAOConeSize, "UniformAO", "_DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_UNIFORM");
        public static FieldDescriptor DataBasedSpecularOcclusionAOConeSizeCosWeightedAO = new FieldDescriptor(kDataBasedSpecularOcclusionAOConeSize, "CosWeightedAO", "_DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS");
        public static FieldDescriptor DataBasedSpecularOcclusionAOConeSizeCosWeightedBentCorrectAO = new FieldDescriptor(kDataBasedSpecularOcclusionAOConeSize, "CosWeightedBentCorrectAO", "_DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION");

        // Specular Occlusion Cone Fixup Method
        public static FieldDescriptor SpecularOcclusionConeFixupMethodOff = new FieldDescriptor(kSpecularOcclusionConeFixupMethod, "Off", "_BENT_VISIBILITY_FIXUP_FLAGS BENT_VISIBILITY_FIXUP_FLAGS_NONE");
        public static FieldDescriptor SpecularOcclusionConeFixupMethodBoostBSDFRoughness = new FieldDescriptor(kSpecularOcclusionConeFixupMethod, "BoostBSDFRoughness", "_BENT_VISIBILITY_FIXUP_FLAGS BENT_VISIBILITY_FIXUP_FLAGS_BOOST_BSDF_ROUGHNESS");
        public static FieldDescriptor SpecularOcclusionConeFixupMethodTiltDirectionToGeomNormal = new FieldDescriptor(kSpecularOcclusionConeFixupMethod, "TiltDirectionToGeomNormal", "_BENT_VISIBILITY_FIXUP_FLAGS BENT_VISIBILITY_FIXUP_FLAGS_TILT_BENTNORMAL_TO_GEOM");
        public static FieldDescriptor SpecularOcclusionConeFixupMethodBoostAndTilt = new FieldDescriptor(kSpecularOcclusionConeFixupMethod, "BoostAndTilt", "_BENT_VISIBILITY_FIXUP_FLAGS (BENT_VISIBILITY_FIXUP_FLAGS_BOOST_BSDF_ROUGHNESS|BENT_VISIBILITY_FIXUP_FLAGS_TILT_BENTNORMAL_TO_GEOM)");

        public struct ShaderPass
        {
            public static FieldDescriptor RayTracingGBuffer = new FieldDescriptor(kShaderPass, "Raytracing GBuffer","SHADERPASS_RAYTRACING_GBUFFER");
            public static FieldDescriptor RaytracingSubSurface = new FieldDescriptor(kShaderPass, "Raytracing SubSurface", "SHADERPASS_RAYTRACING_SUB_SURFACE");
            public static FieldDescriptor RaytracingIndirect = new FieldDescriptor(kShaderPass, "Raytracing Indirect", "SHADERPASS_RAYTRACING_INDIRECT");
            public static FieldDescriptor RaytracingForward = new FieldDescriptor(kShaderPass, "Raytracing Forward", "SHADERPASS_RAYTRACING_FORWARD");
            public static FieldDescriptor RaytracingPathTracing = new FieldDescriptor(kShaderPass, "Raytracing Path Tracing", "SHADERPASS_PATH_TRACING");
            public static FieldDescriptor RaytracingVisibility = new FieldDescriptor(kShaderPass, "Raytracing Visibility", "SHADERPASS_RAYTRACING_VISIBILITY");
        }

        public struct SubShader
        {
            public static FieldDescriptor Lit = new FieldDescriptor(kSubShader, "Lit Subshader", "");
            public static FieldDescriptor Fabric = new FieldDescriptor(kSubShader, "Fabric SubShader", "");
            public static FieldDescriptor Unlit = new FieldDescriptor(kSubShader, "Unlit SubShader", "");
        }

#endregion
    }
}
