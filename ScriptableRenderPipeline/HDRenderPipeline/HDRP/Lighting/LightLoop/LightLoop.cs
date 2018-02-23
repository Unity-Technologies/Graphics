using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering.HDPipeline.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class ShadowSetup : IDisposable
    {
        // shadow related stuff
        const int k_MaxShadowDataSlots              = 64;
        const int k_MaxPayloadSlotsPerShadowData    =  4;
        ShadowmapBase[]         m_Shadowmaps;
        ShadowManager           m_ShadowMgr;
        static ComputeBuffer    s_ShadowDataBuffer;
        static ComputeBuffer    s_ShadowPayloadBuffer;

        public static GPUShadowType HDShadowLightType(Light l)
        {
            // We only process light with additional data
            var ald = l.GetComponent<HDAdditionalLightData>();

            if (ald == null)
            {
                return ShadowRegistry.ShadowLightType(l);
            }

            GPUShadowType shadowType = GPUShadowType.Unknown;

            switch (ald.lightTypeExtent)
            {
                case LightTypeExtent.Punctual:
                    shadowType = ShadowRegistry.ShadowLightType(l);
                    break;

                // Area and projector not supported yet
            }

            return shadowType;
        }

        public ShadowSetup(RenderPipelineResources resources, ShadowInitParameters shadowInit, ShadowSettings shadowSettings, out IShadowManager shadowManager)
        {
            s_ShadowDataBuffer      = new ComputeBuffer( k_MaxShadowDataSlots, System.Runtime.InteropServices.Marshal.SizeOf( typeof( ShadowData ) ) );
            s_ShadowPayloadBuffer   = new ComputeBuffer( k_MaxShadowDataSlots * k_MaxPayloadSlotsPerShadowData, System.Runtime.InteropServices.Marshal.SizeOf( typeof( ShadowPayload ) ) );
            ShadowAtlas.AtlasInit atlasInit;
            atlasInit.baseInit.width           = (uint)shadowInit.shadowAtlasWidth;
            atlasInit.baseInit.height          = (uint)shadowInit.shadowAtlasHeight;
            atlasInit.baseInit.slices          = 1;
            atlasInit.baseInit.shadowmapBits   = 32;
            atlasInit.baseInit.shadowmapFormat = RenderTextureFormat.Shadowmap;
            atlasInit.baseInit.samplerState    = SamplerState.Default();
            atlasInit.baseInit.comparisonSamplerState = ComparisonSamplerState.Default();
            atlasInit.baseInit.clearColor      = new Vector4( 0.0f, 0.0f, 0.0f, 0.0f );
            atlasInit.baseInit.maxPayloadCount = 0;
            atlasInit.baseInit.shadowSupport   = ShadowmapBase.ShadowSupport.Directional | ShadowmapBase.ShadowSupport.Point | ShadowmapBase.ShadowSupport.Spot;
            atlasInit.shaderKeyword            = null;
            atlasInit.shadowClearShader         = resources.shadowClearShader;
            atlasInit.shadowBlurMoments         = resources.shadowBlurMoments;

            var varianceInit = atlasInit;
            varianceInit.baseInit.shadowmapFormat = ShadowVariance.GetFormat( false, false, true );

            var varianceInit2 = varianceInit;
            varianceInit2.baseInit.shadowmapFormat = ShadowVariance.GetFormat( true, true, false );

            var varianceInit3 = varianceInit;
            varianceInit3.baseInit.shadowmapFormat = ShadowVariance.GetFormat( true, false, true );

            m_Shadowmaps = new ShadowmapBase[] { new ShadowVariance(ref varianceInit), new ShadowVariance(ref varianceInit2), new ShadowVariance(ref varianceInit3), new ShadowAtlas(ref atlasInit) };

            ShadowContext.SyncDel syncer = (ShadowContext sc) =>
                {
                    // update buffers
                    uint offset, count;
                    ShadowData[] sds;
                    sc.GetShadowDatas(out sds, out offset, out count);
                    Debug.Assert(offset == 0);
                    s_ShadowDataBuffer.SetData(sds);   // unfortunately we can't pass an offset or count to this function
                    ShadowPayload[] payloads;
                    sc.GetPayloads(out payloads, out offset, out count);
                    Debug.Assert(offset == 0);
                    s_ShadowPayloadBuffer.SetData(payloads);
                };

            // binding code. This needs to be in sync with ShadowContext.hlsl
            ShadowContext.BindDel binder = (ShadowContext sc, CommandBuffer cb, ComputeShader computeShader, int computeKernel) =>
                {
                    uint offset, count;
                    RenderTargetIdentifier[] tex;
                    sc.GetTex2DArrays(out tex, out offset, out count);

                    // bind buffers
                    cb.SetGlobalBuffer(HDShaderIDs._ShadowDatasExp, s_ShadowDataBuffer);
                    cb.SetGlobalBuffer(HDShaderIDs._ShadowPayloads, s_ShadowPayloadBuffer);
                    // bind textures
                    cb.SetGlobalTexture(HDShaderIDs._ShadowmapExp_VSM_0, tex[0]);
                    cb.SetGlobalTexture(HDShaderIDs._ShadowmapExp_VSM_1, tex[1]);
                    cb.SetGlobalTexture(HDShaderIDs._ShadowmapExp_VSM_2, tex[2]);
                    cb.SetGlobalTexture(HDShaderIDs._ShadowmapExp_PCF, tex[3]);

                    // TODO: Currently samplers are hard coded in ShadowContext.hlsl, so we can't really set them here
                };

            ShadowContext.CtxtInit scInit;
            scInit.storage.maxShadowDataSlots        = k_MaxShadowDataSlots;
            scInit.storage.maxPayloadSlots           = k_MaxShadowDataSlots * k_MaxPayloadSlotsPerShadowData;
            scInit.storage.maxTex2DArraySlots        = 4;
            scInit.storage.maxTexCubeArraySlots      = 0;
            scInit.storage.maxComparisonSamplerSlots = 1;
            scInit.storage.maxSamplerSlots           = 4;
            scInit.dataSyncer                        = syncer;
            scInit.resourceBinder                    = binder;

            ShadowManager.ShadowBudgets budgets;
            budgets.maxPointLights       = 6;
            budgets.maxSpotLights        = 12;
            budgets.maxDirectionalLights = 1;

            m_ShadowMgr = new ShadowManager(shadowSettings, ref scInit, ref budgets, m_Shadowmaps);
            // set global overrides - these need to match the override specified in LightLoop/Shadow.hlsl
            bool useGlobalOverrides = true;
            m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Point        , ShadowAlgorithm.PCF, ShadowVariant.V2, ShadowPrecision.High, useGlobalOverrides );
            m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Spot         , ShadowAlgorithm.PCF, ShadowVariant.V2, ShadowPrecision.High, useGlobalOverrides );
            m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Directional  , ShadowAlgorithm.PCF, ShadowVariant.V3, ShadowPrecision.High, useGlobalOverrides );

            m_ShadowMgr.SetShadowLightTypeDelegate(HDShadowLightType);

            shadowManager = m_ShadowMgr;
        }

        public void Dispose()
        {
            if (m_Shadowmaps != null)
            {
                (m_Shadowmaps[0] as ShadowAtlas).Dispose();
                (m_Shadowmaps[1] as ShadowAtlas).Dispose();
                (m_Shadowmaps[2] as ShadowAtlas).Dispose();
                (m_Shadowmaps[3] as ShadowAtlas).Dispose();
                m_Shadowmaps = null;
            }
            m_ShadowMgr = null;

            if (s_ShadowDataBuffer != null)
                s_ShadowDataBuffer.Release();
            if (s_ShadowPayloadBuffer != null)
                s_ShadowPayloadBuffer.Release();
        }
    }

    //-----------------------------------------------------------------------------
    // structure definition
    //-----------------------------------------------------------------------------

    [GenerateHLSL]
    public enum LightVolumeType
    {
        Cone,
        Sphere,
        Box,
        Count
    }

    [GenerateHLSL]
    public enum LightCategory
    {
        Punctual,
        Area,
        Env,
        Decal,
        Count
    }

    [GenerateHLSL]
    public enum LightFeatureFlags
    {
        // Light bit mask must match LightDefinitions.s_LightFeatureMaskFlags value
        Punctual    = 1 << 12,
        Area        = 1 << 13,
        Directional = 1 << 14,
        Env         = 1 << 15,
        Sky         = 1 << 16,
        SSRefraction = 1 << 17,
        SSReflection = 1 << 18
        // If adding more light be sure to not overflow LightDefinitions.s_LightFeatureMaskFlags
    }

    [GenerateHLSL]
    public class LightDefinitions
    {
        public static int s_MaxNrBigTileLightsPlusOne = 512;      // may be overkill but the footprint is 2 bits per pixel using uint16.
        public static float s_ViewportScaleZ = 1.0f;
        public static int s_UseLeftHandCameraSpace = 1;

        public static int s_TileSizeFptl = 16;
        public static int s_TileSizeClustered = 32;

        // feature variants
        public static int s_NumFeatureVariants = 27;

        // Following define the maximum number of bits use in each feature category.
        public static uint s_LightFeatureMaskFlags = 0xFFF000;
        public static uint s_LightFeatureMaskFlagsOpaque = 0xFFF000 & ~((uint)LightFeatureFlags.SSRefraction); // Opaque don't support screen space refraction
        public static uint s_LightFeatureMaskFlagsTransparent = 0xFFF000 & ~((uint)LightFeatureFlags.SSReflection); // Transparent don't support screen space reflection
        public static uint s_MaterialFeatureMaskFlags = 0x000FFF;   // don't use all bits just to be safe from signed and/or float conversions :/
    }

    [GenerateHLSL]
    public struct SFiniteLightBound
    {
        public Vector3 boxAxisX;
        public Vector3 boxAxisY;
        public Vector3 boxAxisZ;
        public Vector3 center;        // a center in camera space inside the bounding volume of the light source.
        public Vector2 scaleXY;
        public float radius;
    };

    [GenerateHLSL]
    public struct LightVolumeData
    {
        public Vector3 lightPos;
        public uint lightVolume;

        public Vector3 lightAxisX;
        public uint lightCategory;

        public Vector3 lightAxisY;
        public float radiusSq;

        public Vector3 lightAxisZ;      // spot +Z axis
        public float cotan;

        public Vector3 boxInnerDist;
        public uint featureFlags;

        public Vector3 boxInvRange;
        public float unused2;
    };

    public class LightLoop
    {
        public enum TileClusterDebug : int
        {
            None,
            Tile,
            Cluster,
            MaterialFeatureVariants
        };

        public enum TileClusterCategoryDebug : int
        {
            Punctual = 1,
            Area = 2,
            AreaAndPunctual = 3,
            Environment = 4,
            EnvironmentAndPunctual = 5,
            EnvironmentAndArea = 6,
            EnvironmentAndAreaAndPunctual = 7,
			Decal = 8
        };

        public const int k_MaxDirectionalLightsOnScreen = 4;
        public const int k_MaxPunctualLightsOnScreen    = 512;
        public const int k_MaxAreaLightsOnScreen        = 64;
        public const int k_MaxDecalsOnScreen = 512;
        public const int k_MaxLightsOnScreen = k_MaxDirectionalLightsOnScreen + k_MaxPunctualLightsOnScreen + k_MaxAreaLightsOnScreen + k_MaxDecalsOnScreen;
        public const int k_MaxEnvLightsOnScreen = 64;
        public const int k_MaxShadowOnScreen = 16;
        public const int k_MaxCascadeCount = 4; //Should be not less than m_Settings.directionalLightCascadeCount;
        public const int k_MaxStereoEyes = 2;
        public static readonly Vector3 k_BoxCullingExtentThreshold = Vector3.one * 0.01f;

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        static ComputeBuffer s_DirectionalLightDatas = null;
        static ComputeBuffer s_LightDatas = null;
        static ComputeBuffer s_EnvLightDatas = null;
        static ComputeBuffer s_shadowDatas = null;
        static ComputeBuffer s_DecalDatas = null;

        static Texture2DArray s_DefaultTexture2DArray;
        static Cubemap s_DefaultTextureCube;

        PlanarReflectionProbeCache m_ReflectionPlanarProbeCache;
        ReflectionProbeCache m_ReflectionProbeCache;
        TextureCache2D m_CookieTexArray;
        TextureCacheCubemap m_CubeCookieTexArray;
        List<Matrix4x4> m_Env2DCaptureVP = new List<Matrix4x4>();

        public class LightList
        {
            public List<DirectionalLightData> directionalLights;
            public List<LightData> lights;
            public List<EnvLightData> envLights;
            public List<ShadowData> shadows;

            public List<SFiniteLightBound> bounds;
            public List<LightVolumeData> lightVolumes;
            public List<SFiniteLightBound> rightEyeBounds;
            public List<LightVolumeData> rightEyeLightVolumes;

            public void Clear()
            {
                directionalLights.Clear();
                lights.Clear();
                envLights.Clear();
                shadows.Clear();

                bounds.Clear();
                lightVolumes.Clear();
                rightEyeBounds.Clear();
                rightEyeLightVolumes.Clear();
            }

            public void Allocate()
            {
                directionalLights = new List<DirectionalLightData>();
                lights = new List<LightData>();
                envLights = new List<EnvLightData>();
                shadows = new List<ShadowData>();

                bounds = new List<SFiniteLightBound>();
                lightVolumes = new List<LightVolumeData>();

                rightEyeBounds = new List<SFiniteLightBound>();
                rightEyeLightVolumes = new List<LightVolumeData>();
            }
        }

        LightList m_lightList;
        int m_punctualLightCount = 0;
        int m_areaLightCount = 0;
        int m_lightCount = 0;
        bool m_enableBakeShadowMask = false; // Track if any light require shadow mask. In this case we will need to enable the keyword shadow mask
        float m_maxShadowDistance = 0.0f; // Save value from shadow settings

        private ComputeShader buildScreenAABBShader { get { return m_Resources.buildScreenAABBShader; } }
        private ComputeShader buildPerTileLightListShader { get { return m_Resources.buildPerTileLightListShader; } }
        private ComputeShader buildPerBigTileLightListShader { get { return m_Resources.buildPerBigTileLightListShader; } }
        private ComputeShader buildPerVoxelLightListShader { get { return m_Resources.buildPerVoxelLightListShader; } }

        private ComputeShader buildMaterialFlagsShader { get { return m_Resources.buildMaterialFlagsShader; } }
        private ComputeShader buildDispatchIndirectShader { get { return m_Resources.buildDispatchIndirectShader; } }
        private ComputeShader clearDispatchIndirectShader { get { return m_Resources.clearDispatchIndirectShader; } }
        private ComputeShader deferredComputeShader { get { return m_Resources.deferredComputeShader; } }
        private ComputeShader deferredDirectionalShadowComputeShader { get { return m_Resources.deferredDirectionalShadowComputeShader; } }


        static int s_GenAABBKernel;
        static int s_GenListPerTileKernel;
        static int s_GenListPerVoxelKernel;
        static int s_ClearVoxelAtomicKernel;
        static int s_ClearDispatchIndirectKernel;
        static int s_BuildDispatchIndirectKernel;
        static int s_BuildMaterialFlagsWriteKernel;
        static int s_BuildMaterialFlagsOrKernel;

        static int s_shadeOpaqueDirectFptlKernel;
        static int s_shadeOpaqueDirectFptlDebugDisplayKernel;
        static int s_shadeOpaqueDirectShadowMaskFptlKernel;
        static int s_shadeOpaqueDirectShadowMaskFptlDebugDisplayKernel;

        static int[] s_shadeOpaqueIndirectFptlKernels = new int[LightDefinitions.s_NumFeatureVariants];
        static int[] s_shadeOpaqueIndirectShadowMaskFptlKernels = new int[LightDefinitions.s_NumFeatureVariants];

        static int s_deferredDirectionalShadowKernel;
        static int s_deferredDirectionalShadow_Contact_Kernel;
        static int s_deferredDirectionalShadow_Normals_Kernel;
        static int s_deferredDirectionalShadow_Contact_Normals_Kernel;

        static ComputeBuffer s_LightVolumeDataBuffer = null;
        static ComputeBuffer s_ConvexBoundsBuffer = null;
        static ComputeBuffer s_AABBBoundsBuffer = null;
        static ComputeBuffer s_LightList = null;
        static ComputeBuffer s_TileList = null;
        static ComputeBuffer s_TileFeatureFlags = null;
        static ComputeBuffer s_DispatchIndirectBuffer = null;

        static ComputeBuffer s_BigTileLightList = null;        // used for pre-pass coarse culling on 64x64 tiles
        static int s_GenListPerBigTileKernel;

        const bool k_UseDepthBuffer = true;      // only has an impact when EnableClustered is true (requires a depth-prepass)

        const int k_Log2NumClusters = 6;     // accepted range is from 0 to 6. NumClusters is 1<<g_iLog2NumClusters
        const float k_ClustLogBase = 1.02f;     // each slice 2% bigger than the previous
        float m_ClustScale;
        static ComputeBuffer s_PerVoxelLightLists = null;
        static ComputeBuffer s_PerVoxelOffset = null;
        static ComputeBuffer s_PerTileLogBaseTweak = null;
        static ComputeBuffer s_GlobalLightListAtomic = null;

        public enum ClusterPrepassSource : int
        {
            None = 0,
            BigTile = 1,
            Count = 2,
        }

        public enum ClusterDepthSource : int
        {
            NoDepth = 0,
            Depth = 1,
            MSAA_Depth = 2,
            Count = 3,
        }

        static string[,] s_ClusterKernelNames = new string[(int)ClusterPrepassSource.Count, (int)ClusterDepthSource.Count]
        {
            { "TileLightListGen_NoDepthRT", "TileLightListGen_DepthRT", "TileLightListGen_DepthRT_MSAA" },
            { "TileLightListGen_NoDepthRT_SrcBigTile", "TileLightListGen_DepthRT_SrcBigTile", "TileLightListGen_DepthRT_MSAA_SrcBigTile" }
        };
        // clustered light list specific buffers and data end

        static int[] s_TempIntArray = new int[2]; // Used to avoid GC stress when calling SetComputeIntParams

        FrameSettings m_FrameSettings = null;
        RenderPipelineResources m_Resources = null;

        // Following is an array of material of size eight for all combination of keyword: OUTPUT_SPLIT_LIGHTING - LIGHTLOOP_TILE_PASS - SHADOWS_SHADOWMASK - USE_FPTL_LIGHTLIST/USE_CLUSTERED_LIGHTLIST - DEBUG_DISPLAY
        Material[] m_deferredLightingMaterial;
        Material m_DebugViewTilesMaterial;
        Material m_DebugShadowMapMaterial;
        Material m_CubeToPanoMaterial;

        Light m_CurrentSunLight;
        int m_CurrentSunLightShadowIndex = -1;

        public Light GetCurrentSunLight() { return m_CurrentSunLight; }

        // shadow related stuff
        FrameId                 m_FrameId = new FrameId();
        ShadowSetup             m_ShadowSetup; // doesn't actually have to reside here, it would be enough to pass the IShadowManager in from the outside
        IShadowManager          m_ShadowMgr;
        List<int>               m_ShadowRequests = new List<int>();
        Dictionary<int, int>    m_ShadowIndices = new Dictionary<int, int>();

        void InitShadowSystem(HDRenderPipelineAsset hdAsset, ShadowSettings shadowSettings)
        {
            m_ShadowSetup = new ShadowSetup(hdAsset.renderPipelineResources, hdAsset.GetRenderPipelineSettings().shadowInitParams, shadowSettings, out m_ShadowMgr);
        }

        void DeinitShadowSystem()
        {
            if (m_ShadowSetup != null)
            {
                m_ShadowSetup.Dispose();
                m_ShadowSetup = null;
                m_ShadowMgr = null;
            }
        }

        int GetNumTileFtplX(HDCamera hdCamera)
        {
            return ((int)hdCamera.screenSize.x + (LightDefinitions.s_TileSizeFptl - 1)) / LightDefinitions.s_TileSizeFptl;
        }

        int GetNumTileFtplY(HDCamera hdCamera)
        {
            return ((int)hdCamera.screenSize.y + (LightDefinitions.s_TileSizeFptl - 1)) / LightDefinitions.s_TileSizeFptl;
        }

        int GetNumTileClusteredX(HDCamera hdCamera)
        {
            return ((int)hdCamera.screenSize.x + (LightDefinitions.s_TileSizeClustered - 1)) / LightDefinitions.s_TileSizeClustered;
        }

        int GetNumTileClusteredY(HDCamera hdCamera)
        {
            return ((int)hdCamera.screenSize.y + (LightDefinitions.s_TileSizeClustered - 1)) / LightDefinitions.s_TileSizeClustered;
        }

        public bool GetFeatureVariantsEnabled()
        {
            return !m_FrameSettings.enableForwardRenderingOnly && m_FrameSettings.lightLoopSettings.isFptlEnabled && m_FrameSettings.lightLoopSettings.enableComputeLightEvaluation &&
                    (m_FrameSettings.lightLoopSettings.enableComputeLightVariants || m_FrameSettings.lightLoopSettings.enableComputeMaterialVariants);
        }

        public LightLoop()
        {}

        int GetDeferredLightingMaterialIndex(int outputSplitLighting, int lightLoopTilePass, int shadowMask, int debugDisplay)
        {
            return (outputSplitLighting) | (lightLoopTilePass << 1) | (shadowMask << 2) | (debugDisplay << 3);
        }

        public void Build(HDRenderPipelineAsset hdAsset, ShadowSettings shadowSettings, IBLFilterGGX iblFilterGGX)
        {
            m_Resources = hdAsset.renderPipelineResources;

            m_DebugViewTilesMaterial = CoreUtils.CreateEngineMaterial(m_Resources.debugViewTilesShader);
            m_DebugShadowMapMaterial = CoreUtils.CreateEngineMaterial(m_Resources.debugShadowMapShader);
            m_CubeToPanoMaterial = CoreUtils.CreateEngineMaterial(m_Resources.cubeToPanoShader);

            m_lightList = new LightList();
            m_lightList.Allocate();
            m_Env2DCaptureVP.Clear();
            for (int i = 0, c = Mathf.Max(1, hdAsset.renderPipelineSettings.lightLoopSettings.maxPlanarReflectionProbes); i < c; ++i)
                m_Env2DCaptureVP.Add(Matrix4x4.identity);

            s_DirectionalLightDatas = new ComputeBuffer(k_MaxDirectionalLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightData)));
            s_LightDatas = new ComputeBuffer(k_MaxPunctualLightsOnScreen + k_MaxAreaLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
            s_EnvLightDatas = new ComputeBuffer(k_MaxEnvLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
            s_shadowDatas = new ComputeBuffer(k_MaxCascadeCount + k_MaxShadowOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShadowData)));
            s_DecalDatas = new ComputeBuffer(k_MaxDecalsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DecalData)));

            GlobalLightLoopSettings gLightLoopSettings = hdAsset.GetRenderPipelineSettings().lightLoopSettings;
            m_CookieTexArray = new TextureCache2D();
            m_CookieTexArray.AllocTextureArray(gLightLoopSettings.cookieTexArraySize, gLightLoopSettings.cookieSize, gLightLoopSettings.cookieSize, TextureFormat.RGBA32, true);
            m_CubeCookieTexArray = new TextureCacheCubemap();
            m_CubeCookieTexArray.AllocTextureArray(gLightLoopSettings.cubeCookieTexArraySize, gLightLoopSettings.pointCookieSize, TextureFormat.RGBA32, true, m_CubeToPanoMaterial);

            TextureFormat probeCacheFormat = gLightLoopSettings.reflectionCacheCompressed ? TextureFormat.BC6H : TextureFormat.RGBAHalf;
            m_ReflectionProbeCache = new ReflectionProbeCache(hdAsset, iblFilterGGX, gLightLoopSettings.reflectionProbeCacheSize, gLightLoopSettings.reflectionCubemapSize, probeCacheFormat, true);

            TextureFormat planarProbeCacheFormat = gLightLoopSettings.planarReflectionCacheCompressed ? TextureFormat.BC6H : TextureFormat.RGBAHalf;
            m_ReflectionPlanarProbeCache = new PlanarReflectionProbeCache(hdAsset, iblFilterGGX, gLightLoopSettings.planarReflectionProbeCacheSize, gLightLoopSettings.planarReflectionTextureSize, planarProbeCacheFormat, true);

            s_GenAABBKernel = buildScreenAABBShader.FindKernel("ScreenBoundsAABB");

            // The bounds and light volumes are view-dependent, and AABB is additionally projection dependent.
            // The view and proj matrices are per eye in stereo. This means we have to double the size of these buffers.
            // TODO: Maybe in stereo, we will only support half as many lights total, in order to minimize buffer size waste.
            // Alternatively, we could re-size these buffers if any stereo camera is active, instead of unilaterally increasing buffer size.
            // TODO: I don't think k_MaxLightsOnScreen corresponds to the actual correct light count for cullable light types (punctual, area, env, decal)
            s_AABBBoundsBuffer = new ComputeBuffer(k_MaxStereoEyes * 2 * k_MaxLightsOnScreen, 3 * sizeof(float));
            s_ConvexBoundsBuffer = new ComputeBuffer(k_MaxStereoEyes* k_MaxLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightBound)));
            s_LightVolumeDataBuffer = new ComputeBuffer(k_MaxStereoEyes* k_MaxLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightVolumeData)));
            s_DispatchIndirectBuffer = new ComputeBuffer(LightDefinitions.s_NumFeatureVariants * 3, sizeof(uint), ComputeBufferType.IndirectArguments);

            // Cluster
            {
                s_ClearVoxelAtomicKernel = buildPerVoxelLightListShader.FindKernel("ClearAtomic");
                s_GlobalLightListAtomic = new ComputeBuffer(1, sizeof(uint));
            }

            s_GenListPerBigTileKernel = buildPerBigTileLightListShader.FindKernel("BigTileLightListGen");

            s_BuildDispatchIndirectKernel = buildDispatchIndirectShader.FindKernel("BuildDispatchIndirect");
            s_ClearDispatchIndirectKernel = clearDispatchIndirectShader.FindKernel("ClearDispatchIndirect");

            s_BuildMaterialFlagsOrKernel = buildMaterialFlagsShader.FindKernel("MaterialFlagsGen_Or");
            s_BuildMaterialFlagsWriteKernel = buildMaterialFlagsShader.FindKernel("MaterialFlagsGen_Write");

            s_shadeOpaqueDirectFptlKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl");
            s_shadeOpaqueDirectFptlDebugDisplayKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl_DebugDisplay");

            s_shadeOpaqueDirectShadowMaskFptlKernel = deferredComputeShader.FindKernel("Deferred_Direct_ShadowMask_Fptl");
            s_shadeOpaqueDirectShadowMaskFptlDebugDisplayKernel = deferredComputeShader.FindKernel("Deferred_Direct_ShadowMask_Fptl_DebugDisplay");

            s_deferredDirectionalShadowKernel = deferredDirectionalShadowComputeShader.FindKernel("DeferredDirectionalShadow");
            s_deferredDirectionalShadow_Contact_Kernel = deferredDirectionalShadowComputeShader.FindKernel("DeferredDirectionalShadow_Contact");
            s_deferredDirectionalShadow_Normals_Kernel = deferredDirectionalShadowComputeShader.FindKernel("DeferredDirectionalShadow_Normals");
            s_deferredDirectionalShadow_Contact_Normals_Kernel = deferredDirectionalShadowComputeShader.FindKernel("DeferredDirectionalShadow_Contact_Normals");

            for (int variant = 0; variant < LightDefinitions.s_NumFeatureVariants; variant++)
            {
                s_shadeOpaqueIndirectFptlKernels[variant] = deferredComputeShader.FindKernel("Deferred_Indirect_Fptl_Variant" + variant);
                s_shadeOpaqueIndirectShadowMaskFptlKernels[variant] = deferredComputeShader.FindKernel("Deferred_Indirect_ShadowMask_Fptl_Variant" + variant);
            }

            s_LightList = null;
            s_TileList = null;
            s_TileFeatureFlags = null;

            // OUTPUT_SPLIT_LIGHTING - LIGHTLOOP_TILE_PASS - SHADOWS_SHADOWMASK - DEBUG_DISPLAY
            m_deferredLightingMaterial = new Material[16];

            for (int outputSplitLighting = 0; outputSplitLighting < 2; ++outputSplitLighting)
            {
                for (int lightLoopTilePass = 0; lightLoopTilePass < 2; ++lightLoopTilePass)
                {
                    for (int shadowMask = 0; shadowMask < 2; ++shadowMask)
                    {
                        for (int debugDisplay = 0; debugDisplay < 2; ++debugDisplay)
                        {
                            int index = GetDeferredLightingMaterialIndex(outputSplitLighting, lightLoopTilePass, shadowMask, debugDisplay);

                            m_deferredLightingMaterial[index] = CoreUtils.CreateEngineMaterial(m_Resources.deferredShader);
                            CoreUtils.SetKeyword(m_deferredLightingMaterial[index], "OUTPUT_SPLIT_LIGHTING", outputSplitLighting == 1);
                            CoreUtils.SelectKeyword(m_deferredLightingMaterial[index], "LIGHTLOOP_TILE_PASS", "LIGHTLOOP_SINGLE_PASS", lightLoopTilePass == 1);
                            CoreUtils.SetKeyword(m_deferredLightingMaterial[index], "SHADOWS_SHADOWMASK", shadowMask == 1);
                            CoreUtils.SetKeyword(m_deferredLightingMaterial[index], "DEBUG_DISPLAY", debugDisplay == 1);

                            m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilMask, (int)HDRenderPipeline.StencilBitMask.LightingMask);
                            m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilRef, outputSplitLighting == 1 ? (int)StencilLightingUsage.SplitLighting : (int)StencilLightingUsage.RegularLighting);
                            m_deferredLightingMaterial[index].SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);
                            m_deferredLightingMaterial[index].SetInt(HDShaderIDs._SrcBlend, (int)BlendMode.One);
                            m_deferredLightingMaterial[index].SetInt(HDShaderIDs._DstBlend, (int)BlendMode.Zero);
                        }
                    }
                }
            }

            s_DefaultTexture2DArray = new Texture2DArray(1, 1, 1, TextureFormat.ARGB32, false);
            s_DefaultTexture2DArray.SetPixels32(new Color32[1] { new Color32(128, 128, 128, 128) }, 0);
            s_DefaultTexture2DArray.Apply();

            s_DefaultTextureCube = new Cubemap(16, TextureFormat.ARGB32, false);
            s_DefaultTextureCube.Apply();

            InitShadowSystem(hdAsset, shadowSettings);
        }

        public void Cleanup()
        {
            DeinitShadowSystem();

            CoreUtils.SafeRelease(s_DirectionalLightDatas);
            CoreUtils.SafeRelease(s_LightDatas);
            CoreUtils.SafeRelease(s_EnvLightDatas);
            CoreUtils.SafeRelease(s_shadowDatas);
            CoreUtils.SafeRelease(s_DecalDatas);

            if (m_ReflectionProbeCache != null)
            {
                m_ReflectionProbeCache.Release();
                m_ReflectionProbeCache = null;
            }
            if (m_ReflectionPlanarProbeCache != null)
            {
                m_ReflectionPlanarProbeCache.Release();
                m_ReflectionPlanarProbeCache = null;
            }
            if (m_CookieTexArray != null)
            {
                m_CookieTexArray.Release();
                m_CookieTexArray = null;
            }
            if (m_CubeCookieTexArray != null)
            {
                m_CubeCookieTexArray.Release();
                m_CubeCookieTexArray = null;
            }

            ReleaseResolutionDependentBuffers();

            CoreUtils.SafeRelease(s_AABBBoundsBuffer);
            CoreUtils.SafeRelease(s_ConvexBoundsBuffer);
            CoreUtils.SafeRelease(s_LightVolumeDataBuffer);
            CoreUtils.SafeRelease(s_DispatchIndirectBuffer);

            // enableClustered
            CoreUtils.SafeRelease(s_GlobalLightListAtomic);

            for (int outputSplitLighting = 0; outputSplitLighting < 2; ++outputSplitLighting)
            {
                for (int lightLoopTilePass = 0; lightLoopTilePass < 2; ++lightLoopTilePass)
                {
                    for (int shadowMask = 0; shadowMask < 2; ++shadowMask)
                    {
                        for (int debugDisplay = 0; debugDisplay < 2; ++debugDisplay)
                        {
                            int index = GetDeferredLightingMaterialIndex(outputSplitLighting, lightLoopTilePass, shadowMask, debugDisplay);
                            CoreUtils.Destroy(m_deferredLightingMaterial[index]);
                        }
                    }
                }
            }

            CoreUtils.Destroy(m_DebugViewTilesMaterial);
            CoreUtils.Destroy(m_DebugShadowMapMaterial);
            CoreUtils.Destroy(m_CubeToPanoMaterial);
        }

        public void NewFrame(FrameSettings frameSettings)
        {
            m_FrameSettings = frameSettings;

            // Cluster
            {
                var clustPrepassSourceIdx = m_FrameSettings.lightLoopSettings.enableBigTilePrepass ? ClusterPrepassSource.BigTile : ClusterPrepassSource.None;
                var clustDepthSourceIdx = ClusterDepthSource.NoDepth;
                if (k_UseDepthBuffer)
                {
                    if (m_FrameSettings.enableMSAA)
                        clustDepthSourceIdx = ClusterDepthSource.MSAA_Depth;
                    else
                        clustDepthSourceIdx = ClusterDepthSource.Depth;
                }
                var kernelName = s_ClusterKernelNames[(int)clustPrepassSourceIdx, (int)clustDepthSourceIdx];

                s_GenListPerVoxelKernel = buildPerVoxelLightListShader.FindKernel(kernelName);
            }

            if (GetFeatureVariantsEnabled())
            {
                s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel(m_FrameSettings.lightLoopSettings.enableBigTilePrepass ? "TileLightListGen_SrcBigTile_FeatureFlags" : "TileLightListGen_FeatureFlags");
            }
            else
            {
                s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel(m_FrameSettings.lightLoopSettings.enableBigTilePrepass ? "TileLightListGen_SrcBigTile" : "TileLightListGen");
            }

            m_CookieTexArray.NewFrame();
            m_CubeCookieTexArray.NewFrame();
            m_ReflectionProbeCache.NewFrame();
            m_ReflectionPlanarProbeCache.NewFrame();
        }

        public bool NeedResize()
        {
            return s_LightList == null || s_TileList == null || s_TileFeatureFlags == null ||
                (s_BigTileLightList == null && m_FrameSettings.lightLoopSettings.enableBigTilePrepass) ||
                (s_PerVoxelLightLists == null);
        }

        public void ReleaseResolutionDependentBuffers()
        {
            CoreUtils.SafeRelease(s_LightList);
            CoreUtils.SafeRelease(s_TileList);
            CoreUtils.SafeRelease(s_TileFeatureFlags);

            // enableClustered
            CoreUtils.SafeRelease(s_PerVoxelLightLists);
            CoreUtils.SafeRelease(s_PerVoxelOffset);
            CoreUtils.SafeRelease(s_PerTileLogBaseTweak);

            // enableBigTilePrepass
            CoreUtils.SafeRelease(s_BigTileLightList);
        }

        int NumLightIndicesPerClusteredTile()
        {
            return 8 * (1 << k_Log2NumClusters);       // total footprint for all layers of the tile (measured in light index entries)
        }

        public void AllocResolutionDependentBuffers(int width, int height, bool stereoEnabled)
        {
            var nrStereoLayers = stereoEnabled ? 2 : 1;

            var nrTilesX = (width + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
            var nrTilesY = (height + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
            var nrTiles = nrTilesX * nrTilesY * nrStereoLayers;
            const int capacityUShortsPerTile = 32;
            const int dwordsPerTile = (capacityUShortsPerTile + 1) >> 1;        // room for 31 lights and a nrLights value.

            s_LightList = new ComputeBuffer((int)LightCategory.Count * dwordsPerTile * nrTiles, sizeof(uint));       // enough list memory for a 4k x 4k display
            s_TileList = new ComputeBuffer((int)LightDefinitions.s_NumFeatureVariants * nrTiles, sizeof(uint));
            s_TileFeatureFlags = new ComputeBuffer(nrTiles, sizeof(uint));

            // Cluster
            {
                var nrClustersX = (width + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                var nrClustersY = (height + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                var nrClusterTiles = nrClustersX * nrClustersY * nrStereoLayers;

                s_PerVoxelOffset = new ComputeBuffer((int)LightCategory.Count * (1 << k_Log2NumClusters) * nrClusterTiles, sizeof(uint));
                s_PerVoxelLightLists = new ComputeBuffer(NumLightIndicesPerClusteredTile() * nrClusterTiles, sizeof(uint));

                if (k_UseDepthBuffer)
                {
                    s_PerTileLogBaseTweak = new ComputeBuffer(nrClusterTiles, sizeof(float));
                }
            }

            if (m_FrameSettings.lightLoopSettings.enableBigTilePrepass)
            {
                var nrBigTilesX = (width + 63) / 64;
                var nrBigTilesY = (height + 63) / 64;
                var nrBigTiles = nrBigTilesX * nrBigTilesY * nrStereoLayers;
                s_BigTileLightList = new ComputeBuffer(LightDefinitions.s_MaxNrBigTileLightsPlusOne * nrBigTiles, sizeof(uint));
            }
        }

        public static Matrix4x4 WorldToCamera(Camera camera)
        {
            // camera.worldToCameraMatrix is RHS and Unity's transforms are LHS
            // We need to flip it to work with transforms
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * camera.worldToCameraMatrix;
        }

        static Matrix4x4 WorldToViewStereo(Camera camera, Camera.StereoscopicEye eyeIndex)
        {
            return Matrix4x4.Scale(new Vector3(1, 1, -1)) * camera.GetStereoViewMatrix(eyeIndex);
        }

        // For light culling system, we need non oblique projection matrices
        static Matrix4x4 CameraProjectionNonObliqueLHS(HDCamera camera)
        {
            // camera.projectionMatrix expect RHS data and Unity's transforms are LHS
            // We need to flip it to work with transforms
            return camera.nonObliqueProjMatrix * Matrix4x4.Scale(new Vector3(1, 1, -1));
        }

        static Matrix4x4 CameraProjectionStereoLHS(Camera camera, Camera.StereoscopicEye eyeIndex)
        {
            return camera.GetStereoProjectionMatrix(eyeIndex) * Matrix4x4.Scale(new Vector3(1, 1, -1));
        }

        public Vector3 GetLightColor(VisibleLight light)
        {
            return new Vector3(light.finalColor.r, light.finalColor.g, light.finalColor.b);
        }

        public bool GetDirectionalLightData(CommandBuffer cmd, ShadowSettings shadowSettings, GPULightType gpuLightType, VisibleLight light, HDAdditionalLightData additionalData, AdditionalShadowData additionalShadowData, int lightIndex)
        {
            var directionalLightData = new DirectionalLightData();

            float diffuseDimmer = m_FrameSettings.diffuseGlobalDimmer * additionalData.lightDimmer;
            float specularDimmer = m_FrameSettings.specularGlobalDimmer * additionalData.lightDimmer;
            if (diffuseDimmer  <= 0.0f && specularDimmer <= 0.0f)
                return false;

            // Light direction for directional is opposite to the forward direction
            directionalLightData.forward = light.light.transform.forward;
            // Rescale for cookies and windowing.
            directionalLightData.right      = light.light.transform.right * 2 / Mathf.Max(additionalData.shapeWidth, 0.001f);
            directionalLightData.up         = light.light.transform.up    * 2 / Mathf.Max(additionalData.shapeHeight, 0.001f);
            directionalLightData.positionWS = light.light.transform.position;
            directionalLightData.color = GetLightColor(light);

            // Caution: This is bad but if additionalData == HDUtils.s_DefaultHDAdditionalLightData it mean we are trying to promote legacy lights, which is the case for the preview for example, so we need to multiply by PI as legacy Unity do implicit divide by PI for direct intensity.
            // So we expect that all light with additionalData == HDUtils.s_DefaultHDAdditionalLightData are currently the one from the preview, light in scene MUST have additionalData
            directionalLightData.color *= (HDUtils.s_DefaultHDAdditionalLightData == additionalData) ? Mathf.PI : 1.0f;

            directionalLightData.diffuseScale = additionalData.affectDiffuse ? diffuseDimmer : 0.0f;
            directionalLightData.specularScale = additionalData.affectSpecular ? specularDimmer : 0.0f;
            directionalLightData.shadowIndex = directionalLightData.cookieIndex = -1;

            if (light.light.cookie != null)
            {
                directionalLightData.tileCookie = light.light.cookie.wrapMode == TextureWrapMode.Repeat ? 1 : 0;
                directionalLightData.cookieIndex = m_CookieTexArray.FetchSlice(cmd, light.light.cookie);
            }
            // fix up shadow information
            int shadowIdx;
            if (m_ShadowIndices.TryGetValue(lightIndex, out shadowIdx))
            {
                directionalLightData.shadowIndex = shadowIdx;
                m_CurrentSunLight = light.light;
                m_CurrentSunLightShadowIndex = shadowIdx;
            }

            // TODO: Currently m_maxShadowDistance is based on shadow settings, but this value is define for a whole level. We should be able to change this value during gameplay
            float scale;
            float bias;
            GetScaleAndBiasForLinearDistanceFade(m_maxShadowDistance, out scale, out bias);
            directionalLightData.fadeDistanceScaleAndBias = new Vector2(scale, bias);
            directionalLightData.shadowMaskSelector = Vector4.zero;

            if (IsBakedShadowMaskLight(light.light))
            {
                directionalLightData.shadowMaskSelector[light.light.bakingOutput.occlusionMaskChannel] = 1.0f;
                // TODO: make this option per light, not global
                directionalLightData.dynamicShadowCasterOnly = QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 1 : 0;
            }
            else
            {
                // use -1 to say that we don't use shadow mask
                directionalLightData.shadowMaskSelector.x = -1.0f;
                directionalLightData.dynamicShadowCasterOnly = 0;
            }

            // Fallback to the first non shadow casting directional light.
            m_CurrentSunLight = m_CurrentSunLight == null ? light.light : m_CurrentSunLight;

            m_lightList.directionalLights.Add(directionalLightData);

            return true;
        }

        void GetScaleAndBiasForLinearDistanceFade(float fadeDistance, out float scale, out float bias)
        {
            // Fade with distance calculation is just a linear fade from 90% of fade distance to fade distance. 90% arbitrarily chosen but should work well enough.
            float distanceFadeNear = 0.9f * fadeDistance;
            scale = 1.0f / (fadeDistance - distanceFadeNear);
            bias = - distanceFadeNear / (fadeDistance - distanceFadeNear);
        }

        float ComputeLinearDistanceFade(float distanceToCamera, float fadeDistance)
        {
            float scale;
            float bias;
            GetScaleAndBiasForLinearDistanceFade(fadeDistance, out scale, out bias);

            return 1.0f - Mathf.Clamp01(distanceToCamera * scale + bias);
        }

        public bool GetLightData(CommandBuffer cmd, ShadowSettings shadowSettings, Camera camera, GPULightType gpuLightType,
                                 VisibleLight light, HDAdditionalLightData additionalLightData, AdditionalShadowData additionalshadowData,
                                 int lightIndex, ref Vector3 lightDimensions)
        {
            var lightData = new LightData();

            lightData.lightType = gpuLightType;

            lightData.positionWS = light.light.transform.position;
            // Setting 0 for invSqrAttenuationRadius mean we have no range attenuation, but still have inverse square attenuation.
            bool applyRangeAttenuation = additionalLightData.applyRangeAttenuation && (gpuLightType != GPULightType.ProjectorBox);
            lightData.invSqrAttenuationRadius = applyRangeAttenuation ? 1.0f / (light.range * light.range) : 0.0f;
            lightData.color = GetLightColor(light);

            lightData.forward = light.light.transform.forward; // Note: Light direction is oriented backward (-Z)
            lightData.up = light.light.transform.up;
            lightData.right = light.light.transform.right;

            lightDimensions.x = additionalLightData.shapeWidth;
            lightDimensions.y = additionalLightData.shapeHeight;
            lightDimensions.z = light.range;

            if (lightData.lightType == GPULightType.ProjectorBox)
            {
                lightData.size.x = light.range;

                // Rescale for cookies and windowing.
                lightData.right *= 2.0f / Mathf.Max(additionalLightData.shapeWidth, 0.001f);
                lightData.up    *= 2.0f / Mathf.Max(additionalLightData.shapeHeight, 0.001f);
            }
            else if (lightData.lightType == GPULightType.ProjectorPyramid)
            {
                // Get width and height for the current frustum
                var spotAngle = light.spotAngle;

                float frustumWidth, frustumHeight;

                if (additionalLightData.aspectRatio >= 1.0f)
                {
                    frustumHeight = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                    frustumWidth = frustumHeight * additionalLightData.aspectRatio;
                }
                else
                {
                    frustumWidth = 2.0f * Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad);
                    frustumHeight = frustumWidth / additionalLightData.aspectRatio;
                }

                // Adjust based on the new parametrization.
                lightDimensions.x = frustumWidth;
                lightDimensions.y = frustumHeight;

                // Rescale for cookies and windowing.
                lightData.right *= 2.0f / frustumWidth;
                lightData.up *= 2.0f / frustumHeight;
            }

            if (lightData.lightType == GPULightType.Spot)
            {
                var spotAngle = light.spotAngle;

                var innerConePercent = additionalLightData.GetInnerSpotPercent01();
                var cosSpotOuterHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * Mathf.Deg2Rad), 0.0f, 1.0f);
                var sinSpotOuterHalfAngle = Mathf.Sqrt(1.0f - cosSpotOuterHalfAngle * cosSpotOuterHalfAngle);
                var cosSpotInnerHalfAngle = Mathf.Clamp(Mathf.Cos(spotAngle * 0.5f * innerConePercent * Mathf.Deg2Rad), 0.0f, 1.0f); // inner cone

                var val = Mathf.Max(0.001f, (cosSpotInnerHalfAngle - cosSpotOuterHalfAngle));
                lightData.angleScale = 1.0f / val;
                lightData.angleOffset = -cosSpotOuterHalfAngle * lightData.angleScale;

                // Rescale for cookies and windowing.
                float cotOuterHalfAngle = cosSpotOuterHalfAngle / sinSpotOuterHalfAngle;
                lightData.up    *= cotOuterHalfAngle;
                lightData.right *= cotOuterHalfAngle;
            }
            else
            {
                // These are the neutral values allowing GetAngleAnttenuation in shader code to return 1.0
                lightData.angleScale = 0.0f;
                lightData.angleOffset = 1.0f;
            }

            if (lightData.lightType == GPULightType.Rectangle || lightData.lightType == GPULightType.Line)
            {
                lightData.size = new Vector2(additionalLightData.shapeWidth, additionalLightData.shapeHeight);
            }

            float distanceToCamera = (lightData.positionWS - camera.transform.position).magnitude;
            float distanceFade = ComputeLinearDistanceFade(distanceToCamera, additionalLightData.fadeDistance);
            float lightScale = additionalLightData.lightDimmer * distanceFade;

            lightData.diffuseScale = additionalLightData.affectDiffuse ? lightScale * m_FrameSettings.diffuseGlobalDimmer : 0.0f;
            lightData.specularScale = additionalLightData.affectSpecular ? lightScale * m_FrameSettings.specularGlobalDimmer : 0.0f;

            if (lightData.diffuseScale <= 0.0f && lightData.specularScale <= 0.0f)
                return false;

            lightData.cookieIndex = -1;
            lightData.shadowIndex = -1;

            if (light.light.cookie != null)
            {
                // TODO: add texture atlas support for cookie textures.
                switch (light.lightType)
                {
                    case LightType.Spot:
                        lightData.cookieIndex = m_CookieTexArray.FetchSlice(cmd, light.light.cookie);
                        break;
                    case LightType.Point:
                        lightData.cookieIndex = m_CubeCookieTexArray.FetchSlice(cmd, light.light.cookie);
                        break;
                }
            }
            else if (light.lightType == LightType.Spot && additionalLightData.spotLightShape != SpotLightShape.Cone)
            {
                // Projectors lights must always have a cookie texture.
                // As long as the cache is a texture array and not an atlas, the 4x4 white texture will be rescaled to 128
                lightData.cookieIndex = m_CookieTexArray.FetchSlice(cmd, Texture2D.whiteTexture);
            }

            if (additionalshadowData)
            {
                float shadowDistanceFade = ComputeLinearDistanceFade(distanceToCamera, additionalshadowData.shadowFadeDistance);
                lightData.shadowDimmer = additionalshadowData.shadowDimmer * shadowDistanceFade;
            }
            else
            {
                lightData.shadowDimmer = 1.0f;
            }

            // fix up shadow information
            int shadowIdx;
            if (m_ShadowIndices.TryGetValue(lightIndex, out shadowIdx))
            {
                lightData.shadowIndex = shadowIdx;
            }

            // Value of max smoothness is from artists point of view, need to convert from perceptual smoothness to roughness
            lightData.minRoughness = (1.0f - additionalLightData.maxSmoothness) * (1.0f - additionalLightData.maxSmoothness);

            lightData.shadowMaskSelector = Vector4.zero;

            if (IsBakedShadowMaskLight(light.light))
            {
                lightData.shadowMaskSelector[light.light.bakingOutput.occlusionMaskChannel] = 1.0f;
                // TODO: make this option per light, not global
                lightData.dynamicShadowCasterOnly = QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 1 : 0;
            }
            else
            {
                // use -1 to say that we don't use shadow mask
                lightData.shadowMaskSelector.x = -1.0f;
                lightData.dynamicShadowCasterOnly = 0;
            }

            m_lightList.lights.Add(lightData);

            return true;
        }

        // TODO: we should be able to do this calculation only with LightData without VisibleLight light, but for now pass both
        public void GetLightVolumeDataAndBound(LightCategory lightCategory, GPULightType gpuLightType, LightVolumeType lightVolumeType,
                                               VisibleLight light, LightData lightData, Vector3 lightDimensions, Matrix4x4 worldToView,
                                               Camera.StereoscopicEye eyeIndex = Camera.StereoscopicEye.Left)
        {
            // Then Culling side
            var range = lightDimensions.z;
            var lightToWorld = light.localToWorld;
            Vector3 positionWS = lightData.positionWS;
            Vector3 positionVS = worldToView.MultiplyPoint(positionWS);

            Matrix4x4 lightToView = worldToView * lightToWorld;
            Vector3   xAxisVS     = lightToView.GetColumn(0);
            Vector3   yAxisVS     = lightToView.GetColumn(1);
            Vector3   zAxisVS     = lightToView.GetColumn(2);

            // Fill bounds
            var bound = new SFiniteLightBound();
            var lightVolumeData = new LightVolumeData();

            lightVolumeData.lightCategory = (uint)lightCategory;
            lightVolumeData.lightVolume = (uint)lightVolumeType;

            if (gpuLightType == GPULightType.Spot || gpuLightType == GPULightType.ProjectorPyramid)
            {
                Vector3 lightDir = lightToWorld.GetColumn(2);

                // represents a left hand coordinate system in world space since det(worldToView)<0
                Vector3 vx = xAxisVS;
                Vector3 vy = yAxisVS;
                Vector3 vz = zAxisVS;

                const float pi = 3.1415926535897932384626433832795f;
                const float degToRad = (float)(pi / 180.0);

                var sa = light.light.spotAngle;
                var cs = Mathf.Cos(0.5f * sa * degToRad);
                var si = Mathf.Sin(0.5f * sa * degToRad);

                if (gpuLightType == GPULightType.ProjectorPyramid)
                {
                    Vector3 lightPosToProjWindowCorner = (0.5f * lightDimensions.x) * vx + (0.5f * lightDimensions.y) * vy + 1.0f * vz;
                    cs = Vector3.Dot(vz, Vector3.Normalize(lightPosToProjWindowCorner));
                    si = Mathf.Sqrt(1.0f - cs * cs);
                }

                const float FltMax = 3.402823466e+38F;
                var ta = cs > 0.0f ? (si / cs) : FltMax;
                var cota = si > 0.0f ? (cs / si) : FltMax;

                //const float cotasa = l.GetCotanHalfSpotAngle();

                // apply nonuniform scale to OBB of spot light
                var squeeze = true;//sa < 0.7f * 90.0f;      // arb heuristic
                var fS = squeeze ? ta : si;
                bound.center = worldToView.MultiplyPoint(positionWS + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

                // scale axis to match box or base of pyramid
                bound.boxAxisX = (fS * range) * vx;
                bound.boxAxisY = (fS * range) * vy;
                bound.boxAxisZ = (0.5f * range) * vz;

                // generate bounding sphere radius
                var fAltDx = si;
                var fAltDy = cs;
                fAltDy = fAltDy - 0.5f;
                //if(fAltDy<0) fAltDy=-fAltDy;

                fAltDx *= range; fAltDy *= range;

                // Handle case of pyramid with this select (currently unused)
                var altDist = Mathf.Sqrt(fAltDy * fAltDy + (true ? 1.0f : 2.0f) * fAltDx * fAltDx);
                bound.radius = altDist > (0.5f * range) ? altDist : (0.5f * range);       // will always pick fAltDist
                bound.scaleXY = squeeze ? new Vector2(0.01f, 0.01f) : new Vector2(1.0f, 1.0f);

                lightVolumeData.lightAxisX = vx;
                lightVolumeData.lightAxisY = vy;
                lightVolumeData.lightAxisZ = vz;
                lightVolumeData.lightPos = positionVS;
                lightVolumeData.radiusSq = range * range;
                lightVolumeData.cotan = cota;
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
            }
            else if (gpuLightType == GPULightType.Point)
            {
                Vector3 vx = xAxisVS;
                Vector3 vy = yAxisVS;
                Vector3 vz = zAxisVS;

                bound.center   = positionVS;
                bound.boxAxisX = vx * range;
                bound.boxAxisY = vy * range;
                bound.boxAxisZ = vz * range;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = range;

                // fill up ldata
                lightVolumeData.lightAxisX = vx;
                lightVolumeData.lightAxisY = vy;
                lightVolumeData.lightAxisZ = vz;
                lightVolumeData.lightPos = bound.center;
                lightVolumeData.radiusSq = range * range;
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
            }
            else if (gpuLightType == GPULightType.Line)
            {
                Vector3 dimensions = new Vector3(lightDimensions.x + 2 * range, 2 * range, 2 * range); // Omni-directional
                Vector3 extents = 0.5f * dimensions;

                bound.center = positionVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = extents.magnitude;

                lightVolumeData.lightPos = positionVS;
                lightVolumeData.lightAxisX = xAxisVS;
                lightVolumeData.lightAxisY = yAxisVS;
                lightVolumeData.lightAxisZ = zAxisVS;
                lightVolumeData.boxInnerDist = new Vector3(lightDimensions.x, 0, 0);
                lightVolumeData.boxInvRange.Set(1.0f / range, 1.0f / range, 1.0f / range);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
            }
            else if (gpuLightType == GPULightType.Rectangle)
            {
                Vector3 dimensions = new Vector3(lightDimensions.x + 2 * range, lightDimensions.y + 2 * range, range); // One-sided
                Vector3 extents = 0.5f * dimensions;
                Vector3 centerVS = positionVS + extents.z * zAxisVS;

                bound.center = centerVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = extents.magnitude;

                lightVolumeData.lightPos     = centerVS;
                lightVolumeData.lightAxisX   = xAxisVS;
                lightVolumeData.lightAxisY   = yAxisVS;
                lightVolumeData.lightAxisZ   = zAxisVS;
                lightVolumeData.boxInnerDist = extents;
                lightVolumeData.boxInvRange.Set(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
            }
            else if (gpuLightType == GPULightType.ProjectorBox)
            {
                Vector3 dimensions  = new Vector3(lightDimensions.x, lightDimensions.y, range);  // One-sided
                Vector3 extents = 0.5f * dimensions;
                Vector3 centerVS = positionVS + extents.z * zAxisVS;

                bound.center   = centerVS;
                bound.boxAxisX = extents.x * xAxisVS;
                bound.boxAxisY = extents.y * yAxisVS;
                bound.boxAxisZ = extents.z * zAxisVS;
                bound.radius   = extents.magnitude;
                bound.scaleXY.Set(1.0f, 1.0f);

                lightVolumeData.lightPos     = centerVS;
                lightVolumeData.lightAxisX   = xAxisVS;
                lightVolumeData.lightAxisY   = yAxisVS;
                lightVolumeData.lightAxisZ   = zAxisVS;
                lightVolumeData.boxInnerDist = extents;
                lightVolumeData.boxInvRange.Set(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Punctual;
            }
            else
            {
                Debug.Assert(false, "TODO: encountered an unknown GPULightType.");
            }

            if (eyeIndex == Camera.StereoscopicEye.Left)
            {
                m_lightList.bounds.Add(bound);
                m_lightList.lightVolumes.Add(lightVolumeData);
            }
            else
            {
                m_lightList.rightEyeBounds.Add(bound);
                m_lightList.rightEyeLightVolumes.Add(lightVolumeData);
            }
        }


        public bool GetEnvLightData(CommandBuffer cmd, Camera camera, ProbeWrapper probe)
        {
            // For now we won't display real time probe when rendering one.
            // TODO: We may want to display last frame result but in this case we need to be careful not to update the atlas before all realtime probes are rendered (for frame coherency).
            // Unfortunately we don't have this information at the moment.
            if (probe.mode == ReflectionProbeMode.Realtime && camera.cameraType == CameraType.Reflection)
                return false;

            var capturePosition = Vector3.zero;
            var influenceToWorld = probe.influenceToWorld;

            var sampleDirectionDiscardWS = Vector3.zero;

            // 31 bits index, 1 bit cache type
            var envIndex = -1;
            if (probe.planarReflectionProbe != null)
            {
                var fetchIndex = m_ReflectionPlanarProbeCache.FetchSlice(cmd, probe.texture);
                envIndex = (fetchIndex << 1) | (int)EnvCacheType.Texture2D;

                float nearClipPlane, farClipPlane, aspect, fov;
                Color backgroundColor;
                CameraClearFlags clearFlags;
                Quaternion captureRotation;
                Matrix4x4 worldToCamera, projection;

                ReflectionSystem.CalculateCaptureCameraProperties(
                    probe.planarReflectionProbe,
                    out nearClipPlane, out farClipPlane,
                    out aspect, out fov, out clearFlags, out backgroundColor,
                    out worldToCamera, out projection, out capturePosition, out captureRotation,
                    camera);

                var gpuProj = GL.GetGPUProjectionMatrix(projection, true); // Had to change this from 'false'
                var gpuView = worldToCamera;

                // We transform it to object space by translating the capturePosition
                var vp = gpuProj * gpuView * Matrix4x4.Translate(capturePosition);
                m_Env2DCaptureVP[fetchIndex] = vp;
                sampleDirectionDiscardWS = captureRotation * Vector3.forward;
            }
            else if (probe.reflectionProbe != null)
            {
                envIndex = m_ReflectionProbeCache.FetchSlice(cmd, probe.texture);
                envIndex = envIndex << 1 | (int)EnvCacheType.Cubemap;
                capturePosition = (Vector3)influenceToWorld.GetColumn(3) - probe.reflectionProbe.center;
            }
            // -1 means that the texture is not ready yet (ie not convolved/compressed yet)
            if (envIndex == -1)
                return false;

            // Build light data
            var envLightData = new EnvLightData();


            envLightData.influenceShapeType = probe.influenceShapeType;
            envLightData.dimmer = probe.dimmer;
            envLightData.influenceExtents = probe.influenceExtents;
            envLightData.blendNormalDistancePositive = probe.blendNormalDistancePositive;
            envLightData.blendNormalDistanceNegative = probe.blendNormalDistanceNegative;
            envLightData.blendDistancePositive = probe.blendDistancePositive;
            envLightData.blendDistanceNegative = probe.blendDistanceNegative;
            envLightData.boxSideFadePositive = probe.boxSideFadePositive;
            envLightData.boxSideFadeNegative = probe.boxSideFadeNegative;
            envLightData.sampleDirectionDiscardWS = sampleDirectionDiscardWS;

            envLightData.influenceRight = influenceToWorld.GetColumn(0).normalized;
            envLightData.influenceUp = influenceToWorld.GetColumn(1).normalized;
            envLightData.influenceForward = influenceToWorld.GetColumn(2).normalized;
            envLightData.capturePositionWS = capturePosition;
            envLightData.influencePositionWS = influenceToWorld.GetColumn(3);

            envLightData.envIndex = envIndex;

            // Proxy data
            var proxyToWorld = probe.proxyToWorld;
            envLightData.proxyExtents = probe.proxyExtents;
            envLightData.minProjectionDistance = probe.infiniteProjection ? 65504f : 0;
            envLightData.proxyRight = proxyToWorld.GetColumn(0).normalized;
            envLightData.proxyUp = proxyToWorld.GetColumn(1).normalized;
            envLightData.proxyForward = proxyToWorld.GetColumn(2).normalized;
            envLightData.proxyPositionWS = proxyToWorld.GetColumn(3);

            m_lightList.envLights.Add(envLightData);
            return true;
        }

        public void GetEnvLightVolumeDataAndBound(ProbeWrapper probe, LightVolumeType lightVolumeType, Matrix4x4 worldToView, Camera.StereoscopicEye eyeIndex = Camera.StereoscopicEye.Left)
        {
            var bound = new SFiniteLightBound();
            var lightVolumeData = new LightVolumeData();

            // C is reflection volume center in world space (NOT same as cube map capture point)
            var influenceExtents = probe.influenceExtents;       // 0.5f * Vector3.Max(-boxSizes[p], boxSizes[p]);

            var influenceToWorld = probe.influenceToWorld;

            // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
            var influenceRightVS = worldToView.MultiplyVector(influenceToWorld.GetColumn(0).normalized);
            var influenceUpVS = worldToView.MultiplyVector(influenceToWorld.GetColumn(1).normalized);
            var influenceForwardVS = worldToView.MultiplyVector(influenceToWorld.GetColumn(2).normalized);
            var influencePositionVS = worldToView.MultiplyPoint(influenceToWorld.GetColumn(3));

            lightVolumeData.lightCategory = (uint)LightCategory.Env;
            lightVolumeData.lightVolume = (uint)lightVolumeType;
            lightVolumeData.featureFlags = (uint)LightFeatureFlags.Env;

            switch (lightVolumeType)
            {
                case LightVolumeType.Sphere:
                {
                    lightVolumeData.lightPos = influencePositionVS;
                    lightVolumeData.radiusSq = influenceExtents.x * influenceExtents.x;
                    lightVolumeData.lightAxisX = influenceRightVS;
                    lightVolumeData.lightAxisY = influenceUpVS;
                    lightVolumeData.lightAxisZ = influenceForwardVS;

                    bound.center = influencePositionVS;
                    bound.boxAxisX = influenceRightVS * influenceExtents.x;
                    bound.boxAxisY = influenceUpVS * influenceExtents.x;
                    bound.boxAxisZ = influenceForwardVS * influenceExtents.x;
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = influenceExtents.x;
                    break;
                }
                case LightVolumeType.Box:
                {
                    bound.center = influencePositionVS;
                    bound.boxAxisX = influenceExtents.x * influenceRightVS;
                    bound.boxAxisY = influenceExtents.y * influenceUpVS;
                    bound.boxAxisZ = influenceExtents.z * influenceForwardVS;
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = influenceExtents.magnitude;

                    // The culling system culls pixels that are further
                    //   than a threshold to the box influence extents.
                    // So we use an arbitrary threshold here (k_BoxCullingExtentOffset)
                    lightVolumeData.lightPos = influencePositionVS;
                    lightVolumeData.lightAxisX = influenceRightVS;
                    lightVolumeData.lightAxisY = influenceUpVS;
                    lightVolumeData.lightAxisZ = influenceForwardVS;
                    lightVolumeData.boxInnerDist = influenceExtents - k_BoxCullingExtentThreshold;
                    lightVolumeData.boxInvRange.Set(1.0f / k_BoxCullingExtentThreshold.x, 1.0f / k_BoxCullingExtentThreshold.y, 1.0f / k_BoxCullingExtentThreshold.z);
                    break;
                }
            }

            if (eyeIndex == Camera.StereoscopicEye.Left)
            {
                m_lightList.bounds.Add(bound);
                m_lightList.lightVolumes.Add(lightVolumeData);
            }
            else
            {
                m_lightList.rightEyeBounds.Add(bound);
                m_lightList.rightEyeLightVolumes.Add(lightVolumeData);
            }
        }

        public int GetCurrentShadowCount()
        {
            return m_ShadowRequests.Count;
        }

        public int GetShadowAtlasCount()
        {
            return (m_ShadowMgr == null) ? 0 : (int)m_ShadowMgr.GetShadowMapCount();
        }

        public void UpdateCullingParameters(ref ScriptableCullingParameters cullingParams)
        {
            m_ShadowMgr.UpdateCullingParameters( ref cullingParams );
        }

        public bool IsBakedShadowMaskLight(Light light)
        {
            return light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                    light.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask &&
                    light.bakingOutput.occlusionMaskChannel != -1; // We need to have an occlusion mask channel assign, else we have no shadow mask
        }

        // Return true if BakedShadowMask are enabled
        public bool PrepareLightsForGPU(CommandBuffer cmd, ShadowSettings shadowSettings, CullResults cullResults, ReflectionProbeCullResults reflectionProbeCullResults, Camera camera)
        {
            using (new ProfilingSample(cmd, "Prepare Lights For GPU"))
            {
                // If any light require it, we need to enabled bake shadow mask feature
                m_enableBakeShadowMask = false;

                m_lightList.Clear();

                // We need to properly reset this here otherwise if we go from 1 light to no visible light we would keep the old reference active.
                m_CurrentSunLight = null;
                m_CurrentSunLightShadowIndex = -1;

                var stereoEnabled = m_FrameSettings.enableStereo;

                // Note: Light with null intensity/Color are culled by the C++, no need to test it here
                if (cullResults.visibleLights.Count != 0 || cullResults.visibleReflectionProbes.Count != 0)
                {
                    // 0. deal with shadows
                    {
                        m_FrameId.frameCount++;
                        // get the indices for all lights that want to have shadows
                        m_ShadowRequests.Clear();
                        m_ShadowRequests.Capacity = cullResults.visibleLights.Count;
                        int lcnt = cullResults.visibleLights.Count;
                        for (int i = 0; i < lcnt; ++i)
                        {
                            VisibleLight vl = cullResults.visibleLights[i];
                            if (vl.light.shadows == LightShadows.None)
                                continue;

                            AdditionalShadowData asd = vl.light.GetComponent<AdditionalShadowData>();
                            if (asd != null && asd.shadowDimmer > 0.0f)
                            {
                                m_ShadowRequests.Add(i);

                                // Discover sun light and update cascade info from Volumes
                                // TODO: This should be moved to GetDirectionalLightData when we merge the two loops here.
                                // Careful it must still be done BEFORE the call to ProcessShadowRequests
                                if (vl.lightType == LightType.Directional && m_CurrentSunLight == null)
                                {
                                    var hdShadowSettings = VolumeManager.instance.stack.GetComponent<HDShadowSettings>();
                                    asd.SetShadowCascades(hdShadowSettings.cascadeShadowSplitCount, hdShadowSettings.cascadeShadowSplits, hdShadowSettings.cascadeShadowBorders );
                                }
                            }
                        }
                        // pass this list to a routine that assigns shadows based on some heuristic
                        uint shadowRequestCount = (uint)m_ShadowRequests.Count;
                        //TODO: Do not call ToArray here to avoid GC, refactor API
                        int[]   shadowRequests = m_ShadowRequests.ToArray();
                        int[]   shadowDataIndices;

                        m_ShadowMgr.ProcessShadowRequests(m_FrameId, cullResults, camera, ShaderConfig.s_CameraRelativeRendering != 0, cullResults.visibleLights,
                            ref shadowRequestCount, shadowRequests, out shadowDataIndices);

                        // update the visibleLights with the shadow information
                        m_ShadowIndices.Clear();
                        for (uint i = 0; i < shadowRequestCount; i++)
                        {
                            m_ShadowIndices.Add(shadowRequests[i], shadowDataIndices[i]);
                        }
                    }

                    // 1. Count the number of lights and sort all lights by category, type and volume - This is required for the fptl/cluster shader code
                    // If we reach maximum of lights available on screen, then we discard the light.
                    // Lights are processed in order, so we don't discards light based on their importance but based on their ordering in visible lights list.
                    int directionalLightcount = 0;
                    int punctualLightcount = 0;
                    int areaLightCount = 0;

                    int lightCount = Math.Min(cullResults.visibleLights.Count, k_MaxLightsOnScreen);
                    var sortKeys = new uint[lightCount];
                    int sortCount = 0;

                    for (int lightIndex = 0, numLights = cullResults.visibleLights.Count; (lightIndex < numLights) && (sortCount < lightCount); ++lightIndex)
                    {
                        var light = cullResults.visibleLights[lightIndex];

                        // Light should always have additional data, however preview light right don't have, so we must handle the case by assigning HDUtils.s_DefaultHDAdditionalLightData
                        var additionalData = GetHDAdditionalLightData(light);

                        LightCategory lightCategory = LightCategory.Count;
                        GPULightType gpuLightType = GPULightType.Point;
                        LightVolumeType lightVolumeType = LightVolumeType.Count;

                        if (additionalData.lightTypeExtent == LightTypeExtent.Punctual)
                        {
                            lightCategory = LightCategory.Punctual;

                            switch (light.lightType)
                            {
                                case LightType.Spot:
                                    if (punctualLightcount >= k_MaxPunctualLightsOnScreen)
                                        continue;
                                    switch (additionalData.spotLightShape)
                                    {
                                        case SpotLightShape.Cone:
                                            gpuLightType = GPULightType.Spot;
                                            lightVolumeType = LightVolumeType.Cone;
                                            break;
                                        case SpotLightShape.Pyramid:
                                            gpuLightType = GPULightType.ProjectorPyramid;
                                            lightVolumeType = LightVolumeType.Cone;
                                            break;
                                        case SpotLightShape.Box:
                                            gpuLightType = GPULightType.ProjectorBox;
                                            lightVolumeType = LightVolumeType.Box;
                                            break;
                                        default:
                                            Debug.Assert(false, "Encountered an unknown SpotLightShape.");
                                            break;
                                    }
                                    break;

                                case LightType.Directional:
                                    if (directionalLightcount >= k_MaxDirectionalLightsOnScreen)
                                        continue;
                                    gpuLightType = GPULightType.Directional;
                                    // No need to add volume, always visible
                                    lightVolumeType = LightVolumeType.Count; // Count is none
                                    break;

                                case LightType.Point:
                                    if (punctualLightcount >= k_MaxPunctualLightsOnScreen)
                                        continue;
                                    gpuLightType = GPULightType.Point;
                                    lightVolumeType = LightVolumeType.Sphere;
                                    break;

                                default:
                                    Debug.Assert(false, "Encountered an unknown LightType.");
                                    break;
                            }
                        }
                        else
                        {
                            lightCategory = LightCategory.Area;

                            switch (additionalData.lightTypeExtent)
                            {
                                case LightTypeExtent.Rectangle:
                                    if (areaLightCount >= k_MaxAreaLightsOnScreen)
                                        continue;
                                    gpuLightType = GPULightType.Rectangle;
                                    lightVolumeType = LightVolumeType.Box;
                                    break;

                                case LightTypeExtent.Line:
                                    if (areaLightCount >= k_MaxAreaLightsOnScreen)
                                        continue;
                                    gpuLightType = GPULightType.Line;
                                    lightVolumeType = LightVolumeType.Box;
                                    break;

                                default:
                                    Debug.Assert(false, "Encountered an unknown LightType.");
                                    break;
                            }
                        }

                        uint shadow = m_ShadowIndices.ContainsKey(lightIndex) ? 1u : 0;
                        // 5 bit (0x1F) light category, 5 bit (0x1F) GPULightType, 5 bit (0x1F) lightVolume, 1 bit for shadow casting, 16 bit index
                        sortKeys[sortCount++] = (uint)lightCategory << 27 | (uint)gpuLightType << 22 | (uint)lightVolumeType << 17 | shadow << 16 | (uint)lightIndex;
                    }

                    CoreUtils.QuickSort(sortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                    // TODO: Refactor shadow management
                    // The good way of managing shadow:
                    // Here we sort everyone and we decide which light is important or not (this is the responsibility of the lightloop)
                    // we allocate shadow slot based on maximum shadow allowed on screen and attribute slot by bigger solid angle
                    // THEN we ask to the ShadowRender to render the shadow, not the reverse as it is today (i.e render shadow than expect they
                    // will be use...)
                    // The lightLoop is in charge, not the shadow pass.
                    // For now we will still apply the maximum of shadow here but we don't apply the sorting by priority + slot allocation yet

                    // 2. Go through all lights, convert them to GPU format.
                    // Simultaneously create data for culling (LightVolumeData and SFiniteLightBound)
                    Vector3 camPosWS = camera.transform.position;

                    var worldToView = WorldToCamera(camera);
                    var rightEyeWorldToView = Matrix4x4.identity;
                    if (stereoEnabled)
                    {
                        worldToView = WorldToViewStereo(camera, Camera.StereoscopicEye.Left);
                        rightEyeWorldToView = WorldToViewStereo(camera, Camera.StereoscopicEye.Right);
                    }

                    for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                    {
                        // In 1. we have already classify and sorted the light, we need to use this sorted order here
                        uint sortKey = sortKeys[sortIndex];
                        LightCategory lightCategory = (LightCategory)((sortKey >> 27) & 0x1F);
                        GPULightType gpuLightType = (GPULightType)((sortKey >> 22) & 0x1F);
                        LightVolumeType lightVolumeType = (LightVolumeType)((sortKey >> 17) & 0x1F);
                        int lightIndex = (int)(sortKey & 0xFFFF);

                        var light = cullResults.visibleLights[lightIndex];

                        m_enableBakeShadowMask = m_enableBakeShadowMask || IsBakedShadowMaskLight(light.light);

                        // Light should always have additional data, however preview light right don't have, so we must handle the case by assigning HDUtils.s_DefaultHDAdditionalLightData
                        var additionalLightData = GetHDAdditionalLightData(light);
                        var additionalShadowData = light.light.GetComponent<AdditionalShadowData>(); // Can be null

                        // Directional rendering side, it is separated as it is always visible so no volume to handle here
                        if (gpuLightType == GPULightType.Directional)
                        {
                            if (GetDirectionalLightData(cmd, shadowSettings, gpuLightType, light, additionalLightData, additionalShadowData, lightIndex))
                            {
                                directionalLightcount++;

                                // We make the light position camera-relative as late as possible in order
                                // to allow the preceding code to work with the absolute world space coordinates.
                                if (ShaderConfig.s_CameraRelativeRendering != 0)
                                {
                                    // Caution: 'DirectionalLightData.positionWS' is camera-relative after this point.
                                    int n = m_lightList.directionalLights.Count;
                                    DirectionalLightData lightData = m_lightList.directionalLights[n - 1];
                                    lightData.positionWS -= camPosWS;
                                    m_lightList.directionalLights[n - 1] = lightData;
                                }
                            }
                            continue;
                        }

                        Vector3 lightDimensions = new Vector3(); // X = length or width, Y = height, Z = range (depth)

                        // Punctual, area, projector lights - the rendering side.
                        if (GetLightData(cmd, shadowSettings, camera, gpuLightType, light, additionalLightData, additionalShadowData, lightIndex, ref lightDimensions))
                        {
                            switch (lightCategory)
                            {
                                case LightCategory.Punctual:
                                    punctualLightcount++;
                                    break;
                                case LightCategory.Area:
                                    areaLightCount++;
                                    break;
                                default:
                                    Debug.Assert(false, "TODO: encountered an unknown LightCategory.");
                                    break;
                            }

                            // Then culling side. Must be call in this order as we pass the created Light data to the function
                            GetLightVolumeDataAndBound(lightCategory, gpuLightType, lightVolumeType, light, m_lightList.lights[m_lightList.lights.Count - 1], lightDimensions, worldToView);
                            if (stereoEnabled)
                                GetLightVolumeDataAndBound(lightCategory, gpuLightType, lightVolumeType, light, m_lightList.lights[m_lightList.lights.Count - 1], lightDimensions, rightEyeWorldToView, Camera.StereoscopicEye.Right);

                            // We make the light position camera-relative as late as possible in order
                            // to allow the preceding code to work with the absolute world space coordinates.
                            if (ShaderConfig.s_CameraRelativeRendering != 0)
                            {
                                // Caution: 'LightData.positionWS' is camera-relative after this point.
                                int n = m_lightList.lights.Count;
                                LightData lightData = m_lightList.lights[n - 1];
                                lightData.positionWS -= camPosWS;
                                m_lightList.lights[n - 1] = lightData;
                            }
                        }
                    }

                    // Sanity check
                    Debug.Assert(m_lightList.directionalLights.Count == directionalLightcount);
                    Debug.Assert(m_lightList.lights.Count == areaLightCount + punctualLightcount);

                    m_punctualLightCount = punctualLightcount;
                    m_areaLightCount = areaLightCount;

                    // Redo everything but this time with envLights
                    int envLightCount = 0;

                    var totalProbes = cullResults.visibleReflectionProbes.Count + reflectionProbeCullResults.visiblePlanarReflectionProbeCount;
                    int probeCount = Math.Min(totalProbes, k_MaxEnvLightsOnScreen);
                    sortKeys = new uint[probeCount];
                    sortCount = 0;

                    for (int probeIndex = 0, numProbes = totalProbes; (probeIndex < numProbes) && (sortCount < probeCount); probeIndex++)
                    {
                        if (probeIndex < cullResults.visibleReflectionProbes.Count)
                    {
                        VisibleReflectionProbe probe = cullResults.visibleReflectionProbes[probeIndex];
                        HDAdditionalReflectionData additional = probe.probe.GetComponent<HDAdditionalReflectionData>();

                        // probe.texture can be null when we are adding a reflection probe in the editor
                        if (probe.texture == null || envLightCount >= k_MaxEnvLightsOnScreen)
                            continue;

                        // Work around the culling issues. TODO: fix culling in C++.
                        if (probe.probe == null || !probe.probe.isActiveAndEnabled)
                            continue;

                        // Work around the data issues.
                        if (probe.localToWorld.determinant == 0)
                        {
                            Debug.LogError("Reflection probe " + probe.probe.name + " has an invalid local frame and needs to be fixed.");
                            continue;
                        }

                        LightVolumeType lightVolumeType = LightVolumeType.Box;
                        if (additional != null && additional.influenceShape == ShapeType.Sphere)
                            lightVolumeType = LightVolumeType.Sphere;
                        ++envLightCount;

                            var logVolume = CalculateProbeLogVolume(probe.bounds);

                            sortKeys[sortCount++] = PackProbeKey(logVolume, lightVolumeType, 0u, probeIndex); // Sort by volume
                        }
                        else
                        {
                            var planarProbeIndex = probeIndex - cullResults.visibleReflectionProbes.Count;
                            var probe = reflectionProbeCullResults.visiblePlanarReflectionProbes[planarProbeIndex];

                            // probe.texture can be null when we are adding a reflection probe in the editor
                            if (probe.texture == null || envLightCount >= k_MaxEnvLightsOnScreen)
                                continue;

                            var lightVolumeType = LightVolumeType.Box;
                            if (probe.influenceVolume.shapeType == ShapeType.Sphere)
                                lightVolumeType = LightVolumeType.Sphere;
                            ++envLightCount;

                            var logVolume = CalculateProbeLogVolume(probe.bounds);

                            sortKeys[sortCount++] = PackProbeKey(logVolume, lightVolumeType, 1u, planarProbeIndex); // Sort by volume
                        }
                    }

                    // Not necessary yet but call it for future modification with sphere influence volume
                    CoreUtils.QuickSort(sortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                    for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                    {
                        // In 1. we have already classify and sorted the light, we need to use this sorted order here
                        uint sortKey = sortKeys[sortIndex];
                        LightVolumeType lightVolumeType;
                        int probeIndex;
                        int listType;
                        UnpackProbeSortKey(sortKey, out lightVolumeType, out probeIndex, out listType);

                        PlanarReflectionProbe planarProbe = null;
                        VisibleReflectionProbe probe = default(VisibleReflectionProbe);
                        if (listType == 0)
                            probe = cullResults.visibleReflectionProbes[probeIndex];
                        else
                            planarProbe = reflectionProbeCullResults.visiblePlanarReflectionProbes[probeIndex];

                        var probeWrapper = ProbeWrapper.Wrap(probe, planarProbe);

                        if (GetEnvLightData(cmd, camera, probeWrapper))
                        {
                            GetEnvLightVolumeDataAndBound(probeWrapper, lightVolumeType, worldToView);
                            if (stereoEnabled)
                                GetEnvLightVolumeDataAndBound(probeWrapper, lightVolumeType, rightEyeWorldToView, Camera.StereoscopicEye.Right);

                            // We make the light position camera-relative as late as possible in order
                            // to allow the preceding code to work with the absolute world space coordinates.
                            if (ShaderConfig.s_CameraRelativeRendering != 0)
                            {
                                // Caution: 'EnvLightData.positionWS' is camera-relative after this point.
                                int n = m_lightList.envLights.Count;
                                EnvLightData envLightData = m_lightList.envLights[n - 1];
                                envLightData.capturePositionWS -= camPosWS;
                                envLightData.influencePositionWS -= camPosWS;
                                envLightData.proxyPositionWS -= camPosWS;
                                m_lightList.envLights[n - 1] = envLightData;
                            }
                        }
                    }
                }

                m_lightCount = m_lightList.lights.Count + m_lightList.envLights.Count;
                Debug.Assert(m_lightList.bounds.Count == m_lightCount);
                Debug.Assert(m_lightList.lightVolumes.Count == m_lightCount);

                int decalDatasCount = Math.Min(DecalSystem.m_DecalDatasCount, k_MaxDecalsOnScreen);
                if (decalDatasCount > 0)
                {
                    for (int i = 0; i < decalDatasCount; i++)
                    {
                        m_lightList.bounds.Add(DecalSystem.m_Bounds[i]);
                        m_lightList.lightVolumes.Add(DecalSystem.m_LightVolumes[i]);
                    }
                    m_lightCount += decalDatasCount;
                }

                if (stereoEnabled)
                {
                    // TODO: Proper decal + stereo cull management

                    Debug.Assert(m_lightList.rightEyeBounds.Count == m_lightCount);
                    Debug.Assert(m_lightList.rightEyeLightVolumes.Count == m_lightCount);

                    // TODO: GC considerations?
                    m_lightList.bounds.AddRange(m_lightList.rightEyeBounds);
                    m_lightList.lightVolumes.AddRange(m_lightList.rightEyeLightVolumes);
                }
                
                UpdateDataBuffers();

                m_maxShadowDistance = shadowSettings.maxShadowDistance;

                return m_enableBakeShadowMask;
            }
        }

        static float CalculateProbeLogVolume(Bounds bounds)
        {
            float boxVolume = 8 * bounds.extents.x * bounds.extents.y * bounds.extents.z;
            float logVolume = Mathf.Clamp(256 + Mathf.Log(boxVolume, 1.05f), 0, 4095); // Allow for negative exponents
            return logVolume;
        }

        static void UnpackProbeSortKey(uint sortKey, out LightVolumeType lightVolumeType, out int probeIndex, out int listType)
        {
            lightVolumeType = (LightVolumeType)((sortKey >> 17) & 0x3);
            probeIndex = (int)(sortKey & 0xFFFF);
            listType = (int)((sortKey >> 16) & 1);
        }

        static uint PackProbeKey(float logVolume, LightVolumeType lightVolumeType, uint listType, int probeIndex)
        {
            // 12 bit volume, 3 bit LightVolumeType, 1 bit list type, 16 bit index
            return (uint)logVolume << 20 | (uint)lightVolumeType << 17 | listType << 16 | ((uint)probeIndex & 0xFFFF);
        }

        void VoxelLightListGeneration(CommandBuffer cmd, HDCamera hdCamera, Matrix4x4 projscr, Matrix4x4 invProjscr, RenderTargetIdentifier cameraDepthBufferRT)
        {
            Camera camera = hdCamera.camera;
            // clear atomic offset index
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, s_GlobalLightListAtomic);
            cmd.DispatchCompute(buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, 1, 1, 1);

            bool isOrthographic = camera.orthographic;
            cmd.SetComputeIntParam(buildPerVoxelLightListShader, HDShaderIDs.g_isOrthographic, isOrthographic ? 1 : 0);
            cmd.SetComputeIntParam(buildPerVoxelLightListShader, HDShaderIDs._EnvLightIndexShift, m_lightList.lights.Count);
            cmd.SetComputeIntParam(buildPerVoxelLightListShader, HDShaderIDs._DecalIndexShift, m_lightList.lights.Count + m_lightList.envLights.Count);
            cmd.SetComputeIntParam(buildPerVoxelLightListShader, HDShaderIDs.g_iNrVisibLights, m_lightCount);
            cmd.SetComputeMatrixParam(buildPerVoxelLightListShader, HDShaderIDs.g_mScrProjection, projscr);
            cmd.SetComputeMatrixParam(buildPerVoxelLightListShader, HDShaderIDs.g_mInvScrProjection, invProjscr);

            cmd.SetComputeIntParam(buildPerVoxelLightListShader, HDShaderIDs.g_iLog2NumClusters, k_Log2NumClusters);

            cmd.SetComputeVectorParam(buildPerVoxelLightListShader, HDShaderIDs.g_screenSize, hdCamera.screenSize);
            cmd.SetComputeIntParam(buildPerVoxelLightListShader, HDShaderIDs.g_iNumSamplesMSAA, (int)hdCamera.msaaSamples);

            //Vector4 v2_near = invProjscr * new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            //Vector4 v2_far = invProjscr * new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
            //float nearPlane2 = -(v2_near.z/v2_near.w);
            //float farPlane2 = -(v2_far.z/v2_far.w);
            var nearPlane = camera.nearClipPlane;
            var farPlane = camera.farClipPlane;
            cmd.SetComputeFloatParam(buildPerVoxelLightListShader, HDShaderIDs.g_fNearPlane, nearPlane);
            cmd.SetComputeFloatParam(buildPerVoxelLightListShader, HDShaderIDs.g_fFarPlane, farPlane);

            const float C = (float)(1 << k_Log2NumClusters);
            var geomSeries = (1.0 - Mathf.Pow(k_ClustLogBase, C)) / (1 - k_ClustLogBase);        // geometric series: sum_k=0^{C-1} base^k
            m_ClustScale = (float)(geomSeries / (farPlane - nearPlane));

            cmd.SetComputeFloatParam(buildPerVoxelLightListShader, HDShaderIDs.g_fClustScale, m_ClustScale);
            cmd.SetComputeFloatParam(buildPerVoxelLightListShader, HDShaderIDs.g_fClustBase, k_ClustLogBase);

            cmd.SetComputeTextureParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, HDShaderIDs.g_depth_tex, cameraDepthBufferRT);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, HDShaderIDs.g_vLayeredLightList, s_PerVoxelLightLists);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, HDShaderIDs.g_LayeredOffset, s_PerVoxelOffset);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, HDShaderIDs.g_LayeredSingleIdxBuffer, s_GlobalLightListAtomic);
            if (m_FrameSettings.lightLoopSettings.enableBigTilePrepass)
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, HDShaderIDs.g_vBigTileLightList, s_BigTileLightList);

            if (k_UseDepthBuffer)
            {
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, HDShaderIDs.g_logBaseBuffer, s_PerTileLogBaseTweak);
            }

            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, HDShaderIDs.g_vBoundsBuffer, s_AABBBoundsBuffer);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, HDShaderIDs._LightVolumeData, s_LightVolumeDataBuffer);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, HDShaderIDs.g_data, s_ConvexBoundsBuffer);

            var numTilesX = GetNumTileClusteredX(hdCamera);
            var numTilesY = GetNumTileClusteredY(hdCamera);
            cmd.DispatchCompute(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, numTilesX, numTilesY, 1);
        }

        public void BuildGPULightListsCommon(HDCamera hdCamera, CommandBuffer cmd, RenderTargetIdentifier cameraDepthBufferRT, RenderTargetIdentifier stencilTextureRT, bool skyEnabled)
        {
            var camera = hdCamera.camera;
            cmd.BeginSample("Build Light List");

            var w = (int)hdCamera.screenSize.x;
            var h = (int)hdCamera.screenSize.y;
            s_TempIntArray[0] = w;
            s_TempIntArray[1] = h;
            var numBigTilesX = (w + 63) / 64;
            var numBigTilesY = (h + 63) / 64;

            var temp = new Matrix4x4();
            temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
            temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
            temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
            temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            bool isOrthographic = camera.orthographic;

            // camera to screen matrix (and it's inverse)
            var projArr = new Matrix4x4[2];
            var projscrArr = new Matrix4x4[2];
            var invProjscrArr = new Matrix4x4[2];
            if (m_FrameSettings.enableStereo)
            {
                for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
                {
                    projArr[eyeIndex] = CameraProjectionStereoLHS(hdCamera.camera, (Camera.StereoscopicEye)eyeIndex);
                    projscrArr[eyeIndex] = temp * projArr[eyeIndex];
                    invProjscrArr[eyeIndex] = projscrArr[eyeIndex].inverse;
                }
            }
            else
            {
                projArr[0] = CameraProjectionNonObliqueLHS(hdCamera);
                projscrArr[0] = temp * projArr[0];
                invProjscrArr[0] = projscrArr[0].inverse;
            }


            // generate screen-space AABBs (used for both fptl and clustered).
            if (m_lightCount != 0)
            {
                temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                var projhArr = new Matrix4x4[2];
                var invProjhArr = new Matrix4x4[2];
                if (m_FrameSettings.enableStereo)
                {
                    for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
                    {
                        projhArr[eyeIndex] = temp * projArr[eyeIndex];
                        invProjhArr[eyeIndex] = projhArr[eyeIndex].inverse;
                    }
                }
                else
                {
                    projhArr[0] = temp * projArr[0];
                    invProjhArr[0] = projhArr[0].inverse;
                }

                cmd.SetComputeIntParam(buildScreenAABBShader, HDShaderIDs.g_isOrthographic, isOrthographic ? 1 : 0);

                // In the stereo case, we have two sets of light bounds to iterate over (bounds are in per-eye view space)
                cmd.SetComputeIntParam(buildScreenAABBShader, HDShaderIDs.g_iNrVisibLights, m_lightCount);
                cmd.SetComputeBufferParam(buildScreenAABBShader, s_GenAABBKernel, HDShaderIDs.g_data, s_ConvexBoundsBuffer);

                cmd.SetComputeMatrixArrayParam(buildScreenAABBShader, HDShaderIDs.g_mProjectionArr, projhArr);
                cmd.SetComputeMatrixArrayParam(buildScreenAABBShader, HDShaderIDs.g_mInvProjectionArr, invProjhArr);

                // In stereo, we output two sets of AABB bounds
                cmd.SetComputeBufferParam(buildScreenAABBShader, s_GenAABBKernel, HDShaderIDs.g_vBoundsBuffer, s_AABBBoundsBuffer);

                int tgY = m_FrameSettings.enableStereo ? 2 : 1;
                cmd.DispatchCompute(buildScreenAABBShader, s_GenAABBKernel, (m_lightCount + 7) / 8, tgY, 1);
            }

            // enable coarse 2D pass on 64x64 tiles (used for both fptl and clustered).
            if (m_FrameSettings.lightLoopSettings.enableBigTilePrepass)
            {
                cmd.SetComputeIntParam(buildPerBigTileLightListShader, HDShaderIDs.g_isOrthographic, isOrthographic ? 1 : 0);
                cmd.SetComputeIntParams(buildPerBigTileLightListShader, HDShaderIDs.g_viDimensions, s_TempIntArray);
                cmd.SetComputeIntParam(buildPerBigTileLightListShader, HDShaderIDs._EnvLightIndexShift, m_lightList.lights.Count);
                cmd.SetComputeIntParam(buildPerBigTileLightListShader, HDShaderIDs._DecalIndexShift, m_lightList.lights.Count + m_lightList.envLights.Count);
                cmd.SetComputeIntParam(buildPerBigTileLightListShader, HDShaderIDs.g_iNrVisibLights, m_lightCount);

                cmd.SetComputeMatrixParam(buildPerBigTileLightListShader, HDShaderIDs.g_mScrProjection, projscrArr[0]);
                cmd.SetComputeMatrixParam(buildPerBigTileLightListShader, HDShaderIDs.g_mInvScrProjection, invProjscrArr[0]);

                cmd.SetComputeFloatParam(buildPerBigTileLightListShader, HDShaderIDs.g_fNearPlane, camera.nearClipPlane);
                cmd.SetComputeFloatParam(buildPerBigTileLightListShader, HDShaderIDs.g_fFarPlane, camera.farClipPlane);
                cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, HDShaderIDs.g_vLightList, s_BigTileLightList);
                cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, HDShaderIDs.g_vBoundsBuffer, s_AABBBoundsBuffer);
                cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, HDShaderIDs._LightVolumeData, s_LightVolumeDataBuffer);
                cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, HDShaderIDs.g_data, s_ConvexBoundsBuffer);
                cmd.DispatchCompute(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, numBigTilesX, numBigTilesY, 1);
            }

            var numTilesX = GetNumTileFtplX(hdCamera);
            var numTilesY = GetNumTileFtplY(hdCamera);
            var numTiles = numTilesX * numTilesY;
            bool enableFeatureVariants = GetFeatureVariantsEnabled();

            // optimized for opaques only
            if (m_FrameSettings.lightLoopSettings.isFptlEnabled)
            {
                cmd.SetComputeIntParam(buildPerTileLightListShader, HDShaderIDs.g_isOrthographic, isOrthographic ? 1 : 0);
                cmd.SetComputeIntParams(buildPerTileLightListShader, HDShaderIDs.g_viDimensions, s_TempIntArray);
                cmd.SetComputeIntParam(buildPerTileLightListShader, HDShaderIDs._EnvLightIndexShift, m_lightList.lights.Count);
                cmd.SetComputeIntParam(buildPerTileLightListShader, HDShaderIDs._DecalIndexShift, m_lightList.lights.Count + m_lightList.envLights.Count);
                cmd.SetComputeIntParam(buildPerTileLightListShader, HDShaderIDs.g_iNrVisibLights, m_lightCount);

                cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, HDShaderIDs.g_vBoundsBuffer, s_AABBBoundsBuffer);
                cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, HDShaderIDs._LightVolumeData, s_LightVolumeDataBuffer);
                cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, HDShaderIDs.g_data, s_ConvexBoundsBuffer);

                cmd.SetComputeMatrixParam(buildPerTileLightListShader, HDShaderIDs.g_mScrProjection, projscrArr[0]);
                cmd.SetComputeMatrixParam(buildPerTileLightListShader, HDShaderIDs.g_mInvScrProjection, invProjscrArr[0]);

                cmd.SetComputeTextureParam(buildPerTileLightListShader, s_GenListPerTileKernel, HDShaderIDs.g_depth_tex, cameraDepthBufferRT);
                cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, HDShaderIDs.g_vLightList, s_LightList);
                if (m_FrameSettings.lightLoopSettings.enableBigTilePrepass)
                    cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, HDShaderIDs.g_vBigTileLightList, s_BigTileLightList);

                if (enableFeatureVariants)
                {
                    uint baseFeatureFlags = 0;
                    if (m_lightList.directionalLights.Count > 0)
                    {
                        baseFeatureFlags |= (uint)LightFeatureFlags.Directional;
                    }
                    if (skyEnabled)
                    {
                        baseFeatureFlags |= (uint)LightFeatureFlags.Sky;
                    }
                    if (!m_FrameSettings.lightLoopSettings.enableComputeMaterialVariants)
                    {
                        baseFeatureFlags |= LightDefinitions.s_MaterialFeatureMaskFlags;
                    }
                    cmd.SetComputeIntParam(buildPerTileLightListShader, HDShaderIDs.g_BaseFeatureFlags, (int)baseFeatureFlags);
                    cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, HDShaderIDs.g_TileFeatureFlags, s_TileFeatureFlags);
                }

                cmd.DispatchCompute(buildPerTileLightListShader, s_GenListPerTileKernel, numTilesX, numTilesY, 1);
            }

            // Cluster
            VoxelLightListGeneration(cmd, hdCamera, projscrArr[0], invProjscrArr[0], cameraDepthBufferRT);

            if (enableFeatureVariants)
            {
                // material classification
                if (m_FrameSettings.lightLoopSettings.enableComputeMaterialVariants)
                {
                    int buildMaterialFlagsKernel = s_BuildMaterialFlagsOrKernel;

                    uint baseFeatureFlags = 0;
                    if (!m_FrameSettings.lightLoopSettings.enableComputeLightVariants)
                    {
                        buildMaterialFlagsKernel = s_BuildMaterialFlagsWriteKernel;
                        baseFeatureFlags |= LightDefinitions.s_LightFeatureMaskFlags;
                    }

                    cmd.SetComputeIntParam(buildMaterialFlagsShader, HDShaderIDs.g_BaseFeatureFlags, (int)baseFeatureFlags);
                    cmd.SetComputeIntParams(buildMaterialFlagsShader, HDShaderIDs.g_viDimensions, s_TempIntArray);
                    cmd.SetComputeBufferParam(buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs.g_TileFeatureFlags, s_TileFeatureFlags);

                    cmd.SetComputeTextureParam(buildMaterialFlagsShader, buildMaterialFlagsKernel, HDShaderIDs._StencilTexture, stencilTextureRT);

                    cmd.DispatchCompute(buildMaterialFlagsShader, buildMaterialFlagsKernel, numTilesX, numTilesY, 1);
                }

                // clear dispatch indirect buffer
                cmd.SetComputeBufferParam(clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, s_DispatchIndirectBuffer);
                cmd.DispatchCompute(clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, 1, 1, 1);

                // add tiles to indirect buffer
                cmd.SetComputeBufferParam(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, HDShaderIDs.g_DispatchIndirectBuffer, s_DispatchIndirectBuffer);
                cmd.SetComputeBufferParam(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, HDShaderIDs.g_TileList, s_TileList);
                cmd.SetComputeBufferParam(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, HDShaderIDs.g_TileFeatureFlags, s_TileFeatureFlags);
                cmd.SetComputeIntParam(buildDispatchIndirectShader, HDShaderIDs.g_NumTiles, numTiles);
                cmd.SetComputeIntParam(buildDispatchIndirectShader, HDShaderIDs.g_NumTilesX, numTilesX);
                cmd.DispatchCompute(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, (numTiles + 63) / 64, 1, 1);
            }

            cmd.EndSample("Build Light List");
        }

        public void BuildGPULightLists(HDCamera hdCamera, CommandBuffer cmd, RenderTargetIdentifier cameraDepthBufferRT, RenderTargetIdentifier stencilTextureRT, bool skyEnabled)
        {
            cmd.SetRenderTarget(BuiltinRenderTextureType.None);

            BuildGPULightListsCommon(hdCamera, cmd, cameraDepthBufferRT, stencilTextureRT, skyEnabled);
            PushGlobalParams(hdCamera, cmd);
        }

        public GPUFence BuildGPULightListsAsyncBegin(HDCamera hdCamera, ScriptableRenderContext renderContext, RenderTargetIdentifier cameraDepthBufferRT, RenderTargetIdentifier stencilTextureRT, GPUFence startFence, bool skyEnabled)
        {
            var cmd = CommandBufferPool.Get("Build light list");
            cmd.WaitOnGPUFence(startFence);

            BuildGPULightListsCommon(hdCamera, cmd, cameraDepthBufferRT, stencilTextureRT, skyEnabled);
            GPUFence completeFence = cmd.CreateGPUFence();
            renderContext.ExecuteCommandBufferAsync(cmd, ComputeQueueType.Background);
            CommandBufferPool.Release(cmd);

            return completeFence;
        }

        public void BuildGPULightListAsyncEnd(HDCamera hdCamera, CommandBuffer cmd, GPUFence doneFence)
        {
            cmd.WaitOnGPUFence(doneFence);
            PushGlobalParams(hdCamera, cmd);
        }

        void UpdateDataBuffers()
        {
            s_DirectionalLightDatas.SetData(m_lightList.directionalLights);
            s_LightDatas.SetData(m_lightList.lights);
            s_EnvLightDatas.SetData(m_lightList.envLights);
            s_shadowDatas.SetData(m_lightList.shadows);
            s_DecalDatas.SetData(DecalSystem.m_DecalDatas, 0, 0, Math.Min(DecalSystem.m_DecalDatasCount, k_MaxDecalsOnScreen)); // don't add more than the size of the buffer

            // These two buffers have been set in Rebuild()
            s_ConvexBoundsBuffer.SetData(m_lightList.bounds);
            s_LightVolumeDataBuffer.SetData(m_lightList.lightVolumes);
        }

        HDAdditionalReflectionData GetHDAdditionalReflectionData(VisibleReflectionProbe probe)
        {
            var add = probe.probe.GetComponent<HDAdditionalReflectionData>();
            if (add == null)
            {
                add = HDUtils.s_DefaultHDAdditionalReflectionData;
                add.blendDistancePositive = Vector3.one * probe.blendDistance;
                add.blendDistanceNegative = add.blendDistancePositive;
                add.influenceShape = ShapeType.Box;
            }
            return add;
        }

        HDAdditionalLightData GetHDAdditionalLightData(VisibleLight light)
        {
            var add = light.light.GetComponent<HDAdditionalLightData>();
            if (add == null)
            {
                add = HDUtils.s_DefaultHDAdditionalLightData;
            }
            return add;
        }

        void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "Push Global Parameters", CustomSamplerId.TPPushGlobalParameters.GetSampler()))
            {
                Camera camera = hdCamera.camera;
                // Shadows
                m_ShadowMgr.SyncData();
                m_ShadowMgr.BindResources(cmd, null, 0);

                cmd.SetGlobalTexture(HDShaderIDs._CookieTextures, m_CookieTexArray.GetTexCache());
                cmd.SetGlobalTexture(HDShaderIDs._CookieCubeTextures, m_CubeCookieTexArray.GetTexCache());
                cmd.SetGlobalTexture(HDShaderIDs._EnvCubemapTextures, m_ReflectionProbeCache.GetTexCache());
                cmd.SetGlobalTexture(HDShaderIDs._Env2DTextures, m_ReflectionPlanarProbeCache.GetTexCache());
                cmd.SetGlobalMatrixArray(HDShaderIDs._Env2DCaptureVP, m_Env2DCaptureVP);

                cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, s_DirectionalLightDatas);
                cmd.SetGlobalInt(HDShaderIDs._DirectionalLightCount, m_lightList.directionalLights.Count);
                cmd.SetGlobalBuffer(HDShaderIDs._LightDatas, s_LightDatas);
                cmd.SetGlobalInt(HDShaderIDs._PunctualLightCount, m_punctualLightCount);
                cmd.SetGlobalInt(HDShaderIDs._AreaLightCount, m_areaLightCount);
                cmd.SetGlobalBuffer(HDShaderIDs._EnvLightDatas, s_EnvLightDatas);
                cmd.SetGlobalInt(HDShaderIDs._EnvLightCount, m_lightList.envLights.Count);
                cmd.SetGlobalBuffer(HDShaderIDs._DecalDatas, s_DecalDatas);
                cmd.SetGlobalInt(HDShaderIDs._DecalCount, DecalSystem.m_DecalDatasCount);
                cmd.SetGlobalBuffer(HDShaderIDs._ShadowDatas, s_shadowDatas);

                cmd.SetGlobalInt(HDShaderIDs._NumTileFtplX, GetNumTileFtplX(hdCamera));
                cmd.SetGlobalInt(HDShaderIDs._NumTileFtplY, GetNumTileFtplY(hdCamera));

                cmd.SetGlobalInt(HDShaderIDs._NumTileClusteredX, GetNumTileClusteredX(hdCamera));
                cmd.SetGlobalInt(HDShaderIDs._NumTileClusteredY, GetNumTileClusteredY(hdCamera));

                if (m_FrameSettings.lightLoopSettings.enableBigTilePrepass)
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vBigTileLightList, s_BigTileLightList);

                // Cluster
                {
                    cmd.SetGlobalFloat(HDShaderIDs.g_fClustScale, m_ClustScale);
                    cmd.SetGlobalFloat(HDShaderIDs.g_fClustBase, k_ClustLogBase);
                    cmd.SetGlobalFloat(HDShaderIDs.g_fNearPlane, camera.nearClipPlane);
                    cmd.SetGlobalFloat(HDShaderIDs.g_fFarPlane, camera.farClipPlane);
                    cmd.SetGlobalInt(HDShaderIDs.g_iLog2NumClusters, k_Log2NumClusters);

                    cmd.SetGlobalInt(HDShaderIDs.g_isLogBaseBufferEnabled, k_UseDepthBuffer ? 1 : 0);

                    cmd.SetGlobalBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, s_PerVoxelOffset);
                    if (k_UseDepthBuffer)
                    {
                        cmd.SetGlobalBuffer(HDShaderIDs.g_logBaseBuffer, s_PerTileLogBaseTweak);
                    }

                    // Set up clustered lighting for volumetrics.
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, s_PerVoxelLightLists);
                }
            }
        }

        public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults)
        {
            // kick off the shadow jobs here
            m_ShadowMgr.RenderShadows(m_FrameId, renderContext, cmd, cullResults, cullResults.visibleLights);
        }

        public struct LightingPassOptions
        {
            public bool outputSplitLighting;
        }

        public void RenderDeferredDirectionalShadow(HDCamera hdCamera, RTHandle deferredShadowRT, RenderTargetIdentifier depthTexture, CommandBuffer cmd)
        {
            if (m_CurrentSunLight == null || m_CurrentSunLight.GetComponent<AdditionalShadowData>() == null)
            {
                cmd.SetGlobalTexture(HDShaderIDs._DeferredShadowTexture, RuntimeUtilities.blackTexture);
                return;
            }

            using (new ProfilingSample(cmd, "Deferred Directional Shadow", CustomSamplerId.TPDeferredDirectionalShadow.GetSampler()))
            {
                ContactShadows contactShadows = VolumeManager.instance.stack.GetComponent<ContactShadows>();

                bool enableContactShadows = m_FrameSettings.enableContactShadows && contactShadows.enable && contactShadows.length > 0.0f;
                int kernel;
                if (enableContactShadows)
                    kernel = m_FrameSettings.enableForwardRenderingOnly ? s_deferredDirectionalShadow_Contact_Kernel : s_deferredDirectionalShadow_Contact_Normals_Kernel;
                else
                    kernel = m_FrameSettings.enableForwardRenderingOnly ? s_deferredDirectionalShadowKernel : s_deferredDirectionalShadow_Normals_Kernel;

                m_ShadowMgr.BindResources(cmd, deferredDirectionalShadowComputeShader, kernel);

                if (enableContactShadows)
                {
                    float contactShadowRange = Mathf.Clamp(contactShadows.fadeDistance, 0.0f, contactShadows.maxDistance);
                    float contactShadowFadeEnd = contactShadows.maxDistance;
                    float contactShadowOneOverFadeRange = 1.0f / (contactShadowRange);
                    Vector4 contactShadowParams = new Vector4(contactShadows.length, contactShadows.distanceScaleFactor, contactShadowFadeEnd, contactShadowOneOverFadeRange);
                    cmd.SetComputeVectorParam(deferredDirectionalShadowComputeShader, HDShaderIDs._DirectionalContactShadowParams, contactShadowParams);
                    cmd.SetComputeIntParam(deferredDirectionalShadowComputeShader, HDShaderIDs._DirectionalContactShadowSampleCount, contactShadows.sampleCount);
                }

                cmd.SetComputeIntParam(deferredDirectionalShadowComputeShader, HDShaderIDs._DirectionalShadowIndex, m_CurrentSunLightShadowIndex);
                cmd.SetComputeVectorParam(deferredDirectionalShadowComputeShader, HDShaderIDs._DirectionalLightDirection, -m_CurrentSunLight.transform.forward);
                cmd.SetComputeTextureParam(deferredDirectionalShadowComputeShader, kernel, HDShaderIDs._DeferredShadowTextureUAV, deferredShadowRT);
                cmd.SetComputeTextureParam(deferredDirectionalShadowComputeShader, kernel, HDShaderIDs._MainDepthTexture, depthTexture);

                int deferredShadowTileSize = 16; // Must match DeferreDirectionalShadow.compute
                int numTilesX = (hdCamera.actualWidth + (deferredShadowTileSize - 1)) / deferredShadowTileSize;
                int numTilesY = (hdCamera.actualHeight + (deferredShadowTileSize - 1)) / deferredShadowTileSize;

                hdCamera.SetupComputeShader(deferredDirectionalShadowComputeShader, cmd);

                // TODO: Update for stereo
                cmd.DispatchCompute(deferredDirectionalShadowComputeShader, kernel, numTilesX, numTilesY, 1);

                cmd.SetGlobalTexture(HDShaderIDs._DeferredShadowTexture, deferredShadowRT);
            }
        }

        public void RenderDeferredLighting( HDCamera hdCamera, CommandBuffer cmd, DebugDisplaySettings debugDisplaySettings,
                                            RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthStencilBuffer, RenderTargetIdentifier depthTexture,
                                            LightingPassOptions options)
        {
            cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, s_LightList);

            if (m_FrameSettings.lightLoopSettings.enableTileAndCluster && m_FrameSettings.lightLoopSettings.enableComputeLightEvaluation && options.outputSplitLighting)
            {
                // The CS is always in the MRT mode. Do not execute the same shader twice.
                return;
            }


            // Predeclared to reduce GC pressure
            string tilePassName = "TilePass - Deferred Lighting Pass";
            string tilePassMRTName = "TilePass - Deferred Lighting Pass MRT";
            string singlePassName = "SinglePass - Deferred Lighting Pass";
            string SinglePassMRTName = "SinglePass - Deferred Lighting Pass MRT";

            string sLabel = m_FrameSettings.lightLoopSettings.enableTileAndCluster ?
                (options.outputSplitLighting ? tilePassMRTName : tilePassName) :
                (options.outputSplitLighting ? SinglePassMRTName : singlePassName);

            using (new ProfilingSample(cmd, sLabel, CustomSamplerId.TPRenderDeferredLighting.GetSampler()))
            {
                var camera = hdCamera.camera;

                // Compute path
                if (m_FrameSettings.lightLoopSettings.enableTileAndCluster && m_FrameSettings.lightLoopSettings.enableComputeLightEvaluation)
                {
                    int w = camera.pixelWidth;
                    int h = camera.pixelHeight;
                    int numTilesX = (w + 15) / 16;
                    int numTilesY = (h + 15) / 16;
                    int numTiles = numTilesX * numTilesY;

                    bool enableFeatureVariants = GetFeatureVariantsEnabled() && !debugDisplaySettings.IsDebugDisplayEnabled();

                    int numVariants = 1;
                    if (enableFeatureVariants)
                        numVariants = LightDefinitions.s_NumFeatureVariants;


                    hdCamera.SetupComputeShader(deferredComputeShader, cmd);

                    for (int variant = 0; variant < numVariants; variant++)
                    {
                        int kernel;

                        if (enableFeatureVariants)
                        {
                            if (m_enableBakeShadowMask)
                                kernel = s_shadeOpaqueIndirectShadowMaskFptlKernels[variant];
                            else
                                kernel = s_shadeOpaqueIndirectFptlKernels[variant];
                        }
                        else
                        {
                            if (m_enableBakeShadowMask)
                            {
                                kernel = debugDisplaySettings.IsDebugDisplayEnabled() ? s_shadeOpaqueDirectShadowMaskFptlDebugDisplayKernel : s_shadeOpaqueDirectShadowMaskFptlKernel;
                            }
                            else
                            {
                                kernel = debugDisplaySettings.IsDebugDisplayEnabled() ? s_shadeOpaqueDirectFptlDebugDisplayKernel : s_shadeOpaqueDirectFptlKernel;
                            }
                        }

                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, HDShaderIDs._MainDepthTexture, depthTexture);

                        // TODO: Is it possible to setup this outside the loop ? Can figure out how, get this: Property (specularLightingUAV) at kernel index (21) is not set
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, HDShaderIDs.specularLightingUAV, colorBuffers[0]);
                        cmd.SetComputeTextureParam(deferredComputeShader, kernel, HDShaderIDs.diffuseLightingUAV,  colorBuffers[1]);

                        // always do deferred lighting in blocks of 16x16 (not same as tiled light size)

                        if (enableFeatureVariants)
                        {
                            cmd.SetComputeBufferParam(deferredComputeShader, kernel, HDShaderIDs.g_TileFeatureFlags, s_TileFeatureFlags);
                            cmd.SetComputeIntParam(deferredComputeShader, HDShaderIDs.g_TileListOffset, variant * numTiles);
                            cmd.SetComputeBufferParam(deferredComputeShader, kernel, HDShaderIDs.g_TileList, s_TileList);
                            cmd.DispatchCompute(deferredComputeShader, kernel, s_DispatchIndirectBuffer, (uint)variant * 3 * sizeof(uint));
                        }
                        else
                        {
                            cmd.DispatchCompute(deferredComputeShader, kernel, numTilesX, numTilesY, 1);
                        }
                    }
                }
                else // Pixel shader evaluation
                {
                    int index = GetDeferredLightingMaterialIndex(   options.outputSplitLighting ? 1 : 0,
                                                                            m_FrameSettings.lightLoopSettings.enableTileAndCluster ? 1 : 0,
                                                                            m_enableBakeShadowMask ? 1 : 0,
                                                                    debugDisplaySettings.IsDebugDisplayEnabled() ? 1 : 0);

                    Material currentLightingMaterial = m_deferredLightingMaterial[index];

                    if (options.outputSplitLighting)
                    {
                        CoreUtils.DrawFullScreen(cmd, currentLightingMaterial, colorBuffers, depthStencilBuffer);
                    }
                    else
                    {
                        // If SSS is disable, do lighting for both split lighting and no split lighting
                        // This is for debug purpose, so fine to use immediate material mode here to modify render state
                        if (!m_FrameSettings.enableSubsurfaceScattering)
                        {
                            currentLightingMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.NoLighting);
                            currentLightingMaterial.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.NotEqual);
                        }
                        else
                        {
                            currentLightingMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilLightingUsage.RegularLighting);
                            currentLightingMaterial.SetInt(HDShaderIDs._StencilCmp, (int)CompareFunction.Equal);
                        }

                        CoreUtils.DrawFullScreen(cmd, currentLightingMaterial, colorBuffers[0], depthStencilBuffer);
                    }
                }
            } // End profiling
        }

        public void RenderForward(Camera camera, CommandBuffer cmd, bool renderOpaque)
        {
            // Note: SHADOWS_SHADOWMASK keyword is enabled in HDRenderPipeline.cs ConfigureForShadowMask

            // Note: if we use render opaque with deferred tiling we need to render a opaque depth pass for these opaque objects
            if (!m_FrameSettings.lightLoopSettings.enableTileAndCluster)
            {
                using (new ProfilingSample(cmd, "Forward pass", CustomSamplerId.TPForwardPass.GetSampler()))
                {
                    cmd.EnableShaderKeyword("LIGHTLOOP_SINGLE_PASS");
                    cmd.DisableShaderKeyword("LIGHTLOOP_TILE_PASS");
                }
            }
            else
            {
                // Only opaques can use FPTL, transparent must use clustered!
                bool useFptl = renderOpaque && m_FrameSettings.lightLoopSettings.enableFptlForForwardOpaque;

                using (new ProfilingSample(cmd, useFptl ? "Forward Tiled pass" : "Forward Clustered pass", CustomSamplerId.TPForwardTiledClusterpass.GetSampler()))
                {
                    // say that we want to use tile of single loop
                    cmd.EnableShaderKeyword("LIGHTLOOP_TILE_PASS");
                    cmd.DisableShaderKeyword("LIGHTLOOP_SINGLE_PASS");
                    CoreUtils.SetKeyword(cmd, "USE_FPTL_LIGHTLIST", useFptl);
                    CoreUtils.SetKeyword(cmd, "USE_CLUSTERED_LIGHTLIST", !useFptl);
                    cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, useFptl ? s_LightList : s_PerVoxelLightLists);
                }
            }
        }

        public void RenderDebugOverlay(HDCamera hdCamera, CommandBuffer cmd, DebugDisplaySettings debugDisplaySettings, ref float x, ref float y, float overlaySize, float width)
        {
            LightingDebugSettings lightingDebug = debugDisplaySettings.lightingDebugSettings;

            if (lightingDebug.debugLightingMode == DebugLightingMode.EnvironmentProxyVolume)
                cmd.SetGlobalFloat(HDShaderIDs._DebugEnvironmentProxyDepthScale, lightingDebug.environmentProxyDepthScale);

            using (new ProfilingSample(cmd, "Tiled/cluster Lighting Debug", CustomSamplerId.TPTiledLightingDebug.GetSampler()))
            {
                if (lightingDebug.tileClusterDebug != LightLoop.TileClusterDebug.None)
                {

                    int w = hdCamera.actualWidth;
                    int h = hdCamera.actualHeight;
                    int numTilesX = (w + 15) / 16;
                    int numTilesY = (h + 15) / 16;
                    int numTiles = numTilesX * numTilesY;

                    // Debug tiles
                    if (lightingDebug.tileClusterDebug == LightLoop.TileClusterDebug.MaterialFeatureVariants)
                    {
                        if (GetFeatureVariantsEnabled())
                        {
                            // featureVariants
                            m_DebugViewTilesMaterial.SetInt(HDShaderIDs._NumTiles, numTiles);
                            m_DebugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                            m_DebugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                            m_DebugViewTilesMaterial.SetBuffer(HDShaderIDs.g_TileList, s_TileList);
                            m_DebugViewTilesMaterial.SetBuffer(HDShaderIDs.g_DispatchIndirectBuffer, s_DispatchIndirectBuffer);
                            m_DebugViewTilesMaterial.EnableKeyword("USE_FPTL_LIGHTLIST");
                            m_DebugViewTilesMaterial.DisableKeyword("USE_CLUSTERED_LIGHTLIST");
                            m_DebugViewTilesMaterial.DisableKeyword("SHOW_LIGHT_CATEGORIES");
                            m_DebugViewTilesMaterial.EnableKeyword("SHOW_FEATURE_VARIANTS");
                            cmd.DrawProcedural(Matrix4x4.identity, m_DebugViewTilesMaterial, 0, MeshTopology.Triangles, numTiles * 6);
                        }
                    }
                    else // tile or cluster
                    {
                        bool bUseClustered = lightingDebug.tileClusterDebug == LightLoop.TileClusterDebug.Cluster;

                        // lightCategories
                        m_DebugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                        m_DebugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                        m_DebugViewTilesMaterial.SetBuffer(HDShaderIDs.g_vLightListGlobal, bUseClustered ? s_PerVoxelLightLists : s_LightList);
                        m_DebugViewTilesMaterial.EnableKeyword(bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        m_DebugViewTilesMaterial.DisableKeyword(!bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        m_DebugViewTilesMaterial.EnableKeyword("SHOW_LIGHT_CATEGORIES");
                        m_DebugViewTilesMaterial.DisableKeyword("SHOW_FEATURE_VARIANTS");

                        CoreUtils.DrawFullScreen(cmd, m_DebugViewTilesMaterial, 0);
                    }
                }
            }

            using (new ProfilingSample(cmd, "Display Shadows", CustomSamplerId.TPDisplayShadows.GetSampler()))
            {
                if (lightingDebug.shadowDebugMode == ShadowMapDebugMode.VisualizeShadowMap)
                {
                    int index = (int)lightingDebug.shadowMapIndex;

#if UNITY_EDITOR
                    if(lightingDebug.shadowDebugUseSelection)
                    {
                        index = -1;
                        if (UnityEditor.Selection.activeObject is GameObject)
                        {
                            GameObject go = UnityEditor.Selection.activeObject as GameObject;
                            Light light = go.GetComponent<Light>();
                            if (light != null)
                            {
                                index = m_ShadowMgr.GetShadowRequestIndex(light);
                            }
                        }
                    }
#endif

                    if(index != -1)
                    {
                        uint faceCount = m_ShadowMgr.GetShadowRequestFaceCount((uint)index);
                        for (uint i = 0; i < faceCount; ++i)
                        {
                            m_ShadowMgr.DisplayShadow(cmd, m_DebugShadowMapMaterial, index, i, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue);
                            HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera.actualWidth);
                        }
                    }
                }
                else if (lightingDebug.shadowDebugMode == ShadowMapDebugMode.VisualizeAtlas)
                {
                    m_ShadowMgr.DisplayShadowMap(cmd, m_DebugShadowMapMaterial, lightingDebug.shadowAtlasIndex, 0, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue);
                    HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera.actualWidth);
                }
            }
        }
    }
}
