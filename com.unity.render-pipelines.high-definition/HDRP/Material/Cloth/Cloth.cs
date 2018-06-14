using System;
using UnityEngine.Rendering;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class Cloth : RenderPipelineMaterial
    {
        // If change, be sure it match what is done in Lit.hlsl: MaterialFeatureFlagsFromGBuffer
        // Material bit mask must match the size define LightDefinitions.s_MaterialFeatureMaskFlags value
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            ClothCottonWool = 1 << 0,
            ClothSilk = 1 << 1,
            ClothSubsurfaceScattering = 1 << 2,
            CothTransmission = 1 << 3
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1300)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("MaterialFeatures")]
            public uint materialFeatures;

            // Standard
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;
            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;

            [SurfaceDataAttributes(new string[] { "Normal", "Normal View Space" }, true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;

            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            // Fuzz Tint
            [SurfaceDataAttributes("Fuzz Tint", false, true)]
            public Vector3 fuzzTint;

            // MaterialFeature dependent attribute

            // SSS
            [SurfaceDataAttributes("Diffusion Profile")]
            public uint diffusionProfile;
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

        [GenerateHLSL(PackingRules.Exact, false, true, 1350)]
        public struct BSDFData
        {
            public uint materialFeatures;

            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;

            public float specularOcclusion;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3 normalWS;
            public float perceptualRoughness;

            // MaterialFeature dependent attribute

            // SSS
            public uint diffusionProfile;
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

        bool m_isInit;

        public Cloth() { }

        public override void Build(HDRenderPipelineAsset hdAsset)
        {
            PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_CharlieAndClothLambert);
            //LTCAreaLight.instance.Build();

            m_isInit = false;
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_CharlieAndClothLambert);
            //LTCAreaLight.instance.Cleanup();

            m_isInit = false;
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            if (m_isInit)
                return;

            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_CharlieAndClothLambert, cmd);

            m_isInit = true;
        }

        public override void Bind()
        {
            PreIntegratedFGD.instance.Bind(PreIntegratedFGD.FGDIndex.FGD_CharlieAndClothLambert);
            //LTCAreaLight.instance.Bind();
        }
    }
}
