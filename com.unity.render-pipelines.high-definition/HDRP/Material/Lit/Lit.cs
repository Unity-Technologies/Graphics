using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class Lit : RenderPipelineMaterial
    {
        // Currently we have only one materialId (Standard GGX), so it is not store in the GBuffer and we don't test for it

        // If change, be sure it match what is done in Lit.hlsl: MaterialFeatureFlagsFromGBuffer
        // Material bit mask must match the size define LightDefinitions.s_MaterialFeatureMaskFlags value
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            LitStandard             = 1 << 0,   // For material classification we need to identify that we are indeed use as standard material, else we are consider as sky/background element
            LitSpecularColor        = 1 << 1,   // LitSpecularColor is not use statically but only dynamically
            LitSubsurfaceScattering = 1 << 2,
            LitTransmission         = 1 << 3,
            LitAnisotropy           = 1 << 4,
            LitIridescence          = 1 << 5,
            LitClearCoat            = 1 << 6
        };

        public enum RefractionModel
        {
            None = 0,
            Plane = 1,
            Sphere = 2
        };

        [GenerateHLSL]
        public enum ProjectionModel
        {
            None = 0,
            Proxy = 1,
            HiZ = 2,
            Linear = 3
        };

        [GenerateHLSL]
        public enum HiZIntersectionKind
        {
            None,
            Cell,
            Depth
        }

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1000)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("MaterialFeatures")]
            public uint materialFeatures;

            // Standard
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;
            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;

            [SurfaceDataAttributes(new string[] {"Normal", "Normal View Space"}, true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;

            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            [SurfaceDataAttributes("Metallic")]
            public float metallic;

            [SurfaceDataAttributes("Coat mask")]
            public float coatMask;

            // MaterialFeature dependent attribute

            // Specular Color
            [SurfaceDataAttributes("Specular Color", false, true)]
            public Vector3 specularColor;

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

            // Iridescence
            [SurfaceDataAttributes("Iridescence Layer Thickness")]
            public float iridescenceThickness;
            [SurfaceDataAttributes("Iridescence Mask")]
            public float iridescenceMask;

            // Forward property only

            // Transparency
            // Reuse thickness from SSS

            [SurfaceDataAttributes("Index of refraction")]
            public float ior;
            [SurfaceDataAttributes("Transmittance Color")]
            public Vector3 transmittanceColor;
            [SurfaceDataAttributes("Transmittance Absorption Distance")]
            public float atDistance;
            [SurfaceDataAttributes("Transmittance mask")]
            public float transmittanceMask;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 1050)]
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

            public float coatMask;

            // MaterialFeature dependent attribute

            // SpecularColor fold into fresnel0

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

            // Iridescence
            public float iridescenceThickness;
            public float iridescenceMask;

            // ClearCoat
            public float coatRoughness; // Automatically fill

            // Forward property only

            // Transparency
            public float ior;
            // Reuse thickness from SSS
            public Vector3 absorptionCoefficient;
            public float transmittanceMask;
        };

        //-----------------------------------------------------------------------------
        // RenderLoop management
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact)]
        public enum GBufferMaterial
        {
            // Note: This count doesn't include the velocity buffer. On shader and csharp side the velocity buffer will be added by the framework
            Count = 4
        };

        //-----------------------------------------------------------------------------
        // GBuffer management
        //-----------------------------------------------------------------------------

        public override int GetMaterialGBufferCount() { return (int)GBufferMaterial.Count; }

        RenderTextureFormat[] m_RTFormat4 = { RenderTextureFormat.ARGB32, RenderTextureFormat.ARGB32, RenderTextureFormat.ARGB32, RenderTextureFormat.RGB111110Float };
        bool[] m_RTsRGBFlag4 = { true, false, false, false };

        public override void GetMaterialGBufferDescription(out RenderTextureFormat[] RTFormat, out bool[] sRGBFlag)
        {
            RTFormat = m_RTFormat4;
            sRGBFlag = m_RTsRGBFlag4;
        }

        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------

        bool m_isInit;

        public Lit() {}

        public override void Build(HDRenderPipelineAsset hdAsset)
        {
            PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Build();

            m_isInit = false;
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Cleanup();

            m_isInit = false;
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            if (m_isInit)
                return;

            PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse, cmd);

            m_isInit = true;
        }

        public override void Bind()
        {
            PreIntegratedFGD.instance.Bind(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Bind();
        }
    }
}
