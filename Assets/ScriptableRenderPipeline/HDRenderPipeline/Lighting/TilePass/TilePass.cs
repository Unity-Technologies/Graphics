using UnityEngine.Rendering;
using System.Collections.Generic;
using System;

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

            switch (ald.archetype)
            {
                case LightArchetype.Punctual:
                    shadowType = ShadowRegistry.ShadowLightType(l);
                    break;
                // Area and projector not supported yet
            }

            return shadowType;
        }

        public ShadowSetup(ShadowInitParameters shadowInit, ShadowSettings shadowSettings, out IShadowManager shadowManager)
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

            var varianceInit = atlasInit;
            varianceInit.baseInit.shadowmapFormat = ShadowVariance.GetFormat( false, false, true );

            var varianceInit2 = varianceInit;
            varianceInit2.baseInit.shadowmapFormat = ShadowVariance.GetFormat( true, true, false );

            var varianceInit3 = varianceInit;
            varianceInit3.baseInit.shadowmapFormat = ShadowVariance.GetFormat( true, false, true );

            m_Shadowmaps = new ShadowmapBase[] { new ShadowVariance( ref varianceInit ), new ShadowVariance( ref varianceInit2 ), new ShadowVariance( ref varianceInit3 ), new ShadowAtlas( ref atlasInit ) };

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

                    if(computeShader)
                    {
                        // bind buffers
                        cb.SetComputeBufferParam(computeShader, computeKernel, "_ShadowDatasExp", s_ShadowDataBuffer);
                        cb.SetComputeBufferParam(computeShader, computeKernel, "_ShadowPayloads", s_ShadowPayloadBuffer);
                        // bind textures
                        cb.SetComputeTextureParam(computeShader, computeKernel, "_ShadowmapExp_VSM_0", tex[0]);
                        cb.SetComputeTextureParam(computeShader, computeKernel, "_ShadowmapExp_VSM_1", tex[1]);
                        cb.SetComputeTextureParam(computeShader, computeKernel, "_ShadowmapExp_VSM_2", tex[2]);
                        cb.SetComputeTextureParam(computeShader, computeKernel, "_ShadowmapExp_PCF", tex[3]);
                    }
                    else
                    {
                        // bind buffers
                        cb.SetGlobalBuffer("_ShadowDatasExp", s_ShadowDataBuffer);
                        cb.SetGlobalBuffer("_ShadowPayloads", s_ShadowPayloadBuffer);
                        // bind textures
                        cb.SetGlobalTexture("_ShadowmapExp_VSM_0", tex[0]);
                        cb.SetGlobalTexture("_ShadowmapExp_VSM_1", tex[1]);
                        cb.SetGlobalTexture("_ShadowmapExp_VSM_2", tex[2]);
                        cb.SetGlobalTexture("_ShadowmapExp_PCF", tex[3]);
                    }

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

            m_ShadowMgr = new ShadowManager(shadowSettings, ref scInit, m_Shadowmaps);
            // set global overrides - these need to match the override specified in ShadowDispatch.hlsl
            bool useGlobalOverrides = true;
            m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Point        , ShadowAlgorithm.PCF, ShadowVariant.V4, ShadowPrecision.High, useGlobalOverrides );
            m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Spot         , ShadowAlgorithm.PCF, ShadowVariant.V4, ShadowPrecision.High, useGlobalOverrides );
            m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Directional  , ShadowAlgorithm.PCF, ShadowVariant.V4, ShadowPrecision.High, useGlobalOverrides );

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

    namespace TilePass
    {
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
            Count
        }

        [GenerateHLSL]
        public enum LightFeatureFlags
        {
            // Light bit mask must match LightDefinitions.s_LightFeatureMaskFlags value
            Punctual    = 1 << 8,
            Area        = 1 << 9,
            Directional = 1 << 10,
            Env         = 1 << 11,
            Sky         = 1 << 12  // If adding more light be sure to not overflow LightDefinitions.s_LightFeatureMaskFlags
        }

        [GenerateHLSL]
        public class LightDefinitions
        {
            public static int s_MaxNrLightsPerCamera = 1024;
            public static int s_MaxNrBigTileLightsPlusOne = 512;      // may be overkill but the footprint is 2 bits per pixel using uint16.
            public static float s_ViewportScaleZ = 1.0f;

            // enable unity's original left-hand shader camera space (right-hand internally in unity).
            public static int s_UseLeftHandCameraSpace = 1;

            public static int s_TileSizeFptl = 16;
            public static int s_TileSizeClustered = 32;

            // feature variants
            public static int s_NumFeatureVariants = 31;

            // Following define the maximum number of bits use in each feature category.
            public static uint s_LightFeatureMaskFlags = 0xFF00;
            public static uint s_MaterialFeatureMaskFlags = 0x00FF;   // don't use all bits just to be safe from signed and/or float conversions :/
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

        [Serializable]
        public class TileSettings
        {
            public bool enableTileAndCluster; // For debug / test
            public bool enableComputeLightEvaluation;
            public bool enableComputeLightVariants;
            public bool enableComputeMaterialVariants;

            // clustered light list specific buffers and data begin
            public bool enableClustered;
            public bool enableFptlForOpaqueWhenClustered; // still useful on opaques. Should be true by default to force tile on opaque.
            public bool enableBigTilePrepass;

            [Range(0.0f, 1.0f)]
            public float diffuseGlobalDimmer = 1.0f;
            [Range(0.0f, 1.0f)]
            public float specularGlobalDimmer = 1.0f;

            public enum TileDebug : int
            {
                None = 0, Punctual = 1, Area = 2, AreaAndPunctual = 3, Environment = 4, EnvironmentAndPunctual = 5, EnvironmentAndArea = 6, EnvironmentAndAreaAndPunctual = 7,
                FeatureVariants = 8
            }; //TODO: we should probably make this checkboxes

            public TileSettings()
            {
                enableTileAndCluster = true;
                enableComputeLightEvaluation = true;
                enableComputeLightVariants = true;
                enableComputeMaterialVariants = true;

                enableClustered = true;
                enableFptlForOpaqueWhenClustered = true;
                enableBigTilePrepass = true;

                diffuseGlobalDimmer = 1.0f;
                specularGlobalDimmer = 1.0f;
            }
        }

        public class LightLoop
        {
            public const int k_MaxDirectionalLightsOnScreen = 4;
            public const int k_MaxPunctualLightsOnScreen    = 512;
            public const int k_MaxAreaLightsOnScreen        = 64;
            public const int k_MaxLightsOnScreen = k_MaxDirectionalLightsOnScreen + k_MaxPunctualLightsOnScreen + k_MaxAreaLightsOnScreen;
            public const int k_MaxEnvLightsOnScreen = 64;
            public const int k_MaxShadowOnScreen = 16;
            public const int k_MaxCascadeCount = 4; //Should be not less than m_Settings.directionalLightCascadeCount;

            // Static keyword is required here else we get a "DestroyBuffer can only be call in main thread"
            static ComputeBuffer s_DirectionalLightDatas = null;
            static ComputeBuffer s_LightDatas = null;
            static ComputeBuffer s_EnvLightDatas = null;
            static ComputeBuffer s_shadowDatas = null;

            static Texture2DArray m_DefaultTexture2DArray;

            TextureCacheCubemap m_CubeReflTexArray;
            TextureCache2D m_CookieTexArray;
            TextureCacheCubemap m_CubeCookieTexArray;

            public class LightList
            {
                public List<DirectionalLightData> directionalLights;
                public List<LightData> lights;
                public List<EnvLightData> envLights;
                public List<ShadowData> shadows;
                public Vector4[] directionalShadowSplitSphereSqr;

                public List<SFiniteLightBound> bounds;
                public List<LightVolumeData> lightVolumes;

                public void Clear()
                {
                    directionalLights.Clear();
                    lights.Clear();
                    envLights.Clear();
                    shadows.Clear();

                    bounds.Clear();
                    lightVolumes.Clear();
                }

                public void Allocate()
                {
                    directionalLights = new List<DirectionalLightData>();
                    lights = new List<LightData>();
                    envLights = new List<EnvLightData>();
                    shadows = new List<ShadowData>();
                    directionalShadowSplitSphereSqr = new Vector4[k_MaxCascadeCount];

                    bounds = new List<SFiniteLightBound>();
                    lightVolumes = new List<LightVolumeData>();
                }
            }

            LightList m_lightList;
            int m_punctualLightCount = 0;
            int m_areaLightCount = 0;
            int m_lightCount = 0;

            private ComputeShader buildScreenAABBShader { get { return m_Resources.buildScreenAABBShader; } }
            private ComputeShader buildPerTileLightListShader { get { return m_Resources.buildPerTileLightListShader; } }
            private ComputeShader buildPerBigTileLightListShader { get { return m_Resources.buildPerBigTileLightListShader; } }
            private ComputeShader buildPerVoxelLightListShader { get { return m_Resources.buildPerVoxelLightListShader; } }

            private ComputeShader buildMaterialFlagsShader { get { return m_Resources.buildMaterialFlagsShader; } }
            private ComputeShader buildDispatchIndirectShader { get { return m_Resources.buildDispatchIndirectShader; } }
            private ComputeShader clearDispatchIndirectShader { get { return m_Resources.clearDispatchIndirectShader; } }
            private ComputeShader deferredComputeShader { get { return m_Resources.deferredComputeShader; } }

            static int s_GenAABBKernel;
            static int s_GenListPerTileKernel;
            static int s_GenListPerVoxelKernel;
            static int s_ClearVoxelAtomicKernel;
            static int s_ClearDispatchIndirectKernel;
            static int s_BuildDispatchIndirectKernel;
            static int s_BuildMaterialFlagsWriteKernel;
            static int s_BuildMaterialFlagsOrKernel;
            static int s_shadeOpaqueDirectClusteredKernel;
            static int s_shadeOpaqueDirectFptlKernel;
            static int s_shadeOpaqueDirectClusteredDebugDisplayKernel;
            static int s_shadeOpaqueDirectFptlDebugDisplayKernel;
            static int[] s_shadeOpaqueIndirectClusteredKernels = new int[LightDefinitions.s_NumFeatureVariants];
            static int[] s_shadeOpaqueIndirectFptlKernels = new int[LightDefinitions.s_NumFeatureVariants];

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
            const bool k_UseAsyncCompute = true;        // should not use on mobile

            const int k_Log2NumClusters = 6;     // accepted range is from 0 to 6. NumClusters is 1<<g_iLog2NumClusters
            const float k_ClustLogBase = 1.02f;     // each slice 2% bigger than the previous
            float m_ClustScale;
            static ComputeBuffer s_PerVoxelLightLists = null;
            static ComputeBuffer s_PerVoxelOffset = null;
            static ComputeBuffer s_PerTileLogBaseTweak = null;
            static ComputeBuffer s_GlobalLightListAtomic = null;
            // clustered light list specific buffers and data end

            private static GameObject s_DefaultAdditionalLightDataGameObject;
            private static HDAdditionalLightData s_DefaultAdditionalLightData;

            bool usingFptl
            {
                get
                {
                    bool isEnabledMSAA = false;
                    Debug.Assert(!isEnabledMSAA || m_TileSettings.enableClustered);
                    bool disableFptl = (!m_TileSettings.enableFptlForOpaqueWhenClustered && m_TileSettings.enableClustered) || isEnabledMSAA;
                    return !disableFptl;
                }
            }

            private static HDAdditionalLightData DefaultAdditionalLightData
            {
                get
                {
                    if (s_DefaultAdditionalLightDataGameObject == null)
                    {
                        s_DefaultAdditionalLightDataGameObject = new GameObject("Default Light Data");
                        s_DefaultAdditionalLightDataGameObject.hideFlags = HideFlags.HideAndDontSave;
                        s_DefaultAdditionalLightData = s_DefaultAdditionalLightDataGameObject.AddComponent<HDAdditionalLightData>();
                        s_DefaultAdditionalLightDataGameObject.SetActive(false);
                    }
                    return s_DefaultAdditionalLightData;
                }
            }

            Material m_DeferredAllMaterialSRT      = null;
            Material m_DeferredAllMaterialMRT      = null;

            Material m_DebugViewTilesMaterial      = null;

            Material m_SingleDeferredMaterialSRT   = null;
            Material m_SingleDeferredMaterialMRT   = null;

            Light m_CurrentSunLight = null;
            public Light GetCurrentSunLight() { return m_CurrentSunLight; }

            // shadow related stuff
            FrameId                 m_FrameId = new FrameId();
            ShadowSetup             m_ShadowSetup; // doesn't actually have to reside here, it would be enough to pass the IShadowManager in from the outside
            IShadowManager          m_ShadowMgr;
            List<int>               m_ShadowRequests = new List<int>();
            Dictionary<int, int>    m_ShadowIndices = new Dictionary<int, int>();

            void InitShadowSystem(ShadowInitParameters initParam, ShadowSettings shadowSettings)
            {
                m_ShadowSetup = new ShadowSetup(initParam, shadowSettings, out m_ShadowMgr);
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


            int GetNumTileFtplX(Camera camera)
            {
                return (camera.pixelWidth + (LightDefinitions.s_TileSizeFptl - 1)) / LightDefinitions.s_TileSizeFptl;
            }

            int GetNumTileFtplY(Camera camera)
            {
                return (camera.pixelHeight + (LightDefinitions.s_TileSizeFptl - 1)) / LightDefinitions.s_TileSizeFptl;
            }

            int GetNumTileClusteredX(Camera camera)
            {
                return (camera.pixelWidth + (LightDefinitions.s_TileSizeClustered - 1)) / LightDefinitions.s_TileSizeClustered;
            }

            int GetNumTileClusteredY(Camera camera)
            {
                return (camera.pixelHeight + (LightDefinitions.s_TileSizeClustered - 1)) / LightDefinitions.s_TileSizeClustered;
            }

            bool GetFeatureVariantsEnabled()
            {
                return m_TileSettings.enableComputeLightEvaluation && (m_TileSettings.enableComputeLightVariants || m_TileSettings.enableComputeMaterialVariants) && !(m_TileSettings.enableClustered && !m_TileSettings.enableFptlForOpaqueWhenClustered);
            }

            TileSettings m_TileSettings = null;
            RenderPipelineResources m_Resources = null;

            public LightLoop()
            {}

            public void Build(RenderPipelineResources renderPipelineResources, TileSettings tileSettings, TextureSettings textureSettings, ShadowInitParameters shadowInit, ShadowSettings shadowSettings)
            {
                m_Resources = renderPipelineResources;
                m_TileSettings = tileSettings;

                m_lightList = new LightList();
                m_lightList.Allocate();

                s_DirectionalLightDatas = new ComputeBuffer(k_MaxDirectionalLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLightData)));
                s_LightDatas = new ComputeBuffer(k_MaxPunctualLightsOnScreen + k_MaxAreaLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightData)));
                s_EnvLightDatas = new ComputeBuffer(k_MaxEnvLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(EnvLightData)));
                s_shadowDatas = new ComputeBuffer(k_MaxCascadeCount + k_MaxShadowOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShadowData)));

                m_CookieTexArray = new TextureCache2D();
                m_CookieTexArray.AllocTextureArray(8, textureSettings.spotCookieSize, textureSettings.spotCookieSize, TextureFormat.RGBA32, true);
                m_CubeCookieTexArray = new TextureCacheCubemap();
                m_CubeCookieTexArray.AllocTextureArray(4, textureSettings.pointCookieSize, TextureFormat.RGBA32, true);
                m_CubeReflTexArray = new TextureCacheCubemap();
                m_CubeReflTexArray.AllocTextureArray(32, textureSettings.reflectionCubemapSize, TextureCache.GetPreferredHdrCompressedTextureFormat, true);

                s_GenAABBKernel = buildScreenAABBShader.FindKernel("ScreenBoundsAABB");

                bool enableFeatureVariants = GetFeatureVariantsEnabled();
                if (enableFeatureVariants)
                {
                    s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel(m_TileSettings.enableBigTilePrepass ? "TileLightListGen_SrcBigTile_FeatureFlags" : "TileLightListGen_FeatureFlags");
                }
                else
                {
                    s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel(m_TileSettings.enableBigTilePrepass ? "TileLightListGen_SrcBigTile" : "TileLightListGen");
                }
                s_AABBBoundsBuffer = new ComputeBuffer(2 * k_MaxLightsOnScreen, 3 * sizeof(float));
                s_ConvexBoundsBuffer = new ComputeBuffer(k_MaxLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightBound)));
                s_LightVolumeDataBuffer = new ComputeBuffer(k_MaxLightsOnScreen, System.Runtime.InteropServices.Marshal.SizeOf(typeof(LightVolumeData)));
                s_DispatchIndirectBuffer = new ComputeBuffer(LightDefinitions.s_NumFeatureVariants * 3, sizeof(uint), ComputeBufferType.IndirectArguments);

                if (m_TileSettings.enableClustered)
                {
                    var kernelName = m_TileSettings.enableBigTilePrepass ? (k_UseDepthBuffer ? "TileLightListGen_DepthRT_SrcBigTile" : "TileLightListGen_NoDepthRT_SrcBigTile") : (k_UseDepthBuffer ? "TileLightListGen_DepthRT" : "TileLightListGen_NoDepthRT");
                    s_GenListPerVoxelKernel = buildPerVoxelLightListShader.FindKernel(kernelName);
                    s_ClearVoxelAtomicKernel = buildPerVoxelLightListShader.FindKernel("ClearAtomic");
                    s_GlobalLightListAtomic = new ComputeBuffer(1, sizeof(uint));
                }

                if (m_TileSettings.enableBigTilePrepass)
                {
                    s_GenListPerBigTileKernel = buildPerBigTileLightListShader.FindKernel("BigTileLightListGen");
                }

                s_BuildDispatchIndirectKernel = buildDispatchIndirectShader.FindKernel("BuildDispatchIndirect");
                s_ClearDispatchIndirectKernel = clearDispatchIndirectShader.FindKernel("ClearDispatchIndirect");

                s_BuildMaterialFlagsOrKernel = buildMaterialFlagsShader.FindKernel("MaterialFlagsGen_Or");
                s_BuildMaterialFlagsWriteKernel = buildMaterialFlagsShader.FindKernel("MaterialFlagsGen_Write");

                s_shadeOpaqueDirectClusteredKernel = deferredComputeShader.FindKernel("Deferred_Direct_Clustered");
                s_shadeOpaqueDirectFptlKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl");
                s_shadeOpaqueDirectClusteredDebugDisplayKernel = deferredComputeShader.FindKernel("Deferred_Direct_Clustered_DebugDisplay");
                s_shadeOpaqueDirectFptlDebugDisplayKernel = deferredComputeShader.FindKernel("Deferred_Direct_Fptl_DebugDisplay");

                for (int variant = 0; variant < LightDefinitions.s_NumFeatureVariants; variant++)
                {
                    s_shadeOpaqueIndirectClusteredKernels[variant] = deferredComputeShader.FindKernel("Deferred_Indirect_Clustered_Variant" + variant);
                    s_shadeOpaqueIndirectFptlKernels[variant] = deferredComputeShader.FindKernel("Deferred_Indirect_Fptl_Variant" + variant);
                }

                s_LightList = null;
                s_TileList = null;
                s_TileFeatureFlags = null;

                m_DeferredAllMaterialSRT = Utilities.CreateEngineMaterial(m_Resources.deferredShader);
                m_DeferredAllMaterialSRT.EnableKeyword("LIGHTLOOP_TILE_PASS");
                m_DeferredAllMaterialSRT.DisableKeyword("OUTPUT_SPLIT_LIGHTING");
                m_DeferredAllMaterialSRT.SetInt("_StencilRef", (int)StencilLightingUsage.RegularLighting);
                m_DeferredAllMaterialSRT.SetInt("_StencilCmp", (int)CompareFunction.Equal);
                m_DeferredAllMaterialSRT.SetInt("_SrcBlend", (int)BlendMode.One);
                m_DeferredAllMaterialSRT.SetInt("_DstBlend", (int)BlendMode.Zero);

                m_DeferredAllMaterialMRT = Utilities.CreateEngineMaterial(m_Resources.deferredShader);
                m_DeferredAllMaterialMRT.EnableKeyword("LIGHTLOOP_TILE_PASS");
                m_DeferredAllMaterialMRT.EnableKeyword("OUTPUT_SPLIT_LIGHTING");
                m_DeferredAllMaterialMRT.SetInt("_StencilRef", (int)StencilLightingUsage.SplitLighting);
                m_DeferredAllMaterialMRT.SetInt("_StencilCmp", (int)CompareFunction.Equal);
                m_DeferredAllMaterialMRT.SetInt("_SrcBlend", (int)BlendMode.One);
                m_DeferredAllMaterialMRT.SetInt("_DstBlend", (int)BlendMode.Zero);

                m_SingleDeferredMaterialSRT = Utilities.CreateEngineMaterial(m_Resources.deferredShader);
                m_SingleDeferredMaterialSRT.EnableKeyword("LIGHTLOOP_SINGLE_PASS");
                m_SingleDeferredMaterialSRT.DisableKeyword("OUTPUT_SPLIT_LIGHTING");
                m_SingleDeferredMaterialSRT.SetInt("_StencilRef", (int)StencilLightingUsage.RegularLighting);
                m_SingleDeferredMaterialSRT.SetInt("_StencilCmp", (int)CompareFunction.Equal);
                m_SingleDeferredMaterialSRT.SetInt("_SrcBlend", (int)BlendMode.One);
                m_SingleDeferredMaterialSRT.SetInt("_DstBlend", (int)BlendMode.Zero);

                m_SingleDeferredMaterialMRT = Utilities.CreateEngineMaterial(m_Resources.deferredShader);
                m_SingleDeferredMaterialMRT.EnableKeyword("LIGHTLOOP_SINGLE_PASS");
                m_SingleDeferredMaterialMRT.EnableKeyword("OUTPUT_SPLIT_LIGHTING");
                m_SingleDeferredMaterialMRT.SetInt("_StencilRef", (int)StencilLightingUsage.SplitLighting);
                m_SingleDeferredMaterialMRT.SetInt("_StencilCmp", (int)CompareFunction.Equal);
                m_SingleDeferredMaterialMRT.SetInt("_SrcBlend", (int)BlendMode.One);
                m_SingleDeferredMaterialMRT.SetInt("_DstBlend", (int)BlendMode.Zero);

                m_DebugViewTilesMaterial = Utilities.CreateEngineMaterial(m_Resources.debugViewTilesShader);

                m_DefaultTexture2DArray = new Texture2DArray(1, 1, 1, TextureFormat.ARGB32, false);
                m_DefaultTexture2DArray.SetPixels32(new Color32[1] { new Color32(128, 128, 128, 128) }, 0);
                m_DefaultTexture2DArray.Apply();

#if UNITY_EDITOR
                UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;
                UnityEditor.SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif

                InitShadowSystem(shadowInit, shadowSettings);
            }

            public void Cleanup()
            {
                DeinitShadowSystem();

#if UNITY_EDITOR
                UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif

                Utilities.SafeRelease(s_DirectionalLightDatas);
                Utilities.SafeRelease(s_LightDatas);
                Utilities.SafeRelease(s_EnvLightDatas);
                Utilities.SafeRelease(s_shadowDatas);

                if (m_CubeReflTexArray != null)
                {
                    m_CubeReflTexArray.Release();
                    m_CubeReflTexArray = null;
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

                Utilities.SafeRelease(s_AABBBoundsBuffer);
                Utilities.SafeRelease(s_ConvexBoundsBuffer);
                Utilities.SafeRelease(s_LightVolumeDataBuffer);
                Utilities.SafeRelease(s_DispatchIndirectBuffer);

                // enableClustered
                Utilities.SafeRelease(s_GlobalLightListAtomic);

                Utilities.Destroy(m_DeferredAllMaterialSRT);
                Utilities.Destroy(m_DeferredAllMaterialMRT);

                Utilities.Destroy(m_DebugViewTilesMaterial);

                Utilities.Destroy(m_SingleDeferredMaterialSRT);
                Utilities.Destroy(m_SingleDeferredMaterialMRT);

                Utilities.Destroy(s_DefaultAdditionalLightDataGameObject);
                s_DefaultAdditionalLightDataGameObject = null;
                s_DefaultAdditionalLightData = null;
            }

            public void NewFrame()
            {
                m_CookieTexArray.NewFrame();
                m_CubeCookieTexArray.NewFrame();
                m_CubeReflTexArray.NewFrame();
            }

            public bool NeedResize()
            {
                return s_LightList == null || s_TileList == null || s_TileFeatureFlags == null ||
                    (s_BigTileLightList == null && m_TileSettings.enableBigTilePrepass) ||
                    (s_PerVoxelLightLists == null && m_TileSettings.enableClustered);
            }

            public void ReleaseResolutionDependentBuffers()
            {
                Utilities.SafeRelease(s_LightList);
                Utilities.SafeRelease(s_TileList);
                Utilities.SafeRelease(s_TileFeatureFlags);

                // enableClustered
                Utilities.SafeRelease(s_PerVoxelLightLists);
                Utilities.SafeRelease(s_PerVoxelOffset);
                Utilities.SafeRelease(s_PerTileLogBaseTweak);

                // enableBigTilePrepass
                Utilities.SafeRelease(s_BigTileLightList);
            }

            int NumLightIndicesPerClusteredTile()
            {
                return 8 * (1 << k_Log2NumClusters);       // total footprint for all layers of the tile (measured in light index entries)
            }

            public void AllocResolutionDependentBuffers(int width, int height)
            {
                var nrTilesX = (width + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
                var nrTilesY = (height + LightDefinitions.s_TileSizeFptl - 1) / LightDefinitions.s_TileSizeFptl;
                var nrTiles = nrTilesX * nrTilesY;
                const int capacityUShortsPerTile = 32;
                const int dwordsPerTile = (capacityUShortsPerTile + 1) >> 1;        // room for 31 lights and a nrLights value.

                s_LightList = new ComputeBuffer((int)LightCategory.Count * dwordsPerTile * nrTiles, sizeof(uint));       // enough list memory for a 4k x 4k display
                s_TileList = new ComputeBuffer((int)LightDefinitions.s_NumFeatureVariants * nrTiles, sizeof(uint));
                s_TileFeatureFlags = new ComputeBuffer(nrTilesX * nrTilesY, sizeof(uint));

                if (m_TileSettings.enableClustered)
                {
                    var nrClustersX = (width + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                    var nrClustersY = (height + LightDefinitions.s_TileSizeClustered - 1) / LightDefinitions.s_TileSizeClustered;
                    var nrClusterTiles = nrClustersX * nrClustersY;

                    s_PerVoxelOffset = new ComputeBuffer((int)LightCategory.Count * (1 << k_Log2NumClusters) * nrClusterTiles, sizeof(uint));
                    s_PerVoxelLightLists = new ComputeBuffer(NumLightIndicesPerClusteredTile() * nrClusterTiles, sizeof(uint));

                    if (k_UseDepthBuffer)
                    {
                        s_PerTileLogBaseTweak = new ComputeBuffer(nrClusterTiles, sizeof(float));
                    }
                }

                if (m_TileSettings.enableBigTilePrepass)
                {
                    var nrBigTilesX = (width + 63) / 64;
                    var nrBigTilesY = (height + 63) / 64;
                    var nrBigTiles = nrBigTilesX * nrBigTilesY;
                    s_BigTileLightList = new ComputeBuffer(LightDefinitions.s_MaxNrBigTileLightsPlusOne * nrBigTiles, sizeof(uint));
                }
            }

            static Matrix4x4 GetFlipMatrix()
            {
                Matrix4x4 flip = Matrix4x4.identity;
                bool isLeftHand = ((int)LightDefinitions.s_UseLeftHandCameraSpace) != 0;
                if (isLeftHand) flip.SetColumn(2, new Vector4(0.0f, 0.0f, -1.0f, 0.0f));
                return flip;
            }

            static Matrix4x4 WorldToCamera(Camera camera)
            {
                return GetFlipMatrix() * camera.worldToCameraMatrix;
            }

            static Matrix4x4 CameraProjection(Camera camera)
            {
                return camera.projectionMatrix * GetFlipMatrix();
            }

            public Vector3 GetLightColor(VisibleLight light)
            {
                return new Vector3(light.finalColor.r, light.finalColor.g, light.finalColor.b);
            }

            public bool GetDirectionalLightData(ShadowSettings shadowSettings, GPULightType gpuLightType, VisibleLight light, HDAdditionalLightData additionalData, AdditionalShadowData additionalShadowData, int lightIndex)
            {
                var directionalLightData = new DirectionalLightData();

                float diffuseDimmer = m_TileSettings.diffuseGlobalDimmer * additionalData.lightDimmer;
                float specularDimmer = m_TileSettings.specularGlobalDimmer * additionalData.lightDimmer;
                if (diffuseDimmer  <= 0.0f && specularDimmer <= 0.0f)
                    return false;

                // Light direction for directional is opposite to the forward direction
                directionalLightData.forward = light.light.transform.forward;
                // Rescale for cookies and windowing.
                directionalLightData.up         = light.light.transform.up    * 2 / additionalData.lightWidth;
                directionalLightData.right      = light.light.transform.right * 2 / additionalData.lightLength;
                directionalLightData.positionWS = light.light.transform.position;
                directionalLightData.color = GetLightColor(light);
                directionalLightData.diffuseScale = additionalData.affectDiffuse ? diffuseDimmer : 0.0f;
                directionalLightData.specularScale = additionalData.affectSpecular ? specularDimmer : 0.0f;
                directionalLightData.shadowIndex = directionalLightData.cookieIndex = -1;

                if (light.light.cookie != null)
                {
                    directionalLightData.tileCookie = light.light.cookie.wrapMode == TextureWrapMode.Repeat;
                    directionalLightData.cookieIndex = m_CookieTexArray.FetchSlice(light.light.cookie);
                }
                // fix up shadow information
                int shadowIdx;
                if (m_ShadowIndices.TryGetValue(lightIndex, out shadowIdx))
                {
                    directionalLightData.shadowIndex = shadowIdx;
                    m_CurrentSunLight = light.light;
                }
                m_CurrentSunLight = m_CurrentSunLight == null ? light.light : m_CurrentSunLight;

                m_lightList.directionalLights.Add(directionalLightData);

                return true;
            }

            float ComputeLinearDistanceFade(float distanceToCamera, float fadeDistance)
            {
                // Fade with distance calculation is just a linear fade from 90% of fade distance to fade distance. 90% arbitrarily chosen but should work well enough.
                float distanceFadeNear = 0.9f * fadeDistance;
                return 1.0f - Mathf.Clamp01((distanceToCamera - distanceFadeNear) / (fadeDistance - distanceFadeNear));
            }

            public bool GetLightData(ShadowSettings shadowSettings, Camera camera, GPULightType gpuLightType, VisibleLight light, HDAdditionalLightData additionalLightData, AdditionalShadowData additionalshadowData, int lightIndex)
            {
                var lightData = new LightData();

                lightData.lightType = gpuLightType;

                lightData.positionWS = light.light.transform.position;
                lightData.invSqrAttenuationRadius = 1.0f / (light.range * light.range);
                lightData.color = GetLightColor(light);

                lightData.forward = light.light.transform.forward; // Note: Light direction is oriented backward (-Z)
                lightData.up = light.light.transform.up;
                lightData.right = light.light.transform.right;

                lightData.size = new Vector2(additionalLightData.lightLength, additionalLightData.lightWidth);

                if (lightData.lightType == GPULightType.ProjectorBox || lightData.lightType == GPULightType.ProjectorPyramid)
                {
                    // Rescale for cookies and windowing.
                    lightData.right *= 2 / additionalLightData.lightLength;
                    lightData.up    *= 2 / additionalLightData.lightWidth;
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

                float distanceToCamera = (lightData.positionWS - camera.transform.position).magnitude;
                float distanceFade = ComputeLinearDistanceFade(distanceToCamera, additionalLightData.fadeDistance);
                float lightScale = additionalLightData.lightDimmer * distanceFade;

                lightData.diffuseScale = additionalLightData.affectDiffuse ? lightScale * m_TileSettings.diffuseGlobalDimmer : 0.0f;
                lightData.specularScale = additionalLightData.affectSpecular ? lightScale * m_TileSettings.specularGlobalDimmer : 0.0f;

                if (lightData.diffuseScale <= 0.0f && lightData.specularScale <= 0.0f)
                    return false;

                lightData.IESIndex = -1;
                lightData.cookieIndex = -1;
                lightData.shadowIndex = -1;

                if (light.light.cookie != null)
                {
                    // TODO: add texture atlas support for cookie textures.
                    switch (light.lightType)
                    {
                        case LightType.Spot:
                            lightData.cookieIndex = m_CookieTexArray.FetchSlice(light.light.cookie);
                            break;
                        case LightType.Point:
                            lightData.cookieIndex = m_CubeCookieTexArray.FetchSlice(light.light.cookie);
                            break;
                    }
                }
                else if (light.lightType == LightType.Spot && additionalLightData.spotLightShape != SpotLightShape.Cone)
                {
                    // Projectors lights must always have a cookie texture.
                    lightData.cookieIndex = m_CookieTexArray.FetchSlice(Texture2D.whiteTexture);
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

                m_lightList.lights.Add(lightData);

                return true;
            }

            // TODO: we should be able to do this calculation only with LightData without VisibleLight light, but for now pass both
            public void GetLightVolumeDataAndBound(LightCategory lightCategory, GPULightType gpuLightType, LightVolumeType lightVolumeType, VisibleLight light, LightData lightData, Matrix4x4 worldToView)
            {
                // Then Culling side
                var range = light.range;
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
                        Vector3 lightPosToProjWindowCorner = (0.5f * lightData.size.x) * vx + (0.5f * lightData.size.y) * vy + 1.0f * vz;
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
                    bool isNegDeterminant = Vector3.Dot(worldToView.GetColumn(0), Vector3.Cross(worldToView.GetColumn(1), worldToView.GetColumn(2))) < 0.0f; // 3x3 Determinant.

                    bound.center = positionVS;
                    bound.boxAxisX.Set(range, 0, 0);
                    bound.boxAxisY.Set(0, range, 0);
                    bound.boxAxisZ.Set(0, 0, isNegDeterminant ? (-range) : range);    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = range;

                    // represents a left hand coordinate system in world space since det(worldToView)<0
                    Vector3 vx = xAxisVS;
                    Vector3 vy = yAxisVS;
                    Vector3 vz = zAxisVS;

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
                    Vector3 dimensions = new Vector3(lightData.size.x + 2 * range, 2 * range, 2 * range); // Omni-directional
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
                    lightVolumeData.boxInnerDist = new Vector3(lightData.size.x, 0, 0);
                    lightVolumeData.boxInvRange.Set(1.0f / range, 1.0f / range, 1.0f / range);
                    lightVolumeData.featureFlags = (uint)LightFeatureFlags.Area;
                }
                else if (gpuLightType == GPULightType.Rectangle)
                {
                    Vector3 dimensions = new Vector3(lightData.size.x + 2 * range, lightData.size.y + 2 * range, range); // One-sided
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
                    Vector3 dimensions  = new Vector3(lightData.size.x, lightData.size.y, range);  // One-sided
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

                m_lightList.bounds.Add(bound);
                m_lightList.lightVolumes.Add(lightVolumeData);
            }

            public void GetEnvLightData(VisibleReflectionProbe probe)
            {
                var envLightData = new EnvLightData();

                // CAUTION: localToWorld is the transform for the widget of the reflection probe. i.e the world position of the point use to do the cubemap capture (mean it include the local offset)
                envLightData.positionWS = probe.localToWorld.GetColumn(3);

                envLightData.envShapeType = EnvShapeType.None;

                // TODO: Support sphere influence in UI
                if (probe.boxProjection != 0)
                {
                    envLightData.envShapeType = EnvShapeType.Box;
                }

                // remove scale from the matrix (Scale in this matrix is use to scale the widget)
                envLightData.right = probe.localToWorld.GetColumn(0);
                envLightData.right.Normalize();
                envLightData.up = probe.localToWorld.GetColumn(1);
                envLightData.up.Normalize();
                envLightData.forward = probe.localToWorld.GetColumn(2);
                envLightData.forward.Normalize();

                // Artists prefer to have blend distance inside the volume!
                // So we let the current UI but we assume blendDistance is an inside factor instead
                // Blend distance can't be larger than the max radius
                // probe.bounds.extents is BoxSize / 2
                float maxBlendDist = Mathf.Min(probe.bounds.extents.x, Mathf.Min(probe.bounds.extents.y, probe.bounds.extents.z));
                float blendDistance = Mathf.Min(maxBlendDist, probe.blendDistance);
                envLightData.innerDistance = probe.bounds.extents - new Vector3(blendDistance, blendDistance, blendDistance);

                envLightData.envIndex = m_CubeReflTexArray.FetchSlice(probe.texture);

                envLightData.offsetLS = probe.center; // center is misnamed, it is the offset (in local space) from center of the bounding box to the cubemap capture point
                envLightData.blendDistance = blendDistance;

                m_lightList.envLights.Add(envLightData);
            }

            public void GetEnvLightVolumeDataAndBound(VisibleReflectionProbe probe, LightVolumeType lightVolumeType, Matrix4x4 worldToView)
            {
                var bound = new SFiniteLightBound();
                var lightVolumeData = new LightVolumeData();

                var bnds = probe.bounds;
                var boxOffset = probe.center;                  // reflection volume offset relative to cube map capture point
                var blendDistance = probe.blendDistance;

                var mat = probe.localToWorld;

                Vector3 vx = mat.GetColumn(0);
                Vector3 vy = mat.GetColumn(1);
                Vector3 vz = mat.GetColumn(2);
                Vector3 vw = mat.GetColumn(3);
                vx.Normalize(); // Scale shouldn't affect the probe or its bounds
                vy.Normalize();
                vz.Normalize();

                // C is reflection volume center in world space (NOT same as cube map capture point)
                var e = bnds.extents;       // 0.5f * Vector3.Max(-boxSizes[p], boxSizes[p]);
                var C = vx * boxOffset.x + vy * boxOffset.y + vz * boxOffset.z + vw;

                var combinedExtent = e + new Vector3(blendDistance, blendDistance, blendDistance);

                // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                vx = worldToView.MultiplyVector(vx);
                vy = worldToView.MultiplyVector(vy);
                vz = worldToView.MultiplyVector(vz);

                var Cw = worldToView.MultiplyPoint(C);

                bound.center = Cw;
                bound.boxAxisX = combinedExtent.x * vx;
                bound.boxAxisY = combinedExtent.y * vy;
                bound.boxAxisZ = combinedExtent.z * vz;
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = combinedExtent.magnitude;


                lightVolumeData.lightCategory = (uint)LightCategory.Env;
                lightVolumeData.lightVolume = (uint)lightVolumeType;
                lightVolumeData.featureFlags = (uint)LightFeatureFlags.Env;

                lightVolumeData.lightPos = Cw;
                lightVolumeData.lightAxisX = vx;
                lightVolumeData.lightAxisY = vy;
                lightVolumeData.lightAxisZ = vz;
                var delta = combinedExtent - e;
                lightVolumeData.boxInnerDist = e;
                lightVolumeData.boxInvRange.Set(1.0f / delta.x, 1.0f / delta.y, 1.0f / delta.z);

                m_lightList.bounds.Add(bound);
                m_lightList.lightVolumes.Add(lightVolumeData);
            }
            public int GetCurrentShadowCount()
            {
                return m_ShadowRequests.Count;
            }

            public int GetShadowAtlasCount()
            {
                return (int)m_ShadowMgr.GetShadowMapCount();
            }

            public void UpdateCullingParameters(ref ScriptableCullingParameters cullingParams)
            {
                m_ShadowMgr.UpdateCullingParameters( ref cullingParams );
            }

            public void PrepareLightsForGPU(ShadowSettings shadowSettings, CullResults cullResults, Camera camera)
            {
                m_lightList.Clear();

                Vector3 camPosWS = camera.transform.position;

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
                                m_ShadowRequests.Add(i);
                        }
                        // pass this list to a routine that assigns shadows based on some heuristic
                        uint    shadowRequestCount = (uint)m_ShadowRequests.Count;
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

                    float oldSpecularGlobalDimmer = m_TileSettings.specularGlobalDimmer;
                    // Change some parameters in case of "special" rendering (can be preview, reflection, etc.)
                    if (camera.cameraType == CameraType.Reflection)
                    {
                        m_TileSettings.specularGlobalDimmer = 0.0f;
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

                        // We only process light with additional data
                        var additionalData = light.light.GetComponent<HDAdditionalLightData>();

                        if (additionalData == null)
                            additionalData = DefaultAdditionalLightData;

                        LightCategory lightCategory = LightCategory.Count;
                        GPULightType gpuLightType = GPULightType.Point;
                        LightVolumeType lightVolumeType = LightVolumeType.Count;

                        // Note: LightType.Area is offline only, use for baking, no need to test it
                        if (additionalData.archetype == LightArchetype.Punctual)
                        {
                            lightCategory = LightCategory.Punctual;
                            switch (light.lightType)
                            {
                                case LightType.Point:
                                    if (punctualLightcount >= k_MaxPunctualLightsOnScreen)
                                        continue;
                                    gpuLightType = GPULightType.Point;
                                    lightVolumeType = LightVolumeType.Sphere;
                                    break;

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

                                default:
                                    Debug.Assert(false, "Encountered an unknown LightType.");
                                    break;
                            }
                        }
                        else // LightArchetype.Area
                        {
                            if (areaLightCount >= k_MaxAreaLightsOnScreen) { continue; }
                            lightCategory   = LightCategory.Area;
                            gpuLightType    = (additionalData.lightWidth > 0) ? GPULightType.Rectangle : GPULightType.Line;
                            lightVolumeType = LightVolumeType.Box;
                        }

                        uint shadow = m_ShadowIndices.ContainsKey(lightIndex) ? 1u : 0;
                        // 5 bit (0x1F) light category, 5 bit (0x1F) GPULightType, 5 bit (0x1F) lightVolume, 1 bit for shadow casting, 16 bit index
                        sortKeys[sortCount++] = (uint)lightCategory << 27 | (uint)gpuLightType << 22 | (uint)lightVolumeType << 17 | shadow << 16 | (uint)lightIndex;
                    }

                    Utilities.QuickSort(sortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                    // TODO: Refactor shadow management
                    // The good way of managing shadow:
                    // Here we sort everyone and we decide which light is important or not (this is the responsibility of the lightloop)
                    // we allocate shadow slot based on maximum shadow allowed on screen and attribute slot by bigger solid angle
                    // THEN we ask to the ShadowRender to render the shadow, not the reverse as it is today (i.e render shadow than expect they
                    // will be use...)
                    // The lightLoop is in charge, not the shadow pass.
                    // For now we will still apply the maximum of shadow here but we don't apply the sorting by priority + slot allocation yet
                    m_CurrentSunLight = null;

                    // 2. Go through all lights, convert them to GPU format.
                    // Create simultaneously data for culling (LigthVolumeData and rendering)
                    var worldToView = WorldToCamera(camera);

                    for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                    {
                        // In 1. we have already classify and sorted the light, we need to use this sorted order here
                        uint sortKey = sortKeys[sortIndex];
                        LightCategory lightCategory = (LightCategory)((sortKey >> 27) & 0x1F);
                        GPULightType gpuLightType = (GPULightType)((sortKey >> 22) & 0x1F);
                        LightVolumeType lightVolumeType = (LightVolumeType)((sortKey >> 17) & 0x1F);
                        int lightIndex = (int)(sortKey & 0xFFFF);

                        var light = cullResults.visibleLights[lightIndex];
                        var additionalLightData = light.light.GetComponent<HDAdditionalLightData>() ?? DefaultAdditionalLightData;
                        var additionalShadowData = light.light.GetComponent<AdditionalShadowData>(); // Can be null

                        // Directional rendering side, it is separated as it is always visible so no volume to handle here
                        if (gpuLightType == GPULightType.Directional)
                        {
                            if (GetDirectionalLightData(shadowSettings, gpuLightType, light, additionalLightData, additionalShadowData, lightIndex))
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

                        // Punctual, area, projector lights - the rendering side.
                        if (GetLightData(shadowSettings, camera, gpuLightType, light, additionalLightData, additionalShadowData, lightIndex))
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
                            GetLightVolumeDataAndBound(lightCategory, gpuLightType, lightVolumeType, light, m_lightList.lights[m_lightList.lights.Count - 1], worldToView);

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
                    m_areaLightCount     = areaLightCount;

                    // Redo everything but this time with envLights
                    int envLightCount = 0;

                    int probeCount = Math.Min(cullResults.visibleReflectionProbes.Count, k_MaxEnvLightsOnScreen);
                    sortKeys = new uint[probeCount];
                    sortCount = 0;

                    for (int probeIndex = 0, numProbes = cullResults.visibleReflectionProbes.Count; (probeIndex < numProbes) && (sortCount < probeCount); probeIndex++)
                    {
                        var probe = cullResults.visibleReflectionProbes[probeIndex];

                        // probe.texture can be null when we are adding a reflection probe in the editor
                        if (probe.texture == null || envLightCount >= k_MaxEnvLightsOnScreen)
                            continue;

                        // TODO: Support LightVolumeType.Sphere, currently in UI there is no way to specify a sphere influence volume
                        LightVolumeType lightVolumeType = probe.boxProjection != 0 ? LightVolumeType.Box : LightVolumeType.Box;
                        ++envLightCount;

                        // 16 bit lightVolume, 16 bit index
                        sortKeys[sortCount++] = (uint)lightVolumeType << 16 | (uint)probeIndex;
                    }

                    // Not necessary yet but call it for future modification with sphere influence volume
                    Utilities.QuickSort(sortKeys, 0, sortCount - 1); // Call our own quicksort instead of Array.Sort(sortKeys, 0, sortCount) so we don't allocate memory (note the SortCount-1 that is different from original call).

                    for (int sortIndex = 0; sortIndex < sortCount; ++sortIndex)
                    {
                        // In 1. we have already classify and sorted the light, we need to use this sorted order here
                        uint sortKey = sortKeys[sortIndex];
                        LightVolumeType lightVolumeType = (LightVolumeType)((sortKey >> 16) & 0xFFFF);
                        int probeIndex = (int)(sortKey & 0xFFFF);

                        VisibleReflectionProbe probe = cullResults.visibleReflectionProbes[probeIndex];

                        GetEnvLightData(probe);

                        GetEnvLightVolumeDataAndBound(probe, lightVolumeType, worldToView);

                        // We make the light position camera-relative as late as possible in order
                        // to allow the preceding code to work with the absolute world space coordinates.
                        if (ShaderConfig.s_CameraRelativeRendering != 0)
                        {
                            // Caution: 'EnvLightData.positionWS' is camera-relative after this point.
                            int n = m_lightList.envLights.Count;
                            EnvLightData envLightData = m_lightList.envLights[n - 1];
                            envLightData.positionWS -= camPosWS;
                            m_lightList.envLights[n - 1] = envLightData;
                        }
                    }

                    // Sanity check
                    Debug.Assert(m_lightList.envLights.Count == envLightCount);

                    // Restore values after "special rendering"
                    m_TileSettings.specularGlobalDimmer = oldSpecularGlobalDimmer;
                }

                m_lightCount = m_lightList.lights.Count + m_lightList.envLights.Count;
                Debug.Assert(m_lightList.bounds.Count == m_lightCount);
                Debug.Assert(m_lightList.lightVolumes.Count == m_lightCount);

                UpdateDataBuffers();
            }

            void VoxelLightListGeneration(CommandBuffer cmd, Camera camera, Matrix4x4 projscr, Matrix4x4 invProjscr, RenderTargetIdentifier cameraDepthBufferRT)
            {
                // clear atomic offset index
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, "g_LayeredSingleIdxBuffer", s_GlobalLightListAtomic);
                cmd.DispatchCompute(buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, 1, 1, 1);

                cmd.SetComputeIntParam(buildPerVoxelLightListShader, "_EnvLightIndexShift", m_lightList.lights.Count);
                cmd.SetComputeIntParam(buildPerVoxelLightListShader, "g_iNrVisibLights", m_lightCount);
                cmd.SetComputeMatrixParam(buildPerVoxelLightListShader, "g_mScrProjection", projscr);
                cmd.SetComputeMatrixParam(buildPerVoxelLightListShader, "g_mInvScrProjection", invProjscr);

                cmd.SetComputeIntParam(buildPerVoxelLightListShader, "g_iLog2NumClusters", k_Log2NumClusters);

                //Vector4 v2_near = invProjscr * new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
                //Vector4 v2_far = invProjscr * new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
                //float nearPlane2 = -(v2_near.z/v2_near.w);
                //float farPlane2 = -(v2_far.z/v2_far.w);
                var nearPlane = camera.nearClipPlane;
                var farPlane = camera.farClipPlane;
                cmd.SetComputeFloatParam(buildPerVoxelLightListShader, "g_fNearPlane", nearPlane);
                cmd.SetComputeFloatParam(buildPerVoxelLightListShader, "g_fFarPlane", farPlane);

                const float C = (float)(1 << k_Log2NumClusters);
                var geomSeries = (1.0 - Mathf.Pow(k_ClustLogBase, C)) / (1 - k_ClustLogBase);        // geometric series: sum_k=0^{C-1} base^k
                m_ClustScale = (float)(geomSeries / (farPlane - nearPlane));

                cmd.SetComputeFloatParam(buildPerVoxelLightListShader, "g_fClustScale", m_ClustScale);
                cmd.SetComputeFloatParam(buildPerVoxelLightListShader, "g_fClustBase", k_ClustLogBase);

                cmd.SetComputeTextureParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_depth_tex", cameraDepthBufferRT);
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_vLayeredLightList", s_PerVoxelLightLists);
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_LayeredOffset", s_PerVoxelOffset);
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_LayeredSingleIdxBuffer", s_GlobalLightListAtomic);
                if (m_TileSettings.enableBigTilePrepass)
                    cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_vBigTileLightList", s_BigTileLightList);

                if (k_UseDepthBuffer)
                {
                    cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_logBaseBuffer", s_PerTileLogBaseTweak);
                }

                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "_LightVolumeData", s_LightVolumeDataBuffer);
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_data", s_ConvexBoundsBuffer);

                var numTilesX = GetNumTileClusteredX(camera);
                var numTilesY = GetNumTileClusteredY(camera);
                cmd.DispatchCompute(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, numTilesX, numTilesY, 1);
            }

            public void BuildGPULightLists(Camera camera, CommandBuffer cmd, RenderTargetIdentifier cameraDepthBufferRT)
            {
                var w = camera.pixelWidth;
                var h = camera.pixelHeight;
                var numBigTilesX = (w + 63) / 64;
                var numBigTilesY = (h + 63) / 64;

                // camera to screen matrix (and it's inverse)
                var proj = CameraProjection(camera);
                var temp = new Matrix4x4();
                temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
                temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
                temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                var projscr = temp * proj;
                var invProjscr = projscr.inverse;

                cmd.SetRenderTarget(new RenderTargetIdentifier((Texture)null));

                // generate screen-space AABBs (used for both fptl and clustered).
                if (m_lightCount != 0)
                {
                    temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                    temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                    temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                    temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                    var projh = temp * proj;
                    var invProjh = projh.inverse;

                    cmd.SetComputeIntParam(buildScreenAABBShader, "g_iNrVisibLights", m_lightCount);
                    cmd.SetComputeBufferParam(buildScreenAABBShader, s_GenAABBKernel, "g_data", s_ConvexBoundsBuffer);

                    cmd.SetComputeMatrixParam(buildScreenAABBShader, "g_mProjection", projh);
                    cmd.SetComputeMatrixParam(buildScreenAABBShader, "g_mInvProjection", invProjh);
                    cmd.SetComputeBufferParam(buildScreenAABBShader, s_GenAABBKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                    cmd.DispatchCompute(buildScreenAABBShader, s_GenAABBKernel, (m_lightCount + 7) / 8, 1, 1);
                }

                // enable coarse 2D pass on 64x64 tiles (used for both fptl and clustered).
                if (m_TileSettings.enableBigTilePrepass)
                {
                    cmd.SetComputeIntParams(buildPerBigTileLightListShader, "g_viDimensions", w, h);
                    cmd.SetComputeIntParam(buildPerBigTileLightListShader, "_EnvLightIndexShift", m_lightList.lights.Count);
                    cmd.SetComputeIntParam(buildPerBigTileLightListShader, "g_iNrVisibLights", m_lightCount);
                    cmd.SetComputeMatrixParam(buildPerBigTileLightListShader, "g_mScrProjection", projscr);
                    cmd.SetComputeMatrixParam(buildPerBigTileLightListShader, "g_mInvScrProjection", invProjscr);
                    cmd.SetComputeFloatParam(buildPerBigTileLightListShader, "g_fNearPlane", camera.nearClipPlane);
                    cmd.SetComputeFloatParam(buildPerBigTileLightListShader, "g_fFarPlane", camera.farClipPlane);
                    cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, "g_vLightList", s_BigTileLightList);
                    cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                    cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, "_LightVolumeData", s_LightVolumeDataBuffer);
                    cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, "g_data", s_ConvexBoundsBuffer);
                    cmd.DispatchCompute(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, numBigTilesX, numBigTilesY, 1);
                }

                var numTilesX = GetNumTileFtplX(camera);
                var numTilesY = GetNumTileFtplY(camera);
                var numTiles = numTilesX * numTilesY;
                bool enableFeatureVariants = GetFeatureVariantsEnabled();

                if (usingFptl)       // optimized for opaques only
                {
                    cmd.SetComputeIntParams(buildPerTileLightListShader, "g_viDimensions", w, h);
                    cmd.SetComputeIntParam(buildPerTileLightListShader, "_EnvLightIndexShift", m_lightList.lights.Count);
                    cmd.SetComputeIntParam(buildPerTileLightListShader, "g_iNrVisibLights", m_lightCount);

                    cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                    cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, "_LightVolumeData", s_LightVolumeDataBuffer);
                    cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_data", s_ConvexBoundsBuffer);

                    cmd.SetComputeMatrixParam(buildPerTileLightListShader, "g_mScrProjection", projscr);
                    cmd.SetComputeMatrixParam(buildPerTileLightListShader, "g_mInvScrProjection", invProjscr);
                    cmd.SetComputeTextureParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_depth_tex", cameraDepthBufferRT);
                    cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_vLightList", s_LightList);
                    if (m_TileSettings.enableBigTilePrepass)
                        cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_vBigTileLightList", s_BigTileLightList);

                    if (enableFeatureVariants)
                    {
                        uint baseFeatureFlags = 0;
                        if (m_lightList.directionalLights.Count > 0)
                        {
                            baseFeatureFlags |= (uint)LightFeatureFlags.Directional;
                        }
                        if (Shader.GetGlobalInt("_EnvLightSkyEnabled") != 0)
                        {
                            baseFeatureFlags |= (uint)LightFeatureFlags.Sky;
                        }
                        if (!m_TileSettings.enableComputeMaterialVariants)
                        {
                            baseFeatureFlags |= LightDefinitions.s_MaterialFeatureMaskFlags;
                        }
                        cmd.SetComputeIntParam(buildPerTileLightListShader, "g_BaseFeatureFlags", (int)baseFeatureFlags);
                        cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_TileFeatureFlags", s_TileFeatureFlags);
                    }

                    cmd.DispatchCompute(buildPerTileLightListShader, s_GenListPerTileKernel, numTilesX, numTilesY, 1);
                }

                if (m_TileSettings.enableClustered)        // works for transparencies too.
                {
                    VoxelLightListGeneration(cmd, camera, projscr, invProjscr, cameraDepthBufferRT);
                }

                if (enableFeatureVariants)
                {
                    // material classification
                    if(m_TileSettings.enableComputeMaterialVariants)
                    {
                        int buildMaterialFlagsKernel = s_BuildMaterialFlagsOrKernel;

                        uint baseFeatureFlags = 0;
                        if (!m_TileSettings.enableComputeLightVariants)
                        {
                            buildMaterialFlagsKernel = s_BuildMaterialFlagsWriteKernel;
                            baseFeatureFlags |= LightDefinitions.s_LightFeatureMaskFlags;
                        }

                        cmd.SetComputeIntParam(buildMaterialFlagsShader, "g_BaseFeatureFlags", (int)baseFeatureFlags);
                        cmd.SetComputeIntParams(buildMaterialFlagsShader, "g_viDimensions", w, h);
                        cmd.SetComputeBufferParam(buildMaterialFlagsShader, buildMaterialFlagsKernel, "g_TileFeatureFlags", s_TileFeatureFlags);

                        cmd.SetComputeTextureParam(buildMaterialFlagsShader, buildMaterialFlagsKernel, "g_depth_tex", cameraDepthBufferRT);
                        cmd.SetComputeTextureParam(buildMaterialFlagsShader, buildMaterialFlagsKernel, "_GBufferTexture0", Shader.PropertyToID("_GBufferTexture0"));
                        cmd.SetComputeTextureParam(buildMaterialFlagsShader, buildMaterialFlagsKernel, "_GBufferTexture1", Shader.PropertyToID("_GBufferTexture1"));
                        cmd.SetComputeTextureParam(buildMaterialFlagsShader, buildMaterialFlagsKernel, "_GBufferTexture2", Shader.PropertyToID("_GBufferTexture2"));
                        cmd.SetComputeTextureParam(buildMaterialFlagsShader, buildMaterialFlagsKernel, "_GBufferTexture3", Shader.PropertyToID("_GBufferTexture3"));

                        cmd.DispatchCompute(buildMaterialFlagsShader, buildMaterialFlagsKernel, numTilesX, numTilesY, 1);
                    }

                    // clear dispatch indirect buffer
                    cmd.SetComputeBufferParam(clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, "g_DispatchIndirectBuffer", s_DispatchIndirectBuffer);
                    cmd.DispatchCompute(clearDispatchIndirectShader, s_ClearDispatchIndirectKernel, 1, 1, 1);

                    // add tiles to indirect buffer
                    cmd.SetComputeBufferParam(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, "g_DispatchIndirectBuffer", s_DispatchIndirectBuffer);
                    cmd.SetComputeBufferParam(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, "g_TileList", s_TileList);
                    cmd.SetComputeBufferParam(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, "g_TileFeatureFlags", s_TileFeatureFlags);
                    cmd.SetComputeIntParam(buildDispatchIndirectShader, "g_NumTiles", numTiles);
                    cmd.SetComputeIntParam(buildDispatchIndirectShader, "g_NumTilesX", numTilesX);
                    cmd.DispatchCompute(buildDispatchIndirectShader, s_BuildDispatchIndirectKernel, (numTiles + 63) / 64, 1, 1);
                }
            }

            // This is a workaround for global properties not being accessible from compute.
            // When activeComputeShader is set, all calls to SetGlobalXXX will set the property on the select compute shader instead of the global scope.
            private ComputeShader activeComputeShader;
            private int activeComputeKernel;
            private CommandBuffer activeCommandBuffer;
            private void SetGlobalPropertyRedirect(ComputeShader computeShader, int computeKernel, CommandBuffer commandBuffer)
            {
                activeComputeShader = computeShader;
                activeComputeKernel = computeKernel;
                activeCommandBuffer = commandBuffer;
            }

            private void SetGlobalTexture(string name, Texture value)
            {
                if (activeComputeShader)
                    activeCommandBuffer.SetComputeTextureParam(activeComputeShader, activeComputeKernel, name, value);
                else
                    activeCommandBuffer.SetGlobalTexture(name, value);
            }

            private void SetGlobalBuffer(string name, ComputeBuffer buffer)
            {
                if (activeComputeShader)
                    activeCommandBuffer.SetComputeBufferParam(activeComputeShader, activeComputeKernel, name, buffer);
                else
                    activeCommandBuffer.SetGlobalBuffer(name, buffer);
            }

            private void SetGlobalInt(string name, int value)
            {
                if (activeComputeShader)
                    activeCommandBuffer.SetComputeIntParam(activeComputeShader, name, value);
                else
                    Shader.SetGlobalInt(name, value);
            }

            private void SetGlobalFloat(string name, float value)
            {
                if (activeComputeShader)
                    activeCommandBuffer.SetComputeFloatParam(activeComputeShader, name, value);
                else
                    activeCommandBuffer.SetGlobalFloat(name, value);
            }

            private void SetGlobalVector(string name, Vector4 value)
            {
                if (activeComputeShader)
                    activeCommandBuffer.SetComputeVectorParam(activeComputeShader, name, value);
                else
                    activeCommandBuffer.SetGlobalVector(name, value);
            }

            private void SetGlobalVectorArray(string name, Vector4[] values)
            {
                if (activeComputeShader)
                {
                    int numVectors = values.Length;
                    var data = new float[numVectors * 4];

                    for (int n = 0; n < numVectors; n++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            data[4 * n + i] = values[n][i];
                        }
                    }

                    activeCommandBuffer.SetComputeFloatParams(activeComputeShader, name, data);
                }
                else
                {
                    activeCommandBuffer.SetGlobalVectorArray(name, values);
                }
            }

            private void UpdateDataBuffers()
            {
                s_DirectionalLightDatas.SetData(m_lightList.directionalLights);
                s_LightDatas.SetData(m_lightList.lights);
                s_EnvLightDatas.SetData(m_lightList.envLights);
                s_shadowDatas.SetData(m_lightList.shadows);

                // These two buffers have been set in Rebuild()
                s_ConvexBoundsBuffer.SetData(m_lightList.bounds);
                s_LightVolumeDataBuffer.SetData(m_lightList.lightVolumes);
            }

            private void BindGlobalParams(CommandBuffer cmd, Camera camera)
            {
                m_ShadowMgr.BindResources(cmd, activeComputeShader, activeComputeKernel);

                SetGlobalBuffer("g_vLightListGlobal", !usingFptl ? s_PerVoxelLightLists : s_LightList);       // opaques list (unless MSAA possibly)

                SetGlobalTexture("_CookieTextures", m_CookieTexArray.GetTexCache());
                SetGlobalTexture("_CookieCubeTextures", m_CubeCookieTexArray.GetTexCache());
                SetGlobalTexture("_EnvTextures", m_CubeReflTexArray.GetTexCache());

                SetGlobalBuffer("_DirectionalLightDatas", s_DirectionalLightDatas);
                SetGlobalInt("_DirectionalLightCount", m_lightList.directionalLights.Count);
                SetGlobalBuffer("_LightDatas", s_LightDatas);
                SetGlobalInt("_PunctualLightCount", m_punctualLightCount);
                SetGlobalInt("_AreaLightCount", m_areaLightCount);
                SetGlobalBuffer("_EnvLightDatas", s_EnvLightDatas);
                SetGlobalInt("_EnvLightCount", m_lightList.envLights.Count);
                SetGlobalBuffer("_ShadowDatas", s_shadowDatas);
                SetGlobalVectorArray("_DirShadowSplitSpheres", m_lightList.directionalShadowSplitSphereSqr);

                SetGlobalInt("_NumTileFtplX", GetNumTileFtplX(camera));
                SetGlobalInt("_NumTileFtplY", GetNumTileFtplY(camera));

                SetGlobalInt("_NumTileClusteredX", GetNumTileClusteredX(camera));
                SetGlobalInt("_NumTileClusteredY", GetNumTileClusteredY(camera));

                if (m_TileSettings.enableBigTilePrepass)
                    SetGlobalBuffer("g_vBigTileLightList", s_BigTileLightList);

                if (m_TileSettings.enableClustered)
                {
                    SetGlobalFloat("g_fClustScale", m_ClustScale);
                    SetGlobalFloat("g_fClustBase", k_ClustLogBase);
                    SetGlobalFloat("g_fNearPlane", camera.nearClipPlane);
                    SetGlobalFloat("g_fFarPlane", camera.farClipPlane);
                    SetGlobalFloat("g_iLog2NumClusters", k_Log2NumClusters);

                    SetGlobalFloat("g_isLogBaseBufferEnabled", k_UseDepthBuffer ? 1 : 0);

                    SetGlobalBuffer("g_vLayeredOffsetsBuffer", s_PerVoxelOffset);
                    if (k_UseDepthBuffer)
                    {
                        SetGlobalBuffer("g_logBaseBuffer", s_PerTileLogBaseTweak);
                    }
                }
            }

            private void PushGlobalParams(Camera camera, CommandBuffer cmd, ComputeShader computeShader, int kernelIndex)
            {
                using (new Utilities.ProfilingSample("Push Global Parameters", cmd))
                {
                    // Shadows
                    m_ShadowMgr.SyncData();

                    SetGlobalPropertyRedirect(computeShader, kernelIndex, cmd);
                    BindGlobalParams(cmd, camera);
                    SetGlobalPropertyRedirect(null, 0, null);
                }
            }

#if UNITY_EDITOR
            private Vector2 m_mousePosition = Vector2.zero;

            private void OnSceneGUI(UnityEditor.SceneView sceneview)
            {
                m_mousePosition = Event.current.mousePosition;
            }

#endif

            public void RenderShadows(ScriptableRenderContext renderContext, CommandBuffer cmd, CullResults cullResults)
            {
                // kick off the shadow jobs here
                m_ShadowMgr.RenderShadows(m_FrameId, renderContext, cmd, cullResults, cullResults.visibleLights);
            }

            private void SetupDebugDisplayMode(bool debugDisplayEnable)
            {
                Utilities.SetKeyword(m_DeferredAllMaterialSRT, "DEBUG_DISPLAY", debugDisplayEnable);
                Utilities.SetKeyword(m_DeferredAllMaterialMRT, "DEBUG_DISPLAY", debugDisplayEnable);
                Utilities.SetKeyword(m_SingleDeferredMaterialSRT, "DEBUG_DISPLAY", debugDisplayEnable);
                Utilities.SetKeyword(m_SingleDeferredMaterialMRT, "DEBUG_DISPLAY", debugDisplayEnable);
            }

            public void RenderLightingDebug(HDCamera hdCamera, CommandBuffer cmd, RenderTargetIdentifier colorBuffer, DebugDisplaySettings debugDisplaySettings)
            {
                LightingDebugSettings lightingDebug = debugDisplaySettings.lightingDebugSettings;
                if (lightingDebug.tileDebugByCategory == TileSettings.TileDebug.None)
                    return;

                using (new Utilities.ProfilingSample("Tiled Lighting Debug", cmd))
                {
                    bool bUseClusteredForDeferred = !usingFptl;

                    int w = hdCamera.camera.pixelWidth;
                    int h = hdCamera.camera.pixelHeight;
                    int numTilesX = (w + 15) / 16;
                    int numTilesY = (h + 15) / 16;
                    int numTiles = numTilesX * numTilesY;

                    Vector2 mousePixelCoord = Input.mousePosition;
    #if UNITY_EDITOR
                    if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        mousePixelCoord = m_mousePosition;
                        mousePixelCoord.y = (hdCamera.screenSize.y - 1.0f) - mousePixelCoord.y;
                    }
    #endif

                    // Debug tiles
                    PushGlobalParams(hdCamera.camera, cmd, null, 0);
                    if (lightingDebug.tileDebugByCategory == TileSettings.TileDebug.FeatureVariants)
                    {
                        if (GetFeatureVariantsEnabled())
                        {
                            // featureVariants
                            m_DebugViewTilesMaterial.SetInt("_NumTiles", numTiles);
                            m_DebugViewTilesMaterial.SetInt("_ViewTilesFlags", (int)lightingDebug.tileDebugByCategory);
                            m_DebugViewTilesMaterial.SetVector("_MousePixelCoord", mousePixelCoord);
                            m_DebugViewTilesMaterial.SetBuffer("g_TileList", s_TileList);
                            m_DebugViewTilesMaterial.SetBuffer("g_DispatchIndirectBuffer", s_DispatchIndirectBuffer);
                            m_DebugViewTilesMaterial.EnableKeyword(bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                            m_DebugViewTilesMaterial.DisableKeyword(!bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                            m_DebugViewTilesMaterial.DisableKeyword("SHOW_LIGHT_CATEGORIES");
                            m_DebugViewTilesMaterial.EnableKeyword("SHOW_FEATURE_VARIANTS");
                            cmd.SetRenderTarget(colorBuffer);
                            cmd.DrawProcedural(Matrix4x4.identity, m_DebugViewTilesMaterial, 0, MeshTopology.Triangles, numTiles * 6);
                        }
                    }
                    else if (lightingDebug.tileDebugByCategory != TileSettings.TileDebug.None)
                    {
                        // lightCategories
                        m_DebugViewTilesMaterial.SetInt("_ViewTilesFlags", (int)lightingDebug.tileDebugByCategory);
                        m_DebugViewTilesMaterial.SetVector("_MousePixelCoord", mousePixelCoord);
                        m_DebugViewTilesMaterial.EnableKeyword(bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        m_DebugViewTilesMaterial.DisableKeyword(!bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        m_DebugViewTilesMaterial.EnableKeyword("SHOW_LIGHT_CATEGORIES");
                        m_DebugViewTilesMaterial.DisableKeyword("SHOW_FEATURE_VARIANTS");

                        Utilities.DrawFullScreen(cmd, m_DebugViewTilesMaterial, 0, colorBuffer);
                    }
                    SetGlobalPropertyRedirect(null, 0, null);
                }
            }

            public void RenderDeferredLighting( HDCamera hdCamera, CommandBuffer cmd,
                                                DebugDisplaySettings debugDisplaySettings,
                                                RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthStencilBuffer, RenderTargetIdentifier depthTexture,
                                                bool outputSplitLighting)
            {
                var bUseClusteredForDeferred = !usingFptl;

                if (m_TileSettings.enableComputeLightEvaluation && outputSplitLighting)
                {
                    // The CS is always in the MRT mode. Do not execute the same shader twice.
                    return;
                }

                // TODO: To reduce GC pressure don't do concat string here
                using (new Utilities.ProfilingSample((m_TileSettings.enableTileAndCluster ? "TilePass - Deferred Lighting Pass" : "SinglePass - Deferred Lighting Pass") + (outputSplitLighting ? " MRT" : ""), cmd))
                {
                    var camera = hdCamera.camera;

                    SetupDebugDisplayMode(debugDisplaySettings.IsDebugDisplayEnabled());

                    if (!m_TileSettings.enableTileAndCluster)
                    {
                        PushGlobalParams(camera, cmd, null, 0);

                        // This is a debug brute force renderer to debug tile/cluster which render all the lights
                        if (outputSplitLighting)
                        {
                            Utilities.DrawFullScreen(cmd, m_SingleDeferredMaterialMRT, colorBuffers, depthStencilBuffer);
                        }
                        else
                        {
                            // If SSS is disable, do lighting for both split lighting and no split lighting
                            if (!debugDisplaySettings.renderingDebugSettings.enableSSSAndTransmission)
                            {
                                m_SingleDeferredMaterialSRT.SetInt("_StencilRef", (int)StencilLightingUsage.NoLighting);
                                m_SingleDeferredMaterialSRT.SetInt("_StencilCmp", (int)CompareFunction.NotEqual);
                            }
                            else
                            {
                                m_SingleDeferredMaterialSRT.SetInt("_StencilRef", (int)StencilLightingUsage.RegularLighting);
                                m_SingleDeferredMaterialSRT.SetInt("_StencilCmp", (int)CompareFunction.Equal);
                            }

                            Utilities.DrawFullScreen(cmd, m_SingleDeferredMaterialSRT, colorBuffers[0], depthStencilBuffer);
                        }
                    }
                    else
                    {
                        int w = camera.pixelWidth;
                        int h = camera.pixelHeight;
                        int numTilesX = (w + 15) / 16;
                        int numTilesY = (h + 15) / 16;
                        int numTiles = numTilesX * numTilesY;

                        if (m_TileSettings.enableComputeLightEvaluation)
                        {
                            bool enableFeatureVariants = GetFeatureVariantsEnabled() && !debugDisplaySettings.IsDebugDisplayEnabled();

                            int numVariants = 1;
                            if (enableFeatureVariants)
                                numVariants = LightDefinitions.s_NumFeatureVariants;


                            int debugViewMaterial = Shader.GetGlobalInt("_DebugViewMaterial");
                            int debugLightingMode = Shader.GetGlobalInt("_DebugLightingMode");
                            Vector4 debugLightingAlbedo = Shader.GetGlobalVector("_DebugLightingAlbedo");
                            Vector4 debugLightingSmoothness = Shader.GetGlobalVector("_DebugLightingSmoothness");

                            int gbufferTexture0 = Shader.PropertyToID("_GBufferTexture0");
                            int gbufferTexture1 = Shader.PropertyToID("_GBufferTexture1");
                            int gbufferTexture2 = Shader.PropertyToID("_GBufferTexture2");
                            int gbufferTexture3 = Shader.PropertyToID("_GBufferTexture3");
                            int ambientOcclusionTexture = Shader.PropertyToID("_AmbientOcclusionTexture");

                            Texture ltcData = Shader.GetGlobalTexture(Shader.PropertyToID("_LtcData"));
                            Texture preIntegratedFGD = Shader.GetGlobalTexture("_PreIntegratedFGD");
                            Texture ltcGGXMatrix = Shader.GetGlobalTexture("_LtcGGXMatrix");
                            Texture ltcDisneyDiffuseMatrix = Shader.GetGlobalTexture("_LtcDisneyDiffuseMatrix");
                            Texture ltcMultiGGXFresnelDisneyDiffuse = Shader.GetGlobalTexture("_LtcMultiGGXFresnelDisneyDiffuse");

                            Matrix4x4 invScrProjection = Shader.GetGlobalMatrix("g_mInvScrProjection");
                            int useTileLightList = Shader.GetGlobalInt("_UseTileLightList");

                            Vector4 time = Shader.GetGlobalVector("_Time");
                            Vector4 sinTime = Shader.GetGlobalVector("_SinTime");
                            Vector4 cosTime = Shader.GetGlobalVector("_CosTime");
                            Vector4 unity_DeltaTime = Shader.GetGlobalVector("unity_DeltaTime");
                            Vector4 worldSpaceCameraPos = Shader.GetGlobalVector("_WorldSpaceCameraPos");
                            Vector4 projectionParams = Shader.GetGlobalVector("_ProjectionParams");
                            Vector4 screenParams = Shader.GetGlobalVector("_ScreenParams");
                            Vector4 zbufferParams = Shader.GetGlobalVector("_ZBufferParams");
                            Vector4 unity_OrthoParams = Shader.GetGlobalVector("unity_OrthoParams");
                            int envLightSkyEnabled = Shader.GetGlobalInt("_EnvLightSkyEnabled");
                            float ambientOcclusionDirectLightStrenght = Shader.GetGlobalFloat("_AmbientOcclusionDirectLightStrenght");

                            int enableSSSAndTransmission = Shader.GetGlobalInt("_EnableSSSAndTransmission");
                            int texturingModeFlags = Shader.GetGlobalInt("_TexturingModeFlags");
                            int transmissionFlags = Shader.GetGlobalInt("_TransmissionFlags");
                            Vector4[] thicknessRemaps = Shader.GetGlobalVectorArray("_ThicknessRemaps");
                            Vector4[] shapeParams = Shader.GetGlobalVectorArray("_ShapeParams");
                            Vector4[] transmissionTints = Shader.GetGlobalVectorArray("_TransmissionTints");

                            for (int variant = 0; variant < numVariants; variant++)
                            {
                                int kernel;

                                if (enableFeatureVariants)
                                {
                                    kernel = usingFptl ? s_shadeOpaqueIndirectFptlKernels[variant] : s_shadeOpaqueIndirectClusteredKernels[variant];
                                }
                                else
                                {
                                    if (debugDisplaySettings.IsDebugDisplayEnabled())
                                    {
                                        kernel = usingFptl ? s_shadeOpaqueDirectFptlDebugDisplayKernel : s_shadeOpaqueDirectClusteredDebugDisplayKernel;
                                    }
                                    else
                                    {
                                        kernel = usingFptl ? s_shadeOpaqueDirectFptlKernel : s_shadeOpaqueDirectClusteredKernel;
                                    }
                                }

                                // Pass global parameters to compute shader
                                // TODO: get rid of this by making global parameters visible to compute shaders
                                PushGlobalParams(camera, cmd, deferredComputeShader, kernel);
                                hdCamera.SetupComputeShader(deferredComputeShader, cmd);

                                // TODO: Update value like in ApplyDebugDisplaySettings() call. Sadly it is high likely that this will not be keep in sync. we really need to get rid of this by making global parameters visible to compute shaders
                                cmd.SetComputeIntParam(deferredComputeShader, "_DebugViewMaterial", debugViewMaterial);
                                cmd.SetComputeIntParam(deferredComputeShader, "_DebugLightingMode", debugLightingMode);
                                cmd.SetComputeVectorParam(deferredComputeShader, "_DebugLightingAlbedo", debugLightingAlbedo);
                                cmd.SetComputeVectorParam(deferredComputeShader, "_DebugLightingSmoothness", debugLightingSmoothness);

                                cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_vLightListGlobal", bUseClusteredForDeferred ? s_PerVoxelLightLists : s_LightList);

                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_MainDepthTexture", depthTexture);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_GBufferTexture0", gbufferTexture0);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_GBufferTexture1", gbufferTexture1);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_GBufferTexture2", gbufferTexture2);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_GBufferTexture3", gbufferTexture3);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_AmbientOcclusionTexture", ambientOcclusionTexture);

                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_LtcData", ltcData);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_PreIntegratedFGD", preIntegratedFGD);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_LtcGGXMatrix", ltcGGXMatrix);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_LtcDisneyDiffuseMatrix", ltcDisneyDiffuseMatrix);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_LtcMultiGGXFresnelDisneyDiffuse", ltcMultiGGXFresnelDisneyDiffuse);

                                cmd.SetComputeMatrixParam(deferredComputeShader, "g_mInvScrProjection", invScrProjection);
                                cmd.SetComputeIntParam(deferredComputeShader, "_UseTileLightList", useTileLightList);

                                cmd.SetComputeVectorParam(deferredComputeShader, "_Time", time);
                                cmd.SetComputeVectorParam(deferredComputeShader, "_SinTime", sinTime);
                                cmd.SetComputeVectorParam(deferredComputeShader, "_CosTime", cosTime);
                                cmd.SetComputeVectorParam(deferredComputeShader, "unity_DeltaTime", unity_DeltaTime);
                                cmd.SetComputeVectorParam(deferredComputeShader, "_WorldSpaceCameraPos", worldSpaceCameraPos);
                                cmd.SetComputeVectorParam(deferredComputeShader, "_ProjectionParams", projectionParams);
                                cmd.SetComputeVectorParam(deferredComputeShader, "_ScreenParams", screenParams);
                                cmd.SetComputeVectorParam(deferredComputeShader, "_ZBufferParams", zbufferParams);
                                cmd.SetComputeVectorParam(deferredComputeShader, "unity_OrthoParams", unity_OrthoParams);
                                cmd.SetComputeIntParam(deferredComputeShader, "_EnvLightSkyEnabled", envLightSkyEnabled);
                                cmd.SetComputeFloatParam(deferredComputeShader, "_AmbientOcclusionDirectLightStrenght", ambientOcclusionDirectLightStrenght);

                                Texture skyTexture = Shader.GetGlobalTexture("_SkyTexture");
                                Texture IESArrayTexture = Shader.GetGlobalTexture("_IESArray");
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_IESArray", IESArrayTexture ? IESArrayTexture : m_DefaultTexture2DArray);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_SkyTexture", skyTexture ? skyTexture : m_DefaultTexture2DArray);

                                // Set SSS parameters.
                                cmd.SetComputeIntParam(   deferredComputeShader, "_EnableSSSAndTransmission", enableSSSAndTransmission);
                                cmd.SetComputeIntParam(   deferredComputeShader, "_TexturingModeFlags",       texturingModeFlags);
                                cmd.SetComputeIntParam(   deferredComputeShader, "_TransmissionFlags",        transmissionFlags);
                                cmd.SetComputeVectorArrayParam(deferredComputeShader, "_ThicknessRemaps",     thicknessRemaps);
                                // We are currently supporting two different SSS mode: Jimenez (with 2-Gaussian profile) and Disney
                                // We have added the ability to switch between each other for subsurface scattering, but for transmittance this is more tricky as we need to add
                                // shader variant for forward, gbuffer and deferred shader. We want to avoid this.
                                // So for transmittance we use Disney profile formulation (that we know is more correct) in both case, and in the case of Jimenez we hack the parameters with 2-Gaussian parameters (Ideally we should fit but haven't find good fit) so it approximately match.
                                // Note: Jimenez SSS is in cm unit whereas Disney is in mm unit making an inconsistency here to compare model side by side
                                cmd.SetComputeVectorArrayParam(deferredComputeShader, "_ShapeParams",       shapeParams);
                                cmd.SetComputeVectorArrayParam(deferredComputeShader, "_TransmissionTints", transmissionTints);

                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "specularLightingUAV", colorBuffers[0]);
                                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "diffuseLightingUAV",  colorBuffers[1]);

                                // always do deferred lighting in blocks of 16x16 (not same as tiled light size)

                                if (enableFeatureVariants)
                                {
                                    cmd.SetComputeIntParam(deferredComputeShader, "g_TileListOffset", variant * numTiles);
                                    cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_TileList", s_TileList);
                                    cmd.DispatchCompute(deferredComputeShader, kernel, s_DispatchIndirectBuffer, (uint)variant * 3 * sizeof(uint));
                                }
                                else
                                {
                                    cmd.DispatchCompute(deferredComputeShader, kernel, numTilesX, numTilesY, 1);
                                }
                            }
                        }
                        else
                        {
                            // Pixel shader evaluation
                            PushGlobalParams(camera, cmd, null, 0);

                            if (outputSplitLighting)
                            {
                                Utilities.SelectKeyword(m_DeferredAllMaterialMRT, "USE_CLUSTERED_LIGHTLIST", "USE_FPTL_LIGHTLIST", bUseClusteredForDeferred);
                                Utilities.DrawFullScreen(cmd, m_DeferredAllMaterialMRT, colorBuffers, depthStencilBuffer);
                            }
                            else
                            {
                                // If SSS is disable, do lighting for both split lighting and no split lighting
                                if (!debugDisplaySettings.renderingDebugSettings.enableSSSAndTransmission)
                                {
                                    m_DeferredAllMaterialSRT.SetInt("_StencilRef", (int)StencilLightingUsage.NoLighting);
                                    m_DeferredAllMaterialSRT.SetInt("_StencilCmp", (int)CompareFunction.NotEqual);
                                }
                                else
                                {
                                    m_DeferredAllMaterialSRT.SetInt("_StencilRef", (int)StencilLightingUsage.RegularLighting);
                                    m_DeferredAllMaterialSRT.SetInt("_StencilCmp", (int)CompareFunction.Equal);
                                }

                                Utilities.SelectKeyword(m_DeferredAllMaterialSRT, "USE_CLUSTERED_LIGHTLIST", "USE_FPTL_LIGHTLIST", bUseClusteredForDeferred);
                                Utilities.DrawFullScreen(cmd, m_DeferredAllMaterialSRT, colorBuffers[0], depthStencilBuffer);
                            }
                        }
                    }

                    SetGlobalPropertyRedirect(null, 0, null);
                } // TilePass - Deferred Lighting Pass
            }

            public void RenderForward(Camera camera, CommandBuffer cmd, bool renderOpaque)
            {
                PushGlobalParams(camera, cmd, null, 0);

                // Note: if we use render opaque with deferred tiling we need to render a opaque depth pass for these opaque objects
                if (!m_TileSettings.enableTileAndCluster)
                {
                    using (new Utilities.ProfilingSample("Forward pass", cmd))
                    {
                        cmd.EnableShaderKeyword("LIGHTLOOP_SINGLE_PASS");
                        cmd.DisableShaderKeyword("LIGHTLOOP_TILE_PASS");
                    }
                }
                else
                {
                    // Only opaques can use FPTL, transparents must use clustered!
                    bool useFptl = renderOpaque && usingFptl;

                    using (new Utilities.ProfilingSample(useFptl ? "Forward Tiled pass" : "Forward Clustered pass", cmd))
                    {
                        // say that we want to use tile of single loop
                        cmd.EnableShaderKeyword("LIGHTLOOP_TILE_PASS");
                        cmd.DisableShaderKeyword("LIGHTLOOP_SINGLE_PASS");
                        cmd.SetGlobalFloat("_UseTileLightList", useFptl ? 1 : 0);      // leaving this as a dynamic toggle for now for forward opaques to keep shader variants down.
                        cmd.SetGlobalBuffer("g_vLightListGlobal", useFptl ? s_LightList : s_PerVoxelLightLists);
                    }
                }
            }

            public void RenderDebugOverlay(Camera camera, CommandBuffer cmd, DebugDisplaySettings debugDisplaySettings, ref float x, ref float y, float overlaySize, float width)
            {
                LightingDebugSettings lightingDebug = debugDisplaySettings.lightingDebugSettings;
                using (new Utilities.ProfilingSample("Display Shadows", cmd))
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
                                m_ShadowMgr.DisplayShadow(cmd, index, i, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue);
                                Utilities.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, camera.pixelWidth);
                            }
                        }
                    }
                    else if (lightingDebug.shadowDebugMode == ShadowMapDebugMode.VisualizeAtlas)
                    {
                        m_ShadowMgr.DisplayShadowMap(cmd, lightingDebug.shadowAtlasIndex, 0, x, y, overlaySize, overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue);
                        Utilities.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, camera.pixelWidth);
                    }
                }
            }
        }
    }
}
