using System.Collections.Generic;
using UnityEngine.Rendering;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Experimental.GlobalIllumination;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    public partial class HDRenderPipeline : UnityEngine.Rendering.RenderPipeline
    {

        public const string k_ShaderTagName = "HDRenderPipeline";

        readonly HDRenderPipelineAsset m_Asset;
        public HDRenderPipelineAsset asset { get { return m_Asset; } }

        public RenderPipelineSettings currentPlatformRenderPipelineSettings { get { return m_Asset.currentPlatformRenderPipelineSettings; } }

        readonly RenderPipelineMaterial m_DeferredMaterial;
        readonly List<RenderPipelineMaterial> m_MaterialList = new List<RenderPipelineMaterial>();

        readonly GBufferManager m_GbufferManager;
        readonly DBufferManager m_DbufferManager;
        readonly SharedRTManager m_SharedRTManager = new SharedRTManager();
        readonly PostProcessSystem m_PostProcessSystem;
        readonly XRSystem m_XRSystem = new XRSystem();

        public bool frameSettingsHistoryEnabled = false;

#if ENABLE_RAYTRACING
        public HDRaytracingManager m_RayTracingManager = new HDRaytracingManager();
        readonly HDRaytracingReflections m_RaytracingReflections = new HDRaytracingReflections();
        readonly HDRaytracingRenderer m_RaytracingRenderer = new HDRaytracingRenderer();
        readonly HDRaytracingIndirectDiffuse m_RaytracingIndirectDiffuse = new HDRaytracingIndirectDiffuse();
        public float GetRaysPerFrame(RayCountManager.RayCountValues rayValues) { return m_RayTracingManager.rayCountManager.GetRaysPerFrame(rayValues); }
#endif

        // Renderer Bake configuration can vary depends on if shadow mask is enabled or no
        PerObjectData m_CurrentRendererConfigurationBakedLighting = HDUtils.k_RendererConfigurationBakedLighting;
        MaterialPropertyBlock m_CopyDepthPropertyBlock = new MaterialPropertyBlock();
        Material m_CopyDepth;
        Material m_DownsampleDepthMaterial;
        Material m_UpsampleTransparency;
        GPUCopy m_GPUCopy;
        MipGenerator m_MipGenerator;
        BlueNoise m_BlueNoise;

        IBLFilterBSDF[] m_IBLFilterArray = null;

        ComputeShader m_ScreenSpaceReflectionsCS { get { return m_Asset.renderPipelineResources.shaders.screenSpaceReflectionsCS; } }
        int m_SsrTracingKernel      = -1;
        int m_SsrReprojectionKernel = -1;

        Material m_ApplyDistortionMaterial;

        Material m_CameraMotionVectorsMaterial;
        Material m_DecalNormalBufferMaterial;

        // Debug material
        Material m_DebugViewMaterialGBuffer;
        Material m_DebugViewMaterialGBufferShadowMask;
        Material m_currentDebugViewMaterialGBuffer;
        Material m_DebugDisplayLatlong;
        Material m_DebugFullScreen;
        MaterialPropertyBlock m_DebugFullScreenPropertyBlock = new MaterialPropertyBlock();
        Material m_DebugColorPicker;
        Material m_ErrorMaterial;

        Material m_Blit;
        Material m_BlitTexArray;
        MaterialPropertyBlock m_BlitPropertyBlock = new MaterialPropertyBlock();


        RenderTargetIdentifier[] m_MRTCache2 = new RenderTargetIdentifier[2];

        // 'm_CameraColorBuffer' does not contain diffuse lighting of SSS materials until the SSS pass. It is stored within 'm_CameraSssDiffuseLightingBuffer'.
        RTHandle m_CameraColorBuffer;
        RTHandle m_CameraSssDiffuseLightingBuffer;

        RTHandle m_ContactShadowBuffer;
        RTHandle m_DistortionBuffer;

        RTHandle m_LowResTransparentBuffer;

        // TODO: remove me, I am just a temporary debug texture. :-)
        // RTHandle m_SsrDebugTexture;
        RTHandle m_SsrHitPointTexture;
        RTHandle m_SsrLightingTexture;
        // MSAA Versions of regular textures
        RTHandle m_CameraColorMSAABuffer;
        RTHandle m_CameraSssDiffuseLightingMSAABuffer;

        // The current MSAA count
        MSAASamples m_MSAASamples;

        // The pass "SRPDefaultUnlit" is a fall back to legacy unlit rendering and is required to support unity 2d + unity UI that render in the scene.
        ShaderTagId[] m_ForwardAndForwardOnlyPassNames = { HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_ForwardName, HDShaderPassNames.s_SRPDefaultUnlitName };
        ShaderTagId[] m_ForwardOnlyPassNames = { HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderTagId[] m_AllTransparentPassNames = {  HDShaderPassNames.s_TransparentBackfaceName,
                                                        HDShaderPassNames.s_ForwardOnlyName,
                                                        HDShaderPassNames.s_ForwardName,
                                                        HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderTagId[] m_TransparentNoBackfaceNames = {  HDShaderPassNames.s_ForwardOnlyName,
                                                        HDShaderPassNames.s_ForwardName,
                                                        HDShaderPassNames.s_SRPDefaultUnlitName };


        ShaderTagId[] m_AllForwardOpaquePassNames = {    HDShaderPassNames.s_ForwardOnlyName,
                                                            HDShaderPassNames.s_ForwardName,
                                                            HDShaderPassNames.s_SRPDefaultUnlitName };

        ShaderTagId[] m_DepthOnlyAndDepthForwardOnlyPassNames = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
        ShaderTagId[] m_DepthForwardOnlyPassNames = { HDShaderPassNames.s_DepthForwardOnlyName };
        ShaderTagId[] m_DepthOnlyPassNames = { HDShaderPassNames.s_DepthOnlyName };
        ShaderTagId[] m_TransparentDepthPrepassNames = { HDShaderPassNames.s_TransparentDepthPrepassName };
        ShaderTagId[] m_TransparentDepthPostpassNames = { HDShaderPassNames.s_TransparentDepthPostpassName };
        ShaderTagId[] m_ForwardErrorPassNames = { HDShaderPassNames.s_AlwaysName, HDShaderPassNames.s_ForwardBaseName, HDShaderPassNames.s_DeferredName, HDShaderPassNames.s_PrepassBaseName, HDShaderPassNames.s_VertexName, HDShaderPassNames.s_VertexLMRGBMName, HDShaderPassNames.s_VertexLMName };
        ShaderTagId[] m_DecalsEmissivePassNames = { HDShaderPassNames.s_MeshDecalsForwardEmissiveName, HDShaderPassNames.s_ShaderGraphMeshDecalsForwardEmissiveName };
        ShaderTagId[] m_SinglePassName = new ShaderTagId[1];

        // Stencil usage in HDRenderPipeline.
        // Currently we use only 2 bits to identify the kind of lighting that is expected from the render pipeline
        // Usage is define in LightDefinitions.cs
        [Flags]
        public enum StencilBitMask
        {
            Clear                           = 0,    // 0x0
            LightingMask                    = 3,    // 0x7  - 2 bit - Lifetime: GBuffer/Forward - SSSSS
            // Free slot 4
            // Note: If required, the usage Decals / DecalsForwardOutputNormalBuffer could be fit at same location as LightingMask as they have a non overlapped lifetime
            Decals                          = 8,    // 0x8  - 1 bit - Lifetime: DBuffer - Patch normal buffer   (This bit is cleared to 0 after Patch normal buffer)
            DecalsForwardOutputNormalBuffer = 16,   // 0x10 - 1 bit - Lifetime: DBuffer - Patch normal buffer   (This bit is cleared to 0 after Patch normal buffer)
            DoesntReceiveSSR                = 32,   // 0x20 - 1 bit - Lifetime: DethPrepass - SSR
            DistortionVectors               = 64,   // 0x40 - 1 bit - Lifetime: Accumulate distortion - Apply distortion (This bit is cleared to 0 after Apply distortion pass)
            SMAA                            = 64,   // 0x40 - 1 bit - Lifetime: SMAA EdgeDetection - SMAA BlendWeight.
            ObjectMotionVectors             = 128,  // 0x80 - 1 bit - Lifetime: Object motion vector pass - Camera motion vector (This bit is cleared to 0 after Camera motion vector pass)
            All                             = 255   // 0xFF - 8 bit
        }

        RenderStateBlock m_DepthStateOpaque;

        // Detect when windows size is changing
        int m_CurrentWidth;
        int m_CurrentHeight;

        // Use to detect frame changes
        int m_FrameCount;
        float m_LastTime, m_Time;

        public GraphicsFormat GetColorBufferFormat()
        {
            return (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.colorBufferFormat;
        }
        public int GetDecalAtlasMipCount()
        {
            int highestDim = Math.Max(currentPlatformRenderPipelineSettings.decalSettings.atlasWidth, currentPlatformRenderPipelineSettings.decalSettings.atlasHeight);
            return (int)Math.Log(highestDim, 2);
        }

        public int GetMaxScreenSpaceShadows()
        {
            return currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows ? currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadows : 0;
        }

        readonly SkyManager m_SkyManager = new SkyManager();
        readonly AmbientOcclusionSystem m_AmbientOcclusionSystem;

        // Debugging
        MaterialPropertyBlock m_SharedPropertyBlock = new MaterialPropertyBlock();
        DebugDisplaySettings m_DebugDisplaySettings = new DebugDisplaySettings();
        public DebugDisplaySettings debugDisplaySettings { get { return m_DebugDisplaySettings; } }
        static DebugDisplaySettings s_NeutralDebugDisplaySettings = new DebugDisplaySettings();
        internal DebugDisplaySettings m_CurrentDebugDisplaySettings;
        RTHandle                        m_DebugColorPickerBuffer;
        RTHandle                        m_DebugFullScreenTempBuffer;
        // This target is only used in Dev builds as an intermediate destination for post process and where debug rendering will be done.
        RTHandle                        m_IntermediateAfterPostProcessBuffer;
        bool                            m_FullScreenDebugPushed;
        bool                            m_ValidAPI; // False by default mean we render normally, true mean we don't render anything
        bool                            m_IsDepthBufferCopyValid;
        RenderTexture                   m_TemporaryTargetForCubemaps;
        Stack<Camera>                   m_ProbeCameraPool = new Stack<Camera>();

        RenderTargetIdentifier[] m_MRTTransparentMotionVec;
        RenderTargetIdentifier[] m_MRTWithSSS = new RenderTargetIdentifier[3]; // Specular, diffuse, sss buffer;
        RenderTargetIdentifier[] mMRTSingle = new RenderTargetIdentifier[1];
        string m_ForwardPassProfileName;

        Vector2Int m_PyramidSizeV2I = new Vector2Int();
        Vector4 m_PyramidSizeV4F = new Vector4();
        Vector4 m_PyramidScaleLod = new Vector4();
        Vector4 m_PyramidScale = new Vector4();

        public Material GetBlitMaterial(bool useTexArray) { return useTexArray ? m_BlitTexArray : m_Blit; }

        ComputeBuffer m_DepthPyramidMipLevelOffsetsBuffer = null;

        ScriptableCullingParameters frozenCullingParams;
        bool frozenCullingParamAvailable = false;

        public bool showCascade
        {
            get => m_CurrentDebugDisplaySettings.GetDebugLightingMode() == DebugLightingMode.VisualizeCascade;
            set
            {
                if (value)
                    m_CurrentDebugDisplaySettings.SetDebugLightingMode(DebugLightingMode.VisualizeCascade);
                else
                    m_CurrentDebugDisplaySettings.SetDebugLightingMode(DebugLightingMode.None);
            }
        }

        public HDRenderPipeline(HDRenderPipelineAsset asset)
        {
            m_Asset = asset;
            HDProbeSystem.Parameters = asset.reflectionSystemParameters;

            DebugManager.instance.RefreshEditor();

            m_ValidAPI = true;

            if (!SetRenderingFeatures())
            {
                m_ValidAPI = false;

                return;
            }

#if UNITY_EDITOR
            // The first thing we need to do is to set the defines that depend on the render pipeline settings
            m_Asset.EvaluateSettings();

            UpgradeResourcesIfNeeded();

            ValidateResources();
#endif

            // Initial state of the RTHandle system.
            // Tells the system that we will require MSAA or not so that we can avoid wasteful render texture allocation.
            // TODO: Might want to initialize to at least the window resolution to avoid un-necessary re-alloc in the player
            RTHandles.Initialize(1, 1, m_Asset.currentPlatformRenderPipelineSettings.supportMSAA, m_Asset.currentPlatformRenderPipelineSettings.msaaSampleCount);

            m_GPUCopy = new GPUCopy(asset.renderPipelineResources.shaders.copyChannelCS);

            m_MipGenerator = new MipGenerator(m_Asset);
            m_BlueNoise = new BlueNoise(m_Asset);

            EncodeBC6H.DefaultInstance = EncodeBC6H.DefaultInstance ?? new EncodeBC6H(asset.renderPipelineResources.shaders.encodeBC6HCS);

            // Scan material list and assign it
            m_MaterialList = HDUtils.GetRenderPipelineMaterialList();
            // Find first material that have non 0 Gbuffer count and assign it as deferredMaterial
            m_DeferredMaterial = null;
            foreach (var material in m_MaterialList)
            {
                if (material.IsDefferedMaterial())
                    m_DeferredMaterial = material;
            }

            // TODO: Handle the case of no Gbuffer material
            // TODO: I comment the assert here because m_DeferredMaterial for whatever reasons contain the correct class but with a "null" in the name instead of the real name and then trigger the assert
            // whereas it work. Don't know what is happening, DebugDisplay use the same code and name is correct there.
            // Debug.Assert(m_DeferredMaterial != null);

            m_GbufferManager = new GBufferManager(asset, m_DeferredMaterial);
            m_DbufferManager = new DBufferManager();

            m_SharedRTManager.Build(asset);
            m_PostProcessSystem = new PostProcessSystem(asset);
            m_AmbientOcclusionSystem = new AmbientOcclusionSystem(asset);

            // Initialize various compute shader resources
            m_SsrTracingKernel      = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsTracing");
            m_SsrReprojectionKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsReprojection");

            // General material
            m_CameraMotionVectorsMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.cameraMotionVectorsPS);
            m_DecalNormalBufferMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.decalNormalBufferPS);

            m_CopyDepth = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.copyDepthBufferPS);
            m_DownsampleDepthMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.downsampleDepthPS);
            m_UpsampleTransparency = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.upsampleTransparentPS);

            m_ApplyDistortionMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.applyDistortionPS);

            InitializeDebugMaterials();
            XRDebugMenu.Reset();

            m_MaterialList.ForEach(material => material.Build(asset));

            if (m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.supportFabricConvolution)
            {
                m_IBLFilterArray = new IBLFilterBSDF[2];
                m_IBLFilterArray[0] = new IBLFilterGGX(asset.renderPipelineResources, m_MipGenerator);
                m_IBLFilterArray[1] = new IBLFilterCharlie(asset.renderPipelineResources, m_MipGenerator);
            }
            else
            {
                m_IBLFilterArray = new IBLFilterBSDF[1];
                m_IBLFilterArray[0] = new IBLFilterGGX(asset.renderPipelineResources, m_MipGenerator);
            }

            InitializeLightLoop(m_IBLFilterArray);

            m_SkyManager.Build(asset, m_IBLFilterArray);

            InitializeVolumetricLighting();
            InitializeSubsurfaceScattering();

            m_DebugDisplaySettings.RegisterDebug();
#if UNITY_EDITOR
            // We don't need the debug of Scene View at runtime (each camera have its own debug settings)
            // All scene view will share the same FrameSettings for now as sometimes Dispose is called after
            // another instance of HDRenderPipeline constructor is called.

            Camera firstSceneViewCamera = UnityEditor.SceneView.sceneViews.Count > 0 ? (UnityEditor.SceneView.sceneViews[0] as UnityEditor.SceneView).camera : null;
            if (firstSceneViewCamera != null && !FrameSettingsHistory.isRegisteredSceneViewCamera(firstSceneViewCamera))
            {
                var history = FrameSettingsHistory.RegisterDebug(firstSceneViewCamera, null);
                DebugManager.instance.RegisterData(history);
            }
#endif

            m_DepthPyramidMipLevelOffsetsBuffer = new ComputeBuffer(15, sizeof(int) * 2);

            InitializeRenderTextures();

            // For debugging
            MousePositionDebug.instance.Build();

            InitializeRenderStateBlocks();

            // Keep track of the original msaa sample value
            // TODO : Bind this directly to the debug menu instead of having an intermediate value
            m_MSAASamples = m_Asset ? m_Asset.currentPlatformRenderPipelineSettings.msaaSampleCount : MSAASamples.None;

            // Propagate it to the debug menu
            m_DebugDisplaySettings.data.msaaSamples = m_MSAASamples;

            m_MRTTransparentMotionVec = new RenderTargetIdentifier[2];
#if ENABLE_RAYTRACING
            m_RayTracingManager.Init(m_Asset.currentPlatformRenderPipelineSettings, m_Asset.renderPipelineResources, m_Asset.renderPipelineRayTracingResources, m_BlueNoise, this, m_SharedRTManager, m_DebugDisplaySettings);
            m_RaytracingReflections.Init(m_Asset, m_SkyManager, m_RayTracingManager, m_SharedRTManager, m_GbufferManager);
            m_RaytracingRenderer.Init(m_Asset, m_SkyManager, m_RayTracingManager, m_SharedRTManager);
            m_AmbientOcclusionSystem.InitRaytracing(m_RayTracingManager, m_SharedRTManager);
            m_RaytracingIndirectDiffuse.Init(m_Asset, m_SkyManager, m_RayTracingManager, m_SharedRTManager, m_GbufferManager);
#endif

            // Initialize screen space shadows
            InitializeScreenSpaceShadows();

            CameraCaptureBridge.enabled = true;
        }

#if UNITY_EDITOR
        void UpgradeResourcesIfNeeded()
        {
            // The first thing we need to do is to set the defines that depend on the render pipeline settings
            m_Asset.EvaluateSettings();

            // Check that the serialized Resources are not broken
            if ((GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineResources == null)
                (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineResources
                    = UnityEditor.AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineResources.asset");
			ResourceReloader.ReloadAllNullIn((GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineResources, HDUtils.GetHDRenderPipelinePath());

#if ENABLE_RAYTRACING
            if ((GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineRayTracingResources == null)
                (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineRayTracingResources
                    = UnityEditor.AssetDatabase.LoadAssetAtPath<HDRenderPipelineRayTracingResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineRayTracingResources.asset");
            ResourceReloader.ReloadAllNullIn((GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineRayTracingResources, HDUtils.GetHDRenderPipelinePath());
#endif

            if ((GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineEditorResources == null)
                (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineEditorResources
                    = UnityEditor.AssetDatabase.LoadAssetAtPath<HDRenderPipelineEditorResources>(HDUtils.GetHDRenderPipelinePath() + "Editor/RenderPipelineResources/HDRenderPipelineEditorResources.asset");
            ResourceReloader.ReloadAllNullIn((GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineEditorResources, HDUtils.GetHDRenderPipelinePath());

            // Upgrade the resources (re-import every references in RenderPipelineResources) if the resource version mismatches
            // It's done here because we know every HDRP assets have been imported before
            (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineResources?.UpgradeIfNeeded();
        }

        void ValidateResources()
        {
            var resources = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineResources;

            // We iterate over all compute shader to verify if they are all compiled, if it's not the case
            // then we throw an exception to avoid allocating resources and crashing later on by using a null
            // compute kernel.
            foreach (var computeShader in resources.shaders.GetAllComputeShaders())
            {
                foreach (var message in UnityEditor.ShaderUtil.GetComputeShaderMessages(computeShader))
                {
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    {
                        // Will be catched by the try in HDRenderPipelineAsset.CreatePipeline()
                        throw new Exception(String.Format(
                            "Compute Shader compilation error on platform {0} in file {1}:{2}: {3}{4}\n" +
                            "HDRP will not run until the error is fixed.\n",
                            message.platform, message.file, message.line, message.message, message.messageDetails
                        ));
                    }
                }
            }
        }

#endif

        void InitializeRenderTextures()
        {
            RenderPipelineSettings settings = m_Asset.currentPlatformRenderPipelineSettings;

            if (settings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly)
                m_GbufferManager.CreateBuffers();

            if (settings.supportDecals)
                m_DbufferManager.CreateBuffers();

            InitSSSBuffers();
            m_SharedRTManager.InitSharedBuffers(m_GbufferManager, m_Asset.currentPlatformRenderPipelineSettings, m_Asset.renderPipelineResources);

            m_CameraColorBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GetColorBufferFormat(), enableRandomWrite: true, useMipMap: false, useDynamicScale: true, name: "CameraColor");
            m_CameraSssDiffuseLightingBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite: true, useDynamicScale: true, name: "CameraSSSDiffuseLighting");

            m_DistortionBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: Builtin.GetDistortionBufferFormat(), useDynamicScale: true, name: "Distortion");

            m_ContactShadowBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R32_UInt, enableRandomWrite: true, useDynamicScale: true, name: "ContactShadowsBuffer");

            if (m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings.enabled)
            {
                // We need R16G16B16A16_SFloat as we need a proper alpha channel for compositing.
                m_LowResTransparentBuffer = RTHandles.Alloc(Vector2.one * 0.5f, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, name: "Low res transparent");
            }

            if (settings.supportSSR)
            {
                // m_SsrDebugTexture    = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: RenderTextureFormat.ARGBFloat, sRGB: false, enableRandomWrite: true, useDynamicScale: true, name: "SSR_Debug_Texture");
                m_SsrHitPointTexture = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R16G16_UNorm, enableRandomWrite: true, useDynamicScale: true, name: "SSR_Hit_Point_Texture");
                m_SsrLightingTexture = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, name: "SSR_Lighting_Texture");
            }

            if (Debug.isDebugBuild)
            {
                m_DebugColorPickerBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, useDynamicScale: true, name: "DebugColorPicker");
                m_DebugFullScreenTempBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, useDynamicScale: true, name: "DebugFullScreen");
                m_IntermediateAfterPostProcessBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GetColorBufferFormat(), useDynamicScale: true, name: "AfterPostProcess"); // Needs to be FP16 because output target might be HDR
            }

            // Let's create the MSAA textures
            if (m_Asset.currentPlatformRenderPipelineSettings.supportMSAA)
            {
                m_CameraColorMSAABuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GetColorBufferFormat(), bindTextureMS: true, enableMSAA: true, useDynamicScale: true, name: "CameraColorMSAA");
                m_CameraSssDiffuseLightingMSAABuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GetColorBufferFormat(), bindTextureMS: true, enableMSAA: true, useDynamicScale: true, name: "CameraSSSDiffuseLightingMSAA");
            }
        }

        void DestroyRenderTextures()
        {
            m_GbufferManager.DestroyBuffers();
            m_DbufferManager.DestroyBuffers();
            m_MipGenerator.Release();
            m_XRSystem.ClearAll();

            RTHandles.Release(m_CameraColorBuffer);
            RTHandles.Release(m_CameraSssDiffuseLightingBuffer);

            RTHandles.Release(m_DistortionBuffer);
            RTHandles.Release(m_ContactShadowBuffer);

            RTHandles.Release(m_LowResTransparentBuffer);

            // RTHandles.Release(m_SsrDebugTexture);
            RTHandles.Release(m_SsrHitPointTexture);
            RTHandles.Release(m_SsrLightingTexture);

            RTHandles.Release(m_DebugColorPickerBuffer);
            RTHandles.Release(m_DebugFullScreenTempBuffer);
            RTHandles.Release(m_IntermediateAfterPostProcessBuffer);

            RTHandles.Release(m_CameraColorMSAABuffer);
            RTHandles.Release(m_CameraSssDiffuseLightingMSAABuffer);
        }

        bool SetRenderingFeatures()
        {
            // Set sub-shader pipeline tag
            Shader.globalRenderPipeline = "HDRenderPipeline";

            // HD use specific GraphicsSettings
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;

            GraphicsSettings.useScriptableRenderPipelineBatching = m_Asset.enableSRPBatcher;

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.Rotation,
                defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly,
                mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly | SupportedRenderingFeatures.LightmapMixedBakeModes.Shadowmask,
                lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime,
                lightmapsModes = LightmapsMode.NonDirectional | LightmapsMode.CombinedDirectional,
                lightProbeProxyVolumes = true,
                motionVectors = true,
                receiveShadows = false,
                reflectionProbes = true,
                rendererPriority = true,
                overridesEnvironmentLighting = true,
                overridesFog = true,
                overridesOtherLightingSettings = true,
                editableMaterialRenderQueue = false
            };

            Lightmapping.SetDelegate(GlobalIlluminationUtils.hdLightsDelegate);

#if UNITY_EDITOR
            SceneViewDrawMode.SetupDrawMode();

            if (UnityEditor.PlayerSettings.colorSpace == ColorSpace.Gamma)
            {
                Debug.LogError("High Definition Render Pipeline doesn't support Gamma mode, change to Linear mode");
            }
#endif

            GraphicsDeviceType unsupportedDeviceType;
            if (!IsSupportedPlatform(out unsupportedDeviceType))
            {
                CoreUtils.DisplayUnsupportedAPIMessage(unsupportedDeviceType.ToString());

                // Display more information to the users when it should have use Metal instead of OpenGL
                if (SystemInfo.graphicsDeviceType.ToString().StartsWith("OpenGL"))
                {
                    if (SystemInfo.operatingSystem.StartsWith("Mac"))
                        CoreUtils.DisplayUnsupportedMessage("Use Metal API instead.");
                    else if (SystemInfo.operatingSystem.StartsWith("Windows"))
                        CoreUtils.DisplayUnsupportedMessage("Use Vulkan API instead.");
                }

                return false;
            }

            return true;
        }

        // Note: If you add new platform in this function, think about adding support when building the player to in HDRPCustomBuildProcessor.cs
        bool IsSupportedPlatform(out GraphicsDeviceType unsupportedGraphicDevice)
        {
            unsupportedGraphicDevice = SystemInfo.graphicsDeviceType;

            if (!SystemInfo.supportsComputeShaders)
                return false;

#if UNITY_EDITOR
            UnityEditor.BuildTarget activeBuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            // If the build target matches the operating system of the editor
            if (SystemInfo.operatingSystemFamily == HDUtils.BuildTargetToOperatingSystemFamily(activeBuildTarget))
            {
                bool autoAPI = UnityEditor.PlayerSettings.GetUseDefaultGraphicsAPIs(activeBuildTarget);

                // then, there is two configuration possible:
                if (autoAPI)
                {
                    // if the graphic api is chosen automatically, then only the system's graphic device type matters
                    if (!HDUtils.IsSupportedGraphicDevice(SystemInfo.graphicsDeviceType))
                        return false;
                }
                else
                {
                    // otherwise, we need to iterate over every graphic api available in the list to track every non-supported APIs
                    return HDUtils.AreGraphicsAPIsSupported(activeBuildTarget, out unsupportedGraphicDevice);
                }
            }
            else // if the build target does not match the editor OS, then we have to check using the graphic api list
            {
                return HDUtils.AreGraphicsAPIsSupported(activeBuildTarget, out unsupportedGraphicDevice);
            }

            if (!HDUtils.IsSupportedBuildTarget(activeBuildTarget))
                return false;
#else
            if (!HDUtils.IsSupportedGraphicDevice(SystemInfo.graphicsDeviceType))
                return false;
#endif

            if (!HDUtils.IsOperatingSystemSupported(SystemInfo.operatingSystem))
                return false;

            return true;
        }

        void UnsetRenderingFeatures()
        {
            Shader.globalRenderPipeline = "";

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

            // Reset srp batcher state just in case
            GraphicsSettings.useScriptableRenderPipelineBatching = false;

            Lightmapping.ResetDelegate();
        }

        void InitializeDebugMaterials()
        {
            m_DebugViewMaterialGBuffer = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.debugViewMaterialGBufferPS);
            m_DebugViewMaterialGBufferShadowMask = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.debugViewMaterialGBufferPS);
            m_DebugViewMaterialGBufferShadowMask.EnableKeyword("SHADOWS_SHADOWMASK");
            m_DebugDisplayLatlong = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.debugDisplayLatlongPS);
            m_DebugFullScreen = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.debugFullScreenPS);
            m_DebugColorPicker = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.debugColorPickerPS);
            m_Blit = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.blitPS);
            m_ErrorMaterial = CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader");

            // With texture array enabled, we still need the normal blit version for other systems like atlas
            if (TextureXR.useTexArray)
            {
                m_Blit.EnableKeyword("DISABLE_TEXTURE2D_X_ARRAY");
                m_BlitTexArray = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.blitPS);
        }
        }

        void InitializeRenderStateBlocks()
        {
            m_DepthStateOpaque = new RenderStateBlock
            {
                depthState = new DepthState(true, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };
        }

        protected override void Dispose(bool disposing)
        {
            DisposeProbeCameraPool();

            UnsetRenderingFeatures();

            if (!m_ValidAPI)
                return;

            base.Dispose(disposing);

            ReleaseScreenSpaceShadows();

#if ENABLE_RAYTRACING
            m_RaytracingIndirectDiffuse.Release();
            m_RaytracingRenderer.Release();
            m_RaytracingReflections.Release();
            m_RayTracingManager.Release();
#endif
            m_DebugDisplaySettings.UnregisterDebug();

            CleanupLightLoop();

            // For debugging
            MousePositionDebug.instance.Cleanup();

            DecalSystem.instance.Cleanup();

            m_MaterialList.ForEach(material => material.Cleanup());

            CoreUtils.Destroy(m_CameraMotionVectorsMaterial);
            CoreUtils.Destroy(m_DecalNormalBufferMaterial);

            CoreUtils.Destroy(m_DebugViewMaterialGBuffer);
            CoreUtils.Destroy(m_DebugViewMaterialGBufferShadowMask);
            CoreUtils.Destroy(m_DebugDisplayLatlong);
            CoreUtils.Destroy(m_DebugFullScreen);
            CoreUtils.Destroy(m_DebugColorPicker);
            CoreUtils.Destroy(m_Blit);
            CoreUtils.Destroy(m_BlitTexArray);
            CoreUtils.Destroy(m_CopyDepth);
            CoreUtils.Destroy(m_ErrorMaterial);
            CoreUtils.Destroy(m_DownsampleDepthMaterial);
            CoreUtils.Destroy(m_UpsampleTransparency);

            CleanupSubsurfaceScattering();
            m_SharedRTManager.Cleanup();
            m_SkyManager.Cleanup();
            CleanupVolumetricLighting();

            for(int bsdfIdx = 0; bsdfIdx < m_IBLFilterArray.Length; ++bsdfIdx)
            {
                m_IBLFilterArray[bsdfIdx].Cleanup();
            }

            m_PostProcessSystem.Cleanup();
            m_AmbientOcclusionSystem.Cleanup();
            m_BlueNoise.Cleanup();

            HDCamera.ClearAll();

            DestroyRenderTextures();
            CullingGroupManager.instance.Cleanup();

            CoreUtils.SafeRelease(m_DepthPyramidMipLevelOffsetsBuffer);

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();

            // Do not attempt to unregister SceneView FrameSettings. It is shared amongst every scene view and take only a little place.
            // For removing it, you should be sure that Dispose could never be called after the constructor of another instance of this SRP.
            // Also, at the moment, applying change to hdrpAsset cause the SRP to be Disposed and Constructed again.
            // Not always in that order.
#endif

            // Dispose m_ProbeCameraPool properly
            void DisposeProbeCameraPool()
            {
#if UNITY_EDITOR
                // Special case here: when the HDRP asset is modified in the Editor,
                //   it is disposed during an `OnValidate` call.
                //   But during `OnValidate` call, game object must not be destroyed.
                //   So, only when this method was called during an `OnValidate` call, the destruction of the
                //   pool is delayed, otherwise, it is destroyed as usual with `CoreUtiles.Destroy`
                var isInOnValidate = false;
                isInOnValidate = new StackTrace().ToString().Contains("OnValidate");
                if (isInOnValidate)
                {
                    var pool = m_ProbeCameraPool;
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        while (pool.Count > 0)
                            CoreUtils.Destroy(pool.Pop().gameObject);
                    };
                    m_ProbeCameraPool = null;
                }
                else
                {
#endif
                    while (m_ProbeCameraPool.Count > 0)
                        CoreUtils.Destroy(m_ProbeCameraPool.Pop().gameObject);
#if UNITY_EDITOR
                }
#endif
            }

            CameraCaptureBridge.enabled = false;
        }


        void Resize(HDCamera hdCamera)
        {
            bool resolutionChanged = (hdCamera.actualWidth != m_CurrentWidth) || (hdCamera.actualHeight != m_CurrentHeight);

            if (resolutionChanged || LightLoopNeedResize(hdCamera, m_TileAndClusterData))
            {
                if (m_CurrentWidth > 0 && m_CurrentHeight > 0)
                    LightLoopReleaseResolutionDependentBuffers();

                LightLoopAllocResolutionDependentBuffers(hdCamera);
            }

            // update recorded window resolution
            m_CurrentWidth = hdCamera.actualWidth;
            m_CurrentHeight = hdCamera.actualHeight;
        }

        public void PushGlobalParams(HDCamera hdCamera, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "Push Global Parameters", CustomSamplerId.PushGlobalParameters.GetSampler()))
            {
                // Set up UnityPerFrame CBuffer.
                PushSubsurfaceScatteringGlobalParams(hdCamera, cmd);

                m_DbufferManager.PushGlobalParams(hdCamera, cmd);

                PushVolumetricLightingGlobalParams(hdCamera, cmd, m_FrameCount);

                var ssRefraction = VolumeManager.instance.stack.GetComponent<ScreenSpaceRefraction>()
                    ?? ScreenSpaceRefraction.@default;
                ssRefraction.PushShaderParameters(cmd);

                // Set up UnityPerView CBuffer.
                hdCamera.SetupGlobalParams(cmd, m_Time, m_LastTime, m_FrameCount);

                cmd.SetGlobalVector(HDShaderIDs._IndirectLightingMultiplier, new Vector4(VolumeManager.instance.stack.GetComponent<IndirectLightingController>().indirectDiffuseIntensity.value, 0, 0, 0));

                // It will be overridden for transparent pass.
                cmd.SetGlobalInt(HDShaderIDs._ColorMaskTransparentVel, (int)UnityEngine.Rendering.ColorWriteMask.All);

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors))
                {
                    var buf = m_SharedRTManager.GetMotionVectorsBuffer();

                    cmd.SetGlobalTexture(HDShaderIDs._CameraMotionVectorsTexture, buf);
                    cmd.SetGlobalVector( HDShaderIDs._CameraMotionVectorsSize, new Vector4(buf.referenceSize.x,
                                                                                           buf.referenceSize.y,
                                                                                           1.0f / buf.referenceSize.x,
                                                                                           1.0f / buf.referenceSize.y));
                    cmd.SetGlobalVector(HDShaderIDs._CameraMotionVectorsScale, new Vector4(buf.referenceSize.x / (float)buf.rt.width,
                                                                                           buf.referenceSize.y / (float)buf.rt.height));
                }
                else
                {
                    cmd.SetGlobalTexture(HDShaderIDs._CameraMotionVectorsTexture, TextureXR.GetBlackTexture());
                }

                // Light loop stuff...
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR))
                    cmd.SetGlobalTexture(HDShaderIDs._SsrLightingTexture, m_SsrLightingTexture);
                else
                    cmd.SetGlobalTexture(HDShaderIDs._SsrLightingTexture, TextureXR.GetClearTexture());

                // Off screen rendering is disabled for most of the frame by default.
                cmd.SetGlobalInt(HDShaderIDs._OffScreenRendering, 0);
                cmd.SetGlobalInt(HDShaderIDs._EnableSpecularLighting, hdCamera.frameSettings.IsEnabled(FrameSettingsField.SpecularLighting) ? 1 : 0);
            }
        }

        void CopyDepthBufferIfNeeded(CommandBuffer cmd)
        {
            if (!m_IsDepthBufferCopyValid)
            {
                using (new ProfilingSample(cmd, "Copy depth buffer", CustomSamplerId.CopyDepthBuffer.GetSampler()))
                {
                    // TODO: maybe we don't actually need the top MIP level?
                    // That way we could avoid making the copy, and build the MIP hierarchy directly.
                    // The downside is that our SSR tracing accuracy would decrease a little bit.
                    // But since we never render SSR at full resolution, this may be acceptable.

                    // TODO: reading the depth buffer with a compute shader will cause it to decompress in place.
                    // On console, to preserve the depth test performance, we must NOT decompress the 'm_CameraDepthStencilBuffer' in place.
                    // We should call decompressDepthSurfaceToCopy() and decompress it to 'm_CameraDepthBufferMipChain'.
                    m_GPUCopy.SampleCopyChannel_xyzw2x(cmd, m_SharedRTManager.GetDepthStencilBuffer(), m_SharedRTManager.GetDepthTexture(), new RectInt(0, 0, m_CurrentWidth, m_CurrentHeight));
                }
                m_IsDepthBufferCopyValid = true;
            }
        }

        public void SetMicroShadowingSettings(CommandBuffer cmd)
        {
            MicroShadowing microShadowingSettings = VolumeManager.instance.stack.GetComponent<MicroShadowing>();
            cmd.SetGlobalFloat(HDShaderIDs._MicroShadowOpacity, microShadowingSettings.enable.value ? microShadowingSettings.opacity.value : 0.0f);
        }

        public void ConfigureKeywords(bool enableBakeShadowMask, HDCamera hdCamera, CommandBuffer cmd)
        {
            // Globally enable (for GBuffer shader and forward lit (opaque and transparent) the keyword SHADOWS_SHADOWMASK
            CoreUtils.SetKeyword(cmd, "SHADOWS_SHADOWMASK", enableBakeShadowMask);
            // Configure material to use depends on shadow mask option
            m_CurrentRendererConfigurationBakedLighting = enableBakeShadowMask ? HDUtils.k_RendererConfigurationBakedLightingWithShadowMask : HDUtils.k_RendererConfigurationBakedLighting;
            m_currentDebugViewMaterialGBuffer = enableBakeShadowMask ? m_DebugViewMaterialGBufferShadowMask : m_DebugViewMaterialGBuffer;

            CoreUtils.SetKeyword(cmd, "LIGHT_LAYERS", hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers));
            cmd.SetGlobalInt(HDShaderIDs._EnableLightLayers, hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers) ? 1 : 0);

            // configure keyword for both decal.shader and material
            if (m_Asset.currentPlatformRenderPipelineSettings.supportDecals)
            {
                CoreUtils.SetKeyword(cmd, "DECALS_OFF", false);
                CoreUtils.SetKeyword(cmd, "DECALS_3RT", !m_Asset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask);
                CoreUtils.SetKeyword(cmd, "DECALS_4RT", m_Asset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask);
            }
            else
            {
                CoreUtils.SetKeyword(cmd, "DECALS_OFF", true);
                CoreUtils.SetKeyword(cmd, "DECALS_3RT", false);
                CoreUtils.SetKeyword(cmd, "DECALS_4RT", false);
            }

            // Raise the normal buffer flag only if we are in forward rendering
            CoreUtils.SetKeyword(cmd, "WRITE_NORMAL_BUFFER", hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward);

            // Raise or remove the depth msaa flag based on the frame setting
            CoreUtils.SetKeyword(cmd, "WRITE_MSAA_DEPTH", hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA));
        }

        struct RenderRequest
        {
            public struct Target
            {
                public RenderTargetIdentifier id;
                public CubemapFace face;
                public RenderTexture copyToTarget;
            }
            public HDCamera hdCamera;
            public bool destroyCamera;
            public Target target;
            public HDCullingResults cullingResults;
            public int index;
            // Indices of render request to render before this one
            public List<int> dependsOnRenderRequestIndices;
            public CameraSettings cameraSettings;
        }
        struct HDCullingResults
        {
            public CullingResults cullingResults;
            public HDProbeCullingResults hdProbeCullingResults;
            public DecalSystem.CullResult decalCullResults;
            // TODO: DecalCullResults

            internal void Reset()
            {
                hdProbeCullingResults.Reset();
                if (decalCullResults != null)
                    decalCullResults.Clear();
                else
                    decalCullResults = GenericPool<DecalSystem.CullResult>.Get();
            }
        }

        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            if (!m_ValidAPI || cameras.Length == 0)
                return;

            UnityEngine.Rendering.RenderPipeline.BeginFrameRendering(renderContext, cameras);

            // Check if we can speed up FrameSettings process by skiping history
            // or go in detail if debug is activated. Done once for all renderer.
            frameSettingsHistoryEnabled = FrameSettingsHistory.enabled;

            {
                // SRP.Render() can be called several times per frame.
                // Also, most Time variables do not consistently update in the Scene View.
                // This makes reliable detection of the start of the new frame VERY hard.
                // One of the exceptions is 'Time.realtimeSinceStartup'.
                // Therefore, outside of the Play Mode we update the time at 60 fps,
                // and in the Play Mode we rely on 'Time.frameCount'.
                float t = Time.realtimeSinceStartup;
                int c = Time.frameCount;

                bool newFrame;

                if (Application.isPlaying)
                {
                    newFrame = m_FrameCount != c;

                    m_FrameCount = c;
                }
                else
                {
                    // If we switch to other scene Time.realtimeSinceStartup is reset, so we need to
                    // reset also m_Time. Here we simply detect ill case to trigger the reset.
                    m_Time = m_Time > t ? 0.0f : m_Time;

                    newFrame = (t - m_Time) > 0.0166f;

                    if (newFrame)
                        m_FrameCount++;
                }

                if (newFrame)
                {
                    HDCamera.CleanUnused();

                    // Make sure both are never 0.
                    m_LastTime = (m_Time > 0) ? m_Time : t;
                    m_Time = t;
                }
            }

			// TODO: Check with Fred if it make sense to put that here now that we have refactor the loop
#if ENABLE_RAYTRACING
            // This call need to happen once per frame, it evaluates if we need to fetch the geometry/lights for some subscenes
            m_RayTracingManager.CheckSubScenes();

            // Before rendering any camera, we call this function to flag everything as not updated
            m_RayTracingManager.UpdateFrameData();
#endif

            var dynResHandler = HDDynamicResolutionHandler.instance;
            dynResHandler.Update(m_Asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings, () =>
            {
                m_PostProcessSystem.ResetHistory();
                if(dynResHandler.DynamicResolutionEnabled())
                {
                    m_SharedRTManager.ComputeDepthBufferMipChainSize(dynResHandler.GetRTHandleScale(new Vector2Int(m_SharedRTManager.GetDepthStencilBuffer().rt.width, m_SharedRTManager.GetDepthStencilBuffer().rt.height)));
                }
            }
            );

            using (ListPool<RenderRequest>.Get(out List<RenderRequest> renderRequests))
            using (ListPool<int>.Get(out List<int> rootRenderRequestIndices))
            using (DictionaryPool<HDProbe, List<int>>.Get(out Dictionary<HDProbe, List<int>> renderRequestIndicesWhereTheProbeIsVisible))
            using (ListPool<CameraSettings>.Get(out List<CameraSettings> cameraSettings))
            using (ListPool<CameraPositionSettings>.Get(out List<CameraPositionSettings> cameraPositionSettings))
            {
                // With XR multi-pass enabled, each camera can be rendered multiple times with different parameters
                var multipassCameras = m_XRSystem.SetupFrame(cameras);

#if UNITY_EDITOR
                // See comment below about the preview camera workaround
                var hasGameViewCamera = cameras.Any(c => c.cameraType == CameraType.Game);
#endif

                // Culling loop
                foreach ((Camera camera, XRPass xrPass) in multipassCameras)
                {
                    if (camera == null)
                        continue;

#if UNITY_EDITOR
                    // We selecting a camera in the editor, we have a preview that is drawn.
                    // For legacy reasons, Unity will render all preview cameras when rendering the GameView
                    // Actually, we don't need this here because we call explicitly Camera.Render when we
                    // need a preview rendering.
                    //
                    // This is an issue, because at some point, you end up with 2 cameras to render:
                    // - Main Camera (game view)
                    // - Preview Camera (preview)
                    // If the preview camera is rendered last, it will alter the "GameView RT" RenderTexture
                    // that was previously rendered by the Main Camera.
                    // This is an issue.
                    //
                    // Meanwhile, skipping all preview camera when rendering the game views is sane,
                    // and will workaround the aformentionned issue.
                    if (hasGameViewCamera && camera.cameraType == CameraType.Preview)
                        continue;
#endif

                    bool cameraRequestedDynamicRes = false;
                    if (camera.GetComponent<HDAdditionalCameraData>() != null)
                    {
                        cameraRequestedDynamicRes = camera.GetComponent<HDAdditionalCameraData>().allowDynamicResolution;

                        // We are in a case where the platform does not support hw dynamic resolution, so we force the software fallback.
                        // TODO: Expose the graphics caps info on whether the platform supports hw dynamic resolution or not.
                        if (dynResHandler.RequestsHardwareDynamicResolution() && cameraRequestedDynamicRes && !camera.allowDynamicResolution)
                        {
                            dynResHandler.ForceSoftwareFallback();
                        }
                    }

                    dynResHandler.SetCurrentCameraRequest(cameraRequestedDynamicRes);
                    RTHandles.SetHardwareDynamicResolutionState(dynResHandler.HardwareDynamicResIsEnabled());

                    UnityEngine.Experimental.VFX.VFXManager.ProcessCamera(camera); //Visual Effect Graph is not yet a required package but calling this method when there isn't any VisualEffect component has no effect (but needed for Camera sorting in Visual Effect Graph context)

                    // Reset pooled variables
                    cameraSettings.Clear();
                    cameraPositionSettings.Clear();

                    var cullingResults = GenericPool<HDCullingResults>.Get();
                    cullingResults.Reset();

                    // Try to compute the parameters of the request or skip the request
                    var skipRequest = !TryCalculateFrameParameters(
                            camera,
                            xrPass,
                            out var additionalCameraData,
                            out var hdCamera,
                            out var cullingParameters);

                        // Note: In case of a custom render, we have false here and 'TryCull' is not executed
                    if (!skipRequest)
                    {
                        var needCulling = true;

                        // In XR multipass, culling results can be shared if the pass has the same culling id
                        if (xrPass.multipassId > 0)
                        {
                            foreach (var req in renderRequests)
                            {
                                if (req.hdCamera.xr.cullingPassId == xrPass.cullingPassId)
                                {
                                    cullingResults = req.cullingResults;
                                    needCulling = false;
                                }
                            }
                        }

                        if (needCulling)
                            skipRequest = !TryCull(camera, hdCamera, renderContext, cullingParameters, ref cullingResults);
                    }

                    if (additionalCameraData != null && additionalCameraData.hasCustomRender)
                    {
                        skipRequest = true;
                        // Execute custom render
                        additionalCameraData.ExecuteCustomRender(renderContext, hdCamera);
                    }

                    if (skipRequest)
                    {
                        // Submit render context and free pooled resources for this request
                        renderContext.Submit();
                        GenericPool<HDCullingResults>.Release(cullingResults);
                        continue;
                    }

                    // Select render target
                    RenderTargetIdentifier targetId = camera.targetTexture ?? new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

                    // XRTODO(2019.3) : remove once XRE-445 is done, use hdCamera.xr.renderTarget directly
                    if (hdCamera.xr.enabled && hdCamera.xr.tempRenderTexture != null)
                        targetId = hdCamera.xr.tempRenderTexture;

                    // Add render request
                    var request = new RenderRequest
                    {
                        hdCamera = hdCamera,
                        cullingResults = cullingResults,
                        target = new RenderRequest.Target
                        {
                            id = targetId,
                            face = CubemapFace.Unknown
                        },
                        dependsOnRenderRequestIndices = ListPool<int>.Get(),
                        index = renderRequests.Count,
                        cameraSettings = CameraSettings.From(hdCamera)
                        // TODO: store DecalCullResult
                    };
                    renderRequests.Add(request);
                    // This is a root render request
                    rootRenderRequestIndices.Add(request.index);

                    // Add visible probes to list
                    for (var i = 0; i < cullingResults.cullingResults.visibleReflectionProbes.Length; ++i)
                    {
                        var visibleProbe = cullingResults.cullingResults.visibleReflectionProbes[i];

                        // TODO: The following fix is temporary.
                        // We should investigate why we got null cull result when we change scene
                        if (visibleProbe == null || visibleProbe.Equals(null) || visibleProbe.reflectionProbe == null || visibleProbe.reflectionProbe.Equals(null))
                            continue;

                        var additionalReflectionData =
                            visibleProbe.reflectionProbe.GetComponent<HDAdditionalReflectionData>()
                            ?? visibleProbe.reflectionProbe.gameObject.AddComponent<HDAdditionalReflectionData>();

                        AddVisibleProbeVisibleIndexIfUpdateIsRequired(additionalReflectionData, request.index);
                    }
                    for (var i = 0; i < cullingResults.hdProbeCullingResults.visibleProbes.Count; ++i)
                        AddVisibleProbeVisibleIndexIfUpdateIsRequired(cullingResults.hdProbeCullingResults.visibleProbes[i], request.index);

                    // local function to help insertion of visible probe
                    void AddVisibleProbeVisibleIndexIfUpdateIsRequired(HDProbe probe, int visibleInIndex)
                    {
                        // Don't add it if it has already been updated this frame or not a real time probe
                        // TODO: discard probes that are baked once per frame and already baked this frame
                        if (!probe.requiresRealtimeUpdate)
                            return;

                        // Notify that we render the probe at this frame
                        probe.SetIsRendered(Time.frameCount);

                        if (!renderRequestIndicesWhereTheProbeIsVisible.TryGetValue(probe, out var visibleInIndices))
                        {
                            visibleInIndices = ListPool<int>.Get();
                            renderRequestIndicesWhereTheProbeIsVisible.Add(probe, visibleInIndices);
                        }
                        if (!visibleInIndices.Contains(visibleInIndex))
                            visibleInIndices.Add(visibleInIndex);
                    }
                }

                foreach (var probeToRenderAndDependencies in renderRequestIndicesWhereTheProbeIsVisible)
                {
                    var visibleProbe = probeToRenderAndDependencies.Key;
                    var visibleInIndices = probeToRenderAndDependencies.Value;

                    // Two cases:
                    //   - If the probe is view independent, we add only one render request per face that is
                    //      a dependency for all its 'visibleIn' render requests
                    //   - If the probe is view dependent, we add one render request per face per 'visibleIn'
                    //      render requests
                    var isViewDependent = visibleProbe.type == ProbeSettings.ProbeType.PlanarProbe;

                    if (isViewDependent)
                    {
                        for (int i = 0; i < visibleInIndices.Count; ++i)
                    {
                            var visibleInIndex = visibleInIndices[i];
                            var visibleInRenderRequest = renderRequests[visibleInIndices[i]];
                            var viewerTransform = visibleInRenderRequest.hdCamera.camera.transform;

                            AddHDProbeRenderRequests(
                                visibleProbe,
                                viewerTransform,
                                Enumerable.Repeat(visibleInIndex, 1),
                                visibleInRenderRequest.hdCamera.camera.fieldOfView
                            );
                        }
                    }
                    else
                        AddHDProbeRenderRequests(visibleProbe, null, visibleInIndices);
                }
                foreach (var pair in renderRequestIndicesWhereTheProbeIsVisible)
                    ListPool<int>.Release(pair.Value);
                renderRequestIndicesWhereTheProbeIsVisible.Clear();

                // Local function to share common code between view dependent and view independent requests
                void AddHDProbeRenderRequests(
                    HDProbe visibleProbe,
                    Transform viewerTransform,
                    IEnumerable<int> visibleInIndices,
                    float referenceFieldOfView = 90
                )
                {
                    var position = ProbeCapturePositionSettings.ComputeFrom(
                        visibleProbe,
                        viewerTransform
                    );
                    cameraSettings.Clear();
                    cameraPositionSettings.Clear();
                    HDRenderUtilities.GenerateRenderingSettingsFor(
                        visibleProbe.settings, position,
                        cameraSettings, cameraPositionSettings,
                        referenceFieldOfView: referenceFieldOfView
                    );

                    switch (visibleProbe.type)
                    {
                        case ProbeSettings.ProbeType.ReflectionProbe:
                            int desiredProbeSize = (int)((HDRenderPipeline)RenderPipelineManager.currentPipeline).currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCubemapSize;
                            if (visibleProbe.realtimeTexture == null || visibleProbe.realtimeTexture.width != desiredProbeSize)
                            {
                                visibleProbe.SetTexture(ProbeSettings.Mode.Realtime, HDRenderUtilities.CreateReflectionProbeRenderTarget(desiredProbeSize));
                            }
                            break;
                        case ProbeSettings.ProbeType.PlanarProbe:
                            int desiredPlanarProbeSize = (int)((HDRenderPipeline)RenderPipelineManager.currentPipeline).currentPlatformRenderPipelineSettings.lightLoopSettings.planarReflectionTextureSize;
                            if (visibleProbe.realtimeTexture == null || visibleProbe.realtimeTexture.width != desiredPlanarProbeSize)
                            {
                                visibleProbe.SetTexture(ProbeSettings.Mode.Realtime, HDRenderUtilities.CreatePlanarProbeRenderTarget(desiredPlanarProbeSize));
                            }
                            // Set the viewer's camera as the default camera anchor
                            for (var i = 0; i < cameraSettings.Count; ++i)
                            {
                                var v = cameraSettings[i];
                                if (v.volumes.anchorOverride == null)
                                {
                                    v.volumes.anchorOverride = viewerTransform;
                                    cameraSettings[i] = v;
                                }
                            }
                            break;
                    }

                    for (int j = 0; j < cameraSettings.Count; ++j)
                    {
                        Camera camera = (m_ProbeCameraPool.Count == 0)
                            ? new GameObject().AddComponent<Camera>()
                            : m_ProbeCameraPool.Pop();

                        camera.targetTexture = visibleProbe.realtimeTexture; // We need to set a targetTexture with the right otherwise when setting pixelRect, it will be rescaled internally to the size of the screen
                        camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                        camera.gameObject.SetActive(false);
                        camera.name = ComputeProbeCameraName(visibleProbe.name, j, viewerTransform?.name);
                        camera.ApplySettings(cameraSettings[j]);
                        camera.ApplySettings(cameraPositionSettings[j]);
                        camera.cameraType = CameraType.Reflection;
                        camera.pixelRect = new Rect(0, 0, visibleProbe.realtimeTexture.width, visibleProbe.realtimeTexture.height);

                        var _cullingResults = GenericPool<HDCullingResults>.Get();
                        _cullingResults.Reset();

                        if (!(TryCalculateFrameParameters(
                                camera,
                                m_XRSystem.emptyPass,
                                out _,
                                out var hdCamera,
                                out var cullingParameters
                            )
                            && TryCull(
                                camera, hdCamera, renderContext, cullingParameters,
                                ref _cullingResults
                            )))
                        {
                            // Skip request and free resources
                            Object.Destroy(camera);
                            GenericPool<HDCullingResults>.Release(_cullingResults);
                            continue;
                        }
                        camera.GetComponent<HDAdditionalCameraData>().flipYMode
                            = visibleProbe.type == ProbeSettings.ProbeType.ReflectionProbe
                                ? HDAdditionalCameraData.FlipYMode.ForceFlipY
                                : HDAdditionalCameraData.FlipYMode.Automatic;

                        if (!visibleProbe.realtimeTexture.IsCreated())
                            visibleProbe.realtimeTexture.Create();

                        visibleProbe.SetRenderData(
                            ProbeSettings.Mode.Realtime,
                            new HDProbe.RenderData(
                                camera.worldToCameraMatrix,
                                camera.projectionMatrix,
                                camera.transform.position,
                                camera.transform.rotation,
                                cameraSettings[j].frustum.fieldOfView
                            )
                        );

                        // TODO: Assign the actual final target to render to.
                        //   Currently, we use a target for each probe, and then copy it into the cache before using it
                        //   during the lighting pass.
                        //   But what we actually want here, is to render directly into the cache (either CubeArray,
                        //   or Texture2DArray)
                        //   To do so, we need to first allocate in the cache the location of the target and then assign
                        //   it here.
                        var request = new RenderRequest
                        {
                            hdCamera = hdCamera,
                            cullingResults = _cullingResults,
                            destroyCamera = true,
                            dependsOnRenderRequestIndices = ListPool<int>.Get(),
                            index = renderRequests.Count,
                            cameraSettings = cameraSettings[j]
                            // TODO: store DecalCullResult
                        };

                        // As we render realtime texture on GPU side, we must tag the texture so our texture array cache detect that something have change
                        visibleProbe.realtimeTexture.IncrementUpdateCount();

                        if (cameraSettings.Count > 1)
                        {
                            var face = (CubemapFace)j;
                            request.target = new RenderRequest.Target
                            {
                                copyToTarget = visibleProbe.realtimeTexture,
                                face = face
                            };
                        }
                        else
                        {
                            request.target = new RenderRequest.Target
                            {
                                id = visibleProbe.realtimeTexture,
                                face = CubemapFace.Unknown
                            };
                        }
                        renderRequests.Add(request);


                        foreach (var visibleInIndex in visibleInIndices)
                            renderRequests[visibleInIndex].dependsOnRenderRequestIndices.Add(request.index);
                    }
                }

                // TODO: Refactor into a method. If possible remove the intermediate target
                // Find max size for Cubemap face targets and resize/allocate if required the intermediate render target
                {
                    var size = Vector2Int.zero;
                    for (int i = 0; i < renderRequests.Count; ++i)
                    {
                        var renderRequest = renderRequests[i];
                        var isCubemapFaceTarget = renderRequest.target.face != CubemapFace.Unknown;
                        if (!isCubemapFaceTarget)
                            continue;

                        var width = renderRequest.hdCamera.actualWidth;
                        var height = renderRequest.hdCamera.actualHeight;
                        size.x = Mathf.Max(width, size.x);
                        size.y = Mathf.Max(height, size.y);
                    }

                    if (size != Vector2.zero)
                    {
                        if (m_TemporaryTargetForCubemaps != null)
                        {
                            if (m_TemporaryTargetForCubemaps.width != size.x
                                || m_TemporaryTargetForCubemaps.height != size.y)
                            {
                                m_TemporaryTargetForCubemaps.Release();
                                m_TemporaryTargetForCubemaps = null;
                            }
                        }
                        if (m_TemporaryTargetForCubemaps == null)
                        {
                            m_TemporaryTargetForCubemaps = new RenderTexture(
                                size.x, size.y, 1, GraphicsFormat.R16G16B16A16_SFloat
                            )
                            {
                                autoGenerateMips = false,
                                useMipMap = false,
                                name = "Temporary Target For Cubemap Face",
                                volumeDepth = 1,
                                useDynamicScale = false
                            };
                        }
                    }
                }

                using (ListPool<int>.Get(out List<int> renderRequestIndicesToRender))
                {
                    // Flatten the render requests graph in an array that guarantee dependency constraints
                    {
                        using (GenericPool<Stack<int>>.Get(out Stack<int> stack))
                        {
                            stack.Clear();
                            for (int i = rootRenderRequestIndices.Count -1; i >= 0; --i)
                            {
                                stack.Push(rootRenderRequestIndices[i]);
                                while (stack.Count > 0)
                                {
                                    var index = stack.Pop();
                                    if (!renderRequestIndicesToRender.Contains(index))
                                        renderRequestIndicesToRender.Add(index);

                                    var request = renderRequests[index];
                                    for (int j = 0; j < request.dependsOnRenderRequestIndices.Count; ++j)
                                        stack.Push(request.dependsOnRenderRequestIndices[j]);
                                }
                            }
                        }
                    }
                    // Execute render request graph, in reverse order
                    for (int i = renderRequestIndicesToRender.Count - 1; i >= 0; --i)
                    {
                        var renderRequestIndex = renderRequestIndicesToRender[i];
                        var renderRequest = renderRequests[renderRequestIndex];

                        var cmd = CommandBufferPool.Get("");

                        // TODO: Avoid the intermediate target and render directly into final target
                        //  CommandBuffer.Blit does not work on Cubemap faces
                        //  So we use an intermediate RT to perform a CommandBuffer.CopyTexture in the target Cubemap face
                        if (renderRequest.target.face != CubemapFace.Unknown)
                        {
                            if (!m_TemporaryTargetForCubemaps.IsCreated())
                                m_TemporaryTargetForCubemaps.Create();

                            var hdCamera = renderRequest.hdCamera;
                            ref var target = ref renderRequest.target;
                            target.id = m_TemporaryTargetForCubemaps;
                        }


                        var aovRequestIndex = 0;
                        foreach (var aovRequest in renderRequest.hdCamera.aovRequests)
                        {
                        using (new ProfilingSample(
                            cmd,
                                $"HDRenderPipeline::Render {renderRequest.hdCamera.camera.name} - AOVRequest {aovRequestIndex++}",
                                CustomSamplerId.HDRenderPipelineRender.GetSampler())
                            )
                            {
                                cmd.SetInvertCulling(renderRequest.cameraSettings.invertFaceCulling);
                                ExecuteRenderRequest(renderRequest, renderContext, cmd, aovRequest);
                                cmd.SetInvertCulling(false);
                            }
                            renderContext.ExecuteCommandBuffer(cmd);
                            CommandBufferPool.Release(cmd);
                            renderContext.Submit();
                            cmd = CommandBufferPool.Get();
                        }

                        using (new ProfilingSample(
                            cmd,
                            $"HDRenderPipeline::Render {renderRequest.hdCamera.camera.name}",
                            CustomSamplerId.HDRenderPipelineRender.GetSampler())
                        )
                        {
                            cmd.SetInvertCulling(renderRequest.cameraSettings.invertFaceCulling);
                            UnityEngine.Rendering.RenderPipeline.BeginCameraRendering(renderContext, renderRequest.hdCamera.camera);
                            ExecuteRenderRequest(renderRequest, renderContext, cmd, AOVRequestData.@default);
                            cmd.SetInvertCulling(false);
                            UnityEngine.Rendering.RenderPipeline.EndCameraRendering(renderContext, renderRequest.hdCamera.camera);
                        }

                        {
                            var target = renderRequest.target;
                            // Handle the copy if requested
                            if (target.copyToTarget != null)
                            {
                                cmd.CopyTexture(
                                    target.id, 0, 0, 0, 0, renderRequest.hdCamera.actualWidth, renderRequest.hdCamera.actualHeight,
                                    target.copyToTarget, (int)target.face, 0, 0, 0
                                );
                            }
                            // Destroy the camera if requested
                            if (renderRequest.destroyCamera)
                            {
                                renderRequest.hdCamera.camera.targetTexture = null; // release reference because the RenderTexture might be destroyed before camera
                                m_ProbeCameraPool.Push(renderRequest.hdCamera.camera);
                            }

                            ListPool<int>.Release(renderRequest.dependsOnRenderRequestIndices);
                            renderRequest.cullingResults.decalCullResults?.Clear();
                            GenericPool<HDCullingResults>.Release(renderRequest.cullingResults);
                        }

                        renderContext.ExecuteCommandBuffer(cmd);

                        CommandBufferPool.Release(cmd);
                        renderContext.Submit();
                    }
                }
            }

            m_XRSystem.ReleaseFrame();
            UnityEngine.Rendering.RenderPipeline.EndFrameRendering(renderContext, cameras);
        }

        void ExecuteRenderRequest(
            RenderRequest renderRequest,
            ScriptableRenderContext renderContext,
            CommandBuffer cmd,
            AOVRequestData aovRequest
        )
        {
            InitializeGlobalResources(renderContext);

            var hdCamera = renderRequest.hdCamera;
            var camera = hdCamera.camera;
            var cullingResults = renderRequest.cullingResults.cullingResults;
            var hdProbeCullingResults = renderRequest.cullingResults.hdProbeCullingResults;
            var decalCullingResults = renderRequest.cullingResults.decalCullResults;
            var target = renderRequest.target;

            // Updates RTHandle
            hdCamera.BeginRender();

            using (ListPool<RTHandle>.Get(out var aovBuffers))
            {
                aovRequest.AllocateTargetTexturesIfRequired(ref aovBuffers);

            // If we render a reflection view or a preview we should not display any debug information
            // This need to be call before ApplyDebugDisplaySettings()
            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
            {
                // Neutral allow to disable all debug settings
                m_CurrentDebugDisplaySettings = s_NeutralDebugDisplaySettings;
            }
            else
            {
                // Make sure we are in sync with the debug menu for the msaa count
                m_MSAASamples = m_DebugDisplaySettings.data.msaaSamples;
                m_SharedRTManager.SetNumMSAASamples(m_MSAASamples);

                m_DebugDisplaySettings.UpdateCameraFreezeOptions();

                m_CurrentDebugDisplaySettings = m_DebugDisplaySettings;

                bool sceneLightingIsDisabled = CoreUtils.IsSceneLightingDisabled(hdCamera.camera);
                if (m_CurrentDebugDisplaySettings.GetDebugLightingMode() != DebugLightingMode.MatcapView)
                {
                    if(sceneLightingIsDisabled)
                    {
                        m_CurrentDebugDisplaySettings.SetDebugLightingMode(DebugLightingMode.MatcapView);
                    }
                }

                if(hdCamera.sceneLightingWasDisabledForCamera && !CoreUtils.IsSceneLightingDisabled(hdCamera.camera))
                {
                    m_CurrentDebugDisplaySettings.SetDebugLightingMode(DebugLightingMode.None);
                }
                hdCamera.sceneLightingWasDisabledForCamera = sceneLightingIsDisabled;
            }

            aovRequest.SetupDebugData(ref m_CurrentDebugDisplaySettings);

#if ENABLE_RAYTRACING
            // Must update after getting DebugDisplaySettings
            m_RayTracingManager.rayCountManager.ClearRayCount(cmd, hdCamera);
#endif


            m_DbufferManager.enableDecals = false;
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
            {
                using (new ProfilingSample(null, "DBufferPrepareDrawData", CustomSamplerId.DBufferPrepareDrawData.GetSampler()))
                {
                    // TODO: update singleton with DecalCullResults
                    m_DbufferManager.enableDecals = true;              // mesh decals are renderers managed by c++ runtime and we have no way to query if any are visible, so set to true
                    DecalSystem.instance.LoadCullResults(decalCullingResults);
                    DecalSystem.instance.UpdateCachedMaterialData();    // textures, alpha or fade distances could've changed
                    DecalSystem.instance.CreateDrawData();              // prepare data is separate from draw
                    DecalSystem.instance.UpdateTextureAtlas(cmd);       // as this is only used for transparent pass, would've been nice not to have to do this if no transparent renderers are visible, needs to happen after CreateDrawData
                }
            }

            using (new ProfilingSample(cmd, "Volume Update", CustomSamplerId.VolumeUpdate.GetSampler()))
            {
                VolumeManager.instance.Update(hdCamera.volumeAnchor, hdCamera.volumeLayerMask);
            }

            // Do anything we need to do upon a new frame.
            // The NewFrame must be after the VolumeManager update and before Resize because it uses properties set in NewFrame
            LightLoopNewFrame(hdCamera.frameSettings);

            // Apparently scissor states can leak from editor code. As it is not used currently in HDRP (appart from VR). We disable scissor at the beginning of the frame.
            cmd.DisableScissorRect();

            Resize(hdCamera);
            m_PostProcessSystem.BeginFrame(cmd, hdCamera);

            ApplyDebugDisplaySettings(hdCamera, cmd);
            m_SkyManager.UpdateCurrentSkySettings(hdCamera);

            SetupCameraProperties(hdCamera, renderContext, cmd);

            PushGlobalParams(hdCamera, cmd);

            // TODO: Find a correct place to bind these material textures
            // We have to bind the material specific global parameters in this mode
            m_MaterialList.ForEach(material => material.Bind(cmd));

            // Frustum cull density volumes on the CPU. Can be performed as soon as the camera is set up.
            DensityVolumeList densityVolumes = PrepareVisibleDensityVolumeList(hdCamera, cmd, m_Time);

            // Note: Legacy Unity behave like this for ShadowMask
            // When you select ShadowMask in Lighting panel it recompile shaders on the fly with the SHADOW_MASK keyword.
            // However there is no C# function that we can query to know what mode have been select in Lighting Panel and it will be wrong anyway. Lighting Panel setup what will be the next bake mode. But until light is bake, it is wrong.
            // Currently to know if you need shadow mask you need to go through all visible lights (of CullResult), check the LightBakingOutput struct and look at lightmapBakeType/mixedLightingMode. If one light have shadow mask bake mode, then you need shadow mask features (i.e extra Gbuffer).
            // It mean that when we build a standalone player, if we detect a light with bake shadow mask, we generate all shader variant (with and without shadow mask) and at runtime, when a bake shadow mask light is visible, we dynamically allocate an extra GBuffer and switch the shader.
            // So the first thing to do is to go through all the light: PrepareLightsForGPU
            bool enableBakeShadowMask;
            using (new ProfilingSample(cmd, "TP_PrepareLightsForGPU", CustomSamplerId.TPPrepareLightsForGPU.GetSampler()))
            {
                enableBakeShadowMask = PrepareLightsForGPU(cmd, hdCamera, cullingResults, hdProbeCullingResults, densityVolumes, m_CurrentDebugDisplaySettings, aovRequest);
            }
            // Configure all the keywords
            ConfigureKeywords(enableBakeShadowMask, hdCamera, cmd);

            hdCamera.xr.StartLegacyStereo(camera, cmd, renderContext);

            ClearBuffers(hdCamera, cmd);

            bool shouldRenderMotionVectorAfterGBuffer = RenderDepthPrepass(cullingResults, hdCamera, renderContext, cmd);
            if (!shouldRenderMotionVectorAfterGBuffer)
            {
                // If objects motion vectors if enabled, this will render the objects with motion vector into the target buffers (in addition to the depth)
                // Note: An object with motion vector must not be render in the prepass otherwise we can have motion vector write that should have been rejected
                RenderObjectsMotionVectors(cullingResults, hdCamera, renderContext, cmd);
            }

            // Now that all depths have been rendered, resolve the depth buffer
            m_SharedRTManager.ResolveSharedRT(cmd, hdCamera);

            // This will bind the depth buffer if needed for DBuffer)
            RenderDBuffer(hdCamera, cmd, renderContext, cullingResults);
            // We can call DBufferNormalPatch after RenderDBuffer as it only affect forward material and isn't affected by RenderGBuffer
            // This reduce lifteime of stencil bit
            DBufferNormalPatch(hdCamera, cmd, renderContext, cullingResults);

#if ENABLE_RAYTRACING
            bool validIndirectDiffuse = m_RaytracingIndirectDiffuse.ValidIndirectDiffuseState();
            cmd.SetGlobalInt(HDShaderIDs._RaytracedIndirectDiffuse, validIndirectDiffuse ? 1 : 0);
#endif

            RenderGBuffer(cullingResults, hdCamera, renderContext, cmd);

            // We can now bind the normal buffer to be use by any effect
            m_SharedRTManager.BindNormalBuffer(cmd);

            // In both forward and deferred, everything opaque should have been rendered at this point so we can safely copy the depth buffer for later processing.
            GenerateDepthPyramid(hdCamera, cmd, FullScreenDebugMode.DepthPyramid);
            // Depth texture is now ready, bind it (Depth buffer could have been bind before if DBuffer is enable)
            cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, m_SharedRTManager.GetDepthTexture());

            if (shouldRenderMotionVectorAfterGBuffer)
            {
                // See the call RenderObjectsMotionVectors() above and comment
                RenderObjectsMotionVectors(cullingResults, hdCamera, renderContext, cmd);
            }

            RenderCameraMotionVectors(cullingResults, hdCamera, renderContext, cmd);

            hdCamera.xr.StopLegacyStereo(camera, cmd, renderContext);

            // Caution: We require sun light here as some skies use the sun light to render, it means that UpdateSkyEnvironment must be called after PrepareLightsForGPU.
            // TODO: Try to arrange code so we can trigger this call earlier and use async compute here to run sky convolution during other passes (once we move convolution shader to compute).
            if(m_CurrentDebugDisplaySettings.GetDebugLightingMode() != DebugLightingMode.MatcapView)
                UpdateSkyEnvironment(hdCamera, cmd);

            hdCamera.xr.StartLegacyStereo(camera, cmd, renderContext);

#if ENABLE_RAYTRACING
            bool raytracedIndirectDiffuse = m_RaytracingIndirectDiffuse.RenderIndirectDiffuse(hdCamera, cmd, renderContext, m_FrameCount);
            if(raytracedIndirectDiffuse)
            {
                PushFullScreenDebugTexture(hdCamera, cmd, m_RaytracingIndirectDiffuse.GetIndirectDiffuseTexture(), FullScreenDebugMode.IndirectDiffuse);
            }
#endif

#if UNITY_EDITOR
            var showGizmos = camera.cameraType == CameraType.Game
                || camera.cameraType == CameraType.SceneView;
#endif

            if (m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled() || m_CurrentDebugDisplaySettings.IsMaterialValidationEnabled())
            {
                RenderDebugViewMaterial(cullingResults, hdCamera, renderContext, cmd);
            }
            else
            {
                if (!hdCamera.frameSettings.SSAORunsAsync())
                    m_AmbientOcclusionSystem.Render(cmd, hdCamera, m_SharedRTManager, renderContext, m_FrameCount);

                CopyStencilBufferIfNeeded(cmd, hdCamera, m_SharedRTManager.GetDepthStencilBuffer(), m_SharedRTManager.GetStencilBufferCopy(), m_CopyStencil, m_CopyStencilForSSR);

                // When debug is enabled we need to clear otherwise we may see non-shadows areas with stale values.
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ContactShadows) && m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.ContactShadows)
                {
                    HDUtils.SetRenderTarget(cmd, m_ContactShadowBuffer, ClearFlag.Color, Color.clear);
                }

#if ENABLE_RAYTRACING
                // Update the light clusters that we need to update
                m_RayTracingManager.UpdateCameraData(cmd, hdCamera);

                // We only request the light cluster if we are gonna use it for debug mode
                if (FullScreenDebugMode.LightCluster == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
                {
                    var rSettings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>();
                    var rrSettings = VolumeManager.instance.stack.GetComponent<RecursiveRendering>();
                    HDRaytracingEnvironment rtEnv = m_RayTracingManager.CurrentEnvironment();
                    if (rSettings.enableRaytracing.value && rtEnv != null)
                    {
                        HDRaytracingLightCluster lightCluster = m_RayTracingManager.RequestLightCluster(rtEnv.reflLayerMask);
                        PushFullScreenDebugTexture(hdCamera, cmd, lightCluster.m_DebugLightClusterTexture, FullScreenDebugMode.LightCluster);
                    }
                    else if (rrSettings.enable.value && rtEnv != null)
                    {
                        HDRaytracingLightCluster lightCluster = m_RayTracingManager.RequestLightCluster(rtEnv.raytracedLayerMask);
                        PushFullScreenDebugTexture(hdCamera, cmd, lightCluster.m_DebugLightClusterTexture, FullScreenDebugMode.LightCluster);
                    }
                }
#endif

                hdCamera.xr.StopLegacyStereo(camera, cmd, renderContext);

                var buildLightListTask = new HDGPUAsyncTask("Build light list", ComputeQueueType.Background);
                // It is important that this task is in the same queue as the build light list due to dependency it has on it. If really need to move it, put an extra fence to make sure buildLightListTask has finished.
                var volumeVoxelizationTask = new HDGPUAsyncTask("Volumetric voxelization", ComputeQueueType.Background);
                var SSRTask = new HDGPUAsyncTask("Screen Space Reflection", ComputeQueueType.Background);
                var SSAOTask = new HDGPUAsyncTask("SSAO", ComputeQueueType.Background);
                var contactShadowsTask = new HDGPUAsyncTask("Screen Space Shadows", ComputeQueueType.Background);

                var haveAsyncTaskWithShadows = false;
                if (hdCamera.frameSettings.BuildLightListRunsAsync())
                {
                    buildLightListTask.Start(cmd, renderContext, Callback, !haveAsyncTaskWithShadows);

                    haveAsyncTaskWithShadows = true;

                    void Callback(CommandBuffer asyncCmd)
                        => BuildGPULightListsCommon(hdCamera, asyncCmd, m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)), m_SharedRTManager.GetStencilBufferCopy());
                }

                if (hdCamera.frameSettings.VolumeVoxelizationRunsAsync())
                {
                    volumeVoxelizationTask.Start(cmd, renderContext, Callback, !haveAsyncTaskWithShadows);

                    haveAsyncTaskWithShadows = true;

                    void Callback(CommandBuffer asyncCmd)
                        => VolumeVoxelizationPass(hdCamera, asyncCmd, m_FrameCount, densityVolumes);
                }

                if (hdCamera.frameSettings.SSRRunsAsync())
                {
                    SSRTask.Start(cmd, renderContext, Callback, !haveAsyncTaskWithShadows);

                    haveAsyncTaskWithShadows = true;

                    void Callback(CommandBuffer asyncCmd)
                        => RenderSSR(hdCamera, asyncCmd, renderContext);
                }

                if (hdCamera.frameSettings.SSAORunsAsync())
                {
                    void AsyncSSAODispatch(CommandBuffer asyncCmd) => m_AmbientOcclusionSystem.Dispatch(asyncCmd, hdCamera, m_SharedRTManager, m_FrameCount);
                    SSAOTask.Start(cmd, renderContext, AsyncSSAODispatch, !haveAsyncTaskWithShadows);
                    haveAsyncTaskWithShadows = true;
                }

                using (new ProfilingSample(cmd, "Render shadow maps", CustomSamplerId.RenderShadowMaps.GetSampler()))
                {
                    // This call overwrites camera properties passed to the shader system.
                    RenderShadowMaps(renderContext, cmd, cullingResults, hdCamera);

                    hdCamera.SetupGlobalParams(cmd, m_Time, m_LastTime, m_FrameCount);
                }

                if (!hdCamera.frameSettings.SSRRunsAsync())
                {
                    // Needs the depth pyramid and motion vectors, as well as the render of the previous frame.
                    RenderSSR(hdCamera, cmd, renderContext);
                }

                // Contact shadows needs the light loop so we do them after the build light list
                if (hdCamera.frameSettings.BuildLightListRunsAsync())
                {
                    buildLightListTask.EndWithPostWork(cmd, Callback);

                    void Callback()
                    {
                        var globalParams = PrepareLightLoopGlobalParameters(hdCamera);
                        PushLightLoopGlobalParams(globalParams, cmd);
                        // Run the contact shadow as they now need the light list
                        DispatchContactShadows();
                    }
                }
                else
                {
                    using (new ProfilingSample(cmd, "Build Light list", CustomSamplerId.BuildLightList.GetSampler()))
                    {
                        BuildGPULightLists(hdCamera, cmd, m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)), m_SharedRTManager.GetStencilBufferCopy());
                    }

                    DispatchContactShadows();
                }

                using (new ProfilingSample(cmd, "Render screen space shadows", CustomSamplerId.ScreenSpaceShadows.GetSampler()))
                {
                    RenderScreenSpaceShadows(hdCamera, cmd);
                }

                // Contact shadows needs the light loop so we do them after the build light list
                void DispatchContactShadows()
                {
                    if (hdCamera.frameSettings.ContactShadowsRunAsync())
                    {
                        contactShadowsTask.Start(cmd, renderContext, ContactShadowStartCallback, !haveAsyncTaskWithShadows);

                        haveAsyncTaskWithShadows = true;

                        void ContactShadowStartCallback(CommandBuffer asyncCmd)
                        {
                            var firstMipOffsetY = m_SharedRTManager.GetDepthBufferMipChainInfo().mipLevelOffsets[1].y;
                            RenderContactShadows(hdCamera, m_ContactShadowBuffer, hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? m_SharedRTManager.GetDepthValuesTexture() : m_SharedRTManager.GetDepthTexture(), firstMipOffsetY, asyncCmd);
                        }
                    }
                    else
                    {
                        HDUtils.CheckRTCreated(m_ContactShadowBuffer);

                        int firstMipOffsetY = m_SharedRTManager.GetDepthBufferMipChainInfo().mipLevelOffsets[1].y;
                        RenderContactShadows(hdCamera, m_ContactShadowBuffer, hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? m_SharedRTManager.GetDepthValuesTexture() : m_SharedRTManager.GetDepthTexture(), firstMipOffsetY, cmd);

                        PushFullScreenDebugTexture(hdCamera, cmd, m_ContactShadowBuffer, FullScreenDebugMode.ContactShadows);
                    }
                }

                {
                    // Set fog parameters for volumetric lighting.
                    var visualEnv = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
                    visualEnv.PushFogShaderParameters(hdCamera, cmd);
                }

                if (hdCamera.frameSettings.VolumeVoxelizationRunsAsync())
                {
                    volumeVoxelizationTask.End(cmd);
                }
                else
                {
                    // Perform the voxelization step which fills the density 3D texture.
                    VolumeVoxelizationPass(hdCamera, cmd, m_FrameCount, densityVolumes);
                }

                // Render the volumetric lighting.
                // The pass requires the volume properties, the light list and the shadows, and can run async.
                VolumetricLightingPass(hdCamera, cmd, m_FrameCount);

                SetMicroShadowingSettings(cmd);

                if (hdCamera.frameSettings.SSAORunsAsync())
                {
                    SSAOTask.EndWithPostWork(cmd, Callback);
                    void Callback() => m_AmbientOcclusionSystem.PostDispatchWork(cmd, hdCamera, m_SharedRTManager);
                }

                if (hdCamera.frameSettings.ContactShadowsRunAsync())
                {
                    contactShadowsTask.EndWithPostWork(cmd, Callback);

                    void Callback()
                    {
                        SetContactShadowsTexture(hdCamera, m_ContactShadowBuffer, cmd);
                        PushFullScreenDebugTexture(hdCamera, cmd, m_ContactShadowBuffer, FullScreenDebugMode.ContactShadows);
                    }
                }
                else
                {
                    SetContactShadowsTexture(hdCamera, m_ContactShadowBuffer, cmd);
                }


                if (hdCamera.frameSettings.SSRRunsAsync())
                {
                    SSRTask.End(cmd);
                }

                hdCamera.xr.StartLegacyStereo(camera, cmd, renderContext);

                RenderDeferredLighting(hdCamera, cmd);

                RenderForwardOpaque(cullingResults, hdCamera, renderContext, cmd);

                m_SharedRTManager.ResolveMSAAColor(cmd, hdCamera, m_CameraSssDiffuseLightingMSAABuffer, m_CameraSssDiffuseLightingBuffer);
                m_SharedRTManager.ResolveMSAAColor(cmd, hdCamera, GetSSSBufferMSAA(), GetSSSBuffer());

                // SSS pass here handle both SSS material from deferred and forward
                RenderSubsurfaceScattering(hdCamera, cmd, hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? m_CameraColorMSAABuffer : m_CameraColorBuffer,
                    m_CameraSssDiffuseLightingBuffer, m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)), m_SharedRTManager.GetDepthTexture());

                RenderForwardEmissive(cullingResults, hdCamera, renderContext, cmd);

                RenderSky(hdCamera, cmd);

                RenderTransparentDepthPrepass(cullingResults, hdCamera, renderContext, cmd);

#if ENABLE_RAYTRACING
                m_RaytracingRenderer.Render(hdCamera, cmd, m_CameraColorBuffer, renderContext, cullingResults);
#endif
                // Render pre refraction objects
                RenderForwardTransparent(cullingResults, hdCamera, true, renderContext, cmd);

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RoughRefraction))
                {
                    // First resolution of the color buffer for the color pyramid
                    m_SharedRTManager.ResolveMSAAColor(cmd, hdCamera, m_CameraColorMSAABuffer, m_CameraColorBuffer);

                    RenderColorPyramid(hdCamera, cmd, true);
                }

                // Bind current color pyramid for shader graph SceneColorNode on transparent objects
                var currentColorPyramid = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, currentColorPyramid);

                // Render all type of transparent forward (unlit, lit, complex (hair...)) to keep the sorting between transparent objects.
                RenderForwardTransparent(cullingResults, hdCamera, false, renderContext, cmd);

                // We push the motion vector debug texture here as transparent object can overwrite the motion vector texture content.
                if(m_Asset.currentPlatformRenderPipelineSettings.supportMotionVectors)
                    PushFullScreenDebugTexture(hdCamera, cmd, m_SharedRTManager.GetMotionVectorsBuffer(), FullScreenDebugMode.MotionVectors);

                // Second resolve the color buffer for finishing the frame
                m_SharedRTManager.ResolveMSAAColor(cmd, hdCamera, m_CameraColorMSAABuffer, m_CameraColorBuffer);

                // Render All forward error
                RenderForwardError(cullingResults, hdCamera, renderContext, cmd);

                DownsampleDepthForLowResTransparency(hdCamera, cmd);

                RenderLowResTransparent(cullingResults, hdCamera, renderContext, cmd);

                UpsampleTransparent(hdCamera, cmd);

                // Fill depth buffer to reduce artifact for transparent object during postprocess
                RenderTransparentDepthPostpass(cullingResults, hdCamera, renderContext, cmd);

                RenderColorPyramid(hdCamera, cmd, false);

                AccumulateDistortion(cullingResults, hdCamera, renderContext, cmd);
                RenderDistortion(hdCamera, cmd);

                PushFullScreenDebugTexture(hdCamera, cmd, m_CameraColorBuffer, FullScreenDebugMode.NanTracker);
                PushFullScreenLightingDebugTexture(hdCamera, cmd, m_CameraColorBuffer);

#if UNITY_EDITOR
                // Render gizmos that should be affected by post processes
                if (showGizmos)
                {
                    Gizmos.exposure = m_PostProcessSystem.GetExposureTexture(hdCamera).rt;
                    RenderGizmos(cmd, camera, renderContext, GizmoSubset.PreImageEffects);
                }
#endif
            }


                // At this point, m_CameraColorBuffer has been filled by either debug views are regular rendering so we can push it here.
                PushColorPickerDebugTexture(cmd, hdCamera, m_CameraColorBuffer);

                aovRequest.PushCameraTexture(cmd, AOVBuffers.Color, hdCamera, m_CameraColorBuffer, aovBuffers);
            RenderPostProcess(cullingResults, hdCamera, target.id, renderContext, cmd);

            // In developer build, we always render post process in m_AfterPostProcessBuffer at (0,0) in which we will then render debug.
            // Because of this, we need another blit here to the final render target at the right viewport.
            if (!HDUtils.PostProcessIsFinalPass() || aovRequest.isValid)
            {
                hdCamera.ExecuteCaptureActions(m_IntermediateAfterPostProcessBuffer, cmd);

                RenderDebug(hdCamera, cmd, cullingResults);
                using (new ProfilingSample(cmd, "Final Blit (Dev Build Only)"))
                {
                    var finalBlitParams = PrepareFinalBlitParameters(hdCamera);
                    BlitFinalCameraTexture(finalBlitParams, m_BlitPropertyBlock, m_IntermediateAfterPostProcessBuffer, target.id, cmd);
                }

                aovRequest.PushCameraTexture(cmd, AOVBuffers.Output, hdCamera, m_IntermediateAfterPostProcessBuffer, aovBuffers);
            }

            // XR mirror view and blit do device
            hdCamera.xr.StopLegacyStereo(camera, cmd, renderContext);
            hdCamera.xr.EndCamera(hdCamera, renderContext, cmd);

            // Send all required graphics buffer to client systems.
            SendGraphicsBuffers(cmd, hdCamera);

            // Due to our RT handle system we don't write into the backbuffer depth buffer (as our depth buffer can be bigger than the one provided)
            // So we need to do a copy of the corresponding part of RT depth buffer in the target depth buffer in various situation:
            // - RenderTexture (camera.targetTexture != null) has a depth buffer (camera.targetTexture.depth != 0)
            // - We are rendering into the main game view (i.e not a RenderTexture camera.cameraType == CameraType.Game && hdCamera.camera.targetTexture == null) in the editor for allowing usage of Debug.DrawLine and Debug.Ray.
            // - We draw Gizmo/Icons in the editor (hdCamera.camera.targetTexture != null && camera.targetTexture.depth != 0 - The Scene view has a targetTexture and a depth texture)
            // TODO: If at some point we get proper render target aliasing, we will be able to use the provided depth texture directly with our RT handle system
            // Note: Debug.DrawLine and Debug.Ray only work in editor, not in player
            var copyDepth = hdCamera.camera.targetTexture != null && hdCamera.camera.targetTexture.depth != 0;
#if UNITY_EDITOR
            copyDepth = copyDepth || hdCamera.isMainGameView; // Specific case of Debug.DrawLine and Debug.Ray
#endif
            if (copyDepth)
            {
                using (new ProfilingSample(cmd, "Copy Depth in Target Texture", CustomSamplerId.CopyDepth.GetSampler()))
                {
                    cmd.SetRenderTarget(target.id);
                    cmd.SetViewport(hdCamera.finalViewport);
                    m_CopyDepthPropertyBlock.SetTexture(HDShaderIDs._InputDepth, m_SharedRTManager.GetDepthStencilBuffer());
                    // When we are Main Game View we need to flip the depth buffer ourselves as we are after postprocess / blit that have already flipped the screen
                    m_CopyDepthPropertyBlock.SetInt("_FlipY", hdCamera.isMainGameView ? 1 : 0);
                    CoreUtils.DrawFullScreen(cmd, m_CopyDepth, m_CopyDepthPropertyBlock);
                }
            }
                aovRequest.PushCameraTexture(cmd, AOVBuffers.DepthStencil, hdCamera, m_SharedRTManager.GetDepthStencilBuffer(), aovBuffers);
                aovRequest.PushCameraTexture(cmd, AOVBuffers.Normals, hdCamera, m_SharedRTManager.GetNormalBuffer(), aovBuffers);
                if (m_Asset.currentPlatformRenderPipelineSettings.supportMotionVectors)
                    aovRequest.PushCameraTexture(cmd, AOVBuffers.MotionVectors, hdCamera, m_SharedRTManager.GetMotionVectorsBuffer(), aovBuffers);

#if UNITY_EDITOR
            // We need to make sure the viewport is correctly set for the editor rendering. It might have been changed by debug overlay rendering just before.
            cmd.SetViewport(hdCamera.finalViewport);

            // Render overlay Gizmos
            if (showGizmos)
                RenderGizmos(cmd, camera, renderContext, GizmoSubset.PostImageEffects);
#endif

                aovRequest.Execute(cmd, aovBuffers, RenderOutputProperties.From(hdCamera));
        }
        }

        struct BlitFinalCameraTextureParameters
        {
            public bool                     flip;
            public Rect                     viewport;
            public Material                 blitMaterial;
        }

        public RTHandleSystem.RTHandle GetExposureTexture(HDCamera hdCamera) =>
            m_PostProcessSystem.GetExposureTexture(hdCamera);

        BlitFinalCameraTextureParameters PrepareFinalBlitParameters(HDCamera hdCamera)
        {
            var parameters = new BlitFinalCameraTextureParameters();

            parameters.flip = hdCamera.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY || hdCamera.isMainGameView;
            parameters.blitMaterial = HDUtils.GetBlitMaterial(TextureXR.useTexArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D);
            parameters.viewport = hdCamera.finalViewport;

            return parameters;
        }

        static void BlitFinalCameraTexture(BlitFinalCameraTextureParameters parameters, MaterialPropertyBlock propertyBlock, RTHandle source, RenderTargetIdentifier destination, CommandBuffer cmd)
        {
            // Here we can't use the viewport scale provided in hdCamera. The reason is that this scale is for internal rendering before post process with dynamic resolution factored in.
            // Here the input texture is already at the viewport size but may be smaller than the RT itself (because of the RTHandle system) so we compute the scale specifically here.
            var scaleBias = new Vector4((float)parameters.viewport.width / source.rt.width, (float)parameters.viewport.height / source.rt.height, 0.0f, 0.0f);

            if (parameters.flip)
            {
                scaleBias.w = scaleBias.y;
                scaleBias.y *= -1;
            }

            propertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
            propertyBlock.SetVector(HDShaderIDs._BlitScaleBias, scaleBias);
            propertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);
            HDUtils.DrawFullScreen(cmd, parameters.viewport, parameters.blitMaterial, destination, propertyBlock, 0);
        }

        void SetupCameraProperties(HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            // The next 2 functions are required to flush the command buffer before calling functions directly on the render context.
            // This way, the commands will execute in the order specified by the C# code.
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (hdCamera.xr.legacyMultipassEnabled)
                renderContext.SetupCameraProperties(hdCamera.camera, hdCamera.xr.enabled, hdCamera.xr.legacyMultipassEye);
            else
                renderContext.SetupCameraProperties(hdCamera.camera, hdCamera.xr.enabled);
        }

        void InitializeGlobalResources(ScriptableRenderContext renderContext)
        {
            // Global resources initialization
            {
                // This is the main command buffer used for the frame.
                var cmd = CommandBufferPool.Get("");
                using (new ProfilingSample(
                    cmd, "HDRenderPipeline::Render Initialize Materials",
                    CustomSamplerId.HDRenderPipelineRender.GetSampler())
                )
                {
                    // Init material if needed
                    for (int bsdfIdx = 0; bsdfIdx < m_IBLFilterArray.Length; ++bsdfIdx)
                    {
                        if (!m_IBLFilterArray[bsdfIdx].IsInitialized())
                            m_IBLFilterArray[bsdfIdx].Initialize(cmd);
                    }

                    foreach (var material in m_MaterialList)
                        material.RenderInit(cmd);
                }
                using (new ProfilingSample(
                    cmd, "HDRenderPipeline::Render Initialize Textures",
                    CustomSamplerId.HDRenderPipelineRender.GetSampler())
                )
                {
                    TextureXR.Initialize(cmd, m_Asset.renderPipelineResources.shaders.clearUIntTextureCS);
                }
                renderContext.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        // $"HDProbe RenderCamera ({probeName}: {face:00} for viewer '{viewerName}')"
        unsafe string ComputeProbeCameraName(string probeName, int face, string viewerName)
        {
            // Interpolate the camera name with as few allocation as possible
            const string pattern1 = "HDProbe RenderCamera (";
            const string pattern2 = ": ";
            const string pattern3 = " for viewer '";
            const string pattern4 = "')";
            const int maxCharCountPerName = 40;
            const int charCountPerNumber = 2;

            probeName = probeName ?? string.Empty;
            viewerName = viewerName ?? "null";

            var probeNameSize = Mathf.Min(probeName.Length, maxCharCountPerName);
            var viewerNameSize = Mathf.Min(viewerName.Length, maxCharCountPerName);
            int size = pattern1.Length + probeNameSize
                + pattern2.Length + charCountPerNumber
                + pattern3.Length + viewerNameSize
                + pattern4.Length;

            var buffer = stackalloc char[size];
            var p = buffer;
            int i, c, s = 0;
            for (i = 0; i < pattern1.Length; ++i, ++p)
                *p = pattern1[i];
            for (i = 0, c = Mathf.Min(probeName.Length, maxCharCountPerName); i < c; ++i, ++p)
                *p = probeName[i];
            s += c;
            for (i = 0; i < pattern2.Length; ++i, ++p)
                *p = pattern2[i];

            // Fast, no-GC index.ToString("2")
            var temp = (face * 205) >> 11;  // 205/2048 is nearly the same as /10
            *(p++) = (char)(temp + '0');
            *(p++) = (char)((face - temp * 10) + '0');
            s += charCountPerNumber;

            for (i = 0; i < pattern3.Length; ++i, ++p)
                *p = pattern3[i];
            for (i = 0, c = Mathf.Min(viewerName.Length, maxCharCountPerName); i < c; ++i, ++p)
                *p = viewerName[i];
            s += c;
            for (i = 0; i < pattern4.Length; ++i, ++p)
                *p = pattern4[i];

            s += pattern1.Length + pattern2.Length + pattern3.Length + pattern4.Length;
            return new string(buffer, 0, s);
        }

        bool TryCalculateFrameParameters(
            Camera camera,
            XRPass xrPass,
            out HDAdditionalCameraData additionalCameraData,
            out HDCamera hdCamera,
            out ScriptableCullingParameters cullingParams
        )
        {
            // First, get aggregate of frame settings base on global settings, camera frame settings and debug settings
            // Note: the SceneView camera will never have additionalCameraData
            additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();
            hdCamera = default;
            cullingParams = default;

            FrameSettings currentFrameSettings = new FrameSettings();
            // Compute the FrameSettings actually used to draw the frame
            // FrameSettingsHistory do the same while keeping all step of FrameSettings aggregation in memory for DebugMenu
            if (frameSettingsHistoryEnabled)
                FrameSettingsHistory.AggregateFrameSettings(ref currentFrameSettings, camera, additionalCameraData, m_Asset);
            else
                FrameSettings.AggregateFrameSettings(ref currentFrameSettings, camera, additionalCameraData, m_Asset);

            // Specific pass to simply display the content of the camera buffer if users have fill it themselves (like video player)
            if (additionalCameraData && additionalCameraData.fullscreenPassthrough)
                return false;

            // Retrieve debug display settings to init FrameSettings, unless we are a reflection and in this case we don't have debug settings apply.
            DebugDisplaySettings debugDisplaySettings = (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview) ? s_NeutralDebugDisplaySettings : m_DebugDisplaySettings;

            // Disable post process if we enable debug mode or if the post process layer is disabled
            if (debugDisplaySettings.IsDebugDisplayEnabled())
            {
                if (debugDisplaySettings.IsDebugDisplayRemovePostprocess())
                {
                    currentFrameSettings.SetEnabled(FrameSettingsField.Postprocess, false);
                }

                // Disable exposure if required
                if (!debugDisplaySettings.DebugNeedsExposure())
                {
                    currentFrameSettings.SetEnabled(FrameSettingsField.ExposureControl, false);
                }

                // Disable SSS if luxmeter is enabled
                if (debugDisplaySettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuxMeter)
                {
                    currentFrameSettings.SetEnabled(FrameSettingsField.SubsurfaceScattering, false);
                }
            }

            // Disable object-motion vectors in everything but the game view
            if (camera.cameraType != CameraType.Game)
            {
                currentFrameSettings.SetEnabled(FrameSettingsField.ObjectMotionVectors, false);
            }

            hdCamera = HDCamera.GetOrCreate(camera, xrPass);

            // From this point, we should only use frame settings from the camera
            hdCamera.Update(currentFrameSettings, this, m_MSAASamples, xrPass);

            // Custom Render requires a proper HDCamera, so we return after the HDCamera was setup
            if (additionalCameraData != null && additionalCameraData.hasCustomRender)
                return false;

            if (hdCamera.xr.enabled)
            {
                if (!m_XRSystem.GetCullingParameters(camera, hdCamera.xr, out cullingParams))
                    return false;
            }
            else
            {
                if (!camera.TryGetCullingParameters(camera.stereoEnabled, out cullingParams))
                return false;
            }

            if (m_DebugDisplaySettings.IsCameraFreezeEnabled())
            {
                bool cameraIsFrozen = camera.name.Equals(m_DebugDisplaySettings.GetFrozenCameraName());
                if (cameraIsFrozen)
                {
                    if (!frozenCullingParamAvailable)
                    {
                        frozenCullingParams = cullingParams;
                        frozenCullingParamAvailable = true;
                    }
                    cullingParams = frozenCullingParams;
                }
            }
            else
            {
                frozenCullingParamAvailable = false;
            }

            LightLoopUpdateCullingParameters(ref cullingParams);
            hdCamera.UpdateStereoDependentState(ref cullingParams);

            // If we don't use environment light (like when rendering reflection probes)
            //   we don't have to cull them.
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SpecularLighting))
                cullingParams.cullingOptions |= CullingOptions.NeedsReflectionProbes;
            else
                cullingParams.cullingOptions &= ~CullingOptions.NeedsReflectionProbes;
            return true;
        }

        static bool TryCull(
            Camera camera,
            HDCamera hdCamera,
            ScriptableRenderContext renderContext,
            ScriptableCullingParameters cullingParams,
            ref HDCullingResults cullingResults
        )
        {
#if UNITY_EDITOR
            // emit scene view UI
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif

            // Set the LOD bias and store current value to be able to restore it.
            // Use a try/finalize pattern to be sure to restore properly the qualitySettings.lodBias
            var initialLODBias = QualitySettings.lodBias;
            var initialMaximumLODLevel = QualitySettings.maximumLODLevel;
            try
            {
                QualitySettings.lodBias = hdCamera.frameSettings.lodBiasMode.ComputeValue(
                    QualitySettings.lodBias,
                    hdCamera.frameSettings.lodBias
                );
                QualitySettings.maximumLODLevel = hdCamera.frameSettings.maximumLODLevelMode.ComputeValue(
                    QualitySettings.maximumLODLevel,
                    hdCamera.frameSettings.maximumLODLevel
                );

            var includeEnvLights = hdCamera.frameSettings.IsEnabled(FrameSettingsField.SpecularLighting);

            DecalSystem.CullRequest decalCullRequest = null;
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
            {
                // decal system needs to be updated with current camera, it needs it to set up culling and light list generation parameters
                decalCullRequest = GenericPool<DecalSystem.CullRequest>.Get();
                DecalSystem.instance.CurrentCamera = camera;
                DecalSystem.instance.BeginCull(decalCullRequest);
            }

            // TODO: use a parameter to select probe types to cull depending on what is enabled in framesettings
            var hdProbeCullState = new HDProbeCullState();
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RealtimePlanarReflection) && includeEnvLights)
                hdProbeCullState = HDProbeSystem.PrepareCull(camera);

            using (new ProfilingSample(null, "CullResults.Cull", CustomSamplerId.CullResultsCull.GetSampler()))
                cullingResults.cullingResults = renderContext.Cull(ref cullingParams);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RealtimePlanarReflection) && includeEnvLights)
                HDProbeSystem.QueryCullResults(hdProbeCullState, ref cullingResults.hdProbeCullingResults);
            else
                cullingResults.hdProbeCullingResults = default;

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
            {
                    using (new ProfilingSample(null, "DBufferPrepareDrawData",
                        CustomSamplerId.DBufferPrepareDrawData.GetSampler()))
                    DecalSystem.instance.EndCull(decalCullRequest, cullingResults.decalCullResults);
            }

            if (decalCullRequest != null)
            {
                decalCullRequest.Clear();
                GenericPool<DecalSystem.CullRequest>.Release(decalCullRequest);
            }

            return true;

            }
            finally
            {
                QualitySettings.lodBias = initialLODBias;
                QualitySettings.maximumLODLevel = initialMaximumLODLevel;
        }
        }

        void RenderGizmos(CommandBuffer cmd, Camera camera, ScriptableRenderContext renderContext, GizmoSubset gizmoSubset)
        {
#if UNITY_EDITOR
            if (UnityEditor.Handles.ShouldRenderGizmos())
            {
                bool renderPrePostprocessGizmos = (gizmoSubset == GizmoSubset.PreImageEffects);

                using (new ProfilingSample(cmd,
                    renderPrePostprocessGizmos ? "PrePostprocessGizmos" : "Gizmos",
                    renderPrePostprocessGizmos ? CustomSamplerId.GizmosPrePostprocess.GetSampler() : CustomSamplerId.Gizmos.GetSampler()))
                {
                    renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    renderContext.DrawGizmos(camera, gizmoSubset);
                }
            }
#endif
        }

        static RendererListDesc CreateOpaqueRendererListDesc(
            CullingResults cull,
            Camera camera,
            ShaderTagId passName,
            PerObjectData rendererConfiguration = 0,
            RenderQueueRange? renderQueueRange = null,
            RenderStateBlock? stateBlock = null,
            Material overrideMaterial = null,
            bool excludeObjectMotionVectors = false
        )
        {
            var result = new RendererListDesc(passName, cull, camera)
            {
                rendererConfiguration = rendererConfiguration,
                renderQueueRange = renderQueueRange != null ? renderQueueRange.Value : HDRenderQueue.k_RenderQueue_AllOpaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                stateBlock = stateBlock,
                overrideMaterial = overrideMaterial,
                excludeObjectMotionVectors = excludeObjectMotionVectors
            };
            return result;
        }

        static RendererListDesc CreateOpaqueRendererListDesc(
            CullingResults cull,
            Camera camera,
            ShaderTagId[] passNames,
            PerObjectData rendererConfiguration = 0,
            RenderQueueRange? renderQueueRange = null,
            RenderStateBlock? stateBlock = null,
            Material overrideMaterial = null,
            bool excludeObjectMotionVectors = false
        )
        {
            var result = new RendererListDesc(passNames, cull, camera)
            {
                rendererConfiguration = rendererConfiguration,
                renderQueueRange = renderQueueRange != null ? renderQueueRange.Value : HDRenderQueue.k_RenderQueue_AllOpaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                stateBlock = stateBlock,
                overrideMaterial = overrideMaterial,
                excludeObjectMotionVectors = excludeObjectMotionVectors
            };
            return result;
        }

        protected static RendererListDesc CreateTransparentRendererListDesc(
            CullingResults cull,
            Camera camera,
            ShaderTagId passName,
            PerObjectData rendererConfiguration = 0,
            RenderQueueRange? renderQueueRange = null,
            RenderStateBlock? stateBlock = null,
            Material overrideMaterial = null,
            bool excludeObjectMotionVectors = false
        )
        {
            var result = new RendererListDesc(passName, cull, camera)
            {
                rendererConfiguration = rendererConfiguration,
                renderQueueRange = renderQueueRange != null ? renderQueueRange.Value : HDRenderQueue.k_RenderQueue_AllTransparent,
                sortingCriteria = SortingCriteria.CommonTransparent | SortingCriteria.RendererPriority,
                stateBlock = stateBlock,
                overrideMaterial = overrideMaterial,
                excludeObjectMotionVectors = excludeObjectMotionVectors
            };
            return result;
        }

        protected static RendererListDesc CreateTransparentRendererListDesc(
            CullingResults cull,
            Camera camera,
            ShaderTagId[] passNames,
            PerObjectData rendererConfiguration = 0,
            RenderQueueRange? renderQueueRange = null,
            RenderStateBlock? stateBlock = null,
            Material overrideMaterial = null,
            bool excludeObjectMotionVectors = false
        )
        {
            var result = new RendererListDesc(passNames, cull, camera)
            {
                rendererConfiguration = rendererConfiguration,
                renderQueueRange = renderQueueRange != null ? renderQueueRange.Value : HDRenderQueue.k_RenderQueue_AllTransparent,
                sortingCriteria = SortingCriteria.CommonTransparent | SortingCriteria.RendererPriority,
                stateBlock = stateBlock,
                overrideMaterial = overrideMaterial,
                excludeObjectMotionVectors = excludeObjectMotionVectors
            };
            return result;
        }

        protected static void DrawOpaqueRendererList(in ScriptableRenderContext renderContext, CommandBuffer cmd, in FrameSettings frameSettings, RendererList rendererList)
        {
            if (!frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                return;

            HDUtils.DrawRendererList(renderContext, cmd, rendererList);
        }

        protected static void DrawTransparentRendererList(in ScriptableRenderContext renderContext, CommandBuffer cmd, in FrameSettings frameSettings, RendererList rendererList)
        {
            if (!frameSettings.IsEnabled(FrameSettingsField.TransparentObjects))
                return;

            HDUtils.DrawRendererList(renderContext, cmd, rendererList);
        }

        void AccumulateDistortion(CullingResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Distortion))
                return;

            using (new ProfilingSample(cmd, "Distortion", CustomSamplerId.Distortion.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, m_DistortionBuffer, m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.Color, Color.clear);

                // Only transparent object can render distortion vectors
                var rendererList = RendererList.Create(CreateTransparentRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_DistortionVectorsName));
                DrawTransparentRendererList(renderContext, cmd, hdCamera.frameSettings, rendererList);
            }
        }

        void RenderDistortion(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Distortion))
                return;

            using (new ProfilingSample(cmd, "ApplyDistortion", CustomSamplerId.ApplyDistortion.GetSampler()))
            {
                var currentColorPyramid = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);

                HDUtils.SetRenderTarget(cmd, m_CameraColorBuffer);
                // TODO: Set stencil stuff via parameters rather than hardcoding it in shader.
                m_ApplyDistortionMaterial.SetTexture(HDShaderIDs._DistortionTexture, m_DistortionBuffer);
                m_ApplyDistortionMaterial.SetTexture(HDShaderIDs._ColorPyramidTexture, currentColorPyramid);

                var size = new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight);
                m_ApplyDistortionMaterial.SetVector(HDShaderIDs._Size, size);

                HDUtils.DrawFullScreen(cmd, m_ApplyDistortionMaterial, m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer(), null, 0);
            }
        }

        struct DepthPrepassParameters
        {
            public string              passName;
            public RendererListDesc    depthOnlyRendererListDesc;
            public RendererListDesc    mrtRendererListDesc;
            public bool                hasDepthOnlyPass;
            public bool                shouldRenderMotionVectorAfterGBuffer;
#if ENABLE_RAYTRACING
            public RendererListDesc    rayTracingOpaqueRLDesc;
            public RendererListDesc    rayTracingTransparentRLDesc;
#endif
        }

        DepthPrepassParameters PrepareDepthPrepass(CullingResults cull, HDCamera hdCamera)
        {
            // Guidelines:
            // Lit shader can be in deferred or forward mode. In this case we use "DepthOnly" pass with "GBuffer" or "Forward" pass name
            // Other shader, including unlit are always forward and use "DepthForwardOnly" with "ForwardOnly" pass.
            // Those pass are exclusive so use only "DepthOnly" or "DepthForwardOnly" but not both at the same time, same for "Forward" and "DepthForwardOnly"
            // Any opaque material rendered in forward should have a depth prepass. If there is no depth prepass the lighting will be incorrect (deferred shadowing, contact shadow, SSAO), this may be acceptable depends on usage

            // Whatever the configuration we always render first opaque object then opaque alpha tested as they are more costly to render and could be reject by early-z
            // (but no Hi-z as it is disable with clip instruction). This is handled automatically with the RenderQueue value (OpaqueAlphaTested have a different value and thus are sorted after Opaque)

            // Forward material always output normal buffer.
            // Deferred material never output normal buffer.
            // Caution: Unlit material let normal buffer untouch. Caution as if people try to filter normal buffer, it can result in weird result.
            // TODO: Do we need a stencil bit to identify normal buffer not fill by unlit? So don't execute SSAO / SRR ?

            // Additional guidelines for motion vector:
            // We render object motion vector at the same time than depth prepass with MRT to save drawcall. Depth buffer is then fill with combination of depth prepass + motion vector.
            // For this we render first all objects that render depth only, then object that require object motion vector.
            // We use the excludeMotion filter option of DrawRenderer to gather object without object motion vector (only C++ can know if an object have object motion vector).
            // Caution: if there is no depth prepass we must render object motion vector after GBuffer pass otherwise some depth only objects can hide objects with motion vector and overwrite depth buffer but not update
            // the motion vector buffer resulting in artifacts

            var result = new DepthPrepassParameters();

            // To avoid rendering objects twice (once in the depth pre-pass and once in the motion vector pass when the motion vector pass is enabled) we exclude the objects that have motion vectors.
            bool fullDeferredPrepass = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering) || m_DbufferManager.enableDecals;
            // To avoid rendering objects twice (once in the depth pre-pass and once in the motion vector pass when the motion vector pass is enabled) we exclude the objects that have motion vectors.
            bool objectMotionEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ObjectMotionVectors);

            result.shouldRenderMotionVectorAfterGBuffer = (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred) && !fullDeferredPrepass;
            result.hasDepthOnlyPass = false;

            switch (hdCamera.frameSettings.litShaderMode)
            {
                case LitShaderMode.Forward:
                    result.passName = "Depth Prepass (forward)";
                    result.mrtRendererListDesc = CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_DepthOnlyAndDepthForwardOnlyPassNames, excludeObjectMotionVectors: objectMotionEnabled);
                    break;
                case LitShaderMode.Deferred:
                    result.passName = fullDeferredPrepass ? (m_DbufferManager.enableDecals ? "Depth Prepass (deferred) forced by Decals" : "Depth Prepass (deferred)") : "Depth Prepass (deferred incomplete)";
                    bool excludeMotion = fullDeferredPrepass ? objectMotionEnabled : false;

                    // First deferred alpha tested materials. Alpha tested object have always a prepass even if enableDepthPrepassWithDeferredRendering is disabled
                    var partialPrepassRenderQueueRange = new RenderQueueRange { lowerBound = (int)RenderQueue.AlphaTest, upperBound = (int)RenderQueue.GeometryLast - 1 };

                    result.hasDepthOnlyPass = true;

                    // First deferred material
                    result.depthOnlyRendererListDesc = CreateOpaqueRendererListDesc(
                        cull, hdCamera.camera, m_DepthOnlyPassNames,
                        renderQueueRange: fullDeferredPrepass ? HDRenderQueue.k_RenderQueue_AllOpaque : partialPrepassRenderQueueRange,
                        excludeObjectMotionVectors: excludeMotion);

                    // Then forward only material that output normal buffer
                    result.mrtRendererListDesc = CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_DepthForwardOnlyPassNames, excludeObjectMotionVectors: excludeMotion);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown ShaderLitMode");
            }

#if ENABLE_RAYTRACING
            HDRaytracingEnvironment currentEnv = m_RayTracingManager.CurrentEnvironment();
            RecursiveRendering recursiveRendering = VolumeManager.instance.stack.GetComponent<RecursiveRendering>();

            if (currentEnv != null && recursiveRendering.enable.value)
            {
                result.rayTracingOpaqueRLDesc = CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_DepthOnlyAndDepthForwardOnlyPassNames, renderQueueRange: HDRenderQueue.k_RenderQueue_AllOpaqueRaytracing);
                result.rayTracingTransparentRLDesc = CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_DepthOnlyAndDepthForwardOnlyPassNames, renderQueueRange: HDRenderQueue.k_RenderQueue_AllTransparentRaytracing);
            }
#endif

            return result;
        }

        static void RenderDepthPrepass( ScriptableRenderContext renderContext,
                                        CommandBuffer cmd,
                                        FrameSettings frameSettings,
                                        RenderTargetIdentifier[] mrt,
                                        RTHandle depthBuffer,
                                        in RendererList depthOnlyRendererList,
                                        in RendererList mrtRendererList,
                                        bool hasDepthOnlyPass
#if ENABLE_RAYTRACING
                                        , HDRaytracingManager       rayTracingManager,
                                        in RendererList             rayTracingOpaqueRL,
                                        in RendererList             rayTracingTransparentRL
#endif
                                        )
        {
            HDUtils.SetRenderTarget(cmd, depthBuffer);
                        // XRTODO: wait for XR SDK integration and implement custom version in HDUtils with dynamic resolution support
                        //XRUtils.DrawOcclusionMesh(cmd, hdCamera.camera, hdCamera.camera.stereoEnabled);

            if (hasDepthOnlyPass)
            {
                DrawOpaqueRendererList(renderContext, cmd, frameSettings, depthOnlyRendererList);
            }

            HDUtils.SetRenderTarget(cmd, mrt, depthBuffer);
            DrawOpaqueRendererList(renderContext, cmd, frameSettings, mrtRendererList);

#if ENABLE_RAYTRACING
            // If there is a ray-tracing environment and the feature is enabled we want to push these objects to the prepass
            HDRaytracingEnvironment currentEnv = rayTracingManager.CurrentEnvironment();
            var rrSettings = VolumeManager.instance.stack.GetComponent<RecursiveRendering>();
            // We want the opaque objects to be in the prepass so that we avoid rendering uselessly the pixels before raytracing them
            if (currentEnv != null && rrSettings.enable.value)
            {
                HDUtils.DrawRendererList(renderContext, cmd, rayTracingOpaqueRL);
                HDUtils.DrawRendererList(renderContext, cmd, rayTracingTransparentRL);
            }
#endif
        }

        // RenderDepthPrepass render both opaque and opaque alpha tested based on engine configuration.
        // Lit Forward only: We always render all materials
        // Lit Deferred: We always render depth prepass for alpha tested (optimization), other deferred material are render based on engine configuration.
        // Forward opaque with deferred renderer (DepthForwardOnly pass): We always render all materials
        // True is return if motion vector must be render after GBuffer pass
        bool RenderDepthPrepass(CullingResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            var depthPrepassParameters = PrepareDepthPrepass(cull, hdCamera);
            var depthOnlyRendererList = RendererList.Create(depthPrepassParameters.depthOnlyRendererListDesc);
            var mrtDepthRendererList = RendererList.Create(depthPrepassParameters.mrtRendererListDesc);

#if ENABLE_RAYTRACING
            var rayTracingOpaqueRendererList = RendererList.Create(depthPrepassParameters.rayTracingOpaqueRLDesc);
            var rayTracingTransparentRendererList = RendererList.Create(depthPrepassParameters.rayTracingTransparentRLDesc);
#endif

            using (new ProfilingSample(cmd, depthPrepassParameters.passName, CustomSamplerId.DepthPrepass.GetSampler()))
            {
                RenderDepthPrepass( renderContext, cmd, hdCamera.frameSettings,
                                    m_SharedRTManager.GetPrepassBuffersRTI(hdCamera.frameSettings),
                                    m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)),
                                    depthOnlyRendererList,
                                    mrtDepthRendererList,
                                    depthPrepassParameters.hasDepthOnlyPass
#if ENABLE_RAYTRACING
                                    , m_RayTracingManager,
                                    rayTracingOpaqueRendererList,
                                    rayTracingTransparentRendererList
#endif
                                    );
        }

            return depthPrepassParameters.shouldRenderMotionVectorAfterGBuffer;
        }

        // RenderGBuffer do the gbuffer pass. This is solely call with deferred. If we use a depth prepass, then the depth prepass will perform the alpha testing for opaque alpha tested and we don't need to do it anymore
        // during Gbuffer pass. This is handled in the shader and the depth test (equal and no depth write) is done here.
        void RenderGBuffer(CullingResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred)
                return;

            using (new ProfilingSample(cmd, m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() ? "GBuffer Debug" : "GBuffer", CustomSamplerId.GBuffer.GetSampler()))
            {
                // setup GBuffer for rendering
                HDUtils.SetRenderTarget(cmd, m_GbufferManager.GetBuffersRTI(hdCamera.frameSettings), m_SharedRTManager.GetDepthStencilBuffer());

                var rendererList = RendererList.Create(CreateOpaqueRendererListDesc(cull, hdCamera.camera, HDShaderPassNames.s_GBufferName, m_CurrentRendererConfigurationBakedLighting));
                DrawOpaqueRendererList(renderContext, cmd, hdCamera.frameSettings, rendererList);

                m_GbufferManager.BindBufferAsTextures(cmd);
            }
        }

        void RenderDBuffer(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, CullingResults cullResults)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
                return;

            using (new ProfilingSample(cmd, "DBufferRender", CustomSamplerId.DBufferRender.GetSampler()))
            {
                // We need to copy depth buffer texture if we want to bind it at this stage
                CopyDepthBufferIfNeeded(cmd);

                bool rtCount4 = m_Asset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;
                // Depth texture is now ready, bind it.
                cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, m_SharedRTManager.GetDepthTexture());
                m_DbufferManager.ClearAndSetTargets(cmd, hdCamera, rtCount4, m_SharedRTManager.GetDepthStencilBuffer());
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortingSettings = new SortingSettings(hdCamera.camera)
                {
                    criteria = SortingCriteria.CommonOpaque
                };

                var drawSettings = new DrawingSettings(HDShaderPassNames.s_EmptyName, sortingSettings)
                {
                    perObjectData = PerObjectData.None
                };

                if (rtCount4)
                {
                    drawSettings.SetShaderPassName(0, HDShaderPassNames.s_MeshDecalsMName);
                    drawSettings.SetShaderPassName(1, HDShaderPassNames.s_MeshDecalsAOName);
                    drawSettings.SetShaderPassName(2, HDShaderPassNames.s_MeshDecalsMAOName);
                    drawSettings.SetShaderPassName(3, HDShaderPassNames.s_MeshDecalsSName);
                    drawSettings.SetShaderPassName(4, HDShaderPassNames.s_MeshDecalsMSName);
                    drawSettings.SetShaderPassName(5, HDShaderPassNames.s_MeshDecalsAOSName);
                    drawSettings.SetShaderPassName(6, HDShaderPassNames.s_MeshDecalsMAOSName);
                    drawSettings.SetShaderPassName(7, HDShaderPassNames.s_ShaderGraphMeshDecalsName4RT);
                }
                else
                {
                    drawSettings.SetShaderPassName(0, HDShaderPassNames.s_MeshDecals3RTName);
                    drawSettings.SetShaderPassName(1, HDShaderPassNames.s_ShaderGraphMeshDecalsName3RT);
                }

                FilteringSettings filterRenderersSettings = new FilteringSettings(HDRenderQueue.k_RenderQueue_AllOpaque);
                renderContext.DrawRenderers(cullResults, ref drawSettings, ref filterRenderersSettings);
                DecalSystem.instance.RenderIntoDBuffer(cmd);
                m_DbufferManager.UnSetHTile(cmd);
                m_DbufferManager.SetHTileTexture(cmd);  // mask per 8x8 tile used for optimization when looking up dbuffer values
            }
        }

        // DBufferNormalPatch will patch the normal buffer with data from DBuffer for forward material.
        // As forward material output normal during depth prepass, they aren't affected by decal, and thus we need to patch the normal buffer.
        void DBufferNormalPatch(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, CullingResults cullResults)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
                return;

            if (m_DbufferManager.enableDecals && !hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)) // MSAA not supported
            {
                using (new ProfilingSample(cmd, "DBuffer Normal (forward)", CustomSamplerId.DBufferNormal.GetSampler()))
                {
                    int stencilMask;
                    int stencilRef;
                    switch (hdCamera.frameSettings.litShaderMode)
                    {
                        case LitShaderMode.Forward:  // in forward rendering all pixels that decals wrote into have to be composited
                            stencilMask = (int)StencilBitMask.Decals;
                            stencilRef = (int)StencilBitMask.Decals;
                            break;
                        case LitShaderMode.Deferred: // in deferred rendering only pixels affected by both forward materials and decals need to be composited
                            stencilMask = (int)StencilBitMask.Decals | (int)StencilBitMask.DecalsForwardOutputNormalBuffer;
                            stencilRef = (int)StencilBitMask.Decals | (int)StencilBitMask.DecalsForwardOutputNormalBuffer;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Unknown ShaderLitMode");
                    }

                    m_DecalNormalBufferMaterial.SetInt(HDShaderIDs._DecalNormalBufferStencilReadMask, stencilMask);
                    m_DecalNormalBufferMaterial.SetInt(HDShaderIDs._DecalNormalBufferStencilRef, stencilRef);

                    HDUtils.SetRenderTarget(cmd, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetRandomWriteTarget(1, m_SharedRTManager.GetNormalBuffer());
                    cmd.DrawProcedural(Matrix4x4.identity, m_DecalNormalBufferMaterial, 0, MeshTopology.Triangles, 3, 1);
                    cmd.ClearRandomWriteTargets();
                }
            }
        }

        RendererListDesc PrepareForwardEmissiveRendererList(CullingResults cullResults, HDCamera hdCamera)
        {
            var result = new RendererListDesc(m_DecalsEmissivePassNames, cullResults, hdCamera.camera)
            {
                renderQueueRange = HDRenderQueue.k_RenderQueue_AllOpaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                rendererConfiguration = PerObjectData.None
            };

            return result;
        }

        void RenderForwardEmissive(CullingResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
                return;

            using (new ProfilingSample(cmd, "DecalsForwardEmissive", CustomSamplerId.DecalsForwardEmissive.GetSampler()))
            {
                HDUtils.DrawRendererList(renderContext, cmd, RendererList.Create(PrepareForwardEmissiveRendererList(cullResults, hdCamera)));
                DecalSystem.instance.RenderForwardEmissive(cmd);
            }
        }

        void RenderDebugViewMaterial(CullingResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "DisplayDebug ViewMaterial", CustomSamplerId.DisplayDebugViewMaterial.GetSampler()))
            {
                if (m_CurrentDebugDisplaySettings.data.materialDebugSettings.IsDebugGBufferEnabled() && hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
                {
                    using (new ProfilingSample(cmd, "DebugViewMaterialGBuffer", CustomSamplerId.DebugViewMaterialGBuffer.GetSampler()))
                    {
                        HDUtils.DrawFullScreen(cmd, m_currentDebugViewMaterialGBuffer, m_CameraColorBuffer);
                    }
                }
                else
                {
                    // When rendering debug material we shouldn't rely on a depth prepass for optimizing the alpha clip test. As it is control on the material inspector side
                    // we must override the state here.

                    HDUtils.SetRenderTarget(cmd, m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.All, Color.clear);
                    // Render Opaque forward
                    var rendererListOpaque = RendererList.Create(CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_AllForwardOpaquePassNames, m_CurrentRendererConfigurationBakedLighting, stateBlock: m_DepthStateOpaque));
                    DrawOpaqueRendererList(renderContext, cmd, hdCamera.frameSettings, rendererListOpaque);

                    // Render forward transparent
                    var rendererListTransparent = RendererList.Create(CreateTransparentRendererListDesc(cull, hdCamera.camera, m_AllTransparentPassNames, m_CurrentRendererConfigurationBakedLighting, stateBlock: m_DepthStateOpaque));
                    DrawTransparentRendererList(renderContext, cmd, hdCamera.frameSettings, rendererListTransparent);
                }
            }
        }

        void UpdateSkyEnvironment(HDCamera hdCamera, CommandBuffer cmd)
        {
            m_SkyManager.UpdateEnvironment(hdCamera, GetCurrentSunLight(), cmd);
        }

        public void RequestSkyEnvironmentUpdate()
        {
            m_SkyManager.RequestEnvironmentUpdate();
        }

        void RenderSky(HDCamera hdCamera, CommandBuffer cmd)
        {
            if(m_DebugDisplaySettings.GetDebugLightingMode() == DebugLightingMode.MatcapView)
            {
                return;
            }

            var colorBuffer = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA) ? m_CameraColorMSAABuffer : m_CameraColorBuffer;
            var depthBuffer = m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA));

            var visualEnv = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            m_SkyManager.RenderSky(hdCamera, GetCurrentSunLight(), colorBuffer, depthBuffer, m_CurrentDebugDisplaySettings, cmd);

            if (visualEnv.fogType.value != FogType.None)
            {
                var pixelCoordToViewDirWS = hdCamera.mainViewConstants.pixelCoordToViewDirWS;
                m_SkyManager.RenderOpaqueAtmosphericScattering(cmd, hdCamera, colorBuffer, depthBuffer, pixelCoordToViewDirWS, hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA));
            }
        }

        public Texture2D ExportSkyToTexture()
        {
            return m_SkyManager.ExportSkyToTexture();
        }


        RendererListDesc PrepareForwardOpaqueRendererList(CullingResults cullResults, HDCamera hdCamera)
        {
            var passNames = hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward
                ? m_ForwardAndForwardOnlyPassNames
                : m_ForwardOnlyPassNames;
            return  CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, passNames, m_CurrentRendererConfigurationBakedLighting);
        }


        // Guidelines: In deferred by default there is no opaque in forward. However it is possible to force an opaque material to render in forward
        // by using the pass "ForwardOnly". In this case the .shader should not have "Forward" but only a "ForwardOnly" pass.
        // It must also have a "DepthForwardOnly" and no "DepthOnly" pass as forward material (either deferred or forward only rendering) have always a depth pass.
        // The RenderForward pass will render the appropriate pass depends on the engine settings. In case of forward only rendering, both "Forward" pass and "ForwardOnly" pass
        // material will be render for both transparent and opaque. In case of deferred, both path are used for transparent but only "ForwardOnly" is use for opaque.
        // (Thus why "Forward" and "ForwardOnly" are exclusive, else they will render two times"
        void RenderForwardOpaque(CullingResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            bool debugDisplay = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();
            using (new ProfilingSample(cmd, debugDisplay ? "Forward Opaque Debug" : "Forward Opaque", CustomSamplerId.ForwardPassName.GetSampler()))
            {
                bool useFptl = hdCamera.frameSettings.IsEnabled(FrameSettingsField.FPTLForForwardOpaque);
                bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);

                RenderTargetIdentifier[] renderTarget = null;

                // In case of forward SSS we will bind all the required target. It is up to the shader to write into it or not.
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
                {
                    renderTarget = m_MRTWithSSS;
                    renderTarget[0] = msaa ? m_CameraColorMSAABuffer : m_CameraColorBuffer; // Store the specular color
                    renderTarget[1] = msaa ? m_CameraSssDiffuseLightingMSAABuffer : m_CameraSssDiffuseLightingBuffer;
                    renderTarget[2] = msaa ? GetSSSBufferMSAA() : GetSSSBuffer();
                }
                else
                {
                    renderTarget = mMRTSingle;
                    renderTarget[0] = msaa ? m_CameraColorMSAABuffer : m_CameraColorBuffer;
                }

                RenderForwardRendererList(hdCamera.frameSettings,
                                            RendererList.Create(PrepareForwardOpaqueRendererList(cullResults, hdCamera)),
                                            renderTarget,
                                            m_SharedRTManager.GetDepthStencilBuffer(msaa),
                                            useFptl ? m_TileAndClusterData.lightList : m_TileAndClusterData.perVoxelLightLists,
                                            true, renderContext, cmd);
            }
        }

        static bool NeedMotionVectorForTransparent(FrameSettings frameSettings)
        {
            return frameSettings.IsEnabled(FrameSettingsField.MotionVectors) && frameSettings.IsEnabled(FrameSettingsField.TransparentsWriteMotionVector);
        }

        RendererListDesc PrepareForwardTransparentRendererList(CullingResults cullResults, HDCamera hdCamera, bool preRefraction)
        {
            RenderQueueRange transparentRange;
            if (preRefraction)
            {
                transparentRange = HDRenderQueue.k_RenderQueue_PreRefraction;
            }
            else if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
            {
                transparentRange = HDRenderQueue.k_RenderQueue_Transparent;
            }
            else // Low res transparent disabled
            {
                transparentRange = HDRenderQueue.k_RenderQueue_TransparentWithLowRes;
            }

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RoughRefraction))
            {
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
                    transparentRange = HDRenderQueue.k_RenderQueue_AllTransparent;
                else
                    transparentRange = HDRenderQueue.k_RenderQueue_AllTransparentWithLowRes;
            }

            if (NeedMotionVectorForTransparent(hdCamera.frameSettings))
            {
                m_CurrentRendererConfigurationBakedLighting |= PerObjectData.MotionVectors; // This will enable the flag for low res transparent as well
            }

            var passNames = m_Asset.currentPlatformRenderPipelineSettings.supportTransparentBackface ? m_AllTransparentPassNames : m_TransparentNoBackfaceNames;
            return CreateTransparentRendererListDesc(cullResults, hdCamera.camera, passNames, m_CurrentRendererConfigurationBakedLighting, transparentRange);
        }


        void RenderForwardTransparent(CullingResults cullResults, HDCamera hdCamera, bool preRefraction, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            // If rough refraction are turned off, we render all transparents in the Transparent pass and we skip the PreRefraction one.
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RoughRefraction) && preRefraction)
            {
                return;
            }

            string passName;
            bool debugDisplay = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();
            if (debugDisplay)
                passName = preRefraction ? "Forward PreRefraction Debug" : "Forward Transparent Debug";
            else
                passName = preRefraction ? "Forward PreRefraction" : "Forward Transparent";

            using (new ProfilingSample(cmd, passName, CustomSamplerId.ForwardPassName.GetSampler()))
            {
                bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
                bool renderMotionVecForTransparent = NeedMotionVectorForTransparent(hdCamera.frameSettings);
                cmd.SetGlobalInt(HDShaderIDs._ColorMaskTransparentVel, renderMotionVecForTransparent ? (int)ColorWriteMask.All : 0);

                m_MRTTransparentMotionVec[0] = msaa ? m_CameraColorMSAABuffer : m_CameraColorBuffer;
                m_MRTTransparentMotionVec[1] = renderMotionVecForTransparent ? m_SharedRTManager.GetMotionVectorsBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
                    // It doesn't really matter what gets bound here since the color mask state set will prevent this from ever being written to. However, we still need to bind something
                    // to avoid warnings about unbound render targets. The following rendertarget could really be anything if renderVelocitiesForTransparent, here the normal buffer
                    // as it is guaranteed to exist and to have the same size.
                    // to avoid warnings about unbound render targets.
                    : m_SharedRTManager.GetNormalBuffer(msaa);

                if ((hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals)) && (DecalSystem.m_DecalDatasCount > 0)) // enable d-buffer flag value is being interpreted more like enable decals in general now that we have clustered
                                                                                                                          // decal datas count is 0 if no decals affect transparency
                {
                    DecalSystem.instance.SetAtlas(cmd); // for clustered decals
                }

                RenderForwardRendererList(hdCamera.frameSettings,
                                            RendererList.Create(PrepareForwardTransparentRendererList(cullResults, hdCamera, preRefraction)),
                                            m_MRTTransparentMotionVec,
                                            m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)),
                                            m_TileAndClusterData.perVoxelLightLists,
                                            false, renderContext, cmd);
            }
        }

        static void RenderForwardRendererList(  FrameSettings               frameSettings,
                                                RendererList                rendererList,
                                                RenderTargetIdentifier[]    renderTarget,
                                                RTHandle                    depthBuffer,
                                                ComputeBuffer               lightListBuffer,
                                                bool                        opaque,
                                                ScriptableRenderContext     renderContext,
                                                CommandBuffer               cmd)
        {
            // Note: SHADOWS_SHADOWMASK keyword is enabled in HDRenderPipeline.cs ConfigureForShadowMask
            bool useFptl = opaque && frameSettings.IsEnabled(FrameSettingsField.FPTLForForwardOpaque);

            // say that we want to use tile/cluster light loop
            CoreUtils.SetKeyword(cmd, "USE_FPTL_LIGHTLIST", useFptl);
            CoreUtils.SetKeyword(cmd, "USE_CLUSTERED_LIGHTLIST", !useFptl);
            cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, lightListBuffer);

            HDUtils.SetRenderTarget(cmd, renderTarget, depthBuffer);
            if (opaque)
                DrawOpaqueRendererList(renderContext, cmd, frameSettings, rendererList);
            else
                DrawTransparentRendererList(renderContext, cmd, frameSettings, rendererList);
        }

        // This is use to Display legacy shader with an error shader
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void RenderForwardError(CullingResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            using (new ProfilingSample(cmd, "Forward Error", CustomSamplerId.RenderForwardError.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer());
                var rendererList = RendererList.Create(CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, m_ForwardErrorPassNames, renderQueueRange: RenderQueueRange.all, overrideMaterial: m_ErrorMaterial));
                HDUtils.DrawRendererList(renderContext, cmd, rendererList);
            }
        }

        void RenderTransparentDepthPrepass(CullingResults cull, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentPrepass))
            {
                // Render transparent depth prepass after opaque one
                using (new ProfilingSample(cmd, "Transparent Depth Prepass", CustomSamplerId.TransparentDepthPrepass.GetSampler()))
                {
                    HDUtils.SetRenderTarget(cmd, m_SharedRTManager.GetDepthStencilBuffer());
                    var rendererList = RendererList.Create(CreateTransparentRendererListDesc(cull, hdCamera.camera, m_TransparentDepthPrepassNames));
                    DrawTransparentRendererList(renderContext, cmd, hdCamera.frameSettings, rendererList);
                }
            }
        }

        void RenderTransparentDepthPostpass(CullingResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.TransparentPostpass))
                return;

            using (new ProfilingSample(cmd, "Transparent Depth Post ", CustomSamplerId.TransparentDepthPostpass.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, m_SharedRTManager.GetDepthStencilBuffer());
                var rendererList = RendererList.Create(CreateTransparentRendererListDesc(cullResults, hdCamera.camera, m_TransparentDepthPostpassNames));
                DrawTransparentRendererList(renderContext, cmd, hdCamera.frameSettings, rendererList);

#if ENABLE_RAYTRACING
                // If there is a ray-tracing environment and the feature is enabled we want to push these objects to the transparent postpass (they are not rendered in the first call because they are not in the generic transparent render queue)
                HDRaytracingEnvironment currentEnv = m_RayTracingManager.CurrentEnvironment();
                var rrSettings = VolumeManager.instance.stack.GetComponent<RecursiveRendering>();
                if (currentEnv != null && rrSettings.enable.value)
                {
                    var rendererListRT = RendererList.Create(CreateTransparentRendererListDesc(cullResults, hdCamera.camera, m_TransparentDepthPostpassNames, renderQueueRange: HDRenderQueue.k_RenderQueue_AllTransparentRaytracing));
                    DrawTransparentRendererList(renderContext, cmd, hdCamera.frameSettings, rendererListRT);
                }
#endif
            }
        }

        void RenderLowResTransparent(CullingResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
                return;

            using (new ProfilingSample(cmd, "Low Res Transparent", CustomSamplerId.LowResTransparent.GetSampler()))
            {
                cmd.SetGlobalInt(HDShaderIDs._OffScreenRendering, 1);
                cmd.SetGlobalInt(HDShaderIDs._OffScreenDownsampleFactor, 2);
                HDUtils.SetRenderTarget(cmd, m_LowResTransparentBuffer, m_SharedRTManager.GetLowResDepthBuffer(), clearFlag: ClearFlag.Color, Color.black);
                RenderQueueRange transparentRange = HDRenderQueue.k_RenderQueue_LowTransparent;
                var passNames = m_Asset.currentPlatformRenderPipelineSettings.supportTransparentBackface ? m_AllTransparentPassNames : m_TransparentNoBackfaceNames;
                var rendererList = RendererList.Create(CreateTransparentRendererListDesc(cullResults, hdCamera.camera, passNames, m_CurrentRendererConfigurationBakedLighting, HDRenderQueue.k_RenderQueue_LowTransparent));
                DrawTransparentRendererList(renderContext, cmd, hdCamera.frameSettings, rendererList);
                cmd.SetGlobalInt(HDShaderIDs._OffScreenRendering, 0);
                cmd.SetGlobalInt(HDShaderIDs._OffScreenDownsampleFactor, 1);
            }
        }

        void RenderObjectsMotionVectors(CullingResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ObjectMotionVectors))
                return;

            using (new ProfilingSample(cmd, "Objects Motion Vectors Rendering", CustomSamplerId.ObjectsMotionVector.GetSampler()))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                HDUtils.SetRenderTarget(cmd, m_SharedRTManager.GetMotionVectorsPassBuffersRTI(hdCamera.frameSettings), m_SharedRTManager.GetDepthStencilBuffer(hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)));
                var rendererList = RendererList.Create(CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_MotionVectorsName, PerObjectData.MotionVectors));
                DrawOpaqueRendererList(renderContext, cmd, hdCamera.frameSettings, rendererList);
            }
        }

        void RenderCameraMotionVectors(CullingResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors))
                return;

            using (new ProfilingSample(cmd, "Camera Motion Vectors Rendering", CustomSamplerId.CameraMotionVectors.GetSampler()))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                HDUtils.DrawFullScreen(cmd, m_CameraMotionVectorsMaterial, m_SharedRTManager.GetMotionVectorsBuffer(), m_SharedRTManager.GetDepthStencilBuffer(), null, 0);

#if UNITY_EDITOR

                // In scene view there is no motion vector, so we clear the RT to black
                if (hdCamera.camera.cameraType == CameraType.SceneView && !CoreUtils.AreAnimatedMaterialsEnabled(hdCamera.camera))
                {
                    HDUtils.SetRenderTarget(cmd, m_SharedRTManager.GetMotionVectorsBuffer(), m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.Color, Color.clear);
                }
#endif
            }
        }

        void RenderSSR(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR))
                return;


#if ENABLE_RAYTRACING
            var settings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>();
            if (settings.enableRaytracing.value)
            {
                m_RaytracingReflections.RenderReflections(hdCamera, cmd, m_SsrLightingTexture, renderContext, m_FrameCount);
            }
            else
#endif
            {
                var cs = m_ScreenSpaceReflectionsCS;

                var previousColorPyramid = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);

                int w = hdCamera.actualWidth;
                int h = hdCamera.actualHeight;

                // Evaluate the clear coat mask texture based on the lit shader mode
                RenderTargetIdentifier clearCoatMask = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffer(2).nameID : TextureXR.GetBlackTexture();

                using (new ProfilingSample(cmd, "SSR - Tracing", CustomSamplerId.SsrTracing.GetSampler()))
                {
                    var volumeSettings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>();

                    if (!volumeSettings) volumeSettings = ScreenSpaceReflection.@default;

                    int kernel = m_SsrTracingKernel;

                    float n = hdCamera.camera.nearClipPlane;
                    float f = hdCamera.camera.farClipPlane;

                    float thickness      = volumeSettings.depthBufferThickness.value;
                    float thicknessScale = 1.0f / (1.0f + thickness);
                    float thicknessBias  = -n / (f - n) * (thickness * thicknessScale);

                    HDUtils.PackedMipChainInfo info = m_SharedRTManager.GetDepthBufferMipChainInfo();

                    float roughnessFadeStart             = 1 - volumeSettings.smoothnessFadeStart.value;
                    float roughnessFadeEnd               = 1 - volumeSettings.minSmoothness.value;
                    float roughnessFadeLength            = roughnessFadeEnd - roughnessFadeStart;
                    float roughnessFadeEndTimesRcpLength = (roughnessFadeLength != 0) ? (roughnessFadeEnd * (1.0f / roughnessFadeLength)) : 1;
                    float roughnessFadeRcpLength         = (roughnessFadeLength != 0) ? (1.0f / roughnessFadeLength) : 0;
                    float edgeFadeRcpLength              = Mathf.Min(1.0f / volumeSettings.screenFadeDistance.value, float.MaxValue);

                    cmd.SetComputeIntParam(  cs, HDShaderIDs._SsrIterLimit,                      volumeSettings.rayMaxIterations.value);
                    cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrThicknessScale,                 thicknessScale);
                    cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrThicknessBias,                  thicknessBias);
                    cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrRoughnessFadeEnd,               roughnessFadeEnd);
                    cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrRoughnessFadeRcpLength,         roughnessFadeRcpLength);
                    cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrRoughnessFadeEndTimesRcpLength, roughnessFadeEndTimesRcpLength);
                    cmd.SetComputeIntParam(  cs, HDShaderIDs._SsrDepthPyramidMaxMip,             info.mipLevelCount-1);
                    cmd.SetComputeFloatParam(cs, HDShaderIDs._SsrEdgeFadeRcpLength,              edgeFadeRcpLength);
                    cmd.SetComputeIntParam(  cs, HDShaderIDs._SsrReflectsSky,                    volumeSettings.reflectSky.value ? 1 : 0);
                    cmd.SetComputeIntParam(  cs, HDShaderIDs._SsrStencilExclusionValue,          (int)StencilBitMask.DoesntReceiveSSR);

                    // cmd.SetComputeTextureParam(cs, kernel, "_SsrDebugTexture",    m_SsrDebugTexture);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMask);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SsrHitPointTexture, m_SsrHitPointTexture);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._StencilTexture, m_SharedRTManager.GetStencilBufferCopy());

                    cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._DepthPyramidMipLevelOffsets, info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));

                    cmd.DispatchCompute(cs, kernel, HDUtils.DivRoundUp(w, 8), HDUtils.DivRoundUp(h, 8), hdCamera.viewCount);
                }

                using (new ProfilingSample(cmd, "SSR - Reprojection", CustomSamplerId.SsrReprojection.GetSampler()))
                {
                    int kernel = m_SsrReprojectionKernel;

                    // cmd.SetComputeTextureParam(cs, kernel, "_SsrDebugTexture",    m_SsrDebugTexture);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SsrHitPointTexture,   m_SsrHitPointTexture);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SsrLightingTextureRW, m_SsrLightingTexture);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ColorPyramidTexture,  previousColorPyramid);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMask);

                    cmd.SetComputeVectorParam(cs, HDShaderIDs._ColorPyramidUvScaleAndLimitPrevFrame, HDUtils.ComputeUvScaleAndLimit(hdCamera.historyRTHandleProperties.previousViewportSize, hdCamera.historyRTHandleProperties.previousRenderTargetSize));
                    cmd.SetComputeIntParam(cs, HDShaderIDs._SsrColorPyramidMaxMip, hdCamera.colorPyramidHistoryMipCount - 1);

                    cmd.DispatchCompute(cs, kernel, HDUtils.DivRoundUp(w, 8), HDUtils.DivRoundUp(h, 8), hdCamera.viewCount);
                }

            	if (!hdCamera.colorPyramidHistoryIsValid)
            	{
                	cmd.SetGlobalTexture(HDShaderIDs._SsrLightingTexture, TextureXR.GetClearTexture());
                	hdCamera.colorPyramidHistoryIsValid = true; // For the next frame...
            	}
			}

            PushFullScreenDebugTexture(hdCamera, cmd, m_SsrLightingTexture, FullScreenDebugMode.ScreenSpaceReflections);
        }

        void RenderColorPyramid(HDCamera hdCamera, CommandBuffer cmd, bool isPreRefraction)
        {
            if (isPreRefraction)
            {
                if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RoughRefraction))
                    return;
            }
            else
            {
                // This final Gaussian pyramid can be reused by SSR, so disable it only if there is no distortion
                if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Distortion) && !hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR))
                    return;
            }

            var currentColorPyramid = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);

            int lodCount;

            using (new ProfilingSample(cmd, "Color Gaussian MIP Chain", CustomSamplerId.ColorPyramid.GetSampler()))
            {
                m_PyramidSizeV2I.Set(hdCamera.actualWidth, hdCamera.actualHeight);
                lodCount = m_MipGenerator.RenderColorGaussianPyramid(cmd, m_PyramidSizeV2I, m_CameraColorBuffer, currentColorPyramid);
                hdCamera.colorPyramidHistoryMipCount = lodCount;
            }

            float scaleX = hdCamera.actualWidth / (float)currentColorPyramid.rt.width;
            float scaleY = hdCamera.actualHeight / (float)currentColorPyramid.rt.height;
            m_PyramidSizeV4F.Set(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight);
            m_PyramidScaleLod.Set(scaleX, scaleY, lodCount, 0.0f);
            m_PyramidScale.Set(scaleX, scaleY, 0f, 0f);
            // Warning! Danger!
            // The color pyramid scale is only correct for the most detailed MIP level.
            // For the other MIP levels, due to truncation after division by 2, a row or
            // column of texels may be lost. Since this can happen to BOTH the texture
            // size AND the viewport, (uv * _ColorPyramidScale.xy) can be off by a texel
            // unless the scale is 1 (and it will not be 1 if the texture was resized
            // and is of greater size compared to the viewport).
            cmd.SetGlobalTexture(HDShaderIDs._ColorPyramidTexture, currentColorPyramid);
            cmd.SetGlobalVector(HDShaderIDs._ColorPyramidSize, m_PyramidSizeV4F);
            cmd.SetGlobalVector(HDShaderIDs._ColorPyramidScale, m_PyramidScaleLod);
            PushFullScreenDebugTextureMip(hdCamera, cmd, currentColorPyramid, lodCount, m_PyramidScale, isPreRefraction ? FullScreenDebugMode.PreRefractionColorPyramid : FullScreenDebugMode.FinalColorPyramid);
        }

        void GenerateDepthPyramid(HDCamera hdCamera, CommandBuffer cmd, FullScreenDebugMode debugMode)
        {
            CopyDepthBufferIfNeeded(cmd);

            int mipCount = m_SharedRTManager.GetDepthBufferMipChainInfo().mipLevelCount;

            using (new ProfilingSample(cmd, "Generate Depth Buffer MIP Chain", CustomSamplerId.DepthPyramid.GetSampler()))
            {
                m_MipGenerator.RenderMinDepthPyramid(cmd, m_SharedRTManager.GetDepthTexture(), m_SharedRTManager.GetDepthBufferMipChainInfo());
            }

            float scaleX = hdCamera.actualWidth / (float)m_SharedRTManager.GetDepthTexture().rt.width;
            float scaleY = hdCamera.actualHeight / (float)m_SharedRTManager.GetDepthTexture().rt.height;
            m_PyramidSizeV4F.Set(hdCamera.actualWidth, hdCamera.actualHeight, 1f / hdCamera.actualWidth, 1f / hdCamera.actualHeight);
            m_PyramidScaleLod.Set(scaleX, scaleY, mipCount, 0.0f);
            m_PyramidScale.Set(scaleX, scaleY, 0f, 0f);
            cmd.SetGlobalTexture(HDShaderIDs._DepthPyramidTexture, m_SharedRTManager.GetDepthTexture());
            cmd.SetGlobalVector(HDShaderIDs._DepthPyramidSize, m_PyramidSizeV4F);
            cmd.SetGlobalVector(HDShaderIDs._DepthPyramidScale, m_PyramidScaleLod);
            PushFullScreenDebugTextureMip(hdCamera, cmd, m_SharedRTManager.GetDepthTexture(), mipCount, m_PyramidScale, debugMode);
        }

        void DownsampleDepthForLowResTransparency(HDCamera hdCamera, CommandBuffer cmd)
        {
            var settings = m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings;
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
                return;

            using (new ProfilingSample(cmd, "Downsample Depth Buffer for Low Res Transparency", CustomSamplerId.DownsampleDepth.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, m_SharedRTManager.GetLowResDepthBuffer());
                cmd.SetViewport(new Rect(0, 0, hdCamera.actualWidth * 0.5f, hdCamera.actualHeight * 0.5f));
                // TODO: Add option to switch modes at runtime
                if(settings.checkerboardDepthBuffer)
                {
                    m_DownsampleDepthMaterial.EnableKeyword("CHECKERBOARD_DOWNSAMPLE");
                }
                cmd.DrawProcedural(Matrix4x4.identity, m_DownsampleDepthMaterial, 0, MeshTopology.Triangles, 3, 1, null);
            }
        }

        void UpsampleTransparent(HDCamera hdCamera, CommandBuffer cmd)
        {
            var settings = m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings;
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent))
                return;

            using (new ProfilingSample(cmd, "Upsample Low Res Transparency", CustomSamplerId.UpsampleLowResTransparent.GetSampler()))
            {
                HDUtils.SetRenderTarget(cmd, m_CameraColorBuffer);
                if(settings.upsampleType == LowResTransparentUpsample.Bilinear)
                {
                    m_UpsampleTransparency.EnableKeyword("BILINEAR");
                }
                else if (settings.upsampleType == LowResTransparentUpsample.NearestDepth)
                {
                    m_UpsampleTransparency.EnableKeyword("NEAREST_DEPTH");
                }
                m_UpsampleTransparency.SetTexture(HDShaderIDs._LowResTransparent, m_LowResTransparentBuffer);
                m_UpsampleTransparency.SetTexture(HDShaderIDs._LowResDepthTexture, m_SharedRTManager.GetLowResDepthBuffer());
                cmd.DrawProcedural(Matrix4x4.identity, m_UpsampleTransparency, 0, MeshTopology.Triangles, 3, 1, null);
            }
        }

        public void ApplyDebugDisplaySettings(HDCamera hdCamera, CommandBuffer cmd)
        {
            // See ShaderPassForward.hlsl: for forward shaders, if DEBUG_DISPLAY is enabled and no DebugLightingMode or DebugMipMapMod
            // modes have been set, lighting is automatically skipped (To avoid some crashed due to lighting RT not set on console).
            // However debug mode like colorPickerModes and false color don't need DEBUG_DISPLAY and must work with the lighting.
            // So we will enabled DEBUG_DISPLAY independently

            // Enable globally the keyword DEBUG_DISPLAY on shader that support it with multi-compile
            CoreUtils.SetKeyword(cmd, "DEBUG_DISPLAY", m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled());

            if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() ||
                m_CurrentDebugDisplaySettings.data.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None)
            {
                // This is for texture streaming
                m_CurrentDebugDisplaySettings.UpdateMaterials();

                var lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;
                var materialDebugSettings = m_CurrentDebugDisplaySettings.data.materialDebugSettings;
                var debugAlbedo = new Vector4(lightingDebugSettings.overrideAlbedo ? 1.0f : 0.0f, lightingDebugSettings.overrideAlbedoValue.r, lightingDebugSettings.overrideAlbedoValue.g, lightingDebugSettings.overrideAlbedoValue.b);
                var debugSmoothness = new Vector4(lightingDebugSettings.overrideSmoothness ? 1.0f : 0.0f, lightingDebugSettings.overrideSmoothnessValue, 0.0f, 0.0f);
                var debugNormal = new Vector4(lightingDebugSettings.overrideNormal ? 1.0f : 0.0f, 0.0f, 0.0f, 0.0f);
                var debugSpecularColor = new Vector4(lightingDebugSettings.overrideSpecularColor ? 1.0f : 0.0f, lightingDebugSettings.overrideSpecularColorValue.r, lightingDebugSettings.overrideSpecularColorValue.g, lightingDebugSettings.overrideSpecularColorValue.b);
                var debugEmissiveColor = new Vector4(lightingDebugSettings.overrideEmissiveColor ? 1.0f : 0.0f, lightingDebugSettings.overrideEmissiveColorValue.r, lightingDebugSettings.overrideEmissiveColorValue.g, lightingDebugSettings.overrideEmissiveColorValue.b);
                var debugTrueMetalColor = new Vector4(materialDebugSettings.materialValidateTrueMetal ? 1.0f : 0.0f, materialDebugSettings.materialValidateTrueMetalColor.r, materialDebugSettings.materialValidateTrueMetalColor.g, materialDebugSettings.materialValidateTrueMetalColor.b);

                cmd.SetGlobalFloatArray(HDShaderIDs._DebugViewMaterial, m_CurrentDebugDisplaySettings.GetDebugMaterialIndexes());
                cmd.SetGlobalInt(HDShaderIDs._DebugLightingMode, (int)m_CurrentDebugDisplaySettings.GetDebugLightingMode());
                cmd.SetGlobalInt(HDShaderIDs._DebugShadowMapMode, (int)m_CurrentDebugDisplaySettings.GetDebugShadowMapMode());
                cmd.SetGlobalInt(HDShaderIDs._DebugMipMapMode, (int)m_CurrentDebugDisplaySettings.GetDebugMipMapMode());
                cmd.SetGlobalInt(HDShaderIDs._DebugMipMapModeTerrainTexture, (int)m_CurrentDebugDisplaySettings.GetDebugMipMapModeTerrainTexture());
                cmd.SetGlobalInt(HDShaderIDs._ColorPickerMode, (int)m_CurrentDebugDisplaySettings.GetDebugColorPickerMode());
                cmd.SetGlobalInt(HDShaderIDs._DebugFullScreenMode, (int)m_CurrentDebugDisplaySettings.data.fullScreenDebugMode);

#if UNITY_EDITOR
                cmd.SetGlobalInt(HDShaderIDs._MatcapMixAlbedo, HDRenderPipelinePreferences.matcapViewMixAlbedo ? 1 : 0);
                cmd.SetGlobalFloat(HDShaderIDs._MatcapViewScale, HDRenderPipelinePreferences.matcapViewScale);
#else
                cmd.SetGlobalInt(HDShaderIDs._MatcapMixAlbedo, 0);
                cmd.SetGlobalFloat(HDShaderIDs._MatcapViewScale, 1.0f);
#endif
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingAlbedo, debugAlbedo);
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingSmoothness, debugSmoothness);
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingNormal, debugNormal);
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingSpecularColor, debugSpecularColor);
                cmd.SetGlobalVector(HDShaderIDs._DebugLightingEmissiveColor, debugEmissiveColor);
                cmd.SetGlobalColor(HDShaderIDs._DebugLightingMaterialValidateHighColor, materialDebugSettings.materialValidateHighColor);
                cmd.SetGlobalColor(HDShaderIDs._DebugLightingMaterialValidateLowColor, materialDebugSettings.materialValidateLowColor);
                cmd.SetGlobalColor(HDShaderIDs._DebugLightingMaterialValidatePureMetalColor, debugTrueMetalColor);

                cmd.SetGlobalVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                cmd.SetGlobalVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(hdCamera));
                cmd.SetGlobalTexture(HDShaderIDs._DebugFont, m_Asset.renderPipelineResources.textures.debugFontTex);
                cmd.SetGlobalTexture(HDShaderIDs._DebugMatCapTexture, m_Asset.renderPipelineResources.textures.matcapTex);

                // The DebugNeedsExposure test allows us to set a neutral value if exposure is not needed. This way we don't need to make various tests inside shaders but only in this function.
                cmd.SetGlobalFloat(HDShaderIDs._DebugExposure, m_CurrentDebugDisplaySettings.DebugNeedsExposure() ? lightingDebugSettings.debugExposure : 0.0f);
            }
        }

        public void PushColorPickerDebugTexture(CommandBuffer cmd, HDCamera hdCamera, RTHandle textureID)
        {
            if (m_CurrentDebugDisplaySettings.data.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None || m_DebugDisplaySettings.data.falseColorDebugSettings.falseColor || m_DebugDisplaySettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuminanceMeter)
            {
                using (new ProfilingSample(cmd, "Push To Color Picker"))
                {
                    HDUtils.BlitCameraTexture(cmd, textureID, m_DebugColorPickerBuffer);
                }
            }
        }

        bool NeedsFullScreenDebugMode()
        {
            bool fullScreenDebugEnabled = m_CurrentDebugDisplaySettings.data.fullScreenDebugMode != FullScreenDebugMode.None;
            bool lightingDebugEnabled = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow;

            return fullScreenDebugEnabled || lightingDebugEnabled;
        }

        public void PushFullScreenLightingDebugTexture(HDCamera hdCamera, CommandBuffer cmd, RTHandle textureID)
        {
            if (NeedsFullScreenDebugMode() && m_FullScreenDebugPushed == false)
            {
                m_FullScreenDebugPushed = true;
                HDUtils.BlitCameraTexture(cmd, textureID, m_DebugFullScreenTempBuffer);
            }
        }

        public void PushFullScreenDebugTexture(HDCamera hdCamera, CommandBuffer cmd, RTHandle textureID, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
                HDUtils.BlitCameraTexture(cmd, textureID, m_DebugFullScreenTempBuffer);
            }
        }

        void PushFullScreenDebugTextureMip(HDCamera hdCamera, CommandBuffer cmd, RTHandle texture, int lodCount, Vector4 scaleBias, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.data.fullscreenDebugMip * (lodCount));

                m_FullScreenDebugPushed = true; // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
                HDUtils.BlitCameraTexture(cmd, texture, m_DebugFullScreenTempBuffer, scaleBias, mipIndex);
            }
        }

        void RenderDebug(HDCamera hdCamera, CommandBuffer cmd, CullingResults cullResults)
        {
            // We don't want any overlay for these kind of rendering
            if (hdCamera.camera.cameraType == CameraType.Reflection || hdCamera.camera.cameraType == CameraType.Preview)
                return;

            // Render Debug are only available in dev builds and we always render them in the same RT
            HDUtils.SetRenderTarget(cmd, m_IntermediateAfterPostProcessBuffer, m_SharedRTManager.GetDepthStencilBuffer());

            using (new ProfilingSample(cmd, "Debug", CustomSamplerId.RenderDebug.GetSampler()))
            {
                // First render full screen debug texture
                if (NeedsFullScreenDebugMode() && m_FullScreenDebugPushed)
                {
                    m_FullScreenDebugPushed = false;
                    m_DebugFullScreenPropertyBlock.SetTexture(HDShaderIDs._DebugFullScreenTexture, m_DebugFullScreenTempBuffer);
                    m_DebugFullScreenPropertyBlock.SetFloat(HDShaderIDs._FullScreenDebugMode, (float)m_CurrentDebugDisplaySettings.data.fullScreenDebugMode);
                    HDUtils.PackedMipChainInfo info = m_SharedRTManager.GetDepthBufferMipChainInfo();
                    m_DebugFullScreenPropertyBlock.SetInt(HDShaderIDs._DebugDepthPyramidMip, (int)(m_CurrentDebugDisplaySettings.data.fullscreenDebugMip * info.mipLevelCount));
                    m_DebugFullScreenPropertyBlock.SetBuffer(HDShaderIDs._DebugDepthPyramidOffsets, info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));
                    m_DebugFullScreenPropertyBlock.SetInt(HDShaderIDs._DebugContactShadowLightIndex, (int)(m_CurrentDebugDisplaySettings.data.fullScreenContactShadowLightIndex));

                    HDUtils.DrawFullScreen(cmd, m_DebugFullScreen, m_IntermediateAfterPostProcessBuffer, m_DebugFullScreenPropertyBlock, 0);
                    PushColorPickerDebugTexture(cmd, hdCamera, m_IntermediateAfterPostProcessBuffer);
                }

                // Then overlays
                HDUtils.ResetOverlay();
                float x = 0.0f;
                float overlayRatio = m_CurrentDebugDisplaySettings.data.debugOverlayRatio;
                float overlaySize = Math.Min(hdCamera.actualHeight, hdCamera.actualWidth) * overlayRatio;
                float y = hdCamera.actualHeight - overlaySize;

                var lightingDebug = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;

                if (lightingDebug.displaySkyReflection)
                {
                    var skyReflection = m_SkyManager.skyReflection;
                    m_SharedPropertyBlock.SetTexture(HDShaderIDs._InputCubemap, skyReflection);
                    m_SharedPropertyBlock.SetFloat(HDShaderIDs._Mipmap, lightingDebug.skyReflectionMipmap);
                    m_SharedPropertyBlock.SetFloat(HDShaderIDs._DebugExposure, lightingDebug.debugExposure);
                    cmd.SetViewport(new Rect(x, y, overlaySize, overlaySize));
                    cmd.DrawProcedural(Matrix4x4.identity, m_DebugDisplayLatlong, 0, MeshTopology.Triangles, 3, 1, m_SharedPropertyBlock);
                    HDUtils.NextOverlayCoord(ref x, ref y, overlaySize, overlaySize, hdCamera);
                }

#if ENABLE_RAYTRACING
                m_RayTracingManager.rayCountManager.EvaluateRayCount(cmd, hdCamera);
#endif

                RenderLightLoopDebugOverlay(hdCamera, cmd, m_CurrentDebugDisplaySettings, ref x, ref y, overlaySize, hdCamera.actualWidth, cullResults, m_SharedRTManager.GetDepthTexture(), m_IntermediateAfterPostProcessBuffer);

                DecalSystem.instance.RenderDebugOverlay(hdCamera, cmd, m_CurrentDebugDisplaySettings, ref x, ref y, overlaySize, hdCamera.actualWidth);

                if (m_CurrentDebugDisplaySettings.data.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None || m_CurrentDebugDisplaySettings.data.falseColorDebugSettings.falseColor || m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuminanceMeter)
                {
                    ColorPickerDebugSettings colorPickerDebugSettings = m_CurrentDebugDisplaySettings.data.colorPickerDebugSettings;
                    FalseColorDebugSettings falseColorDebugSettings = m_CurrentDebugDisplaySettings.data.falseColorDebugSettings;
                    var falseColorThresholds = new Vector4(falseColorDebugSettings.colorThreshold0, falseColorDebugSettings.colorThreshold1, falseColorDebugSettings.colorThreshold2, falseColorDebugSettings.colorThreshold3);

                    // Here we have three cases:
                    // - Material debug is enabled, this is the buffer we display
                    // - Otherwise we display the HDR buffer before postprocess and distortion
                    // - If fullscreen debug is enabled we always use it

                    cmd.SetGlobalTexture(HDShaderIDs._DebugColorPickerTexture, m_DebugColorPickerBuffer); // No SetTexture with RenderTarget identifier... so use SetGlobalTexture
                    // TODO: Replace with command buffer call when available
                    m_DebugColorPicker.SetColor(HDShaderIDs._ColorPickerFontColor, colorPickerDebugSettings.fontColor);
                    m_DebugColorPicker.SetInt(HDShaderIDs._FalseColorEnabled, falseColorDebugSettings.falseColor ? 1 : 0);
                    m_DebugColorPicker.SetVector(HDShaderIDs._FalseColorThresholds, falseColorThresholds);
                    // The material display debug perform sRGBToLinear conversion as the final blit currently hardcodes a linearToSrgb conversion. As when we read with color picker this is not done,
                    // we perform it inside the color picker shader. But we shouldn't do it for HDR buffer.
                    m_DebugColorPicker.SetFloat(HDShaderIDs._ApplyLinearToSRGB, m_CurrentDebugDisplaySettings.IsDebugMaterialDisplayEnabled() ? 1.0f : 0.0f);
                    // Everything we have capture is flipped (as it happen before FinalPass/postprocess/Blit. So if we are not in SceneView
                    // (i.e. we have perform a flip, we need to flip the input texture) + we need to handle the case were we debug a fullscreen pass that have already perform the flip

                    HDUtils.DrawFullScreen(cmd, m_DebugColorPicker, m_IntermediateAfterPostProcessBuffer, m_DebugFullScreenPropertyBlock, 0);
                }
            }
        }

        void ClearBuffers(HDCamera hdCamera, CommandBuffer cmd)
        {
            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);

            using (new ProfilingSample(cmd, "ClearBuffers", CustomSamplerId.ClearBuffers.GetSampler()))
            {
                // We clear only the depth buffer, no need to clear the various color buffer as we overwrite them.
                // Clear depth/stencil and init buffers
                using (new ProfilingSample(cmd, "Clear Depth/Stencil", CustomSamplerId.ClearDepthStencil.GetSampler()))
                {
                    if (hdCamera.clearDepth)
                    {
                        HDUtils.SetRenderTarget(cmd, msaa ? m_CameraColorMSAABuffer : m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer(msaa), ClearFlag.Depth);
                        if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
                        {
                            HDUtils.SetRenderTarget(cmd, m_SharedRTManager.GetDepthTexture(true), m_SharedRTManager.GetDepthStencilBuffer(true), ClearFlag.Color, Color.black);
                        }
                    }
                    m_IsDepthBufferCopyValid = false;
                }

                // Clear the HDR target
                using (new ProfilingSample(cmd, "Clear HDR target", CustomSamplerId.ClearHDRTarget.GetSampler()))
                {
                    if (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Color ||
                        // If the luxmeter is enabled, the sky isn't rendered so we clear the background color
                        m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuxMeter ||
                        // If the matcap view is enabled, the sky isn't updated so we clear the background color
                        m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.MatcapView ||
                        // If we want the sky but the sky don't exist, still clear with background color
                        (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky && !m_SkyManager.IsVisualSkyValid()) ||
                        // Special handling for Preview we force to clear with background color (i.e black)
                        // Note that the sky use in this case is the last one setup. If there is no scene or game, there is no sky use as reflection in the preview
                        HDUtils.IsRegularPreviewCamera(hdCamera.camera)
                        )
                    {
                        Color clearColor = hdCamera.backgroundColorHDR;

                        // We set the background color to black when the luxmeter is enabled to avoid picking the sky color
                        if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuxMeter ||
                            m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.MatcapView)
                            clearColor = Color.black;

                        HDUtils.SetRenderTarget(cmd, msaa ? m_CameraColorMSAABuffer : m_CameraColorBuffer, m_SharedRTManager.GetDepthStencilBuffer(msaa), ClearFlag.Color, clearColor);

                    }
                }

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
                {
                    using (new ProfilingSample(cmd, "Clear SSS Lighting Buffer", CustomSamplerId.ClearSssLightingBuffer.GetSampler()))
                    {
                        HDUtils.SetRenderTarget(cmd, msaa ? m_CameraSssDiffuseLightingMSAABuffer : m_CameraSssDiffuseLightingBuffer, ClearFlag.Color, Color.clear);
                    }
                }

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR))
                {
                    using (new ProfilingSample(cmd, "Clear SSR Buffers", CustomSamplerId.ClearSsrBuffers.GetSampler()))
                    {
                        // In practice, these textures are sparse (mostly black). Therefore, clearing them is fast (due to CMASK),
                        // and much faster than fully overwriting them from within SSR shaders.
                        // HDUtils.SetRenderTarget(cmd, hdCamera, m_SsrDebugTexture,    ClearFlag.Color, Color.clear);
                        HDUtils.SetRenderTarget(cmd, m_SsrHitPointTexture, ClearFlag.Color, Color.clear);
                        HDUtils.SetRenderTarget(cmd, m_SsrLightingTexture, ClearFlag.Color, Color.clear);
                    }
                }

                // We don't need to clear the GBuffers as scene is rewrite and we are suppose to only access valid data (invalid data are tagged with stencil as StencilLightingUsage.NoLighting),
                // This is to save some performance
                if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
                {
                    using (new ProfilingSample(cmd, "Clear GBuffer", CustomSamplerId.ClearGBuffer.GetSampler()))
                    {
                        // We still clear in case of debug mode or on demand
                        if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() || hdCamera.frameSettings.IsEnabled(FrameSettingsField.ClearGBuffers))
                        {
                            HDUtils.SetRenderTarget(cmd, m_GbufferManager.GetBuffersRTI(), m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.Color, Color.clear);
                        }

                        // If we are in deferred mode and the ssr is enabled, we need to make sure that the second gbuffer is cleared given that we are using that information for
                        // clear coat selection
                        if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR))
                        {
                            HDUtils.SetRenderTarget(cmd, m_GbufferManager.GetBuffer(2), m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.Color, Color.black);
                        }
                    }
                }
            }
        }

        void RenderPostProcess(CullingResults cullResults, HDCamera hdCamera, RenderTargetIdentifier finalRT, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            // Y-Flip needs to happen during the post process pass only if it's the final pass and is the regular game view
            // SceneView flip is handled by the editor internal code and GameView rendering into render textures should not be flipped in order to respect Unity texture coordinates convention
            bool flipInPostProcesses = HDUtils.PostProcessIsFinalPass() && (hdCamera.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY || hdCamera.isMainGameView);
            RenderTargetIdentifier destination = HDUtils.PostProcessIsFinalPass() ? finalRT : m_IntermediateAfterPostProcessBuffer;

            // We render AfterPostProcess objects first into a separate buffer that will be composited in the final post process pass
            RenderAfterPostProcess(cullResults, hdCamera, renderContext, cmd);

            // Set the depth buffer to the main one to avoid missing out on transparent depth for post process.
            cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, m_SharedRTManager.GetDepthStencilBuffer());

            // Post-processes output straight to the backbuffer
            m_PostProcessSystem.Render(
                cmd: cmd,
                camera: hdCamera,
                blueNoise: m_BlueNoise,
                colorBuffer: m_CameraColorBuffer,
                afterPostProcessTexture: GetAfterPostProcessOffScreenBuffer(),
                lightingBuffer: null,
                finalRT: destination,
                depthBuffer: m_SharedRTManager.GetDepthStencilBuffer(),
                flipY: flipInPostProcesses
            );
        }


        RTHandle GetAfterPostProcessOffScreenBuffer()
        {
            if (currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly)
                return GetSSSBuffer();
            else
                return m_GbufferManager.GetBuffer(0);
        }


        void RenderAfterPostProcess(CullingResults cullResults, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess))
                return;

            using (new ProfilingSample(cmd, "After Post-process", CustomSamplerId.AfterPostProcessing.GetSampler()))
            {
                // Note about AfterPostProcess and TAA:
                // When TAA is enabled rendering is jittered and then resolved during the post processing pass.
                // It means that any rendering done after post processing need to disable jittering. This is what we do with hdCamera.UpdateViewConstants(false);
                // The issue is that the only available depth buffer is jittered so pixels would wobble around depth tested edges.
                // In order to avoid that we decide that objects rendered after Post processes while TAA is active will not benefit from the depth buffer so we disable it.
                bool taaEnabled = hdCamera.IsTAAEnabled();
                hdCamera.UpdateAllViewConstants(false);
                hdCamera.SetupGlobalParams(cmd, m_Time, m_LastTime, m_FrameCount);

                // Here we share GBuffer albedo buffer since it's not needed anymore
                // Note: We bind the depth only if the ZTest for After Post Process is enabled. It is disabled by
                // default so we're consistent in the behavior: no ZTest for After Post Process materials).
                if (taaEnabled || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.ZTestAfterPostProcessTAA))
                    HDUtils.SetRenderTarget(cmd, GetAfterPostProcessOffScreenBuffer(), clearFlag: ClearFlag.Color, clearColor: Color.black);
                else
                    HDUtils.SetRenderTarget(cmd, GetAfterPostProcessOffScreenBuffer(), m_SharedRTManager.GetDepthStencilBuffer(), clearFlag: ClearFlag.Color, clearColor: Color.black);

                cmd.SetGlobalInt(HDShaderIDs._OffScreenRendering, 1);
                var opaqueRendererList = RendererList.Create(CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque));
                DrawOpaqueRendererList(renderContext, cmd, hdCamera.frameSettings, opaqueRendererList);
                // Setup off-screen transparency here
                var transparentRendererList = RendererList.Create(CreateTransparentRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent));
                DrawTransparentRendererList(renderContext, cmd, hdCamera.frameSettings, transparentRendererList);
                cmd.SetGlobalInt(HDShaderIDs._OffScreenRendering, 0);
            }
        }

        void SendGraphicsBuffers(CommandBuffer cmd, HDCamera hdCamera)
        {
            bool needNormalBuffer = false;
            Texture normalBuffer = null;
            bool needDepthBuffer = false;
            Texture depthBuffer = null;

            // Figure out which client systems need which buffers
            // Only VFX systems for now
            VFX.VFXCameraBufferTypes neededVFXBuffers = VFX.VFXManager.IsCameraBufferNeeded(hdCamera.camera);
            needNormalBuffer |= (neededVFXBuffers & VFX.VFXCameraBufferTypes.Normal) != 0;
            needDepthBuffer |= (neededVFXBuffers & VFX.VFXCameraBufferTypes.Depth) != 0;

            // Here if needed for this particular camera, we allocate history buffers.
            // Only one is needed here because the main buffer used for rendering is separate.
            // Ideally, we should double buffer the main rendering buffer but since we don't know in advance if history is going to be needed, it would be a big waste of memory.
            if (needNormalBuffer)
            {
                RTHandle mainNormalBuffer = m_SharedRTManager.GetNormalBuffer();
                RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
                {
                    return rtHandleSystem.Alloc(Vector2.one, colorFormat: mainNormalBuffer.rt.graphicsFormat, enableRandomWrite: mainNormalBuffer.rt.enableRandomWrite, name: $"Normal History Buffer"
                    );
                }

                normalBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal) ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Normal, Allocator, 1);
                cmd.CopyTexture(mainNormalBuffer, 0, 0, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight, normalBuffer, 0, 0, 0, 0);
            }

            if (needDepthBuffer)
            {
                RTHandle mainDepthBuffer = m_SharedRTManager.GetDepthTexture();
                RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
                {
                    return rtHandleSystem.Alloc(Vector2.one, colorFormat: mainDepthBuffer.rt.graphicsFormat, enableRandomWrite: mainDepthBuffer.rt.enableRandomWrite, name: $"Depth History Buffer"
                    );
                }

                depthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth) ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Depth, Allocator, 1);
                cmd.CopyTexture(mainDepthBuffer, 0, 0, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight, depthBuffer, 0, 0, 0, 0);
            }

            // Send buffers to client.
            // For now, only VFX systems
            if ((neededVFXBuffers & VFX.VFXCameraBufferTypes.Depth) != 0)
            {
                VFX.VFXManager.SetCameraBuffer(hdCamera.camera, VFX.VFXCameraBufferTypes.Depth, depthBuffer, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight);
            }

            if ((neededVFXBuffers & VFX.VFXCameraBufferTypes.Normal) != 0)
            {
                VFX.VFXManager.SetCameraBuffer(hdCamera.camera, VFX.VFXCameraBufferTypes.Normal, normalBuffer, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight);
            }

            if ((neededVFXBuffers & VFX.VFXCameraBufferTypes.Color) != 0)
            {
                var colorBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                VFX.VFXManager.SetCameraBuffer(hdCamera.camera, VFX.VFXCameraBufferTypes.Color, colorBuffer, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight);
            }
        }
    }
}
