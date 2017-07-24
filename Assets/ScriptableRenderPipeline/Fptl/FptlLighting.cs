using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.Fptl
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

        public ShadowSetup(ShadowInitParameters shadowInit, ShadowSettings shadowSettings, out IShadowManager shadowManager)
        {
            s_ShadowDataBuffer = new ComputeBuffer(k_MaxShadowDataSlots, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShadowData)));
            s_ShadowPayloadBuffer = new ComputeBuffer(k_MaxShadowDataSlots * k_MaxPayloadSlotsPerShadowData, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShadowPayload)));
            ShadowAtlas.AtlasInit atlasInit;
            atlasInit.baseInit.width                  = (uint)shadowInit.shadowAtlasWidth;
            atlasInit.baseInit.height                 = (uint)shadowInit.shadowAtlasHeight;
            atlasInit.baseInit.slices                 = 1;
            atlasInit.baseInit.shadowmapBits          = 32;
            atlasInit.baseInit.shadowmapFormat        = RenderTextureFormat.Shadowmap;
            atlasInit.baseInit.samplerState           = SamplerState.Default();
            atlasInit.baseInit.comparisonSamplerState = ComparisonSamplerState.Default();
            atlasInit.baseInit.clearColor             = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            atlasInit.baseInit.maxPayloadCount        = 0;
            atlasInit.baseInit.shadowSupport          = ShadowmapBase.ShadowSupport.Directional | ShadowmapBase.ShadowSupport.Point | ShadowmapBase.ShadowSupport.Spot;
            atlasInit.shaderKeyword                   = null;

            m_Shadowmaps = new ShadowmapBase[] { new ShadowAtlas(ref atlasInit) };

            ShadowContext.SyncDel syncer = (ShadowContext sc) =>
                {
                    // update buffers
                    uint offset, count;
                    ShadowData[] sds;
                    sc.GetShadowDatas(out sds, out offset, out count);
                    Debug.Assert(offset == 0);
                    s_ShadowDataBuffer.SetData(sds); // unfortunately we can't pass an offset or count to this function
                    ShadowPayload[] payloads;
                    sc.GetPayloads(out payloads, out offset, out count);
                    Debug.Assert(offset == 0);
                    s_ShadowPayloadBuffer.SetData(payloads);
                };

            // binding code. This needs to be in sync with ShadowContext.hlsl
            ShadowContext.BindDel binder = (ShadowContext sc, CommandBuffer cb, ComputeShader computeShader, int computeKernel) =>
                {
                    // bind buffers
                    cb.SetGlobalBuffer("_ShadowDatasExp", s_ShadowDataBuffer);
                    cb.SetGlobalBuffer("_ShadowPayloads", s_ShadowPayloadBuffer);
                    // bind textures
                    uint offset, count;
                    RenderTargetIdentifier[] tex;
                    sc.GetTex2DArrays(out tex, out offset, out count);
                    cb.SetGlobalTexture("_ShadowmapExp_PCF", tex[0]);
                    // TODO: Currently samplers are hard coded in ShadowContext.hlsl, so we can't really set them here
                };

            ShadowContext.CtxtInit scInit;
            scInit.storage.maxShadowDataSlots        = k_MaxShadowDataSlots;
            scInit.storage.maxPayloadSlots           = k_MaxShadowDataSlots * k_MaxPayloadSlotsPerShadowData;
            scInit.storage.maxTex2DArraySlots        = 1;
            scInit.storage.maxTexCubeArraySlots      = 0;
            scInit.storage.maxComparisonSamplerSlots = 1;
            scInit.storage.maxSamplerSlots           = 0;
            scInit.dataSyncer                        = syncer;
            scInit.resourceBinder                    = binder;

            m_ShadowMgr = new ShadowManager(shadowSettings, ref scInit, m_Shadowmaps);
            // set global overrides - these need to match the override specified in ShadowDispatch.hlsl
            m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Point        , ShadowAlgorithm.PCF, ShadowVariant.V1, ShadowPrecision.High, true );
            m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Spot         , ShadowAlgorithm.PCF, ShadowVariant.V1, ShadowPrecision.High, true );
            m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Directional  , ShadowAlgorithm.PCF, ShadowVariant.V1, ShadowPrecision.High, true );
            shadowManager = m_ShadowMgr;
        }
        public void Dispose()
        {
            if (m_Shadowmaps != null)
            {
                (m_Shadowmaps[0] as ShadowAtlas).Dispose();
                m_Shadowmaps = null;
            }
            m_ShadowMgr = null;

            if( s_ShadowDataBuffer != null )
                s_ShadowDataBuffer.Release();
            if( s_ShadowPayloadBuffer != null )
                s_ShadowPayloadBuffer.Release();
        }
    }

    public class FptlLightingInstance : RenderPipeline
    {
        private readonly FptlLighting m_Owner;

        public FptlLightingInstance(FptlLighting owner)
        {
            m_Owner = owner;

            if (m_Owner != null)
                m_Owner.Build();
        }

        public override void Dispose()
        {
            base.Dispose();
            if (m_Owner != null)
                m_Owner.Cleanup();
        }

        public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            base.Render(renderContext, cameras);
            m_Owner.Render(renderContext, cameras);
        }
    }

    [ExecuteInEditMode]
    public class FptlLighting : RenderPipelineAsset
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("RenderPipeline/Create FPTLRenderPipeline")]
        static void CreateRenderLoopFPTL()
        {
            var instance = ScriptableObject.CreateInstance<FptlLighting>();
            UnityEditor.AssetDatabase.CreateAsset(instance, "Assets/ScriptableRenderPipeline/fptl/FPTLRenderPipeline.asset");
            //AssetDatabase.CreateAsset(instance, "Assets/ScriptableRenderPipeline/fptl/FPTLRenderPipeline.asset");
        }

#endif
        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new FptlLightingInstance(this);
        }

        [SerializeField]
        ShadowSettings m_ShadowSettings = new ShadowSettings();
        ShadowSetup    m_ShadowSetup;
        IShadowManager m_ShadowMgr;
        FrameId        m_FrameId = new FrameId();

        List<int>               m_ShadowRequests = new List<int>();
        Dictionary<int, int>    m_ShadowIndices = new Dictionary<int,int>();

        void InitShadowSystem(ShadowSettings shadowSettings)
        {
            m_ShadowSetup = new ShadowSetup(new ShadowInitParameters(), shadowSettings, out m_ShadowMgr);
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


        [SerializeField]
        TextureSettings m_TextureSettings = new TextureSettings();

        public Shader deferredShader;
        public Shader deferredReflectionShader;
        public ComputeShader deferredComputeShader;
        public Shader finalPassShader;
        public Shader debugLightBoundsShader;

        public ComputeShader buildScreenAABBShader;
        public ComputeShader buildPerTileLightListShader;     // FPTL
        public ComputeShader buildPerBigTileLightListShader;

        public ComputeShader buildPerVoxelLightListShader;    // clustered

        private Material m_DeferredMaterial;
        private Material m_DeferredReflectionMaterial;
        private static int s_GBufferAlbedo;
        private static int s_GBufferSpecRough;
        private static int s_GBufferNormal;
        private static int s_GBufferEmission;
        private static int s_GBufferZ;
        private static int s_CameraTarget;
        private static int s_CameraDepthTexture;

        private static int s_GenAABBKernel;
        private static int s_GenListPerTileKernel;
        private static int s_GenListPerVoxelKernel;
        private static int s_ClearVoxelAtomicKernel;
        private static ComputeBuffer s_LightDataBuffer;
        private static ComputeBuffer s_ConvexBoundsBuffer;
        private static ComputeBuffer s_AABBBoundsBuffer;
        private static ComputeBuffer s_LightList;
        private static ComputeBuffer s_DirLightList;

        private static ComputeBuffer s_BigTileLightList;        // used for pre-pass coarse culling on 64x64 tiles
        private static int s_GenListPerBigTileKernel;

        // clustered light list specific buffers and data begin
        public bool enableClustered = false;
        public bool disableFptlWhenClustered = false;    // still useful on opaques
        public bool enableBigTilePrepass = true;
        public bool enableDrawLightBoundsDebug = false;
        public bool enableDrawTileDebug = false;
        public bool enableReflectionProbeDebug = false;
        public bool enableComputeLightEvaluation = false;
        const bool k_UseDepthBuffer = true;//      // only has an impact when EnableClustered is true (requires a depth-prepass)
        const bool k_UseAsyncCompute = true;        // should not use on mobile

        const int k_Log2NumClusters = 6;     // accepted range is from 0 to 6. NumClusters is 1<<g_iLog2NumClusters
        const float k_ClustLogBase = 1.02f;     // each slice 2% bigger than the previous
        float m_ClustScale;
        private static ComputeBuffer s_PerVoxelLightLists;
        private static ComputeBuffer s_PerVoxelOffset;
        private static ComputeBuffer s_PerTileLogBaseTweak;
        private static ComputeBuffer s_GlobalLightListAtomic;
        // clustered light list specific buffers and data end

        private static int s_WidthOnRecord;
        private static int s_HeightOnRecord;

        Matrix4x4[] m_MatWorldToShadow = new Matrix4x4[k_MaxLights * k_MaxShadowmapPerLights];
        Vector4[] m_DirShadowSplitSpheres = new Vector4[k_MaxDirectionalSplit];
        Vector4[] m_Shadow3X3PCFTerms = new Vector4[4];

        public const int MaxNumLights = 1024;
        public const int MaxNumDirLights = 2;
        public const float FltMax = 3.402823466e+38F;

        const int k_MaxLights = 10;
        const int k_MaxShadowmapPerLights = 6;
        const int k_MaxDirectionalSplit = 4;
        // Directional lights become spotlights at a far distance. This is the distance we pull back to set the spotlight origin.
        const float k_DirectionalLightPullbackDistance = 10000.0f;

        [NonSerialized]
        private TextureCache2D m_CookieTexArray;
        private TextureCacheCubemap m_CubeCookieTexArray;
        private TextureCacheCubemap m_CubeReflTexArray;

        private SkyboxHelper m_SkyboxHelper;

        private Material m_BlitMaterial;
        private Material m_DebugLightBoundsMaterial;

        private Texture2D m_NHxRoughnessTexture;
        private Texture2D m_LightAttentuationTexture;
        private int m_shadowBufferID;

        public void Cleanup()
        {
            if (m_DeferredMaterial) DestroyImmediate(m_DeferredMaterial);
            if (m_DeferredReflectionMaterial) DestroyImmediate(m_DeferredReflectionMaterial);
            if (m_BlitMaterial) DestroyImmediate(m_BlitMaterial);
            if (m_DebugLightBoundsMaterial) DestroyImmediate(m_DebugLightBoundsMaterial);
            if (m_NHxRoughnessTexture) DestroyImmediate(m_NHxRoughnessTexture);
            if (m_LightAttentuationTexture) DestroyImmediate(m_LightAttentuationTexture);

            m_CookieTexArray.Release();
            m_CubeCookieTexArray.Release();
            m_CubeReflTexArray.Release();

            s_AABBBoundsBuffer.Release();
            s_ConvexBoundsBuffer.Release();
            s_LightDataBuffer.Release();
            ReleaseResolutionDependentBuffers();
            s_DirLightList.Release();

            if (enableClustered)
            {
                if (s_GlobalLightListAtomic != null)
                    s_GlobalLightListAtomic.Release();
            }

            ClearComputeBuffers();

            DeinitShadowSystem();
        }

        void ClearComputeBuffers()
        {
            if (s_AABBBoundsBuffer != null)
                s_AABBBoundsBuffer.Release();

            if (s_ConvexBoundsBuffer != null)
                s_ConvexBoundsBuffer.Release();

            if (s_LightDataBuffer != null)
                s_LightDataBuffer.Release();

            ReleaseResolutionDependentBuffers();

            if (s_DirLightList != null)
                s_DirLightList.Release();

            if (enableClustered)
            {
                if (s_GlobalLightListAtomic != null)
                    s_GlobalLightListAtomic.Release();
            }
        }

        public void Build()
        {
            s_GBufferAlbedo = Shader.PropertyToID("_CameraGBufferTexture0");
            s_GBufferSpecRough = Shader.PropertyToID("_CameraGBufferTexture1");
            s_GBufferNormal = Shader.PropertyToID("_CameraGBufferTexture2");
            s_GBufferEmission = Shader.PropertyToID("_CameraGBufferTexture3");
            s_GBufferZ = Shader.PropertyToID("_CameraGBufferZ"); // used while rendering into G-buffer+
            s_CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture"); // copy of that for later sampling in shaders
            s_CameraTarget = Shader.PropertyToID("_CameraTarget");

            m_DeferredMaterial = new Material(deferredShader);
            m_DeferredReflectionMaterial = new Material(deferredReflectionShader);
            m_DeferredMaterial.hideFlags = HideFlags.HideAndDontSave;
            m_DeferredReflectionMaterial.hideFlags = HideFlags.HideAndDontSave;

            s_GenAABBKernel = buildScreenAABBShader.FindKernel("ScreenBoundsAABB");
            s_GenListPerTileKernel = buildPerTileLightListShader.FindKernel(enableBigTilePrepass ? "TileLightListGen_SrcBigTile" : "TileLightListGen");
            s_AABBBoundsBuffer = new ComputeBuffer(2 * MaxNumLights, 3 * sizeof(float));
            s_ConvexBoundsBuffer = new ComputeBuffer(MaxNumLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightBound)));
            s_LightDataBuffer = new ComputeBuffer(MaxNumLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SFiniteLightData)));
            s_DirLightList = new ComputeBuffer(MaxNumDirLights, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DirectionalLight)));

            buildScreenAABBShader.SetBuffer(s_GenAABBKernel, "g_data", s_ConvexBoundsBuffer);
            //m_BuildScreenAABBShader.SetBuffer(kGenAABBKernel, "g_vBoundsBuffer", m_aabbBoundsBuffer);
            m_DeferredMaterial.SetBuffer("g_vLightData", s_LightDataBuffer);
            m_DeferredMaterial.SetBuffer("g_dirLightData", s_DirLightList);
            m_DeferredReflectionMaterial.SetBuffer("g_vLightData", s_LightDataBuffer);

            buildPerTileLightListShader.SetBuffer(s_GenListPerTileKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
            buildPerTileLightListShader.SetBuffer(s_GenListPerTileKernel, "g_vLightData", s_LightDataBuffer);
            buildPerTileLightListShader.SetBuffer(s_GenListPerTileKernel, "g_data", s_ConvexBoundsBuffer);

            if (enableClustered)
            {
                var kernelName = enableBigTilePrepass ? (k_UseDepthBuffer ? "TileLightListGen_DepthRT_SrcBigTile" : "TileLightListGen_NoDepthRT_SrcBigTile") : (k_UseDepthBuffer ? "TileLightListGen_DepthRT" : "TileLightListGen_NoDepthRT");
                s_GenListPerVoxelKernel = buildPerVoxelLightListShader.FindKernel(kernelName);
                s_ClearVoxelAtomicKernel = buildPerVoxelLightListShader.FindKernel("ClearAtomic");
                buildPerVoxelLightListShader.SetBuffer(s_GenListPerVoxelKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                buildPerVoxelLightListShader.SetBuffer(s_GenListPerVoxelKernel, "g_vLightData", s_LightDataBuffer);
                buildPerVoxelLightListShader.SetBuffer(s_GenListPerVoxelKernel, "g_data", s_ConvexBoundsBuffer);

                s_GlobalLightListAtomic = new ComputeBuffer(1, sizeof(uint));
            }

            if (enableBigTilePrepass)
            {
                s_GenListPerBigTileKernel = buildPerBigTileLightListShader.FindKernel("BigTileLightListGen");
                buildPerBigTileLightListShader.SetBuffer(s_GenListPerBigTileKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                buildPerBigTileLightListShader.SetBuffer(s_GenListPerBigTileKernel, "g_vLightData", s_LightDataBuffer);
                buildPerBigTileLightListShader.SetBuffer(s_GenListPerBigTileKernel, "g_data", s_ConvexBoundsBuffer);
            }

            m_CookieTexArray = new TextureCache2D();
            m_CubeCookieTexArray = new TextureCacheCubemap();
            m_CubeReflTexArray = new TextureCacheCubemap();
            m_CookieTexArray.AllocTextureArray(8, m_TextureSettings.spotCookieSize, m_TextureSettings.spotCookieSize, TextureFormat.RGBA32, true);
            m_CubeCookieTexArray.AllocTextureArray(4, m_TextureSettings.pointCookieSize, TextureFormat.RGBA32, true);
            m_CubeReflTexArray.AllocTextureArray(64, m_TextureSettings.reflectionCubemapSize, TextureCache.GetPreferredHdrCompressedTextureFormat, true);

            //m_DeferredMaterial.SetTexture("_spotCookieTextures", m_cookieTexArray.GetTexCache());
            //m_DeferredMaterial.SetTexture("_pointCookieTextures", m_cubeCookieTexArray.GetTexCache());
            //m_DeferredReflectionMaterial.SetTexture("_reflCubeTextures", m_cubeReflTexArray.GetTexCache());

            m_MatWorldToShadow = new Matrix4x4[k_MaxLights * k_MaxShadowmapPerLights];
            m_DirShadowSplitSpheres = new Vector4[k_MaxDirectionalSplit];
            m_Shadow3X3PCFTerms = new Vector4[4];
            InitShadowSystem(m_ShadowSettings);

            m_SkyboxHelper = new SkyboxHelper();
            m_SkyboxHelper.CreateMesh();

            m_BlitMaterial = new Material(finalPassShader) { hideFlags = HideFlags.HideAndDontSave };
            m_DebugLightBoundsMaterial = new Material(debugLightBoundsShader) { hideFlags = HideFlags.HideAndDontSave };

            m_NHxRoughnessTexture = GenerateRoughnessTexture();
            m_LightAttentuationTexture = GenerateLightAttenuationTexture();


            s_LightList = null;
            s_BigTileLightList = null;

            m_shadowBufferID = Shader.PropertyToID("g_tShadowBuffer");
        }

        static void SetupGBuffer(int width, int height, CommandBuffer cmd)
        {
            var format10 = RenderTextureFormat.ARGB32;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010))
                format10 = RenderTextureFormat.ARGB2101010;
            var formatHDR = RenderTextureFormat.DefaultHDR;

            //@TODO: cleanup, right now only because we want to use unmodified Standard shader that encodes emission differently based on HDR or not,
            // so we make it think we always render in HDR
            cmd.EnableShaderKeyword("UNITY_HDR_ON");

            //@TODO: GetGraphicsCaps().buggyMRTSRGBWriteFlag
            cmd.GetTemporaryRT(s_GBufferAlbedo, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            cmd.GetTemporaryRT(s_GBufferSpecRough, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            cmd.GetTemporaryRT(s_GBufferNormal, width, height, 0, FilterMode.Point, format10, RenderTextureReadWrite.Linear);
            cmd.GetTemporaryRT(s_GBufferEmission, width, height, 0, FilterMode.Point, formatHDR, RenderTextureReadWrite.Linear);
            cmd.GetTemporaryRT(s_GBufferZ, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.GetTemporaryRT(s_CameraDepthTexture, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.GetTemporaryRT(s_CameraTarget, width, height, 0, FilterMode.Point, formatHDR, RenderTextureReadWrite.Default, 1, true); // rtv/uav

            var colorMRTs = new RenderTargetIdentifier[4] { s_GBufferAlbedo, s_GBufferSpecRough, s_GBufferNormal, s_GBufferEmission };
            cmd.SetRenderTarget(colorMRTs, new RenderTargetIdentifier(s_GBufferZ));
            cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));

            //@TODO: render VR occlusion mesh
        }

        static void RenderGBuffer(CullResults cull, Camera camera, ScriptableRenderContext loop)
        {
            // setup GBuffer for rendering
            var cmd = CommandBufferPool.Get ("Create G-Buffer");
            SetupGBuffer(camera.pixelWidth, camera.pixelHeight, cmd);
            loop.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // render opaque objects using Deferred pass
            var settings = new DrawRendererSettings(cull, camera, new ShaderPassName("Deferred"))
            {
                sorting = { flags = SortFlags.CommonOpaque },
                rendererConfiguration = RendererConfiguration.PerObjectLightmaps
            };

            //@TODO: need to get light probes + LPPV too?
            settings.inputFilter.SetQueuesOpaque();
            settings.rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe;
            loop.DrawRenderers(ref settings);
        }

        void RenderForward(CullResults cull, Camera camera, ScriptableRenderContext loop, bool opaquesOnly)
        {
            var cmd = CommandBufferPool.Get(opaquesOnly ? "Prep Opaques Only Forward Pass" : "Prep Forward Pass" );

            bool useFptl = opaquesOnly && usingFptl;     // requires depth pre-pass for forward opaques!

            bool haveTiledSolution = opaquesOnly || enableClustered;
            cmd.EnableShaderKeyword(haveTiledSolution ? "TILED_FORWARD" : "REGULAR_FORWARD");
            cmd.SetGlobalFloat("g_isOpaquesOnlyEnabled", useFptl ? 1 : 0);      // leaving this as a dynamic toggle for now for forward opaques to keep shader variants down.
            cmd.SetGlobalBuffer("g_vLightListGlobal", useFptl ? s_LightList : s_PerVoxelLightLists);

            loop.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);


            // render opaque objects using Deferred pass
            var settings = new DrawRendererSettings(cull, camera, new ShaderPassName("ForwardSinglePass"))
            {
                sorting = { flags = SortFlags.CommonOpaque }
            };
            settings.rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe;
            if (opaquesOnly) settings.inputFilter.SetQueuesOpaque();
            else settings.inputFilter.SetQueuesTransparent();

            loop.DrawRenderers(ref settings);
        }

        static void DepthOnlyForForwardOpaques(CullResults cull, Camera camera, ScriptableRenderContext loop)
        {
            var cmd = CommandBufferPool.Get("Forward Opaques - Depth Only" );
            cmd.SetRenderTarget(new RenderTargetIdentifier(s_GBufferZ));
            loop.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // render opaque objects using Deferred pass
            var settings = new DrawRendererSettings(cull, camera, new ShaderPassName("DepthOnly"))
            {
                sorting = { flags = SortFlags.CommonOpaque }
            };
            settings.inputFilter.SetQueuesOpaque();
            loop.DrawRenderers(ref settings);
        }

        bool usingFptl
        {
            get
            {
                bool isEnabledMSAA = false;
                Debug.Assert(!isEnabledMSAA || enableClustered);
                bool disableFptl = (disableFptlWhenClustered && enableClustered) || isEnabledMSAA;
                return !disableFptl;
            }
        }

        static void CopyDepthAfterGBuffer(ScriptableRenderContext loop)
        {
            var cmd = CommandBufferPool.Get("Copy depth");
            cmd.CopyTexture(new RenderTargetIdentifier(s_GBufferZ), new RenderTargetIdentifier(s_CameraDepthTexture));
            loop.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }

        void DoTiledDeferredLighting(Camera camera, ScriptableRenderContext loop, int numLights, int numDirLights)
        {
            var bUseClusteredForDeferred = !usingFptl;
            var cmd = CommandBufferPool.Get();

            m_DeferredMaterial.EnableKeyword(bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
            m_DeferredReflectionMaterial.EnableKeyword(bUseClusteredForDeferred ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
            if (enableDrawTileDebug)
            {
                m_DeferredMaterial.EnableKeyword("ENABLE_DEBUG");
            }
            else
            {
                m_DeferredMaterial.DisableKeyword("ENABLE_DEBUG");
            }

            if (enableReflectionProbeDebug)
            {
                m_DeferredReflectionMaterial.EnableKeyword("ENABLE_DEBUG");
            }
            else
            {
                m_DeferredReflectionMaterial.DisableKeyword("ENABLE_DEBUG");
            }

            cmd.SetGlobalBuffer("g_vLightListGlobal", bUseClusteredForDeferred ? s_PerVoxelLightLists : s_LightList);       // opaques list (unless MSAA possibly)

            // In case of bUseClusteredForDeferred disable toggle option since we're using m_perVoxelLightLists as opposed to lightList
            if (bUseClusteredForDeferred)
            {
                cmd.SetGlobalFloat("g_isOpaquesOnlyEnabled", 0);
            }

            cmd.name = "DoTiledDeferredLighting";

            //cmd.SetRenderTarget(new RenderTargetIdentifier(kGBufferEmission), new RenderTargetIdentifier(kGBufferZ));
            //cmd.Blit (kGBufferNormal, (RenderTexture)null); // debug: display normals

            if (enableComputeLightEvaluation)  //TODO: temporary workaround for "All kernels must use same constant buffer layouts"
            {
                var w = camera.pixelWidth;
                var h = camera.pixelHeight;
                var numTilesX = (w + 7) / 8;
                var numTilesY = (h + 7) / 8;

                string kernelName = "ShadeDeferred" + (bUseClusteredForDeferred ? "_Clustered" : "_Fptl") + (enableDrawTileDebug ? "_Debug" : "");
                int kernel = deferredComputeShader.FindKernel(kernelName);

                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_CameraDepthTexture", new RenderTargetIdentifier(s_CameraDepthTexture));
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_CameraGBufferTexture0", new RenderTargetIdentifier(s_GBufferAlbedo));
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_CameraGBufferTexture1", new RenderTargetIdentifier(s_GBufferSpecRough));
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_CameraGBufferTexture2", new RenderTargetIdentifier(s_GBufferNormal));
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_CameraGBufferTexture3", new RenderTargetIdentifier(s_GBufferEmission));
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_spotCookieTextures", m_CookieTexArray.GetTexCache());
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_pointCookieTextures", m_CubeCookieTexArray.GetTexCache());
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_reflCubeTextures", m_CubeReflTexArray.GetTexCache());
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_reflRootCubeTexture", ReflectionProbe.defaultTexture);
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "g_tShadowBuffer", new RenderTargetIdentifier(m_shadowBufferID));
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "unity_NHxRoughness", m_NHxRoughnessTexture);
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "_LightTextureB0", m_LightAttentuationTexture);

                cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_vLightListGlobal", bUseClusteredForDeferred ? s_PerVoxelLightLists : s_LightList);
                cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_vLightData", s_LightDataBuffer);
                cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_dirLightData", s_DirLightList);

                var defdecode = ReflectionProbe.defaultTextureHDRDecodeValues;
                cmd.SetComputeFloatParam(deferredComputeShader, "_reflRootHdrDecodeMult", defdecode.x);
                cmd.SetComputeFloatParam(deferredComputeShader, "_reflRootHdrDecodeExp", defdecode.y);

                cmd.SetComputeFloatParam(deferredComputeShader, "g_fClustScale", m_ClustScale);
                cmd.SetComputeFloatParam(deferredComputeShader, "g_fClustBase", k_ClustLogBase);
                cmd.SetComputeFloatParam(deferredComputeShader, "g_fNearPlane", camera.nearClipPlane);
                cmd.SetComputeFloatParam(deferredComputeShader, "g_fFarPlane", camera.farClipPlane);
                cmd.SetComputeIntParam(deferredComputeShader, "g_iLog2NumClusters", k_Log2NumClusters);
                cmd.SetComputeIntParam(deferredComputeShader, "g_isLogBaseBufferEnabled", k_UseDepthBuffer ? 1 : 0);
                cmd.SetComputeIntParam(deferredComputeShader, "g_isOpaquesOnlyEnabled", 0);


                //
                var proj = camera.projectionMatrix;
                var temp = new Matrix4x4();
                temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                var projh = temp * proj;
                var invProjh = projh.inverse;

                temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
                temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
                temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                var projscr = temp * proj;
                var invProjscr = projscr.inverse;

                cmd.SetComputeIntParam(deferredComputeShader, "g_iNrVisibLights", numLights);
                cmd.SetComputeMatrixParam(deferredComputeShader, "g_mScrProjection", projscr);
                cmd.SetComputeMatrixParam(deferredComputeShader, "g_mInvScrProjection", invProjscr);
                cmd.SetComputeMatrixParam(deferredComputeShader, "g_mViewToWorld", camera.cameraToWorldMatrix);


                if (bUseClusteredForDeferred)
                {
                    cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_vLayeredOffsetsBuffer", s_PerVoxelOffset);
                    if (k_UseDepthBuffer)
                    {
                        cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_logBaseBuffer", s_PerTileLogBaseTweak);
                    }
                }

                cmd.SetComputeIntParam(deferredComputeShader, "g_widthRT", w);
                cmd.SetComputeIntParam(deferredComputeShader, "g_heightRT", h);
                cmd.SetComputeIntParam(deferredComputeShader, "g_nNumDirLights", numDirLights);
                cmd.SetComputeBufferParam(deferredComputeShader, kernel, "g_dirLightData", s_DirLightList);
                cmd.SetComputeTextureParam(deferredComputeShader, kernel, "uavOutput", new RenderTargetIdentifier(s_CameraTarget));

                cmd.SetComputeMatrixArrayParam(deferredComputeShader, "g_matWorldToShadow", m_MatWorldToShadow);
                cmd.SetComputeVectorArrayParam(deferredComputeShader, "g_vDirShadowSplitSpheres", m_DirShadowSplitSpheres);
                cmd.SetComputeVectorParam(deferredComputeShader, "g_vShadow3x3PCFTerms0", m_Shadow3X3PCFTerms[0]);
                cmd.SetComputeVectorParam(deferredComputeShader, "g_vShadow3x3PCFTerms1", m_Shadow3X3PCFTerms[1]);
                cmd.SetComputeVectorParam(deferredComputeShader, "g_vShadow3x3PCFTerms2", m_Shadow3X3PCFTerms[2]);
                cmd.SetComputeVectorParam(deferredComputeShader, "g_vShadow3x3PCFTerms3", m_Shadow3X3PCFTerms[3]);

                cmd.DispatchCompute(deferredComputeShader, kernel, numTilesX, numTilesY, 1);
            }
            else
            {
                cmd.Blit(BuiltinRenderTextureType.CameraTarget, s_CameraTarget, m_DeferredMaterial, 0);
                cmd.Blit(BuiltinRenderTextureType.CameraTarget, s_CameraTarget, m_DeferredReflectionMaterial, 0);
            }


            // Set the intermediate target for compositing (skybox, etc)
            cmd.SetRenderTarget(new RenderTargetIdentifier(s_CameraTarget), new RenderTargetIdentifier(s_CameraDepthTexture));

            loop.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        static Matrix4x4 GetFlipMatrix()
        {
            Matrix4x4 flip = Matrix4x4.identity;
            bool isLeftHand = ((int)LightDefinitions.USE_LEFTHAND_CAMERASPACE) != 0;
            if (isLeftHand) flip.SetColumn(2, new Vector4(0.0f, 0.0f, -1.0f, 0.0f));
            return flip;
        }

        static Matrix4x4 WorldToCamera(Camera camera)
        {
            return GetFlipMatrix() * camera.worldToCameraMatrix;
        }

        static Matrix4x4 CameraToWorld(Camera camera)
        {
            return camera.cameraToWorldMatrix * GetFlipMatrix();
        }

        static Matrix4x4 CameraProjection(Camera camera)
        {
            return camera.projectionMatrix * GetFlipMatrix();
        }

        static int UpdateDirectionalLights(Camera camera, IList<VisibleLight> visibleLights, Dictionary<int,int> shadowIndices)
        {
            var dirLightCount = 0;
            var lights = new List<DirectionalLight>();
            var worldToView = WorldToCamera(camera);

            for (int nLight = 0; nLight < visibleLights.Count; nLight++)
            {
                var light = visibleLights[nLight];
                if (light.lightType == LightType.Directional)
                {
                    Debug.Assert(dirLightCount < MaxNumDirLights, "Too many directional lights.");

                    var l = new DirectionalLight();

                    var lightToWorld = light.localToWorld;

                    Vector3 lightDir = lightToWorld.GetColumn(2);   // Z axis in world space

                    // represents a left hand coordinate system in world space
                    Vector3 vx = lightToWorld.GetColumn(0);     // X axis in world space
                    Vector3 vy = lightToWorld.GetColumn(1);     // Y axis in world space
                    var vz = lightDir;                      // Z axis in world space

                    vx = worldToView.MultiplyVector(vx);
                    vy = worldToView.MultiplyVector(vy);
                    vz = worldToView.MultiplyVector(vz);

                    int shadowIdx;
                    l.shadowLightIndex = shadowIndices.TryGetValue((int)nLight, out shadowIdx) ? (uint)shadowIdx : 0x80000000;
                    l.lightAxisX = vx;
                    l.lightAxisY = vy;
                    l.lightAxisZ = vz;

                    l.color.Set(light.finalColor.r, light.finalColor.g, light.finalColor.b);
                    l.intensity = light.light.intensity;

                    lights.Add(l);
                    dirLightCount++;
                }
            }
            s_DirLightList.SetData(lights);

            return dirLightCount;
        }

        int GenerateSourceLightBuffers(Camera camera, CullResults inputs)
        {
            // 0. deal with shadows
            {
                m_FrameId.frameCount++;
                // get the indices for all lights that want to have shadows
                m_ShadowRequests.Clear();
                m_ShadowRequests.Capacity = inputs.visibleLights.Count;
                int lcnt = inputs.visibleLights.Count;
                for (int i = 0; i < lcnt; ++i)
                {
                    VisibleLight vl = inputs.visibleLights[i];
                    if (vl.light.shadows != LightShadows.None && vl.light.GetComponent<AdditionalShadowData>().shadowDimmer > 0.0f)
                        m_ShadowRequests.Add(i);
                }
                // pass this list to a routine that assigns shadows based on some heuristic
                uint shadowRequestCount = (uint)m_ShadowRequests.Count;
                int[] shadowRequests = m_ShadowRequests.ToArray();
                int[] shadowDataIndices;
                m_ShadowMgr.ProcessShadowRequests(m_FrameId, inputs, camera, false, inputs.visibleLights,
                    ref shadowRequestCount, shadowRequests, out shadowDataIndices);

                // update the visibleLights with the shadow information
                m_ShadowIndices.Clear();
                for (uint i = 0; i < shadowRequestCount; i++)
                {
                    m_ShadowIndices.Add(shadowRequests[i], shadowDataIndices[i]);
                }
            }

            var probes = inputs.visibleReflectionProbes;
            //ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>();

            var numModels = (int)LightDefinitions.NR_LIGHT_MODELS;
            var numVolTypes = (int)LightDefinitions.MAX_TYPES;
            var numEntries = new int[numModels, numVolTypes];
            var offsets = new int[numModels, numVolTypes];
            var numEntries2nd = new int[numModels, numVolTypes];

            // first pass. Figure out how much we have of each and establish offsets
            foreach (var cl in inputs.visibleLights)
            {
                var volType = cl.lightType == LightType.Spot ? LightDefinitions.SPOT_LIGHT : (cl.lightType == LightType.Point ? LightDefinitions.SPHERE_LIGHT : -1);
                if (volType >= 0) ++numEntries[LightDefinitions.DIRECT_LIGHT, volType];
            }

            foreach (var rl in probes)
            {
                var volType = LightDefinitions.BOX_LIGHT;       // always a box for now
                if (rl.texture != null) ++numEntries[LightDefinitions.REFLECTION_LIGHT, volType];
            }

            // add decals here too similar to the above

            // establish offsets
            for (var m = 0; m < numModels; m++)
            {
                offsets[m, 0] = m == 0 ? 0 : (numEntries[m - 1, numVolTypes - 1] + offsets[m - 1, numVolTypes - 1]);
                for (var v = 1; v < numVolTypes; v++) offsets[m, v] = numEntries[m, v - 1] + offsets[m, v - 1];
            }


            var numLights = inputs.visibleLights.Count;
            var numProbes = probes.Count;
            var numVolumes = numLights + numProbes;


            var lightData = new SFiniteLightData[numVolumes];
            var boundData = new SFiniteLightBound[numVolumes];
            var worldToView = WorldToCamera(camera);
            bool isNegDeterminant = Vector3.Dot(worldToView.GetColumn(0), Vector3.Cross(worldToView.GetColumn(1), worldToView.GetColumn(2))) < 0.0f;      // 3x3 Determinant.

            uint shadowLightIndex = 0;
            foreach (var cl in inputs.visibleLights)
            {
                var range = cl.range;

                var lightToWorld = cl.localToWorld;

                Vector3 lightPos = lightToWorld.GetColumn(3);

                var bound = new SFiniteLightBound();
                var light = new SFiniteLightData();

                bound.boxAxisX.Set(1, 0, 0);
                bound.boxAxisY.Set(0, 1, 0);
                bound.boxAxisZ.Set(0, 0, 1);
                bound.scaleXY.Set(1.0f, 1.0f);
                bound.radius = range;

                light.flags = 0;
                light.recipRange = 1.0f / range;
                light.color.Set(cl.finalColor.r, cl.finalColor.g, cl.finalColor.b);
                light.sliceIndex = 0;
                light.lightModel = (uint)LightDefinitions.DIRECT_LIGHT;

                int shadowIdx;
                light.shadowLightIndex = m_ShadowIndices.TryGetValue( (int) shadowLightIndex, out shadowIdx ) ? (uint) shadowIdx : 0x80000000;
                shadowLightIndex++;

                var bHasCookie = cl.light.cookie != null;
                var bHasShadow = cl.light.shadows != LightShadows.None;

                var idxOut = 0;

                if (cl.lightType == LightType.Spot)
                {
                    var isCircularSpot = !bHasCookie;
                    if (!isCircularSpot)    // square spots always have cookie
                    {
                        light.sliceIndex = m_CookieTexArray.FetchSlice(cl.light.cookie);
                    }

                    Vector3 lightDir = lightToWorld.GetColumn(2);   // Z axis in world space

                    // represents a left hand coordinate system in world space
                    Vector3 vx = lightToWorld.GetColumn(0);     // X axis in world space
                    Vector3 vy = lightToWorld.GetColumn(1);     // Y axis in world space
                    var vz = lightDir;                      // Z axis in world space

                    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                    vx = worldToView.MultiplyVector(vx);
                    vy = worldToView.MultiplyVector(vy);
                    vz = worldToView.MultiplyVector(vz);


                    const float pi = 3.1415926535897932384626433832795f;
                    const float degToRad = (float)(pi / 180.0);


                    var sa = cl.light.spotAngle;

                    var cs = Mathf.Cos(0.5f * sa * degToRad);
                    var si = Mathf.Sin(0.5f * sa * degToRad);
                    var ta = cs > 0.0f ? (si / cs) : FltMax;

                    var cota = si > 0.0f ? (cs / si) : FltMax;

                    //const float cotasa = l.GetCotanHalfSpotAngle();

                    // apply nonuniform scale to OBB of spot light
                    var squeeze = true;//sa < 0.7f * 90.0f;      // arb heuristic
                    var fS = squeeze ? ta : si;
                    bound.center = worldToView.MultiplyPoint(lightPos + ((0.5f * range) * lightDir));    // use mid point of the spot as the center of the bounding volume for building screen-space AABB for tiled lighting.

                    light.lightAxisX = vx;
                    light.lightAxisY = vy;
                    light.lightAxisZ = vz;

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

                    var altDist = Mathf.Sqrt(fAltDy * fAltDy + (isCircularSpot ? 1.0f : 2.0f) * fAltDx * fAltDx);
                    bound.radius = altDist > (0.5f * range) ? altDist : (0.5f * range);       // will always pick fAltDist
                    bound.scaleXY = squeeze ? new Vector2(0.01f, 0.01f) : new Vector2(1.0f, 1.0f);

                    // fill up ldata
                    light.lightType = (uint)LightDefinitions.SPOT_LIGHT;
                    light.lightPos = worldToView.MultiplyPoint(lightPos);
                    light.radiusSq = range * range;
                    light.penumbra = cs;
                    light.cotan = cota;
                    light.flags |= (isCircularSpot ? LightDefinitions.IS_CIRCULAR_SPOT_SHAPE : 0);

                    light.flags |= (bHasCookie ? LightDefinitions.HAS_COOKIE_TEXTURE : 0);
                    light.flags |= (bHasShadow ? LightDefinitions.HAS_SHADOW : 0);

                    int i = LightDefinitions.DIRECT_LIGHT, j = LightDefinitions.SPOT_LIGHT;
                    idxOut = numEntries2nd[i, j] + offsets[i, j]; ++numEntries2nd[i, j];
                }
                else if (cl.lightType == LightType.Point)
                {
                    if (bHasCookie)
                    {
                        light.sliceIndex = m_CubeCookieTexArray.FetchSlice(cl.light.cookie);
                    }

                    bound.center = worldToView.MultiplyPoint(lightPos);
                    bound.boxAxisX.Set(range, 0, 0);
                    bound.boxAxisY.Set(0, range, 0);
                    bound.boxAxisZ.Set(0, 0, isNegDeterminant ? (-range) : range);    // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                    bound.scaleXY.Set(1.0f, 1.0f);
                    bound.radius = range;

                    // represents a left hand coordinate system in world space since det(worldToView)<0
                    var lightToView = worldToView * lightToWorld;
                    Vector3 vx = lightToView.GetColumn(0);
                    Vector3 vy = lightToView.GetColumn(1);
                    Vector3 vz = lightToView.GetColumn(2);

                    // fill up ldata
                    light.lightType = (uint)LightDefinitions.SPHERE_LIGHT;
                    light.lightPos = bound.center;
                    light.radiusSq = range * range;

                    light.lightAxisX = vx;
                    light.lightAxisY = vy;
                    light.lightAxisZ = vz;

                    light.flags |= (bHasCookie ? LightDefinitions.HAS_COOKIE_TEXTURE : 0);
                    light.flags |= (bHasShadow ? LightDefinitions.HAS_SHADOW : 0);

                    int i = LightDefinitions.DIRECT_LIGHT, j = LightDefinitions.SPHERE_LIGHT;
                    idxOut = numEntries2nd[i, j] + offsets[i, j]; ++numEntries2nd[i, j];
                }
                else
                {
                    //Assert(false);
                }

                // next light
                if (cl.lightType == LightType.Spot || cl.lightType == LightType.Point)
                {
                    boundData[idxOut] = bound;
                    lightData[idxOut] = light;
                }
            }
            int numLightsOut = 0;
            for(int v=0; v<numVolTypes; v++) numLightsOut += numEntries[LightDefinitions.DIRECT_LIGHT, v];

            // probe.m_BlendDistance
            // Vector3f extents = 0.5*Abs(probe.m_BoxSize);
            // C center of rendered refl box <-- GetComponent (Transform).GetPosition() + m_BoxOffset;
            // cube map capture point: GetComponent (Transform).GetPosition()
            // shader parameter min and max are C+/-(extents+blendDistance)
            foreach (var rl in probes)
            {
                var cubemap = rl.texture;

                // always a box for now
                if (cubemap == null)
                    continue;

                var bndData = new SFiniteLightBound();
                var lgtData = new SFiniteLightData();

                var idxOut = 0;
                lgtData.flags = 0;

                var bnds = rl.bounds;
                var boxOffset = rl.center;                  // reflection volume offset relative to cube map capture point
                var blendDistance = rl.blendDistance;

                var mat = rl.localToWorld;

                // implicit in CalculateHDRDecodeValues() --> float ints = rl.intensity;
                var boxProj = (rl.boxProjection != 0);
                var decodeVals = rl.hdr;
                //Vector4 decodeVals = rl.CalculateHDRDecodeValues();

                // C is reflection volume center in world space (NOT same as cube map capture point)
                var e = bnds.extents;       // 0.5f * Vector3.Max(-boxSizes[p], boxSizes[p]);
                //Vector3 C = bnds.center;        // P + boxOffset;
                var C = mat.MultiplyPoint(boxOffset);       // same as commented out line above when rot is identity

                var combinedExtent = e + new Vector3(blendDistance, blendDistance, blendDistance);

                Vector3 vx = mat.GetColumn(0);
                Vector3 vy = mat.GetColumn(1);
                Vector3 vz = mat.GetColumn(2);

                // transform to camera space (becomes a left hand coordinate frame in Unity since Determinant(worldToView)<0)
                vx = worldToView.MultiplyVector(vx);
                vy = worldToView.MultiplyVector(vy);
                vz = worldToView.MultiplyVector(vz);

                var Cw = worldToView.MultiplyPoint(C);

                if (boxProj) lgtData.flags |= LightDefinitions.IS_BOX_PROJECTED;

                lgtData.lightPos = Cw;
                lgtData.lightAxisX = vx;
                lgtData.lightAxisY = vy;
                lgtData.lightAxisZ = vz;
                lgtData.localCubeCapturePoint = -boxOffset;
                lgtData.probeBlendDistance = blendDistance;

                lgtData.lightIntensity = decodeVals.x;
                lgtData.decodeExp = decodeVals.y;

                lgtData.sliceIndex = m_CubeReflTexArray.FetchSlice(cubemap);

                var delta = combinedExtent - e;
                lgtData.boxInnerDist = e;
                lgtData.boxInvRange.Set(1.0f / delta.x, 1.0f / delta.y, 1.0f / delta.z);

                bndData.center = Cw;
                bndData.boxAxisX = combinedExtent.x * vx;
                bndData.boxAxisY = combinedExtent.y * vy;
                bndData.boxAxisZ = combinedExtent.z * vz;
                bndData.scaleXY.Set(1.0f, 1.0f);
                bndData.radius = combinedExtent.magnitude;

                // fill up ldata
                lgtData.lightType = (uint)LightDefinitions.BOX_LIGHT;
                lgtData.lightModel = (uint)LightDefinitions.REFLECTION_LIGHT;


                int i = LightDefinitions.REFLECTION_LIGHT, j = LightDefinitions.BOX_LIGHT;
                idxOut = numEntries2nd[i, j] + offsets[i, j]; ++numEntries2nd[i, j];
                boundData[idxOut] = bndData;
                lightData[idxOut] = lgtData;
            }

            int numProbesOut = 0;
            for(int v=0; v<numVolTypes; v++) numProbesOut += numEntries[LightDefinitions.REFLECTION_LIGHT, v];

            for (var m = 0; m < numModels; m++)
            {
                for (var v = 0; v < numVolTypes; v++)
                    Debug.Assert(numEntries[m, v] == numEntries2nd[m, v], "count mismatch on second pass!");
            }

            s_ConvexBoundsBuffer.SetData(boundData);
            s_LightDataBuffer.SetData(lightData);


            return numLightsOut + numProbesOut;
        }

        CullResults m_CullResults;
        public void Render(ScriptableRenderContext renderContext, IEnumerable<Camera> cameras)
        {
            foreach (var camera in cameras)
            {
                ScriptableCullingParameters cullingParams;
                if (!CullResults.GetCullingParameters(camera, out cullingParams))
                    continue;

                m_ShadowMgr.UpdateCullingParameters( ref cullingParams );

                CullResults.Cull(ref cullingParams, renderContext, ref m_CullResults);
                ExecuteRenderLoop(camera, m_CullResults, renderContext);
            }

            renderContext.Submit();
        }

        void FinalPass(ScriptableRenderContext loop)
        {
            var cmd = CommandBufferPool.Get("FinalPass");
            cmd.Blit(s_CameraTarget, BuiltinRenderTextureType.CameraTarget, m_BlitMaterial, 0);
            loop.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }

        void ExecuteRenderLoop(Camera camera, CullResults cullResults, ScriptableRenderContext loop)
        {
            var w = camera.pixelWidth;
            var h = camera.pixelHeight;

            ResizeIfNecessary(w, h);

            // do anything we need to do upon a new frame.
            NewFrame();

            // generate g-buffer before shadows to leverage async compute
            // forward opaques just write to depth.
            loop.SetupCameraProperties(camera);
            RenderGBuffer(cullResults, camera, loop);
            DepthOnlyForForwardOpaques(cullResults, camera, loop);
            CopyDepthAfterGBuffer(loop);

            // camera to screen matrix (and it's inverse)
            var proj = CameraProjection(camera);
            var temp = new Matrix4x4();
            temp.SetRow(0, new Vector4(0.5f * w, 0.0f, 0.0f, 0.5f * w));
            temp.SetRow(1, new Vector4(0.0f, 0.5f * h, 0.0f, 0.5f * h));
            temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
            temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            var projscr = temp * proj;
            var invProjscr = projscr.inverse;


            // build per tile light lists
            var numLights = GenerateSourceLightBuffers(camera, cullResults);
            BuildPerTileLightLists(camera, loop, numLights, projscr, invProjscr);

            CommandBuffer cmdShadow = CommandBufferPool.Get();
            m_ShadowMgr.RenderShadows( m_FrameId, loop, cmdShadow, cullResults, cullResults.visibleLights );
            m_ShadowMgr.SyncData();
            m_ShadowMgr.BindResources( cmdShadow, null, 0 );
            loop.ExecuteCommandBuffer(cmdShadow);
            CommandBufferPool.Release(cmdShadow);

            // Push all global params
            var numDirLights = UpdateDirectionalLights(camera, cullResults.visibleLights, m_ShadowIndices);
            PushGlobalParams(camera, loop, CameraToWorld(camera), projscr, invProjscr, numDirLights);

            // do deferred lighting
            DoTiledDeferredLighting(camera, loop, numLights, numDirLights);

            // render opaques using tiled forward
            RenderForward(cullResults, camera, loop, true);    // opaques only (requires a depth pre-pass)

            // render the backdrop/canvas
            m_SkyboxHelper.Draw(loop, camera);

            // transparencies atm. requires clustered until we get traditional forward
            if (enableClustered) RenderForward(cullResults, camera, loop, false);

            // debug views.
            if (enableDrawLightBoundsDebug) DrawLightBoundsDebug(loop, cullResults.visibleLights.Count);

            // present frame buffer.
            FinalPass(loop);

            // bind depth surface for editor grid/gizmo/selection rendering
            if (camera.cameraType == CameraType.SceneView)
            {
                var cmd = CommandBufferPool.Get();
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, new RenderTargetIdentifier(s_CameraDepthTexture));
                loop.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

            }
            loop.Submit();
        }

        void DrawLightBoundsDebug(ScriptableRenderContext loop, int numLights)
        {
            var cmd = CommandBufferPool.Get("DrawLightBoundsDebug");
            m_DebugLightBoundsMaterial.SetBuffer("g_data", s_ConvexBoundsBuffer);
            cmd.DrawProcedural(Matrix4x4.identity, m_DebugLightBoundsMaterial, 0, MeshTopology.Triangles, 12 * 3 * numLights);
            loop.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

        }

        void NewFrame()
        {
            // update texture caches
            m_CookieTexArray.NewFrame();
            m_CubeCookieTexArray.NewFrame();
            m_CubeReflTexArray.NewFrame();
        }

        void RenderShadowMaps(CullResults cullResults, ScriptableRenderContext loop)
        {
        }

        void ResizeIfNecessary(int curWidth, int curHeight)
        {
            if (curWidth != s_WidthOnRecord || curHeight != s_HeightOnRecord || s_LightList == null ||
                (s_BigTileLightList == null && enableBigTilePrepass) || (s_PerVoxelLightLists == null && enableClustered))
            {
                if (s_WidthOnRecord > 0 && s_HeightOnRecord > 0)
                    ReleaseResolutionDependentBuffers();

                AllocResolutionDependentBuffers(curWidth, curHeight);

                // update recorded window resolution
                s_WidthOnRecord = curWidth;
                s_HeightOnRecord = curHeight;
            }
        }

        void ReleaseResolutionDependentBuffers()
        {
            if (s_LightList != null)
                s_LightList.Release();

            if (enableClustered)
            {
                if (s_PerVoxelLightLists != null)
                    s_PerVoxelLightLists.Release();

                if (s_PerVoxelOffset != null)
                    s_PerVoxelOffset.Release();

                if (k_UseDepthBuffer && s_PerTileLogBaseTweak != null)
                    s_PerTileLogBaseTweak.Release();
            }

            if (enableBigTilePrepass)
            {
                if (s_BigTileLightList != null) s_BigTileLightList.Release();
            }
        }

        int NumLightIndicesPerClusteredTile()
        {
            return 8 * (1 << k_Log2NumClusters);       // total footprint for all layers of the tile (measured in light index entries)
        }

        void AllocResolutionDependentBuffers(int width, int height)
        {
            var nrTilesX = (width + 15) / 16;
            var nrTilesY = (height + 15) / 16;
            var nrTiles = nrTilesX * nrTilesY;
            const int capacityUShortsPerTile = 32;
            const int dwordsPerTile = (capacityUShortsPerTile + 1) >> 1;        // room for 31 lights and a nrLights value.

            s_LightList = new ComputeBuffer(LightDefinitions.NR_LIGHT_MODELS * dwordsPerTile * nrTiles, sizeof(uint));       // enough list memory for a 4k x 4k display

            if (enableClustered)
            {
                var tileSizeClust = LightDefinitions.TILE_SIZE_CLUSTERED;
                var nrTilesClustX = (width + (tileSizeClust - 1)) / tileSizeClust;
                var nrTilesClustY = (height + (tileSizeClust - 1)) / tileSizeClust;
                var nrTilesClust = nrTilesClustX * nrTilesClustY;

                s_PerVoxelOffset = new ComputeBuffer(LightDefinitions.NR_LIGHT_MODELS * (1 << k_Log2NumClusters) * nrTilesClust, sizeof(uint));
                s_PerVoxelLightLists = new ComputeBuffer(NumLightIndicesPerClusteredTile() * nrTilesClust, sizeof(uint));

                if (k_UseDepthBuffer)
                {
                    s_PerTileLogBaseTweak = new ComputeBuffer(nrTilesClust, sizeof(float));
                }
            }

            if (enableBigTilePrepass)
            {
                var nrBigTilesX = (width + 63) / 64;
                var nrBigTilesY = (height + 63) / 64;
                var nrBigTiles = nrBigTilesX * nrBigTilesY;
                s_BigTileLightList = new ComputeBuffer(LightDefinitions.MAX_NR_BIGTILE_LIGHTS_PLUSONE * nrBigTiles, sizeof(uint));
            }
        }

        void VoxelLightListGeneration(CommandBuffer cmd, Camera camera, int numLights, Matrix4x4 projscr, Matrix4x4 invProjscr)
        {
            // clear atomic offset index
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, "g_LayeredSingleIdxBuffer", s_GlobalLightListAtomic);
            cmd.DispatchCompute(buildPerVoxelLightListShader, s_ClearVoxelAtomicKernel, 1, 1, 1);

            bool isOrthographic = camera.orthographic;
            cmd.SetComputeIntParam(buildPerVoxelLightListShader, "g_isOrthographic", isOrthographic ? 1 : 0);
            cmd.SetComputeIntParam(buildPerVoxelLightListShader, "g_iNrVisibLights", numLights);
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

            cmd.SetComputeTextureParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_depth_tex", new RenderTargetIdentifier(s_CameraDepthTexture));
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_vLayeredLightList", s_PerVoxelLightLists);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_LayeredOffset", s_PerVoxelOffset);
            cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_LayeredSingleIdxBuffer", s_GlobalLightListAtomic);
            if (enableBigTilePrepass) cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_vBigTileLightList", s_BigTileLightList);

            if (k_UseDepthBuffer)
            {
                cmd.SetComputeBufferParam(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, "g_logBaseBuffer", s_PerTileLogBaseTweak);
            }

            var tileSizeClust = LightDefinitions.TILE_SIZE_CLUSTERED;
            var nrTilesClustX = (camera.pixelWidth + (tileSizeClust - 1)) / tileSizeClust;
            var nrTilesClustY = (camera.pixelHeight + (tileSizeClust - 1)) / tileSizeClust;

            cmd.DispatchCompute(buildPerVoxelLightListShader, s_GenListPerVoxelKernel, nrTilesClustX, nrTilesClustY, 1);
        }

        void BuildPerTileLightLists(Camera camera, ScriptableRenderContext loop, int numLights, Matrix4x4 projscr, Matrix4x4 invProjscr)
        {
            var w = camera.pixelWidth;
            var h = camera.pixelHeight;
            var numTilesX = (w + 15) / 16;
            var numTilesY = (h + 15) / 16;
            var numBigTilesX = (w + 63) / 64;
            var numBigTilesY = (h + 63) / 64;

            var cmd = CommandBufferPool.Get("Build light list" );

            bool isOrthographic = camera.orthographic;

            // generate screen-space AABBs (used for both fptl and clustered).
            if (numLights != 0)
            {
                var proj = CameraProjection(camera);
                var temp = new Matrix4x4();
                temp.SetRow(0, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                temp.SetRow(1, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
                temp.SetRow(2, new Vector4(0.0f, 0.0f, 0.5f, 0.5f));
                temp.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                var projh = temp * proj;
                var invProjh = projh.inverse;

                cmd.SetComputeIntParam(buildScreenAABBShader, "g_isOrthographic", isOrthographic ? 1 : 0);
                cmd.SetComputeIntParam(buildScreenAABBShader, "g_iNrVisibLights", numLights);
                cmd.SetComputeMatrixParam(buildScreenAABBShader, "g_mProjection", projh);
                cmd.SetComputeMatrixParam(buildScreenAABBShader, "g_mInvProjection", invProjh);
                cmd.SetComputeBufferParam(buildScreenAABBShader, s_GenAABBKernel, "g_vBoundsBuffer", s_AABBBoundsBuffer);
                cmd.DispatchCompute(buildScreenAABBShader, s_GenAABBKernel, (numLights + 7) / 8, 1, 1);
            }

            // enable coarse 2D pass on 64x64 tiles (used for both fptl and clustered).
            if (enableBigTilePrepass)
            {
                cmd.SetComputeIntParam(buildPerBigTileLightListShader, "g_isOrthographic", isOrthographic ? 1 : 0);
                cmd.SetComputeIntParams(buildPerBigTileLightListShader, "g_viDimensions", new int[2] { w, h });
                cmd.SetComputeIntParam(buildPerBigTileLightListShader, "g_iNrVisibLights", numLights);
                cmd.SetComputeMatrixParam(buildPerBigTileLightListShader, "g_mScrProjection", projscr);
                cmd.SetComputeMatrixParam(buildPerBigTileLightListShader, "g_mInvScrProjection", invProjscr);
                cmd.SetComputeFloatParam(buildPerBigTileLightListShader, "g_fNearPlane", camera.nearClipPlane);
                cmd.SetComputeFloatParam(buildPerBigTileLightListShader, "g_fFarPlane", camera.farClipPlane);
                cmd.SetComputeBufferParam(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, "g_vLightList", s_BigTileLightList);
                cmd.DispatchCompute(buildPerBigTileLightListShader, s_GenListPerBigTileKernel, numBigTilesX, numBigTilesY, 1);
            }

            if (usingFptl)        // optimized for opaques only
            {
                cmd.SetComputeIntParam(buildPerTileLightListShader, "g_isOrthographic", isOrthographic ? 1 : 0);
                cmd.SetComputeIntParams(buildPerTileLightListShader, "g_viDimensions", new int[2] { w, h });
                cmd.SetComputeIntParam(buildPerTileLightListShader, "g_iNrVisibLights", numLights);
                cmd.SetComputeMatrixParam(buildPerTileLightListShader, "g_mScrProjection", projscr);
                cmd.SetComputeMatrixParam(buildPerTileLightListShader, "g_mInvScrProjection", invProjscr);
                cmd.SetComputeTextureParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_depth_tex", new RenderTargetIdentifier(s_CameraDepthTexture));
                cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_vLightList", s_LightList);
                if (enableBigTilePrepass) cmd.SetComputeBufferParam(buildPerTileLightListShader, s_GenListPerTileKernel, "g_vBigTileLightList", s_BigTileLightList);
                cmd.DispatchCompute(buildPerTileLightListShader, s_GenListPerTileKernel, numTilesX, numTilesY, 1);
            }

            if (enableClustered)        // works for transparencies too.
            {
                VoxelLightListGeneration(cmd, camera, numLights, projscr, invProjscr);
            }

            loop.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void PushGlobalParams(Camera camera, ScriptableRenderContext loop, Matrix4x4 viewToWorld, Matrix4x4 scrProj, Matrix4x4 incScrProj, int numDirLights)
        {
            var cmd = CommandBufferPool.Get("Push Global Parameters");
                  
            bool isOrthographic = camera.orthographic;
            cmd.SetGlobalFloat("g_isOrthographic", (float) (isOrthographic ? 1 : 0));
            cmd.SetGlobalFloat("g_widthRT", (float)camera.pixelWidth);
            cmd.SetGlobalFloat("g_heightRT", (float)camera.pixelHeight);

            cmd.SetGlobalMatrix("g_mViewToWorld", viewToWorld);
            cmd.SetGlobalMatrix("g_mWorldToView", viewToWorld.inverse);
            cmd.SetGlobalMatrix("g_mScrProjection", scrProj);
            cmd.SetGlobalMatrix("g_mInvScrProjection", incScrProj);

            cmd.SetGlobalBuffer("g_vLightData", s_LightDataBuffer);

            cmd.SetGlobalTexture("_spotCookieTextures", m_CookieTexArray.GetTexCache());
            cmd.SetGlobalTexture("_pointCookieTextures", m_CubeCookieTexArray.GetTexCache());
            cmd.SetGlobalTexture("_reflCubeTextures", m_CubeReflTexArray.GetTexCache());

            var topCube = ReflectionProbe.defaultTexture;
            var defdecode = ReflectionProbe.defaultTextureHDRDecodeValues;
            cmd.SetGlobalTexture("_reflRootCubeTexture", topCube);
            cmd.SetGlobalFloat("_reflRootHdrDecodeMult", defdecode.x);
            cmd.SetGlobalFloat("_reflRootHdrDecodeExp", defdecode.y);

            if (enableBigTilePrepass)
                cmd.SetGlobalBuffer("g_vBigTileLightList", s_BigTileLightList);

            if (enableClustered)
            {
                cmd.SetGlobalFloat("g_fClustScale", m_ClustScale);
                cmd.SetGlobalFloat("g_fClustBase", k_ClustLogBase);
                cmd.SetGlobalFloat("g_fNearPlane", camera.nearClipPlane);
                cmd.SetGlobalFloat("g_fFarPlane", camera.farClipPlane);
                cmd.SetGlobalFloat("g_iLog2NumClusters", k_Log2NumClusters);


                cmd.SetGlobalFloat("g_isLogBaseBufferEnabled", k_UseDepthBuffer ? 1 : 0);

                cmd.SetGlobalBuffer("g_vLayeredOffsetsBuffer", s_PerVoxelOffset);
                if (k_UseDepthBuffer)
                {
                    cmd.SetGlobalBuffer("g_logBaseBuffer", s_PerTileLogBaseTweak);
                }
            }

            cmd.SetGlobalFloat("g_nNumDirLights", numDirLights);
            cmd.SetGlobalBuffer("g_dirLightData", s_DirLightList);

            // Shadow constants
            cmd.SetGlobalMatrixArray("g_matWorldToShadow", m_MatWorldToShadow);
            cmd.SetGlobalVectorArray("g_vDirShadowSplitSpheres", m_DirShadowSplitSpheres);
            cmd.SetGlobalVector("g_vShadow3x3PCFTerms0", m_Shadow3X3PCFTerms[0]);
            cmd.SetGlobalVector("g_vShadow3x3PCFTerms1", m_Shadow3X3PCFTerms[1]);
            cmd.SetGlobalVector("g_vShadow3x3PCFTerms2", m_Shadow3X3PCFTerms[2]);
            cmd.SetGlobalVector("g_vShadow3x3PCFTerms3", m_Shadow3X3PCFTerms[3]);

            loop.ExecuteCommandBuffer(cmd);

        }

        private float PerceptualRoughnessToBlinnPhongPower(float perceptualRoughness)
        {
#pragma warning disable 162 // warning CS0162: Unreachable code detected
            // There is two code here, by default the code corresponding for UNITY_GLOSS_MATCHES_MARMOSET_TOOLBAG2 was use for cloud reasons
            // The other code (not marmoset) is not matching the shader code for cloud reasons.
            // As none of this solution match BRDF 1 or 2, I let the Marmoset code to avoid to break current test. But ideally, all this should be rewrite to match BRDF1
            if (true)
            {
                // from https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html
                float n = 10.0f / Mathf.Log((1.0f - perceptualRoughness) * 0.968f + 0.03f) / Mathf.Log(2.0f);

                return n * n;
            }
            else
            {
                // NOTE: another approximate approach to match Marmoset gloss curve is to
                // multiply roughness by 0.7599 in the code below (makes SpecPower range 4..N instead of 1..N)
                const float UNITY_SPECCUBE_LOD_EXPONENT = 1.5f;

                float m = Mathf.Pow(perceptualRoughness, 2.0f * UNITY_SPECCUBE_LOD_EXPONENT) + 1e-4f;
                // follow the same curve as unity_SpecCube
                float n = (2.0f / m) - 2.0f;                                            // https://dl.dropbox.com/u/55891920/papers/mm_brdf.pdf
                n = Mathf.Max(n, 1.0e-5f);                                              // prevent possible cases of pow(0,0), which could happen when roughness is 1.0 and NdotH is zero

                return n;
            }
#pragma warning restore 162
        }

        private float PerceptualRoughnessToPhongPower(float perceptualRoughness)
        {
            return PerceptualRoughnessToBlinnPhongPower(perceptualRoughness) * 0.25f;
        }

        private float PhongNormalizedTerm(float NdotH, float n)
        {
            // Normalization for Phong when used as RDF (outside a micro-facet model)
            // http://www.thetenthplanet.de/archives/255
            float normTerm = (n + 2.0f) / (2.0f * Mathf.PI);
            float specTerm = Mathf.Pow(NdotH, n);
            return specTerm * normTerm;
        }

        private float EvalNHxRoughness(int x, int y, int maxX, int maxY)
        {
            // both R.L or N.H (cosine) are not linear and approach 1.0 very quickly
            // since we want more resolution closer to where highlight is (close to 1)
            // we warp LUT across horizontal axis
            // NOTE: warp function ^4 or ^5 can be executed in the same instruction as Shlick fresnel approximation (handy for SM2.0 platforms with <=64 instr. limit)
            const float kHorizontalWarpExp = 4.0f;
            float rdotl = Mathf.Pow(((float)x) / ((float)maxX - 1.0f), 1.0f / kHorizontalWarpExp);
            float perceptualRoughness = ((float)y) / ((float)maxY - .5f);
            float specTerm = PhongNormalizedTerm(rdotl, PerceptualRoughnessToPhongPower(perceptualRoughness));

            // Lookup table values are evaluated in Linear space
            // but converted and stored as sRGB to support low-end platforms

            float range = Mathf.GammaToLinearSpace(16.0f);
            float val = Mathf.Clamp01(specTerm / range);    // store in sRGB range of [0..16]
                                                            // OKish range to 'counteract' multiplication by N.L (as in BRDF*N.L)
                                                            // while retaining bright specular spot at both grazing and incident angles
                                                            // and allows some precision in case if AlphaLum16 is not supported
            val = Mathf.LinearToGammaSpace(val);

            // As there is not enough resolution in LUT for tiny highlights,
            // fadeout intensity of the highlight when roughness approaches 0 and N.H approaches 1
            // Prevents from overly big bright highlight on mirror surfaces
            const float fadeOutPerceptualRoughness = .05f;
            bool lastHorizontalPixel = (x >= maxX - 1); // highlights are on the right-side of LUT
            if (perceptualRoughness <= fadeOutPerceptualRoughness && lastHorizontalPixel)
                val *= perceptualRoughness / fadeOutPerceptualRoughness;
            return val;
        }

        private Texture2D GenerateRoughnessTexture()
        {
            const int width = 256;
            const int height = 64;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);   //TODO: no alpha16 support?
            Color[] pixels = new Color[height * width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value = EvalNHxRoughness(x, y, width, height);
                    pixels[y * width + x] = new Color(value, value, value, value);    //TODO: set them in one go
                }
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }

        private const float kConstantFac = 1.000f;
        private const float kQuadraticFac = 25.0f;
        private const float kToZeroFadeStart = 0.8f * 0.8f;

        private float CalculateLightQuadFac(float range)
        {
            return kQuadraticFac / (range * range);
        }

        private float LightAttenuateNormalized(float distSqr)
        {
            // match the vertex lighting falloff
            float atten = 1 / (kConstantFac + CalculateLightQuadFac(1.0f) * distSqr);

            // ...but vertex one does not falloff to zero at light's range;
            // So force it to falloff to zero at the edges.
            if (distSqr >= kToZeroFadeStart)
            {
                if (distSqr > 1)
                    atten = 0;
                else
                    atten *= 1 - (distSqr - kToZeroFadeStart) / (1 - kToZeroFadeStart);
            }

            return atten;
        }

        private float EvalLightAttenuation(int x, int maxX)
        {
            float sqrRange = (float)x / (float)maxX;
            return LightAttenuateNormalized(sqrRange);
        }

        private Texture2D GenerateLightAttenuationTexture()
        {
            const int width = 1024;

            Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBA32, false, true);   //TODO: no alpha16 support?
            Color[] pixels = new Color[width];

            for (int x = 0; x < width; x++)
            {
                float value = EvalLightAttenuation(x, width);
                pixels[x] = new Color(value, value, value, value);
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }
    }
}
