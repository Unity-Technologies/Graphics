using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class Lit : RenderPipelineMaterial
    {
        // Currently we have only one materialId (Standard GGX), so it is not store in the GBuffer and we don't test for it

        // If change, be sure it match what is done in Lit.hlsl: MaterialFeatureFlagsFromGBuffer
        // Material bit mask must match the size define LightDefinitions.s_MaterialFeatureMaskFlags value
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            LitStandard = 1 << 0,   // For material classification we need to identify that we are indeed use as standard material, else we are consider as sky/background element
            LitSpecularColor = 1 << 1,   // LitSpecularColor is not use statically but only dynamically
            LitSubsurfaceScattering = 1 << 2,
            LitTransmission = 1 << 3,
            LitAnisotropy = 1 << 4,
            LitIridescence = 1 << 5,
            LitClearCoat = 1 << 6,
            LitColoredTransmission = 1 << 7,
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1000)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Material Features")]
            public uint materialFeatures;

            // Standard
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Base Color", false, true, FieldPrecision.Real)]
            public Vector3 baseColor;
            [SurfaceDataAttributes("Specular Occlusion", precision = FieldPrecision.Real)]
            public float specularOcclusion;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] { "Normal", "Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 normalWS;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Smoothness", precision = FieldPrecision.Real)]
            public float perceptualSmoothness;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.AmbientOcclusion)]
            [SurfaceDataAttributes("Ambient Occlusion", precision = FieldPrecision.Real)]
            public float ambientOcclusion;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Metal)]
            [SurfaceDataAttributes("Metallic", precision = FieldPrecision.Real)]
            public float metallic;

            [SurfaceDataAttributes("Coat mask", precision = FieldPrecision.Real)]
            public float coatMask;

            // MaterialFeature dependent attribute

            // Specular Color
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Specular)]
            [SurfaceDataAttributes("Specular Color", false, true, FieldPrecision.Real)]
            public Vector3 specularColor;

            // SSS
            [SurfaceDataAttributes("Diffusion Profile Hash")]
            public uint diffusionProfileHash;
            [SurfaceDataAttributes("Subsurface Mask", precision = FieldPrecision.Real)]
            public float subsurfaceMask;

            // Transmission
            // + Diffusion Profile
            [SurfaceDataAttributes("Thickness", precision = FieldPrecision.Real)]
            public float thickness;
            [SurfaceDataAttributes("Transmission Mask", precision = FieldPrecision.Real)]
            public Vector3 transmissionMask;

            // Anisotropic
            [SurfaceDataAttributes("Tangent", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("Anisotropy", precision = FieldPrecision.Real)]
            public float anisotropy; // anisotropic ratio(0->no isotropic; 1->full anisotropy in tangent direction, -1->full anisotropy in bitangent direction)

            // Iridescence
            [SurfaceDataAttributes("Iridescence Layer Thickness", precision = FieldPrecision.Real)]
            public float iridescenceThickness;
            [SurfaceDataAttributes("Iridescence Mask", precision = FieldPrecision.Real)]
            public float iridescenceMask;

            // Forward property only
            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true, precision = FieldPrecision.Real, checkIsNormalized = true)]
            public Vector3 geomNormalWS;

            // Transparency
            // Reuse thickness from SSS

            [SurfaceDataAttributes("Index of refraction", precision = FieldPrecision.Real)]
            public float ior;
            [SurfaceDataAttributes("Transmittance Color", precision = FieldPrecision.Real)]
            public Vector3 transmittanceColor;
            [SurfaceDataAttributes("Transmittance Absorption Distance", precision = FieldPrecision.Real)]
            public float atDistance;
            [SurfaceDataAttributes("Transmittance Mask", precision = FieldPrecision.Real)]
            public float transmittanceMask;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1050)]
        public struct BSDFData
        {
            public uint materialFeatures;

            [SurfaceDataAttributes("", false, true, FieldPrecision.Real)]
            public Vector3 diffuseColor;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 fresnel0;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float fresnel90;

            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float ambientOcclusion; // Caution: This is accessible only if light layer is enabled, otherwise it is 1
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float specularOcclusion;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float perceptualRoughness;

            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float coatMask;

            // MaterialFeature dependent attribute

            // SpecularColor fold into fresnel0

            // SSS
            public uint diffusionProfileIndex;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float subsurfaceMask;

            // Transmission
            // + Diffusion Profile
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float thickness;
            public bool useThickObjectMode; // Read from the diffusion profile
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 transmittance;   // Precomputation of transmittance

            // Anisotropic
            [SurfaceDataAttributes("", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("", true)]
            public Vector3 bitangentWS;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float roughnessT;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float roughnessB;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float anisotropy;

            // Iridescence
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float iridescenceThickness;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float iridescenceMask;

            // ClearCoat
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float coatRoughness; // Automatically fill

            // Forward property only
            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true, precision = FieldPrecision.Real, checkIsNormalized = true)]
            public Vector3 geomNormalWS;

            // Transparency
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float ior;
            // Reuse thickness from SSS
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 absorptionCoefficient;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float transmittanceMask;
        };

        //-----------------------------------------------------------------------------
        // GBuffer management
        //-----------------------------------------------------------------------------

        public override bool IsDefferedMaterial() { return true; }

        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------

        public Lit() { }

        public override void Build(HDRenderPipeline _)
        {
            PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Build();
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Cleanup();
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse, cmd);
        }

        public override void Bind(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.Bind(cmd, PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Bind(cmd);
        }
    }
}
