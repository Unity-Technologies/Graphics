using UnityEngine.Rendering.HighDefinition.Attributes;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Rendering.HighDefinition
{
    class Fabric : RenderPipelineMaterial
    {
        // If change, be sure it match what is done in Lit.hlsl: MaterialFeatureFlagsFromGBuffer
        // Material bit mask must match the size define LightDefinitions.s_MaterialFeatureMaskFlags value
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            FabricCottonWool = 1 << 0,
            FabricSubsurfaceScattering = 1 << 1,
            FabricTransmission = 1 << 2
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1300)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Material Features")]
            public uint materialFeatures;

            // Standard
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;
            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] { "Normal", "Normal View Space" }, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true)]
            public Vector3 geomNormalWS;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.AmbientOcclusion)]
            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            // Specular Tint
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Specular)]
            [SurfaceDataAttributes("Specular Tint", false, true)]
            public Vector3 specularColor;

            // MaterialFeature dependent attribute

            // SSS
            [SurfaceDataAttributes("Diffusion Profile Hash")]
            public uint diffusionProfileHash;
            [SurfaceDataAttributes("Subsurface Mask")]
            public float subsurfaceMask;

            // Transmission
            // + Diffusion Profile
            [SurfaceDataAttributes("Thickness")]
            public float thickness;

            // Anisotropic
            [SurfaceDataAttributes("Tangent", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("Anisotropy")]
            public float anisotropy; // anisotropic ratio(0->no isotropic; 1->full anisotropy in tangent direction, -1->full anisotropy in bitangent direction)
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1350)]
        public struct BSDFData
        {
            public uint materialFeatures;

            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;

            public float ambientOcclusion;
            public float specularOcclusion;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true)]
            public Vector3 geomNormalWS;

            public float perceptualRoughness;

            // MaterialFeature dependent attribute

            // SSS
            public uint diffusionProfileIndex;
            public float subsurfaceMask;

            // Transmission
            // + Diffusion Profile
            public float thickness;
            public bool useThickObjectMode; // Read from the diffusion profile
            public Vector3 transmittance;   // Precomputation of transmittance

            // Anisotropic
            [SurfaceDataAttributes("", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("", true)]
            public Vector3 bitangentWS;
            public float roughnessT;
            public float roughnessB;
            public float anisotropy;
        };

        //-----------------------------------------------------------------------------
        // Init precomputed textures
        //-----------------------------------------------------------------------------

        public Fabric() {}

        public override void Build(HDRenderPipelineAsset hdAsset, RenderPipelineResources defaultResources)
        {
            PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_CharlieAndFabricLambert);
            //LTCAreaLight.instance.Build();
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_CharlieAndFabricLambert);
            //LTCAreaLight.instance.Cleanup();
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_CharlieAndFabricLambert, cmd);
        }

        public override void Bind(CommandBuffer cmd)
        {
            PreIntegratedFGD.instance.Bind(cmd, PreIntegratedFGD.FGDIndex.FGD_CharlieAndFabricLambert);
            //LTCAreaLight.instance.Bind(cmd);
        }
    }
}
