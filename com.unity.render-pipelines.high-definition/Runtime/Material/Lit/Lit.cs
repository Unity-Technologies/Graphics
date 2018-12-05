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

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1000)]
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

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1050, false, true)]
        public struct BSDFData
        {
            [PackingAttribute("DiffuseColor", FieldPacking.NoPacking, 128, 0, 0, 1, false, true)]
            public Vector3 diffuseColor;
            [PackingAttribute("Fresnel0", FieldPacking.R11G11B10, 32, 0, 0, 1, false, true)]
            public uint fresnel0;

            [PackingAttribute("MaterialFeatures", FieldPacking.NoPacking, 0)]
            public uint materialFeatures;

            [PackingAttribute("PerceptualRoughness", FieldPacking.PackedFloat, 8, 24), 
            PackingAttribute("CoatRoughness", FieldPacking.PackedFloat, 8, 16),
            PackingAttribute("AmbientOcclusion", FieldPacking.PackedFloat, 8, 8),
            PackingAttribute("SpecularOcclusion", FieldPacking.PackedFloat, 8, 0)] 
            public uint roughnessesAndOcclusions;

            [PackingAttribute("DiffusionProfile", FieldPacking.PackedUint, 8, 24), 
            PackingAttribute("SubsurfaceMask", FieldPacking.PackedFloat, 8, 16),
            PackingAttribute("IOR", FieldPacking.PackedFloat, 16, 0, 1.0f, 2.5f)] 
            public uint SSSData;                            

            [PackingAttribute("Anisotropy", FieldPacking.PackedFloat, 8, 24), 
            PackingAttribute("RoughnessT", FieldPacking.PackedFloat, 8, 16),
            PackingAttribute("RoughnessB", FieldPacking.PackedFloat, 8, 8),
            PackingAttribute("Flags", FieldPacking.PackedUint, 8, 0)] 
            public uint anisoDataAndFlags;

            [PackingAttribute("IridescenceThickness", FieldPacking.PackedFloat, 8, 24), 
            PackingAttribute("IridescenceMask", FieldPacking.PackedFloat, 8, 16),
            PackingAttribute("TransmittanceMask", FieldPacking.PackedFloat, 8, 8),
            PackingAttribute("CoatMask", FieldPacking.PackedFloat, 8, 0)] 
            public uint iridescenceAndMasks;

            [PackingAttribute(new string[] { "NormalWS", "NormalViewSpace" }, FieldPacking.NoPacking, 0, 128, 0, 1, true, false)]
            public Vector3 normalWS;

            [PackingAttribute("Transmittance", FieldPacking.NoPacking)]
            public Vector3 transmittance;                   // Precomputation of transmittance
            [PackingAttribute("TangentWS", FieldPacking.R11G11B10, 0, 32, 0, 1, true)]
            public uint tangentWS;

            [PackingAttribute("BitangentWS", FieldPacking.R11G11B10, 0, 32, 0, 1, true)]
            public uint bitangentWS;
            [PackingAttribute("AbsorptionCoefficient", FieldPacking.R11G11B10, 0, 32, 0, 1, false, true)]
            public uint absorptionCoefficient;
            [PackingAttribute("Thickness", FieldPacking.NoPacking)]
            public float thickness;                               
        };


        //-----------------------------------------------------------------------------
        // GBuffer management
        //-----------------------------------------------------------------------------

        public override bool IsDefferedMaterial() { return true; }

        protected void GetGBufferOptions(HDRenderPipelineAsset asset, out int gBufferCount, out bool supportShadowMask, out bool supportLightLayers)
        {
            // Caution: This must be in sync with GBUFFERMATERIAL_COUNT definition in 
            supportShadowMask = asset.renderPipelineSettings.supportShadowMask;
            supportLightLayers = asset.renderPipelineSettings.supportLightLayers;
            gBufferCount = 4 + (supportShadowMask ? 1 : 0) + (supportLightLayers ? 1 : 0);
        }

        // This must return the number of GBuffer to allocate
        public override int GetMaterialGBufferCount(HDRenderPipelineAsset asset)
        {
            int gBufferCount;
            bool unused0;
            bool unused1;
            GetGBufferOptions(asset, out gBufferCount, out unused0, out unused1);

            return gBufferCount;
        }

        public override void GetMaterialGBufferDescription(HDRenderPipelineAsset asset, out RenderTextureFormat[] RTFormat, out bool[] sRGBFlag, out GBufferUsage[] gBufferUsage, out bool[] enableWrite)
        {
            int gBufferCount;
            bool supportShadowMask;
            bool supportLightLayers;
            GetGBufferOptions(asset, out gBufferCount, out supportShadowMask, out supportLightLayers);

            RTFormat = new RenderTextureFormat[gBufferCount];
            sRGBFlag = new bool[gBufferCount];
            gBufferUsage = new GBufferUsage[gBufferCount];
            enableWrite = new bool[gBufferCount];

            RTFormat[0] = RenderTextureFormat.ARGB32; // Albedo sRGB / SSSBuffer
            sRGBFlag[0] = true;
            gBufferUsage[0] = GBufferUsage.SubsurfaceScattering;
            enableWrite[0] = false;
            RTFormat[1] = RenderTextureFormat.ARGB32; // Normal Buffer
            sRGBFlag[1] = false;
            gBufferUsage[1] = GBufferUsage.Normal;
            enableWrite[1] = true;                    // normal buffer is used as RWTexture to composite decals in forward
            RTFormat[2] = RenderTextureFormat.ARGB32; // Data
            sRGBFlag[2] = false;
            gBufferUsage[2] = GBufferUsage.None;
            enableWrite[2] = false;
            RTFormat[3] = Builtin.GetLightingBufferFormat();
            sRGBFlag[3] = Builtin.GetLightingBufferSRGBFlag();
            gBufferUsage[3] = GBufferUsage.None;
            enableWrite[3] = false;

            int index = 4;

            if (supportLightLayers)
            {
                RTFormat[index] = RenderTextureFormat.ARGB32;
                sRGBFlag[index] = false;
                gBufferUsage[index] = GBufferUsage.LightLayers;
                index++;
            }

            // All buffer above are fixed. However shadow mask buffer can be setup or not depends on light in view.
            // Thus it need to be the last one, so all indexes stay the same
            if (supportShadowMask)
            {
                RTFormat[index] = Builtin.GetShadowMaskBufferFormat();
                sRGBFlag[index] = Builtin.GetShadowMaskBufferSRGBFlag();
                gBufferUsage[index] = GBufferUsage.ShadowMask;
                index++;
            }
        }


        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------

        public Lit() {}

        public override void Build(HDRenderPipelineAsset hdAsset)
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

        public override void Bind()
        {
            PreIntegratedFGD.instance.Bind(PreIntegratedFGD.FGDIndex.FGD_GGXAndDisneyDiffuse);
            LTCAreaLight.instance.Bind();
        }
    }
}
