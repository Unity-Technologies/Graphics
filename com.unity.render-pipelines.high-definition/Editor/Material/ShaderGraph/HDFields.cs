using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    static class HDFields
    {
        #region Tags
        public const string kMaterial = "Material";
        public const string kDots = "Dots";
        public const string kSpecular = "Specular";
        public const string kDoubleSided = "DoubleSided";
        public const string kDistortion = "Distortion";
        public const string kShaderPass = "ShaderPass";
        public const string kSubShader = "SubShader";
        #endregion

        #region Fields
        // Material
        public static FieldDescriptor Anisotropy =              new FieldDescriptor(kMaterial, "Anisotropy", "_MATERIAL_FEATURE_TRANSMISSION 1");
        public static FieldDescriptor Iridescence =             new FieldDescriptor(kMaterial, "Iridescence", "_MATERIAL_FEATURE_TRANSMISSION 1");
        public static FieldDescriptor SubsurfaceScattering =    new FieldDescriptor(kMaterial, "SubsurfaceScattering", "_MATERIAL_FEATURE_SUBSURFACE_SCATTERING 1");
        public static FieldDescriptor Transmission =            new FieldDescriptor(kMaterial, "Transmission", "_MATERIAL_FEATURE_TRANSMISSION 1");

        // Dots
        public static FieldDescriptor DotsInstancing =          new FieldDescriptor(kDots, "Instancing", "");
        public static FieldDescriptor DotsProperties =          new FieldDescriptor(kDots, "Properties", "");

        // Specular
        public static FieldDescriptor EnergyConservingSpecular = new FieldDescriptor(kSpecular, "EnergyConserving", "_ENERGY_CONSERVING_SPECULAR 1");
        public static FieldDescriptor SpecularAA =              new FieldDescriptor(kSpecular, "AA", "_ENABLE_GEOMETRIC_SPECULAR_AA 1");

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

        // Specular Occlusion
        public static FieldDescriptor SpecularOcclusion =       new FieldDescriptor(string.Empty, "SpecularOcclusion", "_ENABLESPECULAROCCLUSION");
        public static FieldDescriptor SpecularOcclusionFromAO = new FieldDescriptor(string.Empty, "SpecularOcclusionFromAO", "_SPECULAR_OCCLUSION_FROM_AO 1");
        public static FieldDescriptor SpecularOcclusionFromAOBentNormal = new FieldDescriptor(string.Empty, "SpecularOcclusionFromAOBentNormal", "_SPECULAR_OCCLUSION_FROM_AO_BENT_NORMAL 1");
        public static FieldDescriptor SpecularOcclusionCustom = new FieldDescriptor(string.Empty, "SpecularOcclusionCustom", "_SPECULAR_OCCLUSION_CUSTOM 1");

        // Misc
        public static FieldDescriptor DoAlphaTestShadow =       new FieldDescriptor(string.Empty, "DoAlphaTestShadow", "_DO_ALPHA_TEST_SHADOW 1");
        public static FieldDescriptor DoAlphaTestPrepass =      new FieldDescriptor(string.Empty, "DoAlphaTestPrepass", "_DO_ALPHA_TEST_PREPASS 1");
        public static FieldDescriptor BentNormal =              new FieldDescriptor(string.Empty, "BentNormal", "_BENT_NORMAL 1");
        public static FieldDescriptor AmbientOcclusion =        new FieldDescriptor(string.Empty, "AmbientOcclusion", "_AMBIENT_OCCLUSION 1");
        public static FieldDescriptor CoatMask =                new FieldDescriptor(string.Empty, "CoatMask", "_COAT_MASK 1");
        public static FieldDescriptor LightingGI =              new FieldDescriptor(string.Empty, "LightingGI", "_LIGHTING_GI 1");
        public static FieldDescriptor BackLightingGI =          new FieldDescriptor(string.Empty, "BackLightingGI", "_BACK_LIGHTING_GI 1");
        public static FieldDescriptor DepthOffset =             new FieldDescriptor(string.Empty, "DepthOffset", "_DEPTH_OFFSET 1");
        public static FieldDescriptor TransparentBackFace =     new FieldDescriptor(string.Empty, "TransparentBackFace", string.Empty);
        public static FieldDescriptor TransparentDepthPrePass = new FieldDescriptor(string.Empty, "TransparentDepthPrePass", string.Empty);
        public static FieldDescriptor TransparentDepthPostPass = new FieldDescriptor(string.Empty, "TransparentDepthPostPass", string.Empty);
        public static FieldDescriptor RayTracing =              new FieldDescriptor(string.Empty, "RayTracing", string.Empty);
        public static FieldDescriptor Unlit =                   new FieldDescriptor(string.Empty, "Unlit", string.Empty);

        #endregion
    }
}
