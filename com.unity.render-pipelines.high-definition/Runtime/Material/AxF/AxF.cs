using System;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.Experimental.Rendering;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Rendering.HighDefinition
{
    class AxF : RenderPipelineMaterial
    {
        // Misc flags:
        //
        // Warning: first bits are those from the importer, make sure they match.
        //
        // Others are for further material customization.
        // Note that contrary to the usual HDRP shaders' MaterialFeatureFlags,
        // these are NOT necessarily known at static time since we haven't created
        // shader features for each of them to then set eg surfaceData.materialFeatures.
        // For this reason, these flags are set in the same shader property than used
        // by the importer, _Flags.
        // Some may become static shader_features later and thus become just UI state.
        [GenerateHLSL(PackingRules.Exact)]
        public enum FeatureFlags
        {
            AxfAnisotropy = 1 << 0,
            AxfClearCoat = 1 << 1,
            AxfClearCoatRefraction = 1 << 2,
            AxfUseHeightMap = 1 << 3,
            AxfBRDFColorDiagonalClamp = 1 << 4,
            //Some TODO:
            AxfHonorMinRoughness = 1 << 8,
            AxfHonorMinRoughnessCoat = 1 << 9,  // the X-Rite viewer never shows a specular highlight on coat for dirac lights
            AxfDebugTest = 1 << 23,
            //Experimental:
            //
            // Warning: don't go over 23, or need to use float and bitcast on the UI side, and in the shader,
            // use float property/uniform and bitcast in hlsl code asuint()
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1200)]
        public struct SurfaceData
        {
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Smoothness", precision = FieldPrecision.Real)]
            public float perceptualSmoothness; // approximated for raytracing stage

            [MaterialSharedPropertyMapping(MaterialSharedProperty.AmbientOcclusion)]
            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] { "Normal", "Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes("Tangent", true)]
            public Vector3 tangentWS;

            // SVBRDF Variables
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Diffuse Color", false, true)]
            public Vector3 diffuseColor;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Specular)]
            [SurfaceDataAttributes("Specular Color", false, true)]
            public Vector3 specularColor;

            [SurfaceDataAttributes("Fresnel F0")]
            public Vector3 fresnel0;

            [SurfaceDataAttributes("Specular Lobe")]
            public Vector3 specularLobe; // .xy for SVBRDF, .xyz for CARPAINT2, for _CarPaint2_CTSpreads per lobe roughnesses

            [SurfaceDataAttributes("Height")]
            public float height_mm;

            [SurfaceDataAttributes("Anisotropic Angle")]
            public float anisotropyAngle;

            // Car Paint Variables
            [SurfaceDataAttributes("Flakes UV (or PlanarZY)")]
            public Vector2 flakesUVZY;
            [SurfaceDataAttributes("Flakes PlanarXZ")]
            public Vector2 flakesUVXZ;
            [SurfaceDataAttributes("Flakes PlanarXY")]
            public Vector2 flakesUVXY;

            [SurfaceDataAttributes("Flakes Mip (and for PlanarZY)")]
            public float flakesMipLevelZY;
            [SurfaceDataAttributes("Flakes Mip for PlanarXZ")]
            public float flakesMipLevelXZ;
            [SurfaceDataAttributes("Flakes Mip for PlanarXY")]
            public float flakesMipLevelXY;
            [SurfaceDataAttributes("Flakes Triplanar Weights")]
            public Vector3 flakesTriplanarWeights;

            // if non null, we will prefer gradients (to be used statically only!)
            [SurfaceDataAttributes("Flakes ddx (and for PlanarZY)")]
            public Vector2 flakesDdxZY;
            [SurfaceDataAttributes("Flakes ddy (and for PlanarZY)")]
            public Vector2 flakesDdyZY;
            [SurfaceDataAttributes("Flakes ddx for PlanarXZ")]
            public Vector2 flakesDdxXZ;
            [SurfaceDataAttributes("Flakes ddy for PlanarXZ")]
            public Vector2 flakesDdyXZ;
            [SurfaceDataAttributes("Flakes ddx for PlanarXY")]
            public Vector2 flakesDdxXY;
            [SurfaceDataAttributes("Flakes ddy for PlanarXY")]
            public Vector2 flakesDdyXY;
            // BTF Variables

            // Clearcoat
            [SurfaceDataAttributes("Clearcoat Color")]
            public Vector3 clearcoatColor;

            [SurfaceDataAttributes("Clearcoat Normal", true)]
            public Vector3 clearcoatNormalWS;

            [SurfaceDataAttributes("Clearcoat IOR")]
            public float clearcoatIOR;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 geomNormalWS;

            // Needed for raytracing.
            // TODO: should just modify FitToStandardLit in ShaderPassRaytracingGBuffer.hlsl and callee
            // to have "V" (from -incidentDir)
            [SurfaceDataAttributes("View Direction", true)]
            public Vector3 viewWS;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1250)]
        public struct BSDFData
        {
            public float ambientOcclusion;
            public float specularOcclusion;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes("", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("", true)]
            public Vector3 bitangentWS;

            // SVBRDF Variables
            public Vector3 diffuseColor;
            public Vector3 specularColor;
            public Vector3 fresnel0;
            public float perceptualRoughness; // approximated for SSAO
            public Vector3 roughness; // .xy for SVBRDF, .xyz for CARPAINT2, for _CarPaint2_CTSpreads per lobe roughnesses
            public float height_mm;

            // Car Paint Variables
            public Vector2 flakesUVZY;
            public Vector2 flakesUVXZ;
            public Vector2 flakesUVXY;
            public float flakesMipLevelZY;
            public float flakesMipLevelXZ;
            public float flakesMipLevelXY;
            public Vector3 flakesTriplanarWeights;
            public Vector2 flakesDdxZY; // if non null, we will prefer gradients (to be used statically only!)
            public Vector2 flakesDdyZY;
            public Vector2 flakesDdxXZ;
            public Vector2 flakesDdyXZ;
            public Vector2 flakesDdxXY;
            public Vector2 flakesDdyXY;

            // BTF Variables

            // Clearcoat
            public Vector3 clearcoatColor;
            [SurfaceDataAttributes("", true)]
            public Vector3 clearcoatNormalWS;
            public float clearcoatIOR;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true, checkIsNormalized = true)]
            public Vector3 geomNormalWS;

            // Needed for raytracing.
            // TODO: should just modify FitToStandardLit in ShaderPassRaytracingGBuffer.hlsl and callee
            // to have "V" (from -incidentDir)
            [SurfaceDataAttributes("View Direction", true)]
            public Vector3 viewWS;
        };

        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------

        // For area lighting - We pack all texture inside a texture array to reduce the number of resource required
        Texture2DArray m_LtcData; // 0: m_LtcGGXMatrix - RGBA;

        Material m_preIntegratedFGDMaterial_Ward = null;
        Material m_preIntegratedFGDMaterial_CookTorrance = null;
        RenderTexture m_preIntegratedFGD_Ward = null;
        RenderTexture m_preIntegratedFGD_CookTorrance = null;

        private bool m_precomputedFGDTablesAreInit = false;

        public static readonly int _PreIntegratedFGD_Ward = Shader.PropertyToID("_PreIntegratedFGD_Ward");
        public static readonly int _PreIntegratedFGD_CookTorrance = Shader.PropertyToID("_PreIntegratedFGD_CookTorrance");
        public static readonly int _AxFLtcData = Shader.PropertyToID("_AxFLtcData");

        public AxF() { }

        public override void Build(HDRenderPipelineAsset hdAsset, HDRenderPipelineRuntimeResources defaultResources)
        {
            // Create Materials
            m_preIntegratedFGDMaterial_Ward = CoreUtils.CreateEngineMaterial(defaultResources.shaders.preIntegratedFGD_WardPS);
            if (m_preIntegratedFGDMaterial_Ward == null)
                throw new Exception("Failed to create material for Ward BRDF pre-integration!");

            m_preIntegratedFGDMaterial_CookTorrance = CoreUtils.CreateEngineMaterial(defaultResources.shaders.preIntegratedFGD_CookTorrancePS);
            if (m_preIntegratedFGDMaterial_CookTorrance == null)
                throw new Exception("Failed to create material for Cook-Torrance BRDF pre-integration!");

            // Create render textures where we will render the FGD tables
            m_preIntegratedFGD_Ward = new RenderTexture(128, 128, 0, GraphicsFormat.A2B10G10R10_UNormPack32);
            m_preIntegratedFGD_Ward.hideFlags = HideFlags.HideAndDontSave;
            m_preIntegratedFGD_Ward.filterMode = FilterMode.Bilinear;
            m_preIntegratedFGD_Ward.wrapMode = TextureWrapMode.Clamp;
            m_preIntegratedFGD_Ward.hideFlags = HideFlags.DontSave;
            m_preIntegratedFGD_Ward.name = CoreUtils.GetRenderTargetAutoName(128, 128, 1, GraphicsFormat.A2B10G10R10_UNormPack32, "PreIntegratedFGD_Ward");
            m_preIntegratedFGD_Ward.Create();

            m_preIntegratedFGD_CookTorrance = new RenderTexture(128, 128, 0, GraphicsFormat.A2B10G10R10_UNormPack32);
            m_preIntegratedFGD_CookTorrance.hideFlags = HideFlags.HideAndDontSave;
            m_preIntegratedFGD_CookTorrance.filterMode = FilterMode.Bilinear;
            m_preIntegratedFGD_CookTorrance.wrapMode = TextureWrapMode.Clamp;
            m_preIntegratedFGD_CookTorrance.hideFlags = HideFlags.DontSave;
            m_preIntegratedFGD_CookTorrance.name = CoreUtils.GetRenderTargetAutoName(128, 128, 1, GraphicsFormat.A2B10G10R10_UNormPack32, "PreIntegratedFGD_CookTorrance");
            m_preIntegratedFGD_CookTorrance.Create();

            // LTC data

            m_LtcData = new Texture2DArray(LTCAreaLight.k_LtcLUTResolution, LTCAreaLight.k_LtcLUTResolution, 3, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = CoreUtils.GetTextureAutoName(LTCAreaLight.k_LtcLUTResolution, LTCAreaLight.k_LtcLUTResolution, GraphicsFormat.R16G16B16A16_SFloat, depth: 2, dim: TextureDimension.Tex2DArray, name: "LTC_LUT")
            };

            // Caution: This need to match order define in AxFLTCAreaLight
            LTCAreaLight.LoadLUT(m_LtcData, 0, GraphicsFormat.R16G16B16A16_SFloat, LTCAreaLight.s_LtcMatrixData_GGX);
            // Warning: check /Material/AxF/AxFLTCAreaLight/LtcData.GGX2.cs: 5 columns are needed, the entries are NOT normalized!
            // For now, we patch for this in LoadLUT, which should affect the loading of s_LtcGGXMatrixData for the rest of the materials
            // (Lit, etc.)

            m_LtcData.Apply();

            // TODO BugFix:
            // We still bind the original data for now, see AxFLTCAreaLight.hlsl: when using a local table, results differ,
            // even if we patch the non-normalization error in the 8th column when calling the LTCInvMatrix loading routine (LoadLUT).
            LTCAreaLight.instance.Build();
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_preIntegratedFGD_CookTorrance);
            CoreUtils.Destroy(m_preIntegratedFGD_Ward);
            CoreUtils.Destroy(m_preIntegratedFGDMaterial_CookTorrance);
            CoreUtils.Destroy(m_preIntegratedFGDMaterial_Ward);
            m_preIntegratedFGD_CookTorrance = null;
            m_preIntegratedFGD_Ward = null;
            m_preIntegratedFGDMaterial_Ward = null;
            m_preIntegratedFGDMaterial_CookTorrance = null;
            m_precomputedFGDTablesAreInit = false;

            // LTC data
            CoreUtils.Destroy(m_LtcData);
            LTCAreaLight.instance.Cleanup();
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            if (m_precomputedFGDTablesAreInit || m_preIntegratedFGDMaterial_Ward == null || m_preIntegratedFGDMaterial_CookTorrance == null)
            {
                return;
            }

            if (GL.wireframe)
            {
                m_preIntegratedFGD_Ward.Create();
                m_preIntegratedFGD_CookTorrance.Create();
                return;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PreIntegradeWardCookTorrance)))
            {
                CoreUtils.DrawFullScreen(cmd, m_preIntegratedFGDMaterial_Ward, new RenderTargetIdentifier(m_preIntegratedFGD_Ward));
                CoreUtils.DrawFullScreen(cmd, m_preIntegratedFGDMaterial_CookTorrance, new RenderTargetIdentifier(m_preIntegratedFGD_CookTorrance));
            }

            m_precomputedFGDTablesAreInit = true;
        }

        public override void Bind(CommandBuffer cmd)
        {
            if (m_preIntegratedFGD_Ward == null || m_preIntegratedFGD_CookTorrance == null)
            {
                throw new Exception("Ward & Cook-Torrance BRDF pre-integration table not available!");
            }

            cmd.SetGlobalTexture(_PreIntegratedFGD_Ward, m_preIntegratedFGD_Ward);
            cmd.SetGlobalTexture(_PreIntegratedFGD_CookTorrance, m_preIntegratedFGD_CookTorrance);

            // LTC Data
            cmd.SetGlobalTexture(_AxFLtcData, m_LtcData);
            LTCAreaLight.instance.Bind(cmd);
        }
    }
}
