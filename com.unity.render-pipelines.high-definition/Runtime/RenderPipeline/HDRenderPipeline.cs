using System.Collections.Generic;
using UnityEngine.VFX;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor.Rendering;
#endif

#if ENABLE_VIRTUALTEXTURES
using UnityEngine.Rendering.VirtualTexturing;
#endif


namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// High Definition Render Pipeline class.
    /// </summary>
    public partial class HDRenderPipeline : RenderPipeline
    {
        #region Default Settings
        internal static HDRenderPipelineAsset defaultAsset
            => GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset hdrpAsset ? hdrpAsset : null;

        internal static HDRenderPipelineAsset currentAsset
            => GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset hdrpAsset ? hdrpAsset : null;

        internal static HDRenderPipeline currentPipeline
            => RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp ? hdrp : null;

        internal static bool pipelineSupportsRayTracing => HDRenderPipeline.currentPipeline != null && HDRenderPipeline.currentPipeline.rayTracingSupported;

        internal static bool pipelineSupportsScreenSpaceShadows => GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset hdrpAsset ? hdrpAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows : false;


        private static Volume s_DefaultVolume = null;
        static VolumeProfile defaultVolumeProfile
            => defaultAsset?.defaultVolumeProfile;

        static HDRenderPipeline()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () =>
            {
                if (s_DefaultVolume != null && !s_DefaultVolume.Equals(null))
                {
                    CoreUtils.Destroy(s_DefaultVolume.gameObject);
                    s_DefaultVolume = null;
                }
            };
#endif
        }

        internal static Volume GetOrCreateDefaultVolume()
        {
            if (s_DefaultVolume == null || s_DefaultVolume.Equals(null))
            {
                var go = new GameObject("Default Volume") { hideFlags = HideFlags.HideAndDontSave };
                s_DefaultVolume = go.AddComponent<Volume>();
                s_DefaultVolume.isGlobal = true;
                s_DefaultVolume.priority = float.MinValue;
                s_DefaultVolume.sharedProfile = defaultVolumeProfile;
            }

            if (
                // In case the asset was deleted or the reference removed
                s_DefaultVolume.sharedProfile == null || s_DefaultVolume.sharedProfile.Equals(null)
#if UNITY_EDITOR

                // In case the serialization recreated an empty volume sharedProfile

                || !UnityEditor.AssetDatabase.Contains(s_DefaultVolume.sharedProfile)
#endif
            )
            {
                s_DefaultVolume.sharedProfile = defaultVolumeProfile;
            }

            if (s_DefaultVolume.sharedProfile != defaultVolumeProfile)
            {
                s_DefaultVolume.sharedProfile = defaultVolumeProfile;
            }

            return s_DefaultVolume;
        }

        #endregion

        /// <summary>
        /// Shader Tag for the High Definition Render Pipeline.
        /// </summary>
        public const string k_ShaderTagName = "HDRenderPipeline";

        readonly HDRenderPipelineAsset m_Asset;
        internal HDRenderPipelineAsset asset { get { return m_Asset; } }
        readonly HDRenderPipelineAsset m_DefaultAsset;
        internal RenderPipelineResources defaultResources { get { return m_DefaultAsset.renderPipelineResources; } }

        internal RenderPipelineSettings currentPlatformRenderPipelineSettings { get { return m_Asset.currentPlatformRenderPipelineSettings; } }

        readonly List<RenderPipelineMaterial> m_MaterialList = new List<RenderPipelineMaterial>();

#if ENABLE_VIRTUALTEXTURES
        readonly VTBufferManager m_VtBufferManager;
#endif
        readonly PostProcessSystem m_PostProcessSystem;
        readonly XRSystem m_XRSystem;

        // Keep track of previous Graphic and QualitySettings value to reset when switching to another pipeline
        bool m_PreviousLightsUseLinearIntensity;
        bool m_PreviousLightsUseColorTemperature;
        bool m_PreviousSRPBatcher;

#if UNITY_2020_2_OR_NEWER
        uint m_PreviousDefaultRenderingLayerMask;
#endif
        ShadowmaskMode m_PreviousShadowMaskMode;

        bool m_FrameSettingsHistoryEnabled = false;
#if UNITY_EDITOR
        bool m_PreviousEnableCookiesInLightmapper = true;
#endif

        /// <summary>
        /// This functions allows the user to have an approximation of the number of rays that were traced for a given frame.
        /// </summary>
        /// <param name="rayValues">Specifes which ray count value should be returned.</param>
        /// <returns>The approximated ray count for a frame</returns>
        public uint GetRaysPerFrame(RayCountValues rayValues) { return m_RayCountManager.GetRaysPerFrame(rayValues); }

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

        ComputeShader m_ScreenSpaceReflectionsCS { get { return defaultResources.shaders.screenSpaceReflectionsCS; } }
        int m_SsrTracingKernel = -1;
        int m_SsrReprojectionKernel = -1;
        int m_SsrAccumulateKernel = -1;

        Material m_ApplyDistortionMaterial;

        Material m_CameraMotionVectorsMaterial;
        Material m_DecalNormalBufferMaterial;

        Material m_ClearStencilBufferMaterial;

        // Debug material
        Material m_DebugViewMaterialGBuffer;
        Material m_DebugViewMaterialGBufferShadowMask;
        Material m_currentDebugViewMaterialGBuffer;
        Material m_DebugDisplayLatlong;
        Material m_DebugFullScreen;
        Material m_DebugColorPicker;
        Material m_DebugExposure;
        Material m_ErrorMaterial;

        Material m_Blit;
        Material m_BlitTexArray;
        Material m_BlitTexArraySingleSlice;
        Material m_BlitColorAndDepth;

        Lazy<RTHandle> m_CustomPassColorBuffer;
        Lazy<RTHandle> m_CustomPassDepthBuffer;

        // Constant Buffers
        ShaderVariablesGlobal m_ShaderVariablesGlobalCB = new ShaderVariablesGlobal();
        ShaderVariablesXR m_ShaderVariablesXRCB = new ShaderVariablesXR();
        ShaderVariablesDebugDisplay m_ShaderVariablesDebugDisplayCB = new ShaderVariablesDebugDisplay();
        ShaderVariablesRaytracing m_ShaderVariablesRayTracingCB = new ShaderVariablesRaytracing();

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
        ShaderTagId[] m_RayTracingPrepassNames = { HDShaderPassNames.s_RayTracingPrepassName };
        ShaderTagId[] m_FullScreenDebugPassNames = { HDShaderPassNames.s_FullScreenDebugName };
        ShaderTagId[] m_ForwardErrorPassNames = { HDShaderPassNames.s_AlwaysName, HDShaderPassNames.s_ForwardBaseName, HDShaderPassNames.s_DeferredName, HDShaderPassNames.s_PrepassBaseName, HDShaderPassNames.s_VertexName, HDShaderPassNames.s_VertexLMRGBMName, HDShaderPassNames.s_VertexLMName };
        ShaderTagId[] m_DecalsEmissivePassNames = { HDShaderPassNames.s_DecalMeshForwardEmissiveName };
        ShaderTagId[] m_SinglePassName = new ShaderTagId[1];
        ShaderTagId[] m_MeshDecalsPassNames = { HDShaderPassNames.s_DBufferMeshName };

        RenderStateBlock m_DepthStateOpaque;
        RenderStateBlock m_DepthStateNoWrite;
        RenderStateBlock m_AlphaToMaskBlock;

        readonly List<CustomPassVolume> m_ActivePassVolumes = new List<CustomPassVolume>(6);

        // Detect when windows size is changing
        int m_MaxCameraWidth;
        int m_MaxCameraHeight;
        // Keep track of the maximum number of XR instanced views
        int m_MaxViewCount = 1;

        // Use to detect frame changes (for accurate frame count in editor, consider using hdCamera.GetCameraFrameCount)
        int m_FrameCount;

        GraphicsFormat GetColorBufferFormat()
            => (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.colorBufferFormat;

        GraphicsFormat GetCustomBufferFormat()
            => (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.customBufferFormat;

        internal int GetDecalAtlasMipCount()
        {
            int highestDim = Math.Max(currentPlatformRenderPipelineSettings.decalSettings.atlasWidth, currentPlatformRenderPipelineSettings.decalSettings.atlasHeight);
            return (int)Math.Log(highestDim, 2);
        }

        internal int GetCookieAtlasMipCount() => (int)Mathf.Log((int)currentPlatformRenderPipelineSettings.lightLoopSettings.cookieAtlasSize, 2);

        internal int GetPlanarReflectionProbeMipCount()
        {
            int size = (int)currentPlatformRenderPipelineSettings.lightLoopSettings.planarReflectionAtlasSize;
            return (int)Mathf.Log(size, 2);
        }

        internal int GetMaxScreenSpaceShadows()
        {
            return currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows ? currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots : 0;
        }

        readonly SkyManager m_SkyManager = new SkyManager();
        internal SkyManager skyManager { get { return m_SkyManager; } }
        readonly AmbientOcclusionSystem m_AmbientOcclusionSystem;

        // Debugging
        DebugDisplaySettings m_DebugDisplaySettings = new DebugDisplaySettings();
#if ENABLE_VIRTUALTEXTURES
        Material m_VTDebugBlit;
#endif
        /// <summary>
        /// Debug display settings.
        /// </summary>
        public DebugDisplaySettings debugDisplaySettings { get { return m_DebugDisplaySettings; } }
        static DebugDisplaySettings s_NeutralDebugDisplaySettings = new DebugDisplaySettings();
        internal DebugDisplaySettings m_CurrentDebugDisplaySettings;
        // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
        bool                            m_FullScreenDebugPushed;
        bool                            m_ValidAPI; // False by default mean we render normally, true mean we don't render anything
        bool                            m_IsDepthBufferCopyValid;
        RenderTexture                   m_TemporaryTargetForCubemaps;
        HDCamera                        m_CurrentHDCamera;

        private CameraCache<(Transform viewer, HDProbe probe, int face)> m_ProbeCameraCache = new
            CameraCache<(Transform viewer, HDProbe probe, int face)>();

        internal Material GetBlitMaterial(bool useTexArray, bool singleSlice) { return useTexArray ? (singleSlice ? m_BlitTexArraySingleSlice : m_BlitTexArray) : m_Blit; }
        internal Material GetBlitColorAndDepthMaterial() { return m_BlitColorAndDepth; }

        ComputeBuffer m_DepthPyramidMipLevelOffsetsBuffer = null;

        ScriptableCullingParameters frozenCullingParams;
        bool frozenCullingParamAvailable = false;

        internal bool showCascade
        {
            get => m_DebugDisplaySettings.GetDebugLightingMode() == DebugLightingMode.VisualizeCascade;
            set
            {
                if (value)
                    m_DebugDisplaySettings.SetDebugLightingMode(DebugLightingMode.VisualizeCascade);
                else
                    m_DebugDisplaySettings.SetDebugLightingMode(DebugLightingMode.None);
            }
        }

        // RENDER GRAPH
        RenderGraph m_RenderGraph = new RenderGraph("HDRPGraph");

        // MSAA resolve materials
        Material m_ColorResolveMaterial = null;
        Material m_MotionVectorResolve = null;

        internal Material GetMSAAColorResolveMaterial()
        {
            return m_ColorResolveMaterial;
        }

        // Flag that defines if ray tracing is supported by the current asset and platform
        bool m_RayTracingSupported = false;
        /// <summary>
        ///  Flag that defines if ray tracing is supported by the current HDRP asset and platform
        /// </summary>
        public bool rayTracingSupported { get { return m_RayTracingSupported; } }


#if UNITY_EDITOR
        bool m_ResourcesInitialized = false;
#endif

        internal bool reflectionProbeBaking { get; set; }

        /// <summary>
        /// HDRenderPipeline constructor.
        /// </summary>
        /// <param name="asset">Source HDRenderPipelineAsset.</param>
        /// <param name="defaultAsset">Defauklt HDRenderPipelineAsset.</param>
        public HDRenderPipeline(HDRenderPipelineAsset asset, HDRenderPipelineAsset defaultAsset)
        {
            m_Asset = asset;
            m_DefaultAsset = defaultAsset;
            HDProbeSystem.Parameters = asset.reflectionSystemParameters;

            DebugManager.instance.RefreshEditor();

            m_ValidAPI = true;

            SetRenderingFeatures();

            // Initialize lod settings with the default frame settings. This will pull LoD values from the current quality level HDRP asset if necessary.
            // This will make the LoD Group UI consistent with the scene view camera like it is for builtin pipeline.
            QualitySettings.lodBias = m_Asset.GetDefaultFrameSettings(FrameSettingsRenderType.Camera).GetResolvedLODBias(m_Asset);
            QualitySettings.maximumLODLevel = m_Asset.GetDefaultFrameSettings(FrameSettingsRenderType.Camera).GetResolvedMaximumLODLevel(m_Asset);

            // The first thing we need to do is to set the defines that depend on the render pipeline settings
            m_RayTracingSupported = GatherRayTracingSupport(m_Asset.currentPlatformRenderPipelineSettings);

#if UNITY_EDITOR
            // If defaultAsset is not ready (can happen due to loading order issue), then we should return
            // There is a similar check in Render()
            if (HDRenderPipeline.defaultAsset == null)
                return;

            UpgradeResourcesIfNeeded();

            //In case we are loading element in the asset pipeline (occurs when library is not fully constructed) the creation of the HDRenderPipeline is done at a time we cannot access resources.
            //So in this case, the reloader would fail and the resources cannot be validated. So skip validation here.
            //The HDRenderPipeline will be reconstructed in a few frame which will fix this issue.
            if (HDRenderPipeline.defaultAsset.renderPipelineResources == null
                || HDRenderPipeline.defaultAsset.renderPipelineEditorResources == null
                || (m_RayTracingSupported && HDRenderPipeline.defaultAsset.renderPipelineRayTracingResources == null))
                return;
            else
                m_ResourcesInitialized = true;

            ValidateResources();
#endif

            // We need to call this after the resource initialization as we attempt to use them in checking the supported API.
            if (!CheckAPIValidity())
            {
                m_ValidAPI = false;

                return;
            }

            var defaultLensAttenuation = m_DefaultAsset.lensAttenuationMode;
            if (defaultLensAttenuation == LensAttenuationMode.ImperfectLens)
            {
                ColorUtils.s_LensAttenuation = 0.65f;
            }
            else if (defaultLensAttenuation == LensAttenuationMode.PerfectLens)
            {
                ColorUtils.s_LensAttenuation = 0.78f;
            }

#if ENABLE_VIRTUALTEXTURES
            VirtualTexturingSettingsSRP settings = asset.virtualTexturingSettings;

            if (settings == null)
                settings = new VirtualTexturingSettingsSRP();

            VirtualTexturing.Streaming.SetCPUCacheSize(settings.streamingCpuCacheSizeInMegaBytes);

            GPUCacheSetting[] gpuCacheSettings = new GPUCacheSetting[settings.streamingGpuCacheSettings.Count];
            for (int i = 0; i < settings.streamingGpuCacheSettings.Count; ++i)
            {
                GPUCacheSettingSRP srpSetting = settings.streamingGpuCacheSettings[i];
                gpuCacheSettings[i] = new GPUCacheSetting() { format = srpSetting.format, sizeInMegaBytes = srpSetting.sizeInMegaBytes };
            }

            VirtualTexturing.Streaming.SetGPUCacheSettings(gpuCacheSettings);
#endif

            // Initial state of the RTHandle system.
            // Tells the system that we will require MSAA or not so that we can avoid wasteful render texture allocation.
            // We initialize to screen width/height to avoid multiple realloc that can lead to inflated memory usage (as releasing of memory is delayed).
            RTHandles.Initialize(Screen.width, Screen.height, m_Asset.currentPlatformRenderPipelineSettings.supportMSAA, m_Asset.currentPlatformRenderPipelineSettings.msaaSampleCount);

            m_XRSystem = new XRSystem(asset.renderPipelineResources.shaders);
            m_GPUCopy = new GPUCopy(defaultResources.shaders.copyChannelCS);

            m_MipGenerator = new MipGenerator(defaultResources);
            m_BlueNoise = new BlueNoise(defaultResources);

            EncodeBC6H.DefaultInstance = EncodeBC6H.DefaultInstance ?? new EncodeBC6H(defaultResources.shaders.encodeBC6HCS);

            // Scan material list and assign it
            m_MaterialList = HDUtils.GetRenderPipelineMaterialList();

#if ENABLE_VIRTUALTEXTURES
            m_VtBufferManager = new VTBufferManager(asset);
            m_VTDebugBlit = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugViewVirtualTexturingBlit);
#endif

            m_PostProcessSystem = new PostProcessSystem(asset, defaultResources);
            m_AmbientOcclusionSystem = new AmbientOcclusionSystem(asset, defaultResources);

            // Initialize various compute shader resources
            m_SsrTracingKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsTracing");
            m_SsrReprojectionKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsReprojection");
            m_SsrAccumulateKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulate");

            // General material
            m_CameraMotionVectorsMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.cameraMotionVectorsPS);
            m_DecalNormalBufferMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.decalNormalBufferPS);

            m_CopyDepth = CoreUtils.CreateEngineMaterial(defaultResources.shaders.copyDepthBufferPS);
            m_DownsampleDepthMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.downsampleDepthPS);
            m_UpsampleTransparency = CoreUtils.CreateEngineMaterial(defaultResources.shaders.upsampleTransparentPS);

            m_ApplyDistortionMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.applyDistortionPS);

            m_ClearStencilBufferMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.clearStencilBufferPS);

            InitializeDebugMaterials();

            m_MaterialList.ForEach(material => material.Build(asset, defaultResources));

            if (m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.supportFabricConvolution)
            {
                m_IBLFilterArray = new IBLFilterBSDF[2];
                m_IBLFilterArray[0] = new IBLFilterGGX(defaultResources, m_MipGenerator);
                m_IBLFilterArray[1] = new IBLFilterCharlie(defaultResources, m_MipGenerator);
            }
            else
            {
                m_IBLFilterArray = new IBLFilterBSDF[1];
                m_IBLFilterArray[0] = new IBLFilterGGX(defaultResources, m_MipGenerator);
            }

            InitializeLightLoop(m_IBLFilterArray);

            m_SkyManager.Build(asset, defaultResources, m_IBLFilterArray);

            InitializeVolumetricLighting();
            InitializeSubsurfaceScattering();

            m_DebugDisplaySettings.RegisterDebug();
#if UNITY_EDITOR
            // We don't need the debug of Scene View at runtime (each camera have its own debug settings)
            // All scene view will share the same FrameSettings for now as sometimes Dispose is called after
            // another instance of HDRenderPipeline constructor is called.

            Camera firstSceneViewCamera = UnityEditor.SceneView.sceneViews.Count > 0 ? (UnityEditor.SceneView.sceneViews[0] as UnityEditor.SceneView).camera : null;
            if (firstSceneViewCamera != null)
            {
                var history = FrameSettingsHistory.RegisterDebug(null, true);
                DebugManager.instance.RegisterData(history);
            }
#endif

            m_DepthPyramidMipLevelOffsetsBuffer = new ComputeBuffer(15, sizeof(int) * 2);

            // TODO RENDERGRAPH: Moved those out of InitializeRenderTexture as they are still needed in render graph and would be deallocated otherwise when switching it on.
            m_CustomPassColorBuffer = new Lazy<RTHandle>(() => RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GetCustomBufferFormat(), enableRandomWrite: true, useDynamicScale: true, name: "CustomPassColorBuffer"));
            m_CustomPassDepthBuffer = new Lazy<RTHandle>(() => RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R32_UInt, useDynamicScale: true, name: "CustomPassDepthBuffer", depthBufferBits: DepthBits.Depth32));

            // For debugging
            MousePositionDebug.instance.Build();

            InitializeRenderStateBlocks();

            // Keep track of the original msaa sample value
            // TODO : Bind this directly to the debug menu instead of having an intermediate value
            m_MSAASamples = m_Asset ? m_Asset.currentPlatformRenderPipelineSettings.msaaSampleCount : MSAASamples.None;

            if (m_RayTracingSupported)
            {
                InitRayTracingManager();
                InitRayTracedReflections();
                InitRayTracedIndirectDiffuse();
                InitRaytracingDeferred();
                InitRecursiveRenderer();
                InitPathTracing();

                m_AmbientOcclusionSystem.InitRaytracing(this);
            }

            // Initialize the SSGI structures
            InitScreenSpaceGlobalIllumination();

            // Initialize screen space shadows
            InitializeScreenSpaceShadows();

            CameraCaptureBridge.enabled = true;

            InitializePrepass(m_Asset);
            m_ColorResolveMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.colorResolvePS);
            m_MotionVectorResolve = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.resolveMotionVecPS);

            InitializeProbeVolumes();
            CustomPassUtils.Initialize();
        }

#if UNITY_EDITOR
        void UpgradeResourcesInAssetIfNeeded(HDRenderPipelineAsset asset)
        {
            // Check that the serialized Resources are not broken
            if (asset.renderPipelineResources == null)
                asset.renderPipelineResources
                    = UnityEditor.AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineResources.asset");
#if UNITY_EDITOR_LINUX // Temp hack to be able to make linux test run. To clarify
            ResourceReloader.TryReloadAllNullIn(asset.renderPipelineResources, HDUtils.GetHDRenderPipelinePath());
#else
            ResourceReloader.ReloadAllNullIn(asset.renderPipelineResources, HDUtils.GetHDRenderPipelinePath());
#endif

            if (GatherRayTracingSupport(asset.currentPlatformRenderPipelineSettings))
            {
                if (asset.renderPipelineRayTracingResources == null)
                    asset.renderPipelineRayTracingResources
                        = UnityEditor.AssetDatabase.LoadAssetAtPath<HDRenderPipelineRayTracingResources>(HDUtils.GetHDRenderPipelinePath() + "Runtime/RenderPipelineResources/HDRenderPipelineRayTracingResources.asset");
#if UNITY_EDITOR_LINUX // Temp hack to be able to make linux test run. To clarify
                ResourceReloader.TryReloadAllNullIn(asset.renderPipelineRayTracingResources, HDUtils.GetHDRenderPipelinePath());
#else
                ResourceReloader.ReloadAllNullIn(asset.renderPipelineRayTracingResources, HDUtils.GetHDRenderPipelinePath());
#endif
            }
            else
            {
                // If ray tracing is not enabled we do not want to have ray tracing resources referenced
                asset.renderPipelineRayTracingResources = null;
            }

            var editorResourcesPath = HDUtils.GetHDRenderPipelinePath() + "Editor/RenderPipelineResources/HDRenderPipelineEditorResources.asset";
            if (asset.renderPipelineEditorResources == null)
            {
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(editorResourcesPath);
                asset.renderPipelineEditorResources = objs != null && objs.Length > 0 ? objs.First() as HDRenderPipelineEditorResources : null;
            }

            if (ResourceReloader.ReloadAllNullIn(asset.renderPipelineEditorResources,
                HDUtils.GetHDRenderPipelinePath()))
            {
                InternalEditorUtility.SaveToSerializedFileAndForget(
                    new Object[] {asset.renderPipelineEditorResources },
                    editorResourcesPath,
                    true);
            }

            // Upgrade the resources (re-import every references in RenderPipelineResources) if the resource version mismatches
            // It's done here because we know every HDRP assets have been imported before
            asset.renderPipelineResources?.UpgradeIfNeeded();
        }

        void UpgradeResourcesIfNeeded()
        {
            // The first thing we need to do is to set the defines that depend on the render pipeline settings
            m_Asset.EvaluateSettings();

            // Check and fix both the default and current HDRP asset
            UpgradeResourcesInAssetIfNeeded(HDRenderPipeline.defaultAsset);
            UpgradeResourcesInAssetIfNeeded(HDRenderPipeline.currentAsset);
        }

        void ValidateResources()
        {
            var resources = HDRenderPipeline.defaultAsset.renderPipelineResources;

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

        /// <summary>
        /// Resets the reference size of the internal RTHandle System.
        /// This allows users to reduce the memory footprint of render textures after doing a super sampled rendering pass for example.
        /// </summary>
        /// <param name="width">New width of the internal RTHandle System.</param>
        /// <param name="height">New height of the internal RTHandle System.</param>
        public void ResetRTHandleReferenceSize(int width, int height)
        {
            RTHandles.ResetReferenceSize(width, height);
            HDCamera.ResetAllHistoryRTHandleSystems(width, height);
        }

        void SetRenderingFeatures()
        {
            // Set sub-shader pipeline tag
            Shader.globalRenderPipeline = "HDRenderPipeline";

            // HD use specific GraphicsSettings
            m_PreviousLightsUseLinearIntensity = GraphicsSettings.lightsUseLinearIntensity;
            GraphicsSettings.lightsUseLinearIntensity = true;
            m_PreviousLightsUseColorTemperature = GraphicsSettings.lightsUseColorTemperature;
            GraphicsSettings.lightsUseColorTemperature = true;
            m_PreviousSRPBatcher = GraphicsSettings.useScriptableRenderPipelineBatching;
            GraphicsSettings.useScriptableRenderPipelineBatching = m_Asset.enableSRPBatcher;
#if  UNITY_2020_2_OR_NEWER
            m_PreviousDefaultRenderingLayerMask = GraphicsSettings.defaultRenderingLayerMask;
            GraphicsSettings.defaultRenderingLayerMask = ShaderVariablesGlobal.DefaultRenderingLayerMask;
#endif

            // In case shadowmask mode isn't setup correctly, force it to correct usage (as there is no UI to fix it)
            m_PreviousShadowMaskMode = QualitySettings.shadowmaskMode;
            QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.Rotation,
                defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly,
                mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly | (m_Asset.currentPlatformRenderPipelineSettings.supportShadowMask ? SupportedRenderingFeatures.LightmapMixedBakeModes.Shadowmask : 0),
                lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime,
                lightmapsModes = LightmapsMode.NonDirectional | LightmapsMode.CombinedDirectional,
                lightProbeProxyVolumes = true,
                motionVectors = true,
                receiveShadows = false,
                reflectionProbes = false,
                rendererPriority = true,
                overridesFog = true,
                overridesOtherLightingSettings = true,
                editableMaterialRenderQueue = false
                    // Enlighten is deprecated in 2019.3 and above
                , enlighten = false
                , overridesLODBias = true
                , overridesMaximumLODLevel = true
                , terrainDetailUnsupported = true
                , overridesShadowmask = true // Don't display the shadow mask UI in Quality Settings
                , overrideShadowmaskMessage = "\nThe Shadowmask Mode used at run time can be found in the Shadows section of Light component."
                , overridesRealtimeReflectionProbes = true // Don't display the real time reflection probes checkbox UI in Quality Settings
            };

            Lightmapping.SetDelegate(GlobalIlluminationUtils.hdLightsDelegate);

#if UNITY_EDITOR
            // HDRP always enable baking of cookie by default
            m_PreviousEnableCookiesInLightmapper = UnityEditor.EditorSettings.enableCookiesInLightmapper;
            UnityEditor.EditorSettings.enableCookiesInLightmapper = true;

            SceneViewDrawMode.SetupDrawMode();

            if (UnityEditor.PlayerSettings.colorSpace == ColorSpace.Gamma)
            {
                Debug.LogError("High Definition Render Pipeline doesn't support Gamma mode, change to Linear mode (HDRP isn't set up properly. Go to Windows > RenderPipeline > HDRP Wizard to fix your settings).");
            }
#endif
        }

        bool CheckAPIValidity()
        {
            if (!IsSupportedPlatformAndDevice(out GraphicsDeviceType deviceType))
            {
                string msg = HDUtils.GetUnsupportedAPIMessage(deviceType.ToString());
                HDUtils.DisplayMessageNotification(msg);

                return false;
            }

            return true;
        }

        // Note: If you add new platform in this function, think about adding support when building the player too in HDRPCustomBuildProcessor.cs
        bool IsSupportedPlatformAndDevice(out GraphicsDeviceType unsupportedGraphicDevice)
        {
            unsupportedGraphicDevice = SystemInfo.graphicsDeviceType;

            if (!SystemInfo.supportsComputeShaders)
            {
                HDUtils.DisplayMessageNotification("Current platform / API don't support ComputeShaders which is a requirement.");
                return false;
            }

            if (!(defaultResources?.shaders.defaultPS?.isSupported ?? true))
            {
                HDUtils.DisplayMessageNotification("Unable to compile Default Material based on Lit.shader. Either there is a compile error in Lit.shader or the current platform / API isn't compatible.");
                return false;
            }

#if UNITY_EDITOR
            UnityEditor.BuildTarget activeBuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            return HDUtils.IsSupportedBuildTargetAndDevice(activeBuildTarget, out unsupportedGraphicDevice);
#else
            return HDUtils.IsSupportedGraphicDevice(SystemInfo.graphicsDeviceType) && HDUtils.IsOperatingSystemSupported(SystemInfo.operatingSystem);
#endif
        }

        void UnsetRenderingFeatures()
        {
            Shader.globalRenderPipeline = "";

            GraphicsSettings.lightsUseLinearIntensity = m_PreviousLightsUseLinearIntensity;
            GraphicsSettings.lightsUseColorTemperature = m_PreviousLightsUseColorTemperature;
            GraphicsSettings.useScriptableRenderPipelineBatching = m_PreviousSRPBatcher;
#if UNITY_2020_2_OR_NEWER
            GraphicsSettings.defaultRenderingLayerMask = m_PreviousDefaultRenderingLayerMask;
#endif
            QualitySettings.shadowmaskMode = m_PreviousShadowMaskMode;

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

            Lightmapping.ResetDelegate();

#if UNITY_EDITOR
            UnityEditor.EditorSettings.enableCookiesInLightmapper = m_PreviousEnableCookiesInLightmapper;
#endif
        }

        void InitializeRenderGraph()
        {
            m_RenderGraph = new RenderGraph("HDRPGraph");
        }

        void CleanupRenderGraph()
        {
            m_RenderGraph.Cleanup();
            m_RenderGraph = null;
        }

        void InitializeDebugMaterials()
        {
            m_DebugViewMaterialGBuffer = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugViewMaterialGBufferPS);
            m_DebugViewMaterialGBufferShadowMask = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugViewMaterialGBufferPS);
            m_DebugViewMaterialGBufferShadowMask.EnableKeyword("SHADOWS_SHADOWMASK");
            m_DebugDisplayLatlong = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugDisplayLatlongPS);
            m_DebugFullScreen = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugFullScreenPS);
            m_DebugColorPicker = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugColorPickerPS);
            m_DebugExposure = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugExposurePS);
            m_Blit = CoreUtils.CreateEngineMaterial(defaultResources.shaders.blitPS);
            m_BlitColorAndDepth = CoreUtils.CreateEngineMaterial(defaultResources.shaders.blitColorAndDepthPS);
            m_ErrorMaterial = CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader");

            // With texture array enabled, we still need the normal blit version for other systems like atlas
            if (TextureXR.useTexArray)
            {
                m_Blit.EnableKeyword("DISABLE_TEXTURE2D_X_ARRAY");
                m_BlitTexArray = CoreUtils.CreateEngineMaterial(defaultResources.shaders.blitPS);
                m_BlitTexArraySingleSlice = CoreUtils.CreateEngineMaterial(defaultResources.shaders.blitPS);
                m_BlitTexArraySingleSlice.EnableKeyword("BLIT_SINGLE_SLICE");
            }
        }

        void InitializeRenderStateBlocks()
        {
            m_DepthStateOpaque = new RenderStateBlock
            {
                depthState = new DepthState(true, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };

            m_DepthStateNoWrite = new RenderStateBlock
            {
                depthState = new DepthState(false, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };

            m_AlphaToMaskBlock = new RenderStateBlock
            {
                blendState = new BlendState(true, false),
                mask = RenderStateMask.Blend
            };
        }

        /// <summary>
        /// Disposable pattern implementation.
        /// </summary>
        /// <param name="disposing">Is disposing.</param>
        protected override void Dispose(bool disposing)
        {
            DisposeProbeCameraPool();

            UnsetRenderingFeatures();

            if (!m_ValidAPI)
                return;

#if UNITY_EDITOR
            if (!m_ResourcesInitialized)
                return;
#endif

            base.Dispose(disposing);

            ReleaseScreenSpaceShadows();

            if (m_RayTracingSupported)
            {
                ReleaseRayTracingDeferred();
                ReleaseRayTracedIndirectDiffuse();
                ReleasePathTracing();
            }

            ReleaseRayTracingManager();
            m_DebugDisplaySettings.UnregisterDebug();

            CleanupLightLoop();

            // For debugging
            MousePositionDebug.instance.Cleanup();

            DecalSystem.instance.Cleanup();

            m_MaterialList.ForEach(material => material.Cleanup());

            CoreUtils.Destroy(m_CameraMotionVectorsMaterial);
            CoreUtils.Destroy(m_DecalNormalBufferMaterial);

#if ENABLE_VIRTUALTEXTURES
            m_VtBufferManager.Cleanup();
            CoreUtils.Destroy(m_VTDebugBlit);
#endif
            CoreUtils.Destroy(m_DebugViewMaterialGBuffer);
            CoreUtils.Destroy(m_DebugViewMaterialGBufferShadowMask);
            CoreUtils.Destroy(m_DebugDisplayLatlong);
            CoreUtils.Destroy(m_DebugFullScreen);
            CoreUtils.Destroy(m_DebugColorPicker);
            CoreUtils.Destroy(m_DebugExposure);
            CoreUtils.Destroy(m_Blit);
            CoreUtils.Destroy(m_BlitTexArray);
            CoreUtils.Destroy(m_BlitTexArraySingleSlice);
            CoreUtils.Destroy(m_CopyDepth);
            CoreUtils.Destroy(m_ErrorMaterial);
            CoreUtils.Destroy(m_DownsampleDepthMaterial);
            CoreUtils.Destroy(m_UpsampleTransparency);
            CoreUtils.Destroy(m_ApplyDistortionMaterial);
            CoreUtils.Destroy(m_ClearStencilBufferMaterial);

            m_XRSystem.Cleanup();
            m_SkyManager.Cleanup();
            CleanupVolumetricLighting();
            CleanupProbeVolumes();

            for (int bsdfIdx = 0; bsdfIdx < m_IBLFilterArray.Length; ++bsdfIdx)
            {
                m_IBLFilterArray[bsdfIdx].Cleanup();
            }

            m_PostProcessSystem.Cleanup();
            m_BlueNoise.Cleanup();

            HDCamera.ClearAll();

            m_MipGenerator.Release();

            if (m_CustomPassColorBuffer.IsValueCreated)
                RTHandles.Release(m_CustomPassColorBuffer.Value);
            if (m_CustomPassDepthBuffer.IsValueCreated)
                RTHandles.Release(m_CustomPassDepthBuffer.Value);

            CullingGroupManager.instance.Cleanup();

            CoreUtils.SafeRelease(m_DepthPyramidMipLevelOffsetsBuffer);

            CustomPassVolume.Cleanup();

            CleanupPrepass();
            CoreUtils.Destroy(m_ColorResolveMaterial);
            CoreUtils.Destroy(m_MotionVectorResolve);

            CustomPassUtils.Cleanup();
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
                //   pool is delayed, otherwise, it is destroyed as usual with `CoreUtils.Destroy`
                var isInOnValidate = false;
                isInOnValidate = new StackTrace().ToString().Contains("OnValidate");
                if (isInOnValidate)
                {
                    var pool = m_ProbeCameraCache;
                    UnityEditor.EditorApplication.delayCall += () => pool.Dispose();
                    m_ProbeCameraCache = null;
                }
                else
                {
#endif
                m_ProbeCameraCache.Dispose();
                m_ProbeCameraCache = null;
#if UNITY_EDITOR
            }

#endif

                CleanupRenderGraph();
            }

            ConstantBuffer.ReleaseAll();

            CameraCaptureBridge.enabled = false;

            // Dispose of Render Pipeline can be call either by OnValidate() or by OnDisable().
            // Inside an OnValidate() call we can't call a DestroyImmediate().
            // Here we are releasing our singleton to not leak while doing a domain reload.
            // However this is doing a call to DestroyImmediate().
            // To workaround this, and was we only leak with Singleton while doing domain reload (and not in OnValidate)
            // we are detecting if we are in an OnValidate call and releasing the Singleton only if it is not the case.
            if (!m_Asset.isInOnValidateCall)
                HDUtils.ReleaseComponentSingletons();
        }

        void Resize(HDCamera hdCamera)
        {
            // m_MaxCameraWidth and m_MaxCameraHeight start at 0 so we will at least go through this once at first frame to allocate the buffers for the first time.
            bool resolutionChanged = (hdCamera.actualWidth > m_MaxCameraWidth) || (hdCamera.actualHeight > m_MaxCameraHeight) || (hdCamera.viewCount > m_MaxViewCount);

            if (resolutionChanged)
            {
                // update recorded window resolution
                m_MaxCameraWidth = Mathf.Max(m_MaxCameraWidth, hdCamera.actualWidth);
                m_MaxCameraHeight = Mathf.Max(m_MaxCameraHeight, hdCamera.actualHeight);
                m_MaxViewCount = Math.Max(m_MaxViewCount, hdCamera.viewCount);

                if (m_MaxCameraWidth > 0 && m_MaxCameraHeight > 0)
                {
                    LightLoopReleaseResolutionDependentBuffers();
                }

                LightLoopAllocResolutionDependentBuffers(hdCamera, m_MaxCameraWidth, m_MaxCameraHeight);
            }
        }

        void UpdateGlobalConstantBuffers(HDCamera hdCamera, CommandBuffer cmd)
        {
            UpdateShaderVariablesGlobalCB(hdCamera, cmd);
            UpdateShaderVariablesXRCB(hdCamera, cmd);
            UpdateShaderVariablesRaytracingCB(hdCamera, cmd);

            // This one is not in a constant buffer because it's only used as a parameter for some shader's render states. It's not actually used inside shader code.
            cmd.SetGlobalInt(HDShaderIDs._ColorMaskTransparentVel, (int)ColorWriteMask.All);
        }

        void UpdateShaderVariablesGlobalCB(HDCamera hdCamera, CommandBuffer cmd)
        {
            hdCamera.UpdateShaderVariablesGlobalCB(ref m_ShaderVariablesGlobalCB);
            Fog.UpdateShaderVariablesGlobalCB(ref m_ShaderVariablesGlobalCB, hdCamera);
            UpdateShaderVariablesGlobalSubsurface(ref m_ShaderVariablesGlobalCB, hdCamera);
            UpdateShaderVariablesGlobalDecal(ref m_ShaderVariablesGlobalCB, hdCamera);
            UpdateShaderVariablesGlobalVolumetrics(ref m_ShaderVariablesGlobalCB, hdCamera);
            m_ShadowManager.UpdateShaderVariablesGlobalCB(ref m_ShaderVariablesGlobalCB);
            UpdateShaderVariablesGlobalLightLoop(ref m_ShaderVariablesGlobalCB, hdCamera);
            UpdateShaderVariablesGlobalProbeVolumes(ref m_ShaderVariablesGlobalCB, hdCamera);
            m_AmbientOcclusionSystem.UpdateShaderVariableGlobalCB(ref m_ShaderVariablesGlobalCB, hdCamera);

            // Misc
            MicroShadowing microShadowingSettings = hdCamera.volumeStack.GetComponent<MicroShadowing>();
            m_ShaderVariablesGlobalCB._MicroShadowOpacity = microShadowingSettings.enable.value ? microShadowingSettings.opacity.value : 0.0f;

            HDShadowSettings shadowSettings = hdCamera.volumeStack.GetComponent<HDShadowSettings>();
            m_ShaderVariablesGlobalCB._DirectionalTransmissionMultiplier = shadowSettings.directionalTransmissionMultiplier.value;

            ScreenSpaceRefraction ssRefraction = hdCamera.volumeStack.GetComponent<ScreenSpaceRefraction>();
            m_ShaderVariablesGlobalCB._SSRefractionInvScreenWeightDistance = 1.0f / ssRefraction.screenFadeDistance.value;

            IndirectLightingController indirectLightingController = hdCamera.volumeStack.GetComponent<IndirectLightingController>();
            m_ShaderVariablesGlobalCB._IndirectDiffuseLightingMultiplier = indirectLightingController.indirectDiffuseLightingMultiplier.value;
            m_ShaderVariablesGlobalCB._IndirectDiffuseLightingLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers) ? indirectLightingController.GetIndirectDiffuseLightingLayers() : uint.MaxValue;
            m_ShaderVariablesGlobalCB._ReflectionLightingMultiplier = indirectLightingController.reflectionLightingMultiplier.value;
            m_ShaderVariablesGlobalCB._ReflectionLightingLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers) ? indirectLightingController.GetReflectionLightingLayers() : uint.MaxValue;

            m_ShaderVariablesGlobalCB._OffScreenRendering = 0;
            m_ShaderVariablesGlobalCB._OffScreenDownsampleFactor = 1;
            m_ShaderVariablesGlobalCB._ReplaceDiffuseForIndirect = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ReplaceDiffuseForIndirect) ? 1.0f : 0.0f;
            m_ShaderVariablesGlobalCB._EnableSkyReflection = hdCamera.frameSettings.IsEnabled(FrameSettingsField.SkyReflection) ? 1u : 0u;
            m_ShaderVariablesGlobalCB._ContactShadowOpacity = m_ContactShadows.opacity.value;

            int coarseStencilWidth = HDUtils.DivRoundUp(hdCamera.actualWidth, 8);
            int coarseStencilHeight = HDUtils.DivRoundUp(hdCamera.actualHeight, 8);
            m_ShaderVariablesGlobalCB._CoarseStencilBufferSize = new Vector4(coarseStencilWidth, coarseStencilHeight, 1.0f / coarseStencilWidth, 1.0f / coarseStencilHeight);

            m_ShaderVariablesGlobalCB._RaytracingFrameIndex = RayTracingFrameIndex(hdCamera);
            m_ShaderVariablesGlobalCB._IndirectDiffuseMode = (int)GetIndirectDiffuseMode(hdCamera);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
            {
                // Check if recursive rendering is enabled or not. This will control the cull of primitive
                // during the gbuffer and forward pass
                ScreenSpaceReflection settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
                bool enableRaytracedReflections = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value;
                m_ShaderVariablesGlobalCB._EnableRayTracedReflections = enableRaytracedReflections ? 1 : 0;
                RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
                m_ShaderVariablesGlobalCB._EnableRecursiveRayTracing = recursiveSettings.enable.value ? 1u : 0u;

                m_ShaderVariablesGlobalCB._SpecularOcclusionBlend = m_AmbientOcclusionSystem.EvaluateSpecularOcclusionFlag(hdCamera);
            }
            else
            {
                m_ShaderVariablesGlobalCB._EnableRayTracedReflections = 0;
                m_ShaderVariablesGlobalCB._EnableRecursiveRayTracing = 0;
                m_ShaderVariablesGlobalCB._SpecularOcclusionBlend = 1.0f;
            }

            ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
        }

        void UpdateShaderVariablesXRCB(HDCamera hdCamera, CommandBuffer cmd)
        {
            hdCamera.xr.UpdateBuiltinStereoMatrices(cmd, hdCamera);
            hdCamera.UpdateShaderVariablesXRCB(ref m_ShaderVariablesXRCB);
            ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesXRCB, HDShaderIDs._ShaderVariablesXR);
        }

        void UpdateShaderVariablesRaytracingCB(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                return;

            RayTracingSettings rayTracingSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();
            ScreenSpaceReflection screenSpaceReflection = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            // Those are globally set parameters. The others are set per effect and will update the constant buffer as we render.
            m_ShaderVariablesRayTracingCB._RaytracingRayBias = rayTracingSettings.rayBias.value;
            m_ShaderVariablesRayTracingCB._RayCountEnabled = m_RayCountManager.RayCountIsEnabled();
            m_ShaderVariablesRayTracingCB._RaytracingCameraNearPlane = hdCamera.camera.nearClipPlane;
            m_ShaderVariablesRayTracingCB._RaytracingPixelSpreadAngle = GetPixelSpreadAngle(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);
            m_ShaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = screenSpaceReflection.minSmoothness;
            m_ShaderVariablesRayTracingCB._RaytracingReflectionSmoothnessFadeStart = screenSpaceReflection.smoothnessFadeStart;
            m_ShaderVariablesRayTracingCB._DirectionalShadowFallbackIntensity = rayTracingSettings.directionalShadowFallbackIntensity.value;

            ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);
        }

        struct BuildCoarseStencilAndResolveParameters
        {
            public HDCamera         hdCamera;
            public ComputeShader    resolveStencilCS;
            public int              resolveKernel;
            public bool             resolveIsNecessary;
            public bool             resolveOnly;
        }

        BuildCoarseStencilAndResolveParameters PrepareBuildCoarseStencilParameters(HDCamera hdCamera, bool resolveOnly)
        {
            var parameters = new BuildCoarseStencilAndResolveParameters();
            parameters.hdCamera = hdCamera;
            parameters.resolveStencilCS = defaultResources.shaders.resolveStencilCS;

            bool MSAAEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);

            // The following features require a copy of the stencil, if none are active, no need to do the resolve.
            bool resolveIsNecessary = GetFeatureVariantsEnabled(hdCamera.frameSettings);
            resolveIsNecessary = resolveIsNecessary || hdCamera.IsSSREnabled()
                || hdCamera.IsSSREnabled(transparent: true);
            // We need the resolve only with msaa
            parameters.resolveIsNecessary = resolveIsNecessary && MSAAEnabled;

            int kernel = SampleCountToPassIndex(MSAAEnabled ? hdCamera.msaaSamples : MSAASamples.None);
            parameters.resolveKernel = parameters.resolveIsNecessary ? kernel + 3 : kernel; // We have a different variant if we need to resolve to non-MSAA stencil
            parameters.resolveOnly = resolveOnly;

            if (parameters.resolveIsNecessary && resolveOnly)
            {
                parameters.resolveKernel = (kernel - 1) + 7;
            }

            return parameters;
        }

        static void BuildCoarseStencilAndResolveIfNeeded(BuildCoarseStencilAndResolveParameters parameters, RTHandle depthStencilBuffer, RTHandle resolvedStencilBuffer, ComputeBuffer coarseStencilBuffer, CommandBuffer cmd)
        {
            if (parameters.resolveOnly && !parameters.resolveIsNecessary)
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.CoarseStencilGeneration)))
            {
                ComputeShader cs = parameters.resolveStencilCS;
                cmd.SetComputeBufferParam(cs, parameters.resolveKernel, HDShaderIDs._CoarseStencilBuffer, coarseStencilBuffer);
                cmd.SetComputeTextureParam(cs, parameters.resolveKernel, HDShaderIDs._StencilTexture, depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

                if (parameters.resolveIsNecessary)
                {
                    cmd.SetComputeTextureParam(cs, parameters.resolveKernel, HDShaderIDs._OutputStencilBuffer, resolvedStencilBuffer);
                }

                int coarseStencilWidth = HDUtils.DivRoundUp(parameters.hdCamera.actualWidth, 8);
                int coarseStencilHeight = HDUtils.DivRoundUp(parameters.hdCamera.actualHeight, 8);
                cmd.DispatchCompute(cs, parameters.resolveKernel, coarseStencilWidth, coarseStencilHeight, parameters.hdCamera.viewCount);
            }
        }

        void ConfigureKeywords(bool enableBakeShadowMask, HDCamera hdCamera, CommandBuffer cmd)
        {
            // Globally enable (for GBuffer shader and forward lit (opaque and transparent) the keyword SHADOWS_SHADOWMASK
            CoreUtils.SetKeyword(cmd, "SHADOWS_SHADOWMASK", enableBakeShadowMask);
            // Configure material to use depends on shadow mask option
            m_CurrentRendererConfigurationBakedLighting = enableBakeShadowMask ? HDUtils.k_RendererConfigurationBakedLightingWithShadowMask : HDUtils.k_RendererConfigurationBakedLighting;
            m_currentDebugViewMaterialGBuffer = enableBakeShadowMask ? m_DebugViewMaterialGBufferShadowMask : m_DebugViewMaterialGBuffer;

            CoreUtils.SetKeyword(cmd, "LIGHT_LAYERS", hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers));

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

            // Raise the decal buffer flag only if we have decal enabled
            CoreUtils.SetKeyword(cmd, "WRITE_DECAL_BUFFER", hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers));

            // Raise or remove the depth msaa flag based on the frame setting
            CoreUtils.SetKeyword(cmd, "WRITE_MSAA_DEPTH", hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA));
        }

        struct RenderRequest
        {
            public struct Target
            {
                public RenderTargetIdentifier id;
                public CubemapFace face;
                public RTHandle copyToTarget;
                public RTHandle targetDepth;
            }
            public HDCamera hdCamera;
            public bool clearCameraSettings;
            public Target target;
            public HDCullingResults cullingResults;
            public int index;
            // Indices of render request to render before this one
            public List<int> dependsOnRenderRequestIndices;
            public CameraSettings cameraSettings;
            public List<(HDProbe.RenderData, HDProbe)> viewDependentProbesData;
        }

        struct HDCullingResults
        {
            public CullingResults cullingResults;
            public CullingResults? customPassCullingResults;
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

#if UNITY_2021_1_OR_NEWER
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            Render(renderContext, new List<Camera>(cameras));
        }

#endif

        /// <summary>
        /// RenderPipeline Render implementation.
        /// </summary>
        /// <param name="renderContext">Current ScriptableRenderContext.</param>
        /// <param name="cameras">List of cameras to render.</param>
#if UNITY_2021_1_OR_NEWER
        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
#else
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
#endif
        {
#if UNITY_EDITOR
            if (!m_ResourcesInitialized)
                return;
#endif

#if UNITY_2021_1_OR_NEWER
            if (!m_ValidAPI || cameras.Count == 0)
#else
            if (!m_ValidAPI || cameras.Length == 0)
#endif
                return;

            GetOrCreateDefaultVolume();

            // This function should be called once every render (once for all camera)
            LightLoopNewRender();

#if UNITY_2021_1_OR_NEWER
            BeginContextRendering(renderContext, cameras);
#else
            BeginFrameRendering(renderContext, cameras);
#endif

            // Check if we can speed up FrameSettings process by skiping history
            // or go in detail if debug is activated. Done once for all renderer.
            m_FrameSettingsHistoryEnabled = FrameSettingsHistory.enabled;

#if UNITY_EDITOR
            int newCount = m_FrameCount;
            foreach (var c in cameras)
            {
                if (c.cameraType != CameraType.Preview)
                {
                    newCount++;
                    break;
                }
            }
#else
            int newCount = Time.frameCount;
#endif
            if (newCount != m_FrameCount)
            {
                m_FrameCount = newCount;
                m_ProbeCameraCache.ClearCamerasUnusedFor(2, Time.frameCount);
                HDCamera.CleanUnused();
            }

            // This syntax is awful and hostile to debugging, please don't use it...
            using (ListPool<RenderRequest>.Get(out List<RenderRequest> renderRequests))
            using (ListPool<int>.Get(out List<int> rootRenderRequestIndices))
            using (HashSetPool<int>.Get(out HashSet<int> skipClearCullingResults))
            using (DictionaryPool<HDProbe, List<(int index, float weight)>>.Get(out Dictionary<HDProbe, List<(int index, float weight)>> renderRequestIndicesWhereTheProbeIsVisible))
            using (ListPool<CameraSettings>.Get(out List<CameraSettings> cameraSettings))
            using (ListPool<CameraPositionSettings>.Get(out List<CameraPositionSettings> cameraPositionSettings))
            {
                // With XR multi-pass enabled, each camera can be rendered multiple times with different parameters
                var multipassCameras = m_XRSystem.SetupFrame(cameras, m_Asset.currentPlatformRenderPipelineSettings.xrSettings.singlePass, m_DebugDisplaySettings.data.xrSinglePassTestMode);

#if UNITY_EDITOR
                // See comment below about the preview camera workaround
                bool hasGameViewCamera = false;
                foreach (var c in cameras)
                {
                    if (c.cameraType == CameraType.Game)
                    {
                        hasGameViewCamera = true;
                        break;
                    }
                }
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
                    // need a preview
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

                    DynamicResolutionHandler.UpdateAndUseCamera(camera, m_Asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings);
                    var dynResHandler = DynamicResolutionHandler.instance;

                    bool cameraRequestedDynamicRes = false;
                    HDAdditionalCameraData hdCam;
                    if (camera.TryGetComponent<HDAdditionalCameraData>(out hdCam))
                    {
                        cameraRequestedDynamicRes = hdCam.allowDynamicResolution && camera.cameraType == CameraType.Game;

                        // We are in a case where the platform does not support hw dynamic resolution, so we force the software fallback.
                        // TODO: Expose the graphics caps info on whether the platform supports hw dynamic resolution or not.
                        // Temporarily disable HW Dynamic resolution on metal until the problems we have with it are fixed
                        if (dynResHandler.RequestsHardwareDynamicResolution() && cameraRequestedDynamicRes && !camera.allowDynamicResolution)
                        {
                            dynResHandler.ForceSoftwareFallback();
                        }
                    }

                    dynResHandler.SetCurrentCameraRequest(cameraRequestedDynamicRes);
                    RTHandles.SetHardwareDynamicResolutionState(dynResHandler.HardwareDynamicResIsEnabled());

                    VFXManager.PrepareCamera(camera);

                    // Reset pooled variables
                    cameraSettings.Clear();
                    cameraPositionSettings.Clear();
                    skipClearCullingResults.Clear();

                    var cullingResults = UnsafeGenericPool<HDCullingResults>.Get();
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
                                if (camera == req.hdCamera.camera && req.hdCamera.xr.cullingPassId == xrPass.cullingPassId)
                                {
                                    UnsafeGenericPool<HDCullingResults>.Release(cullingResults);
                                    cullingResults = req.cullingResults;
                                    skipClearCullingResults.Add(req.index);
                                    needCulling = false;
                                    m_SkyManager.UpdateCurrentSkySettings(hdCamera);
                                }
                            }
                        }

                        if (needCulling)
                            skipRequest = !TryCull(camera, hdCamera, renderContext, m_SkyManager, cullingParameters, m_Asset, ref cullingResults);
                    }

                    if (additionalCameraData != null && additionalCameraData.hasCustomRender)
                    {
                        skipRequest = true;
                        // Execute custom render
                        UnityEngine.Rendering.RenderPipeline.BeginCameraRendering(renderContext, camera);
                        additionalCameraData.ExecuteCustomRender(renderContext, hdCamera);
                    }

                    if (skipRequest)
                    {
                        // Submit render context and free pooled resources for this request
                        renderContext.Submit();
                        UnsafeGenericPool<HDCullingResults>.Release(cullingResults);
                        UnityEngine.Rendering.RenderPipeline.EndCameraRendering(renderContext, camera);
                        continue;
                    }

                    // Select render target
                    RenderTargetIdentifier targetId = camera.targetTexture ?? new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
                    if (camera.targetTexture != null)
                    {
                        camera.targetTexture.IncrementUpdateCount(); // Necessary if the texture is used as a cookie.
                    }

                    // Render directly to XR render target if active
                    if (hdCamera.xr.enabled && hdCamera.xr.renderTargetValid)
                        targetId = hdCamera.xr.renderTarget;

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
                        cameraSettings = CameraSettings.From(hdCamera),
                        viewDependentProbesData = ListPool<(HDProbe.RenderData, HDProbe)>.Get()
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

                        HDAdditionalReflectionData additionalReflectionData;
                        if (!visibleProbe.reflectionProbe.TryGetComponent<HDAdditionalReflectionData>(out additionalReflectionData))
                            additionalReflectionData = visibleProbe.reflectionProbe.gameObject.AddComponent<HDAdditionalReflectionData>();

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

                        float visibility = ComputeVisibility(visibleInIndex, probe);

                        // Notify that we render the probe at this frame
                        // NOTE: If the probe was rendered on the very first frame, we could have some data that was used and it wasn't in a fully initialized state, which is fine on PC, but on console
                        // might lead to NaNs due to lack of complete initialization. To circumvent this, we force the probe to render again only if it was rendered on the first frame. Note that the problem
                        // doesn't apply if probe is enable any frame other than the very first. Also note that we are likely to be re-rendering the probe anyway due to the issue on sky ambient probe
                        // (see m_SkyManager.HasSetValidAmbientProbe in this function).
                        // Also, we need to set the probe as rendered only if we'll actually render it and this won't happen if visibility is not > 0.
                        if (m_FrameCount > 1 && visibility > 0.0f)
                            probe.SetIsRendered();

                        if (!renderRequestIndicesWhereTheProbeIsVisible.TryGetValue(probe, out var visibleInIndices))
                        {
                            visibleInIndices = ListPool<(int index, float weight)>.Get();
                            renderRequestIndicesWhereTheProbeIsVisible.Add(probe, visibleInIndices);
                        }
                        if (!visibleInIndices.Contains((visibleInIndex, visibility)))
                            visibleInIndices.Add((visibleInIndex, visibility));
                    }

                    float ComputeVisibility(int visibleInIndex, HDProbe visibleProbe)
                    {
                        var visibleInRenderRequest = renderRequests[visibleInIndex];
                        var viewerTransform = visibleInRenderRequest.hdCamera.camera.transform;
                        return HDUtils.ComputeWeightedLinearFadeDistance(visibleProbe.transform.position, viewerTransform.position, visibleProbe.weight, visibleProbe.fadeDistance);
                    }
                }

                foreach (var probeToRenderAndDependencies in renderRequestIndicesWhereTheProbeIsVisible)
                {
                    var visibleProbe = probeToRenderAndDependencies.Key;
                    var visibilities = probeToRenderAndDependencies.Value;

                    // Two cases:
                    //   - If the probe is view independent, we add only one render request per face that is
                    //      a dependency for all its 'visibleIn' render requests
                    //   - If the probe is view dependent, we add one render request per face per 'visibleIn'
                    //      render requests
                    var isViewDependent = visibleProbe.type == ProbeSettings.ProbeType.PlanarProbe;

                    HDCamera hdParentCamera;

                    if (isViewDependent)
                    {
                        for (int i = 0; i < visibilities.Count; ++i)
                        {
                            var visibility = visibilities[i];
                            if (visibility.weight <= 0f)
                                continue;

                            var visibleInIndex = visibility.index;
                            var visibleInRenderRequest = renderRequests[visibleInIndex];
                            var viewerTransform = visibleInRenderRequest.hdCamera.camera.transform;

                            hdParentCamera = visibleInRenderRequest.hdCamera;

                            var renderDatas = ListPool<HDProbe.RenderData>.Get();

                            AddHDProbeRenderRequests(
                                visibleProbe,
                                viewerTransform,
                                new List<(int index, float weight)> {visibility},
                                HDUtils.GetSceneCullingMaskFromCamera(visibleInRenderRequest.hdCamera.camera),
                                hdParentCamera,
                                visibleInRenderRequest.hdCamera.camera.fieldOfView,
                                visibleInRenderRequest.hdCamera.camera.aspect,
                                ref renderDatas
                            );

                            foreach (var renderData in renderDatas)
                            {
                                visibleInRenderRequest.viewDependentProbesData.Add((renderData, visibleProbe));
                            }

                            ListPool<HDProbe.RenderData>.Release(renderDatas);
                        }
                    }
                    else
                    {
                        // No single parent camera for view dependent probes.
                        hdParentCamera = null;

                        bool visibleInOneViewer = false;
                        for (int i = 0; i < visibilities.Count && !visibleInOneViewer; ++i)
                        {
                            if (visibilities[i].weight > 0f)
                                visibleInOneViewer = true;
                        }
                        if (visibleInOneViewer)
                        {
                            var renderDatas = ListPool<HDProbe.RenderData>.Get();
                            AddHDProbeRenderRequests(visibleProbe, null, visibilities, 0, hdParentCamera, referenceFieldOfView: 90, referenceAspect: 1, ref renderDatas);
                            ListPool<HDProbe.RenderData>.Release(renderDatas);
                        }
                    }
                }
                foreach (var pair in renderRequestIndicesWhereTheProbeIsVisible)
                    ListPool<(int index, float weight)>.Release(pair.Value);
                renderRequestIndicesWhereTheProbeIsVisible.Clear();

                // Local function to share common code between view dependent and view independent requests
                void AddHDProbeRenderRequests(
                    HDProbe visibleProbe,
                    Transform viewerTransform,
                    List<(int index, float weight)> visibilities,
                    ulong overrideSceneCullingMask,
                    HDCamera hdParentCamera,
                    float referenceFieldOfView,
                    float referenceAspect,
                    ref List<HDProbe.RenderData> renderDatas
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
                        cameraSettings, cameraPositionSettings, overrideSceneCullingMask,
                        referenceFieldOfView: referenceFieldOfView,
                        referenceAspect: referenceAspect
                    );

                    var probeFormat = (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionProbeFormat;

                    switch (visibleProbe.type)
                    {
                        case ProbeSettings.ProbeType.ReflectionProbe:
                            int desiredProbeSize = (int)((HDRenderPipeline)RenderPipelineManager.currentPipeline).currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCubemapSize;
                            var desiredProbeFormat = ((HDRenderPipeline)RenderPipelineManager.currentPipeline).currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionProbeFormat;

                            if (visibleProbe.realtimeTextureRTH == null || visibleProbe.realtimeTextureRTH.rt.width != desiredProbeSize ||
                                visibleProbe.realtimeTextureRTH.rt.graphicsFormat != probeFormat)
                            {
                                visibleProbe.SetTexture(ProbeSettings.Mode.Realtime, HDRenderUtilities.CreateReflectionProbeRenderTarget(desiredProbeSize, probeFormat));
                            }
                            break;
                        case ProbeSettings.ProbeType.PlanarProbe:
                            int desiredPlanarProbeSize = (int)visibleProbe.resolution;
                            if (visibleProbe.realtimeTextureRTH == null || visibleProbe.realtimeTextureRTH.rt.width != desiredPlanarProbeSize || visibleProbe.realtimeTextureRTH.rt.graphicsFormat != probeFormat)
                            {
                                visibleProbe.SetTexture(ProbeSettings.Mode.Realtime, HDRenderUtilities.CreatePlanarProbeRenderTarget(desiredPlanarProbeSize, probeFormat));
                            }
                            if (visibleProbe.realtimeDepthTextureRTH == null || visibleProbe.realtimeDepthTextureRTH.rt.width != desiredPlanarProbeSize)
                            {
                                visibleProbe.SetDepthTexture(ProbeSettings.Mode.Realtime, HDRenderUtilities.CreatePlanarProbeDepthRenderTarget(desiredPlanarProbeSize));
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
                        var camera = m_ProbeCameraCache.GetOrCreate((viewerTransform, visibleProbe, j), Time.frameCount, CameraType.Reflection);
                        var additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();

                        var settingsCopy = m_Asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings;
                        settingsCopy.forcedPercentage = 100.0f;
                        settingsCopy.forceResolution = true;
                        DynamicResolutionHandler.UpdateAndUseCamera(camera, settingsCopy);

                        if (additionalCameraData == null)
                            additionalCameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
                        additionalCameraData.hasPersistentHistory = true;

                        // We need to set a targetTexture with the right otherwise when setting pixelRect, it will be rescaled internally to the size of the screen
                        camera.targetTexture = visibleProbe.realtimeTexture;
                        camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                        camera.gameObject.SetActive(false);

                        // Warning: accessing Object.name generate 48B of garbage at each frame here
                        // camera.name = HDUtils.ComputeProbeCameraName(visibleProbe.name, j, viewerTransform?.name);
                        // Non Alloc version of ComputeProbeCameraName but without the viewerTransform name part
                        camera.name = visibleProbe.probeName[j];

                        camera.ApplySettings(cameraSettings[j]);
                        camera.ApplySettings(cameraPositionSettings[j]);
                        camera.cameraType = CameraType.Reflection;
                        camera.pixelRect = new Rect(0, 0, visibleProbe.realtimeTexture.width, visibleProbe.realtimeTexture.height);

                        var _cullingResults = UnsafeGenericPool<HDCullingResults>.Get();
                        _cullingResults.Reset();

                        if (!(TryCalculateFrameParameters(
                            camera,
                            m_XRSystem.emptyPass,
                            out _,
                            out var hdCamera,
                            out var cullingParameters
                        )
                              && TryCull(
                                  camera, hdCamera, renderContext, m_SkyManager, cullingParameters, m_Asset,
                                  ref _cullingResults
                              )
                        ))
                        {
                            // Skip request and free resources
                            UnsafeGenericPool<HDCullingResults>.Release(_cullingResults);
                            continue;
                        }

                        // HACK! We render the probe until we know the ambient probe for the associated sky context is ready.
                        // For one-off rendering the dynamic ambient probe will be set to black until they are not processed, leading to faulty rendering.
                        // So we enqueue another rendering and then we will not set the probe texture until we have rendered with valid ambient probe.
                        if (!m_SkyManager.HasSetValidAmbientProbe(hdCamera))
                        {
                            visibleProbe.ForceRenderingNextUpdate();
                        }

                        hdCamera.SetParentCamera(hdParentCamera); // Used to inherit the properties of the view

                        if (visibleProbe.type == ProbeSettings.ProbeType.PlanarProbe)
                        {
                            //cache the resolved settings. Otherwise if we use the internal probe settings, it will be the wrong resolved result.
                            visibleProbe.ExposureControlEnabled = hdCamera.exposureControlFS;
                            if (visibleProbe.ExposureControlEnabled)
                            {
                                RTHandle exposureTexture = GetExposureTexture(hdParentCamera);
                                hdParentCamera.RequestGpuExposureValue(exposureTexture);
                                visibleProbe.SetProbeExposureValue(hdParentCamera.GpuExposureValue());
                                additionalCameraData.deExposureMultiplier = 1.0f;

                                // If the planar is under exposure control, all the pixels will be de-exposed, for the other skies it is handeled in a shader.
                                // For the clear color, we need to do it manually here.
                                additionalCameraData.backgroundColorHDR = additionalCameraData.backgroundColorHDR * visibleProbe.ProbeExposureValue();
                            }
                            else
                            {
                                //the de-exposure multiplier must be used for anything rendering flatly, for example UI or Unlit.
                                //this will cause them to blow up, but will match the standard nomralized exposure.
                                hdParentCamera.RequestGpuDeExposureValue(m_PostProcessSystem.GetExposureTextureHandle(hdParentCamera.currentExposureTextures.previous));
                                visibleProbe.SetProbeExposureValue(1.0f);
                                additionalCameraData.deExposureMultiplier = 1.0f / hdParentCamera.GpuDeExposureValue();
                            }
                        }

                        HDAdditionalCameraData hdCam;
                        camera.TryGetComponent<HDAdditionalCameraData>(out hdCam);
                        hdCam.flipYMode = visibleProbe.type == ProbeSettings.ProbeType.ReflectionProbe
                            ? HDAdditionalCameraData.FlipYMode.ForceFlipY
                            : HDAdditionalCameraData.FlipYMode.Automatic;

                        if (!visibleProbe.realtimeTexture.IsCreated())
                            visibleProbe.realtimeTexture.Create();

                        var renderData = new HDProbe.RenderData(
                            camera.worldToCameraMatrix,
                            camera.projectionMatrix,
                            camera.transform.position,
                            camera.transform.rotation,
                            cameraSettings[j].frustum.fieldOfView,
                            cameraSettings[j].frustum.aspect
                        );

                        renderDatas.Add(renderData);

                        visibleProbe.SetRenderData(
                            ProbeSettings.Mode.Realtime,
                            renderData
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
                            clearCameraSettings = true,
                            dependsOnRenderRequestIndices = ListPool<int>.Get(),
                            index = renderRequests.Count,
                            cameraSettings = cameraSettings[j],
                            viewDependentProbesData = ListPool<(HDProbe.RenderData, HDProbe)>.Get()
                                // TODO: store DecalCullResult
                        };

                        if (m_SkyManager.HasSetValidAmbientProbe(hdCamera))
                        {
                            // As we render realtime texture on GPU side, we must tag the texture so our texture array cache detect that something have change
                            visibleProbe.realtimeTexture.IncrementUpdateCount();

                            if (cameraSettings.Count > 1)
                            {
                                var face = (CubemapFace)j;
                                request.target = new RenderRequest.Target
                                {
                                    copyToTarget = visibleProbe.realtimeTextureRTH,
                                    face = face
                                };
                            }
                            else
                            {
                                request.target = new RenderRequest.Target
                                {
                                    id = visibleProbe.realtimeTextureRTH,
                                    targetDepth = visibleProbe.realtimeDepthTextureRTH,
                                    face = CubemapFace.Unknown
                                };
                            }
                        }

                        renderRequests.Add(request);


                        foreach (var visibility in visibilities)
                            renderRequests[visibility.index].dependsOnRenderRequestIndices.Add(request.index);
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
                        var probeFormat = (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionProbeFormat;
                        if (m_TemporaryTargetForCubemaps != null)
                        {
                            if (m_TemporaryTargetForCubemaps.width != size.x
                                || m_TemporaryTargetForCubemaps.height != size.y
                                || m_TemporaryTargetForCubemaps.graphicsFormat != probeFormat)
                            {
                                m_TemporaryTargetForCubemaps.Release();
                                m_TemporaryTargetForCubemaps = null;
                            }
                        }
                        if (m_TemporaryTargetForCubemaps == null)
                        {
                            m_TemporaryTargetForCubemaps = new RenderTexture(
                                size.x, size.y, 1, probeFormat
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
                            for (int i = rootRenderRequestIndices.Count - 1; i >= 0; --i)
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

                    using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.HDRenderPipelineAllRenderRequest)))
                    {
                        // Warm up the RTHandle system so that it gets init to the maximum resolution available (avoiding to call multiple resizes
                        // that can lead to high memory spike as the memory release is delayed while the creation is immediate).
                        {
                            Vector2Int maxSize = new Vector2Int(1, 1);

                            for (int i = renderRequestIndicesToRender.Count - 1; i >= 0; --i)
                            {
                                var renderRequestIndex = renderRequestIndicesToRender[i];
                                var renderRequest = renderRequests[renderRequestIndex];
                                var hdCamera = renderRequest.hdCamera;

                                maxSize.x = Math.Max((int)hdCamera.finalViewport.size.x, maxSize.x);
                                maxSize.y = Math.Max((int)hdCamera.finalViewport.size.y, maxSize.y);
                            }

                            // Here we use the non scaled resolution for the RTHandleSystem ref size because we assume that at some point we will need full resolution anyway.
                            // This is necessary because we assume that after post processes, we have the full size render target for debug rendering
                            // The only point of calling this here is to grow the render targets. The call in BeginRender will setup the current RTHandle viewport size.
                            RTHandles.SetReferenceSize(maxSize.x, maxSize.y, m_MSAASamples);
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

                            // The HDProbe store only one RenderData per probe, however RenderData can be view dependent (e.g. planar probes).
                            // To avoid that the render data for the wrong view is used, we previously store a copy of the render data
                            // for each viewer and we are going to set it on the probe right before said viewer is rendered.
                            foreach (var probeDataPair in renderRequest.viewDependentProbesData)
                            {
                                var probe = probeDataPair.Item2;
                                var probeRenderData = probeDataPair.Item1;
                                probe.SetRenderData(ProbeSettings.Mode.Realtime, probeRenderData);
                            }

                            // var aovRequestIndex = 0;
                            foreach (var aovRequest in renderRequest.hdCamera.aovRequests)
                            {
                                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.HDRenderPipelineRenderAOV)))
                                {
                                    cmd.SetInvertCulling(renderRequest.cameraSettings.invertFaceCulling);
                                    ExecuteRenderRequest(renderRequest, renderContext, cmd, aovRequest);
                                    cmd.SetInvertCulling(false);
                                }
                                renderContext.ExecuteCommandBuffer(cmd);
                                renderContext.Submit();
                                cmd.Clear();
                            }

                            using (new ProfilingScope(cmd, renderRequest.hdCamera.profilingSampler))
                            {
                                cmd.SetInvertCulling(renderRequest.cameraSettings.invertFaceCulling);
                                ExecuteRenderRequest(renderRequest, renderContext, cmd, AOVRequestData.defaultAOVRequestDataNonAlloc);
                                cmd.SetInvertCulling(false);
                            }

                            //  EndCameraRendering callback should be executed outside of any profiling scope in case user code submits the renderContext
                            EndCameraRendering(renderContext, renderRequest.hdCamera.camera);

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
                                if (renderRequest.clearCameraSettings)
                                    // release reference because the RenderTexture might be destroyed before the camera
                                    renderRequest.hdCamera.camera.targetTexture = null;

                                ListPool<int>.Release(renderRequest.dependsOnRenderRequestIndices);
                                ListPool<(HDProbe.RenderData, HDProbe)>.Release(renderRequest.viewDependentProbesData);

                                // Culling results can be shared between render requests: clear only when required
                                if (!skipClearCullingResults.Contains(renderRequest.index))
                                {
                                    renderRequest.cullingResults.decalCullResults?.Clear();
                                    UnsafeGenericPool<HDCullingResults>.Release(renderRequest.cullingResults);
                                }
                            }

                            // Render XR mirror view once all render requests have been completed
                            if (i == 0 && renderRequest.hdCamera.camera.cameraType == CameraType.Game && renderRequest.hdCamera.camera.targetTexture == null)
                            {
                                if (HDUtils.TryGetAdditionalCameraDataOrDefault(renderRequest.hdCamera.camera).xrRendering)
                                {
                                    m_XRSystem.RenderMirrorView(cmd);
                                }
                            }

                            // Now that all cameras have been rendered, let's propagate the data required for screen space shadows
                            PropagateScreenSpaceShadowData();

                            renderContext.ExecuteCommandBuffer(cmd);
                            CommandBufferPool.Release(cmd);
                            renderContext.Submit();
                        }
                    }
                }
            }

            DynamicResolutionHandler.ClearSelectedCamera();

            m_RenderGraph.EndFrame();
            m_XRSystem.ReleaseFrame();

#if UNITY_2021_1_OR_NEWER
            EndContextRendering(renderContext, cameras);
#else
            EndFrameRendering(renderContext, cameras);
#endif
        }

        void PropagateScreenSpaceShadowData()
        {
            // For every unique light that has been registered, update the previous transform
            foreach (HDAdditionalLightData lightData in m_ScreenSpaceShadowsUnion)
            {
                lightData.previousTransform = lightData.transform.localToWorldMatrix;
            }
        }

        void ExecuteRenderRequest(
            RenderRequest renderRequest,
            ScriptableRenderContext renderContext,
            CommandBuffer cmd,
            AOVRequestData aovRequest
        )
        {
            DynamicResolutionHandler.UpdateAndUseCamera(renderRequest.hdCamera.camera);
            InitializeGlobalResources(renderContext);

            var hdCamera = renderRequest.hdCamera;
            var camera = hdCamera.camera;
            var cullingResults = renderRequest.cullingResults.cullingResults;
            var customPassCullingResults = renderRequest.cullingResults.customPassCullingResults ?? cullingResults;
            var hdProbeCullingResults = renderRequest.cullingResults.hdProbeCullingResults;
            var decalCullingResults = renderRequest.cullingResults.decalCullResults;
            var target = renderRequest.target;

            // Updates RTHandle
            hdCamera.BeginRender(cmd);
            m_CurrentHDCamera = hdCamera;

            if (m_RayTracingSupported)
            {
                // This call need to happen once per camera
                // TODO: This can be wasteful for "compatible" cameras.
                // We need to determine the minimum set of feature used by all the camera and build the minimum number of acceleration structures.
                BuildRayTracingAccelerationStructure(hdCamera);
                CullForRayTracing(cmd, hdCamera);
            }

#if ENABLE_VIRTUALTEXTURES
            m_VtBufferManager.BeginRender(hdCamera);
#endif

            using (ListPool<RTHandle>.Get(out var aovBuffers))
            using (ListPool<RTHandle>.Get(out var aovCustomPassBuffers))
            {
                aovRequest.AllocateTargetTexturesIfRequired(ref aovBuffers, ref aovCustomPassBuffers);

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
                    m_MSAASamples = (m_DebugDisplaySettings.data.msaaSamples != MSAASamples.None) ?
                        m_DebugDisplaySettings.data.msaaSamples :
                        m_Asset.currentPlatformRenderPipelineSettings.msaaSampleCount;

                    m_DebugDisplaySettings.UpdateCameraFreezeOptions();

                    m_CurrentDebugDisplaySettings = m_DebugDisplaySettings;
                }

                aovRequest.SetupDebugData(ref m_CurrentDebugDisplaySettings);

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DBufferPrepareDrawData)))
                    {
                        // TODO: update singleton with DecalCullResults
                        DecalSystem.instance.CurrentCamera = hdCamera.camera; // Singletons are extremely dangerous...
                        DecalSystem.instance.LoadCullResults(decalCullingResults);
                        DecalSystem.instance.UpdateCachedMaterialData(); // textures, alpha or fade distances could've changed
                        DecalSystem.instance.CreateDrawData();          // prepare data is separate from draw
                        DecalSystem.instance.UpdateTextureAtlas(cmd);   // as this is only used for transparent pass, would've been nice not to have to do this if no transparent renderers are visible, needs to happen after CreateDrawData
                    }
                }

                using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.CustomPassVolumeUpdate)))
                {
                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPass))
                        CustomPassVolume.Update(hdCamera);
                }

                // Do anything we need to do upon a new frame.
                // The NewFrame must be after the VolumeManager update and before Resize because it uses properties set in NewFrame
                LightLoopNewFrame(cmd, hdCamera);

                // Apparently scissor states can leak from editor code. As it is not used currently in HDRP (apart from VR). We disable scissor at the beginning of the frame.
                cmd.DisableScissorRect();

                Resize(hdCamera);
                m_PostProcessSystem.BeginFrame(cmd, hdCamera, this);

                ApplyDebugDisplaySettings(hdCamera, cmd);

                if (DebugManager.instance.displayRuntimeUI
#if UNITY_EDITOR
                    || DebugManager.instance.displayEditorUI
#endif
                )
                    m_CurrentDebugDisplaySettings.UpdateAveragedProfilerTimings();

                SetupCameraProperties(hdCamera, renderContext, cmd);

                // TODO: Find a correct place to bind these material textures
                // We have to bind the material specific global parameters in this mode
                foreach (var material in m_MaterialList)
                    material.Bind(cmd);

                // Frustum cull density volumes on the CPU. Can be performed as soon as the camera is set up.
                DensityVolumeList densityVolumes = PrepareVisibleDensityVolumeList(hdCamera, cmd);

                // Frustum cull probe volumes on the CPU. Can be performed as soon as the camera is set up.
                ProbeVolumeList probeVolumes = PrepareVisibleProbeVolumeList(renderContext, hdCamera, cmd);

                // Note: Legacy Unity behave like this for ShadowMask
                // When you select ShadowMask in Lighting panel it recompile shaders on the fly with the SHADOW_MASK keyword.
                // However there is no C# function that we can query to know what mode have been select in Lighting Panel and it will be wrong anyway. Lighting Panel setup what will be the next bake mode. But until light is bake, it is wrong.
                // Currently to know if you need shadow mask you need to go through all visible lights (of CullResult), check the LightBakingOutput struct and look at lightmapBakeType/mixedLightingMode. If one light have shadow mask bake mode, then you need shadow mask features (i.e extra Gbuffer).
                // It mean that when we build a standalone player, if we detect a light with bake shadow mask, we generate all shader variant (with and without shadow mask) and at runtime, when a bake shadow mask light is visible, we dynamically allocate an extra GBuffer and switch the shader.
                // So the first thing to do is to go through all the light: PrepareLightsForGPU
                bool enableBakeShadowMask = PrepareLightsForGPU(cmd, hdCamera, cullingResults, hdProbeCullingResults, densityVolumes, probeVolumes, m_CurrentDebugDisplaySettings, aovRequest);

                UpdateGlobalConstantBuffers(hdCamera, cmd);

                // Do the same for ray tracing if allowed
                if (m_RayTracingSupported)
                {
                    BuildRayTracingLightData(cmd, hdCamera, m_CurrentDebugDisplaySettings);
                }

                // Configure all the keywords
                ConfigureKeywords(enableBakeShadowMask, hdCamera, cmd);

                // Caution: We require sun light here as some skies use the sun light to render, it means that UpdateSkyEnvironment must be called after PrepareLightsForGPU.
                // TODO: Try to arrange code so we can trigger this call earlier and use async compute here to run sky convolution during other passes (once we move convolution shader to compute).
                if (!m_CurrentDebugDisplaySettings.IsMatcapViewEnabled(hdCamera))
                    UpdateSkyEnvironment(hdCamera, renderContext, cmd);
                else
                    cmd.SetGlobalTexture(HDShaderIDs._SkyTexture, CoreUtils.magentaCubeTextureArray);

                VFXManager.ProcessCameraCommand(camera, cmd);

                if (GL.wireframe)
                {
                    RenderWireFrame(cullingResults, hdCamera, target.id, renderContext, cmd);
                    return;
                }

                try
                {
                    ExecuteWithRenderGraph(renderRequest, aovRequest, aovBuffers, aovCustomPassBuffers, renderContext, cmd);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while building Render Graph.");
                    Debug.LogException(e);
                }
            } // using (ListPool<RTHandle>.Get(out var aovCustomPassBuffers))

            // This is required so that all commands up to here are executed before EndCameraRendering is called for the user.
            // Otherwise command would not be rendered in order.
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            m_CurrentHDCamera = null;
        }

        struct BlitFinalCameraTextureParameters
        {
            public bool                     flip;
            public int                      srcTexArraySlice;
            public int                      dstTexArraySlice;
            public Rect                     viewport;
            public Material                 blitMaterial;
        }

        internal RTHandle GetExposureTexture(HDCamera hdCamera) =>
            m_PostProcessSystem.GetExposureTexture(hdCamera);

        BlitFinalCameraTextureParameters PrepareFinalBlitParameters(HDCamera hdCamera, int viewIndex)
        {
            var parameters = new BlitFinalCameraTextureParameters();

            if (hdCamera.xr.enabled)
            {
                parameters.viewport = hdCamera.xr.GetViewport(viewIndex);
                parameters.srcTexArraySlice = viewIndex;
                parameters.dstTexArraySlice = hdCamera.xr.GetTextureArraySlice(viewIndex);
            }
            else
            {
                parameters.viewport = hdCamera.finalViewport;
                parameters.srcTexArraySlice = -1;
                parameters.dstTexArraySlice = -1;
            }

            parameters.flip = hdCamera.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY || hdCamera.isMainGameView;
            parameters.blitMaterial = HDUtils.GetBlitMaterial(TextureXR.useTexArray ? TextureDimension.Tex2DArray : TextureDimension.Tex2D, singleSlice: parameters.srcTexArraySlice >= 0);

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
            propertyBlock.SetInt(HDShaderIDs._BlitTexArraySlice, parameters.srcTexArraySlice);
            HDUtils.DrawFullScreen(cmd, parameters.viewport, parameters.blitMaterial, destination, propertyBlock, 0, parameters.dstTexArraySlice);
        }

        void SetupCameraProperties(HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            // The next 2 functions are required to flush the command buffer before calling functions directly on the render context.
            // This way, the commands will execute in the order specified by the C# code.
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            renderContext.SetupCameraProperties(hdCamera.camera, hdCamera.xr.enabled);
        }

        void InitializeGlobalResources(ScriptableRenderContext renderContext)
        {
            // Global resources initialization
            var cmd = CommandBufferPool.Get("");
            // Init material if needed
            for (int bsdfIdx = 0; bsdfIdx < m_IBLFilterArray.Length; ++bsdfIdx)
            {
                if (!m_IBLFilterArray[bsdfIdx].IsInitialized())
                    m_IBLFilterArray[bsdfIdx].Initialize(cmd);
            }

            foreach (var material in m_MaterialList)
                material.RenderInit(cmd);

            TextureXR.Initialize(cmd, defaultResources.shaders.clearUIntTextureCS);

            renderContext.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
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
            additionalCameraData = HDUtils.TryGetAdditionalCameraDataOrDefault(camera);
            hdCamera = default;
            cullingParams = default;

            FrameSettings currentFrameSettings = new FrameSettings();
            // Compute the FrameSettings actually used to draw the frame
            // FrameSettingsHistory do the same while keeping all step of FrameSettings aggregation in memory for DebugMenu
            if (m_FrameSettingsHistoryEnabled && camera.cameraType != CameraType.Preview && camera.cameraType != CameraType.Reflection)
                FrameSettingsHistory.AggregateFrameSettings(ref currentFrameSettings, camera, additionalCameraData, m_Asset, m_DefaultAsset);
            else
                FrameSettings.AggregateFrameSettings(ref currentFrameSettings, camera, additionalCameraData, m_Asset, m_DefaultAsset);

            // With the Frame Settings now properly set up, we can resolve the sample budget.
            currentFrameSettings.sssResolvedSampleBudget = currentFrameSettings.GetResolvedSssSampleBudget(m_Asset);

            // Specific pass to simply display the content of the camera buffer if users have fill it themselves (like video player)
            if (additionalCameraData.fullscreenPassthrough)
                return false;

            // Retrieve debug display settings to init FrameSettings, unless we are a reflection and in this case we don't have debug settings apply.
            DebugDisplaySettings debugDisplaySettings = (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview) ? s_NeutralDebugDisplaySettings : m_DebugDisplaySettings;

            // Getting the background color from preferences to add to the preview camera
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.Preview)
            {
                camera.backgroundColor = CoreRenderPipelinePreferences.previewBackgroundColor;
            }
#endif

            // Disable post process if we enable debug mode or if the post process layer is disabled
            if (debugDisplaySettings.IsDebugDisplayEnabled())
            {
                if (debugDisplaySettings.IsDebugDisplayRemovePostprocess())
                {
                    currentFrameSettings.SetEnabled(FrameSettingsField.Postprocess, false);
                    currentFrameSettings.SetEnabled(FrameSettingsField.CustomPass, false);
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

            if (CoreUtils.IsSceneLightingDisabled(camera))
            {
                currentFrameSettings.SetEnabled(FrameSettingsField.ExposureControl, false);
            }

            // Disable object-motion vectors in everything but the game view
            if (camera.cameraType != CameraType.Game)
            {
                currentFrameSettings.SetEnabled(FrameSettingsField.ObjectMotionVectors, false);
                currentFrameSettings.SetEnabled(FrameSettingsField.TransparentsWriteMotionVector, false);
            }

            hdCamera = HDCamera.GetOrCreate(camera, xrPass.multipassId);

            // From this point, we should only use frame settings from the camera
            hdCamera.Update(currentFrameSettings, this, m_MSAASamples, xrPass);
            ResizeVolumetricLightingBuffers(hdCamera); // Safe to update the Volumetric Lighting System now

            // Custom Render requires a proper HDCamera, so we return after the HDCamera was setup
            if (additionalCameraData != null && additionalCameraData.hasCustomRender)
                return false;

            if (hdCamera.xr.enabled)
            {
                cullingParams = hdCamera.xr.cullingParams;

                // Sync the FOV on the camera to match the projection from the XR device in order to cull shadows accurately
                if (!camera.usePhysicalProperties)
                    camera.fieldOfView = Mathf.Rad2Deg * Mathf.Atan(1.0f / cullingParams.stereoProjectionMatrix.m11) * 2.0f;
            }
            else
            {
                if (!camera.TryGetCullingParameters(camera.stereoEnabled, out cullingParams))
                    return false;
            }

            if (m_DebugDisplaySettings.IsCameraFreezeEnabled())
            {
                if (m_DebugDisplaySettings.IsCameraFrozen(camera))
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

            LightLoopUpdateCullingParameters(ref cullingParams, hdCamera);

            // If we don't use environment light (like when rendering reflection probes)
            //   we don't have to cull them.
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.ReflectionProbe))
                cullingParams.cullingOptions |= CullingOptions.NeedsReflectionProbes;
            else
                cullingParams.cullingOptions &= ~CullingOptions.NeedsReflectionProbes;

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ShadowMaps) || currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxShadowRequests == 0)
                cullingParams.cullingOptions &= ~CullingOptions.ShadowCasters;

            return true;
        }

        static void OverrideCullingForRayTracing(HDCamera hdCamera, Camera camera, ref ScriptableCullingParameters cullingParams)
        {
            var rayTracingSetting = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            if (rayTracingSetting.extendShadowCulling.value || rayTracingSetting.extendCameraCulling.value)
            {
                // We are in a static function, so we can't really save this allocation easily.
                Plane plane = new Plane();

                // Camera properties is a copy, need to grab it first
                CameraProperties cameraProperties = cullingParams.cameraProperties;

                // Override all the planes
                plane.SetNormalAndPosition(camera.transform.right, camera.transform.position - camera.transform.right * camera.farClipPlane);
                if (rayTracingSetting.extendShadowCulling.value)
                    cameraProperties.SetShadowCullingPlane(0, plane);
                if (rayTracingSetting.extendCameraCulling.value)
                    cullingParams.SetCullingPlane(0, plane);
                plane.SetNormalAndPosition(-camera.transform.right, camera.transform.position + camera.transform.right * camera.farClipPlane);
                if (rayTracingSetting.extendShadowCulling.value)
                    cameraProperties.SetShadowCullingPlane(1, plane);
                if (rayTracingSetting.extendCameraCulling.value)
                    cullingParams.SetCullingPlane(1, plane);
                plane.SetNormalAndPosition(camera.transform.up, camera.transform.position - camera.transform.up * camera.farClipPlane);
                if (rayTracingSetting.extendShadowCulling.value)
                    cameraProperties.SetShadowCullingPlane(2, plane);
                if (rayTracingSetting.extendCameraCulling.value)
                    cullingParams.SetCullingPlane(2, plane);
                plane.SetNormalAndPosition(-camera.transform.up, camera.transform.position + camera.transform.up * camera.farClipPlane);
                if (rayTracingSetting.extendShadowCulling.value)
                    cameraProperties.SetShadowCullingPlane(3, plane);
                if (rayTracingSetting.extendCameraCulling.value)
                    cullingParams.SetCullingPlane(3, plane);
                plane.SetNormalAndPosition(camera.transform.forward, camera.transform.position - camera.transform.forward * camera.farClipPlane);
                if (rayTracingSetting.extendShadowCulling.value)
                    cameraProperties.SetShadowCullingPlane(4, plane);
                if (rayTracingSetting.extendCameraCulling.value)
                    cullingParams.SetCullingPlane(4, plane);
                // The 5th planes doesn't need to be overriden, but just in case.
                plane.SetNormalAndPosition(-camera.transform.forward, camera.transform.position + camera.transform.forward * camera.farClipPlane);
                if (rayTracingSetting.extendShadowCulling.value)
                    cameraProperties.SetShadowCullingPlane(5, plane);
                if (rayTracingSetting.extendCameraCulling.value)
                    cullingParams.SetCullingPlane(5, plane);

                // Propagate the new planes
                cullingParams.cameraProperties = cameraProperties;
            }
        }

        static bool TryCull(
            Camera camera,
            HDCamera hdCamera,
            ScriptableRenderContext renderContext,
            SkyManager skyManager,
            ScriptableCullingParameters cullingParams,
            HDRenderPipelineAsset hdrp,
            ref HDCullingResults cullingResults
        )
        {
            if (camera.cameraType == CameraType.Reflection)
            {
#if UNITY_2020_2_OR_NEWER
                ScriptableRenderContext.EmitGeometryForCamera(camera);
#endif
            }
#if UNITY_EDITOR
            // emit scene view UI
            else if (camera.cameraType == CameraType.SceneView)
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
#if UNITY_2021_1_OR_NEWER
                // Modifying the variables this way does not set the dirty flag, which avoids repainting all views
                QualitySettings.SetLODSettings(hdCamera.frameSettings.GetResolvedLODBias(hdrp), hdCamera.frameSettings.GetResolvedMaximumLODLevel(hdrp), false);
#else
                QualitySettings.lodBias = hdCamera.frameSettings.GetResolvedLODBias(hdrp);
                QualitySettings.maximumLODLevel = hdCamera.frameSettings.GetResolvedMaximumLODLevel(hdrp);
#endif

                // This needs to be called before culling, otherwise in the case where users generate intermediate renderers, it can provoke crashes.
                BeginCameraRendering(renderContext, camera);

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
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.PlanarProbe))
                    hdProbeCullState = HDProbeSystem.PrepareCull(camera);

                // We need to set the ambient probe here because it's passed down to objects during the culling process.
                skyManager.UpdateCurrentSkySettings(hdCamera);
                skyManager.SetupAmbientProbe(hdCamera);

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                {
                    OverrideCullingForRayTracing(hdCamera, camera, ref cullingParams);
                }

                using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.CullResultsCull)))
                {
                    cullingResults.cullingResults = renderContext.Cull(ref cullingParams);
                }

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPass))
                {
                    using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.CustomPassCullResultsCull)))
                    {
                        cullingResults.customPassCullingResults = CustomPassVolume.Cull(renderContext, hdCamera);
                    }
                }

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.PlanarProbe) && hdProbeCullState.cullingGroup != null)
                    HDProbeSystem.QueryCullResults(hdProbeCullState, ref cullingResults.hdProbeCullingResults);
                else
                    cullingResults.hdProbeCullingResults = default;

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
                {
                    using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.DBufferPrepareDrawData)))
                    {
                        DecalSystem.instance.EndCull(decalCullRequest, cullingResults.decalCullResults);
                    }
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
#if UNITY_2021_1_OR_NEWER
                QualitySettings.SetLODSettings(initialLODBias, initialMaximumLODLevel, false);
#else
                QualitySettings.lodBias = initialLODBias;
                QualitySettings.maximumLODLevel = initialMaximumLODLevel;
#endif
            }
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

        static RendererListDesc CreateTransparentRendererListDesc(
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

        static RendererListDesc CreateTransparentRendererListDesc(
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

        static void DrawOpaqueRendererList(in ScriptableRenderContext renderContext, CommandBuffer cmd, in FrameSettings frameSettings, RendererList rendererList)
        {
            if (!frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                return;

            CoreUtils.DrawRendererList(renderContext, cmd, rendererList);
        }

        static void DrawTransparentRendererList(in ScriptableRenderContext renderContext, CommandBuffer cmd, in FrameSettings frameSettings, RendererList rendererList)
        {
            if (!frameSettings.IsEnabled(FrameSettingsField.TransparentObjects))
                return;

            CoreUtils.DrawRendererList(renderContext, cmd, rendererList);
        }

        struct DepthPrepassParameters
        {
            public string           passName;
            public HDProfileId      profilingId;
            public RendererListDesc depthDeferredRendererListDesc;
            public RendererListDesc depthForwardRendererListDesc;
            public bool             hasDepthDeferredPass;
            public bool             shouldRenderMotionVectorAfterGBuffer;
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

            // Additional guideline for decal
            // Decal are in their own render queue to allow to force them to render in depth buffer.
            // Thus it is not required to do a full depth prepass when decal are enabled
            // Mean when decal are enabled and we haven't request a full prepass in deferred, we can't guarantee that the prepass will be complete

            // With all this variant we have the following scenario of render target binding
            // decalsEnabled
            //     LitShaderMode.Forward
            //         Range Opaque both deferred and forward - depth + optional msaa + normal
            //         Range opaqueDecal for both deferred and forward - depth + optional msaa + normal + decal
            //         Range opaqueAlphaTest for both deferred and forward - depth + optional msaa + normal
            //         Range opaqueDecalAlphaTes for both deferred and forward - depth + optional msaa + normal + decal
            //    LitShaderMode.Deferred
            //         fullDeferredPrepass
            //             Range Opaque for deferred - depth
            //             Range opaqueDecal for deferred - depth + decal
            //             Range opaqueAlphaTest for deferred - depth
            //             Range opaqueDecalAlphaTes for deferred - depth + decal

            //             Range Opaque for forward - depth + normal
            //             Range opaqueDecal for forward - depth + normal + decal
            //             Range opaqueAlphaTest for forward - depth + normal
            //             Range opaqueDecalAlphaTes for forward - depth + normal + decal
            //         !fullDeferredPrepass
            //             Range opaqueDecal for deferred - depth + decal
            //             Range opaqueAlphaTest for deferred - depth
            //             Range opaqueDecalAlphaTes for deferred - depth + decal

            //             Range Opaque for forward - depth + normal
            //             Range opaqueDecal for forward - depth + normal + decal
            //             Range opaqueAlphaTest for forward - depth + normal
            //             Range opaqueDecalAlphaTesT for forward - depth + normal + decal
            // !decalsEnabled
            //     LitShaderMode.Forward
            //         Range Opaque..OpaqueDecalAlphaTest for deferred and forward - depth + optional msaa + normal
            //     LitShaderMode.Deferred
            //         fullDeferredPrepass
            //             Range Opaque..OpaqueDecalAlphaTest for deferred - depth

            //             Range Opaque..OpaqueDecalAlphaTest for forward - depth + normal
            //         !fullDeferredPrepass
            //             Range OpaqueAlphaTest..OpaqueDecalAlphaTest for deferred - depth

            //             Range Opaque..OpaqueDecalAlphaTest for forward - depth + normal

            var result = new DepthPrepassParameters();

            bool decalsEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);
            bool fullDeferredPrepass = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering);
            // To avoid rendering objects twice (once in the depth pre-pass and once in the motion vector pass when the motion vector pass is enabled) we exclude the objects that have motion vectors.
            bool objectMotionEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ObjectMotionVectors);

            result.shouldRenderMotionVectorAfterGBuffer = (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred) && !fullDeferredPrepass;
            result.hasDepthDeferredPass = false;
            result.profilingId = HDProfileId.DepthPrepass;

            switch (hdCamera.frameSettings.litShaderMode)
            {
                case LitShaderMode.Forward:
                    result.passName = "Full Depth Prepass (Forward)";

                    RenderStateBlock? stateBlock = null;
                    if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.AlphaToMask))
                        stateBlock = m_AlphaToMaskBlock;

                    result.depthForwardRendererListDesc = CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_DepthOnlyAndDepthForwardOnlyPassNames, stateBlock: stateBlock, excludeObjectMotionVectors: objectMotionEnabled);
                    break;

                case LitShaderMode.Deferred:
                    result.hasDepthDeferredPass = true;
                    result.passName = fullDeferredPrepass ? "Full Depth Prepass (Deferred)" :
                        (decalsEnabled ? "Partial Depth Prepass (Deferred - Decal + AlphaTest)" : "Partial Depth Prepass (Deferred - AlphaTest)");

                    bool excludeMotion = fullDeferredPrepass ? objectMotionEnabled : false;

                    // First deferred materials. Alpha tested object have always a prepass even if enableDepthPrepassWithDeferredRendering is disabled
                    result.depthDeferredRendererListDesc = CreateOpaqueRendererListDesc(
                        cull, hdCamera.camera, m_DepthOnlyPassNames,
                        renderQueueRange: fullDeferredPrepass ? HDRenderQueue.k_RenderQueue_AllOpaque :
                        (decalsEnabled ? HDRenderQueue.k_RenderQueue_OpaqueDecalAndAlphaTest : HDRenderQueue.k_RenderQueue_OpaqueAlphaTest),
                        stateBlock: m_AlphaToMaskBlock,
                        excludeObjectMotionVectors: excludeMotion);

                    // Then forward only material that output normal buffer
                    result.depthForwardRendererListDesc = CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_DepthForwardOnlyPassNames, stateBlock: m_AlphaToMaskBlock, excludeObjectMotionVectors: excludeMotion);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown ShaderLitMode");
            }

            return result;
        }

        static void RenderDepthPrepass(ScriptableRenderContext     renderContext,
            CommandBuffer               cmd,
            FrameSettings               frameSettings,
            RenderTargetIdentifier[]    deferredMrt,
            RenderTargetIdentifier[]    forwardMrt,
            RTHandle                    depthBuffer,
            in RendererList             depthDeferredRendererListDesc,
            in RendererList             depthForwardRendererListDesc,
            bool                        hasDepthDeferredPass
        )
        {
            if (hasDepthDeferredPass)
            {
                if (deferredMrt == null)
                    CoreUtils.SetRenderTarget(cmd, depthBuffer);
                else
                    CoreUtils.SetRenderTarget(cmd, deferredMrt, depthBuffer);

                DrawOpaqueRendererList(renderContext, cmd, frameSettings, depthDeferredRendererListDesc);
            }

            CoreUtils.SetRenderTarget(cmd, forwardMrt, depthBuffer);
            DrawOpaqueRendererList(renderContext, cmd, frameSettings, depthForwardRendererListDesc);
        }

        struct DBufferNormalPatchParameters
        {
            public Material decalNormalBufferMaterial;
            public int dBufferCount;
            public int stencilRef;
            public int stencilMask;
        }

        DBufferNormalPatchParameters PrepareDBufferNormalPatchParameters(HDCamera hdCamera)
        {
            var parameters = new DBufferNormalPatchParameters();
            parameters.dBufferCount = m_Asset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask ? 4 : 3;
            parameters.decalNormalBufferMaterial = m_DecalNormalBufferMaterial;
            switch (hdCamera.frameSettings.litShaderMode)
            {
                case LitShaderMode.Forward:  // in forward rendering all pixels that decals wrote into have to be composited
                    parameters.stencilMask = (int)StencilUsage.Decals;
                    parameters.stencilRef = (int)StencilUsage.Decals;
                    break;
                case LitShaderMode.Deferred: // in deferred rendering only pixels affected by both forward materials and decals need to be composited
                    parameters.stencilMask = (int)StencilUsage.Decals | (int)StencilUsage.RequiresDeferredLighting;
                    parameters.stencilRef = (int)StencilUsage.Decals;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown ShaderLitMode");
            }

            return parameters;
        }

        static void DecalNormalPatch(DBufferNormalPatchParameters    parameters,
            RTHandle[]                      dBuffer,
            RTHandle                        depthStencilBuffer,
            RTHandle                        normalBuffer,
            CommandBuffer                   cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DBufferNormal)))
            {
                parameters.decalNormalBufferMaterial.SetInt(HDShaderIDs._DecalNormalBufferStencilReadMask, parameters.stencilMask);
                parameters.decalNormalBufferMaterial.SetInt(HDShaderIDs._DecalNormalBufferStencilRef, parameters.stencilRef);
                for (int i = 0; i < parameters.dBufferCount; ++i)
                    parameters.decalNormalBufferMaterial.SetTexture(HDShaderIDs._DBufferTexture[i], dBuffer[i]);

                CoreUtils.SetRenderTarget(cmd, depthStencilBuffer);
                cmd.SetRandomWriteTarget(1, normalBuffer);
                cmd.DrawProcedural(Matrix4x4.identity, parameters.decalNormalBufferMaterial, 0, MeshTopology.Triangles, 3, 1);
                cmd.ClearRandomWriteTargets();
            }
        }

        RendererListDesc PrepareMeshDecalsRendererList(CullingResults cullingResults, HDCamera hdCamera, bool use4RTs)
        {
            var desc = new RendererListDesc(m_MeshDecalsPassNames, cullingResults, hdCamera.camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = HDRenderQueue.k_RenderQueue_AllOpaque
            };

            return desc;
        }

        void UpdateShaderVariablesGlobalDecal(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
            {
                cb._EnableDecals  = 1;
                cb._DecalAtlasResolution = new Vector2(HDUtils.hdrpSettings.decalSettings.atlasWidth, HDUtils.hdrpSettings.decalSettings.atlasHeight);
            }
            else
            {
                cb._EnableDecals = 0;
            }
        }

        static RenderTargetIdentifier[] m_Dbuffer3RtIds = new RenderTargetIdentifier[3];

        struct RenderDBufferParameters
        {
            public bool use4RTs;
            public bool useDecalLayers;
        }

        RenderDBufferParameters PrepareRenderDBufferParameters(HDCamera hdCamera)
        {
            var parameters = new RenderDBufferParameters();
            parameters.use4RTs = m_Asset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;
            parameters.useDecalLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers);
            return parameters;
        }

        static void RenderDBuffer(in RenderDBufferParameters  parameters,
            RenderTargetIdentifier[]    mrt,
            RTHandle[]                  rtHandles,
            RTHandle                    depthStencilBuffer,
            RTHandle                    depthTexture,
            RendererList                meshDecalsRendererList,
            RTHandle                    decalPrepassBuffer,
            ScriptableRenderContext     renderContext,
            CommandBuffer               cmd)
        {
            // for alpha compositing, color is cleared to 0, alpha to 1
            // https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html

            // this clears the targets
            // TODO: Once we move to render graph, move this to render targets initialization parameters and remove rtHandles parameters
            Color clearColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            Color clearColorNormal = new Color(0.5f, 0.5f, 0.5f, 1.0f); // for normals 0.5 is neutral
            Color clearColorAOSBlend = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            CoreUtils.SetRenderTarget(cmd, rtHandles[0], ClearFlag.Color, clearColor);
            CoreUtils.SetRenderTarget(cmd, rtHandles[1], ClearFlag.Color, clearColorNormal);
            CoreUtils.SetRenderTarget(cmd, rtHandles[2], ClearFlag.Color, clearColor);

            if (parameters.use4RTs)
            {
                CoreUtils.SetRenderTarget(cmd, rtHandles[3], ClearFlag.Color, clearColorAOSBlend);
                // this actually sets the MRTs and HTile RWTexture, this is done separately because we do not have an api to clear MRTs to different colors
                CoreUtils.SetRenderTarget(cmd, mrt, depthStencilBuffer); // do not clear anymore
            }
            else
            {
                for (int rtindex = 0; rtindex < 3; rtindex++)
                {
                    m_Dbuffer3RtIds[rtindex] = mrt[rtindex];
                }
                // this actually sets the MRTs and HTile RWTexture, this is done separately because we do not have an api to clear MRTs to different colors
                CoreUtils.SetRenderTarget(cmd, m_Dbuffer3RtIds, depthStencilBuffer); // do not clear anymore
            }

            if (parameters.useDecalLayers)
                cmd.SetGlobalTexture(HDShaderIDs._DecalPrepassTexture, decalPrepassBuffer);
            else
                cmd.SetGlobalTexture(HDShaderIDs._DecalPrepassTexture, TextureXR.GetBlackTexture());

            cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, depthTexture);

            CoreUtils.DrawRendererList(renderContext, cmd, meshDecalsRendererList);
            DecalSystem.instance.RenderIntoDBuffer(cmd);

            cmd.ClearRandomWriteTargets();
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

        void RenderWireFrame(CullingResults cull, HDCamera hdCamera, RenderTargetIdentifier backbuffer, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderWireFrame)))
            {
                CoreUtils.SetRenderTarget(cmd, backbuffer, ClearFlag.Color, GetColorBufferClearColor(hdCamera));

                var rendererListOpaque = RendererList.Create(CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_AllForwardOpaquePassNames));
                DrawOpaqueRendererList(renderContext, cmd, hdCamera.frameSettings, rendererListOpaque);

                // Render forward transparent
                var rendererListTransparent = RendererList.Create(CreateTransparentRendererListDesc(cull, hdCamera.camera, m_AllTransparentPassNames));
                DrawTransparentRendererList(renderContext, cmd, hdCamera.frameSettings, rendererListTransparent);
            }
        }

        struct TransparencyOverdrawParameters
        {
            public ShaderVariablesDebugDisplay constantBuffer;
            public RendererListDesc transparencyRL;
            public RendererListDesc transparencyAfterPostRL;
            public RendererListDesc transparencyLowResRL;
            public FrameSettings frameSettings;
        }

        TransparencyOverdrawParameters PrepareTransparencyOverdrawParameters(HDCamera hdCamera, CullingResults cull)
        {
            var parameters = new TransparencyOverdrawParameters();

            var passNames = m_Asset.currentPlatformRenderPipelineSettings.supportTransparentBackface ? m_AllTransparentPassNames : m_TransparentNoBackfaceNames;
            var stateBlock = new RenderStateBlock
            {
                mask = RenderStateMask.Blend,
                blendState = new BlendState
                {
                    blendState0 = new RenderTargetBlendState
                    {
                        destinationColorBlendMode = BlendMode.One,
                        sourceColorBlendMode = BlendMode.One,
                        destinationAlphaBlendMode = BlendMode.One,
                        sourceAlphaBlendMode = BlendMode.One,
                        colorBlendOperation = BlendOp.Add,
                        alphaBlendOperation = BlendOp.Add,
                        writeMask = ColorWriteMask.All
                    }
                }
            };

            parameters.constantBuffer = m_ShaderVariablesDebugDisplayCB;
            parameters.transparencyRL = CreateTransparentRendererListDesc(cull, hdCamera.camera, passNames, stateBlock: stateBlock);
            parameters.transparencyAfterPostRL = CreateTransparentRendererListDesc(cull, hdCamera.camera, passNames, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent, stateBlock: stateBlock);
            parameters.transparencyLowResRL = CreateTransparentRendererListDesc(cull, hdCamera.camera, passNames, renderQueueRange: HDRenderQueue.k_RenderQueue_LowTransparent, stateBlock: stateBlock);
            parameters.frameSettings = hdCamera.frameSettings;

            return parameters;
        }

        static void RenderTransparencyOverdraw(TransparencyOverdrawParameters  parameters,
            RTHandle                        colorBuffer,
            RTHandle                        depthBuffer,
            in RendererList                 transparencyRL,
            in RendererList                 transparencyAfterPostRL,
            in RendererList                 transparencyLowResRL,
            ScriptableRenderContext         renderContext,
            CommandBuffer                   cmd)
        {
            CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlag: ClearFlag.Color, clearColor: Color.black);

            // High res transparent objects, drawing in m_DebugFullScreenTempBuffer
            parameters.constantBuffer._DebugTransparencyOverdrawWeight = 1.0f;
            ConstantBuffer.PushGlobal(cmd, parameters.constantBuffer, HDShaderIDs._ShaderVariablesDebugDisplay);

            DrawTransparentRendererList(renderContext, cmd, parameters.frameSettings, transparencyRL);
            DrawTransparentRendererList(renderContext, cmd, parameters.frameSettings, transparencyAfterPostRL);

            // Low res transparent objects, copying result m_DebugTranparencyLowRes
            parameters.constantBuffer._DebugTransparencyOverdrawWeight = 0.25f;
            ConstantBuffer.PushGlobal(cmd, parameters.constantBuffer, HDShaderIDs._ShaderVariablesDebugDisplay);
            DrawTransparentRendererList(renderContext, cmd, parameters.frameSettings, transparencyLowResRL);

            // weighted sum of m_DebugFullScreenTempBuffer and m_DebugTranparencyLowRes done in DebugFullScreen.shader
        }

        struct FullScreenDebugParameters
        {
            public RendererListDesc rendererList;
            public FrameSettings frameSettings;
        }

        FullScreenDebugParameters PrepareFullScreenDebugParameters(HDCamera hdCamera, CullingResults cull)
        {
            var parameters = new FullScreenDebugParameters();

            parameters.rendererList = CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_FullScreenDebugPassNames, renderQueueRange: RenderQueueRange.all);
            parameters.frameSettings = hdCamera.frameSettings;

            return parameters;
        }

        static void RenderFullScreenDebug(FullScreenDebugParameters   parameters,
            RTHandle                    colorBuffer,
            RTHandle                    depthBuffer,
            ComputeBuffer               debugBuffer,
            in RendererList             rendererList,
            ScriptableRenderContext     renderContext,
            CommandBuffer               cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderFullScreenDebug)))
            {
                CoreUtils.SetRenderTarget(cmd, colorBuffer, depthBuffer);
                cmd.SetRandomWriteTarget(1, debugBuffer);

                CoreUtils.DrawRendererList(renderContext, cmd, rendererList);

                cmd.ClearRandomWriteTargets();
            }
        }

        void UpdateSkyEnvironment(HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            m_SkyManager.UpdateEnvironment(hdCamera, renderContext, GetCurrentSunLight(), cmd);
        }

        /// <summary>
        /// Request an update of the environment lighting.
        /// </summary>
        public void RequestSkyEnvironmentUpdate()
        {
            m_SkyManager.RequestEnvironmentUpdate();
        }

        internal void RequestStaticSkyUpdate()
        {
            m_SkyManager.RequestStaticEnvironmentUpdate();
        }

        /// <summary>
        /// Export the provided camera's sky to a flattened cubemap.
        /// </summary>
        /// <param name="camera">Requested camera.</param>
        /// <returns>Result texture.</returns>
        public Texture2D ExportSkyToTexture(Camera camera)
        {
            return m_SkyManager.ExportSkyToTexture(camera);
        }

        RendererListDesc PrepareForwardOpaqueRendererList(CullingResults cullResults, HDCamera hdCamera)
        {
            var passNames = hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward
                ? m_ForwardAndForwardOnlyPassNames
                : m_ForwardOnlyPassNames;
            return CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, passNames, m_CurrentRendererConfigurationBakedLighting);
        }

        static bool NeedMotionVectorForTransparent(FrameSettings frameSettings)
        {
            return frameSettings.IsEnabled(FrameSettingsField.MotionVectors);
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

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Refraction))
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

        static void RenderForwardRendererList(FrameSettings               frameSettings,
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

            CoreUtils.SetRenderTarget(cmd, renderTarget, depthBuffer);
            if (opaque)
                DrawOpaqueRendererList(renderContext, cmd, frameSettings, rendererList);
            else
                DrawTransparentRendererList(renderContext, cmd, frameSettings, rendererList);
        }

        struct RenderSSRParameters
        {
            public ComputeShader    ssrCS;
            public int              tracingKernel;
            public int              reprojectionKernel;
            public int              accumulateKernel;
            public bool             transparentSSR;
            public bool             usePBRAlgo;

            public int              width, height, viewCount;

            public ComputeBuffer    offsetBufferData;

            public ShaderVariablesScreenSpaceReflection cb;
        }

        RenderSSRParameters PrepareSSRParameters(HDCamera hdCamera, in HDUtils.PackedMipChainInfo depthPyramid, bool transparentSSR)
        {
            var volumeSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            var parameters = new RenderSSRParameters();

            parameters.ssrCS = m_ScreenSpaceReflectionsCS;
            parameters.tracingKernel = m_SsrTracingKernel;
            parameters.reprojectionKernel = m_SsrReprojectionKernel;
            parameters.accumulateKernel = m_SsrAccumulateKernel;
            parameters.transparentSSR = transparentSSR;
            parameters.usePBRAlgo = !transparentSSR && volumeSettings.usedAlgorithm.value == ScreenSpaceReflectionAlgorithm.PBRAccumulation;

            parameters.width = hdCamera.actualWidth;
            parameters.height = hdCamera.actualHeight;
            parameters.viewCount = hdCamera.viewCount;

            float n = hdCamera.camera.nearClipPlane;
            float f = hdCamera.camera.farClipPlane;
            float thickness = volumeSettings.depthBufferThickness.value;

            ref var cb = ref parameters.cb;
            cb._SsrThicknessScale = 1.0f / (1.0f + thickness);
            cb._SsrThicknessBias = -n / (f - n) * (thickness * cb._SsrThicknessScale);
            cb._SsrIterLimit = volumeSettings.rayMaxIterations;
            cb._SsrReflectsSky = volumeSettings.reflectSky.value ? 1 : 0;
            cb._SsrStencilBit = (int)StencilUsage.TraceReflectionRay;
            float roughnessFadeStart = 1 - volumeSettings.smoothnessFadeStart;
            cb._SsrRoughnessFadeEnd = 1 - volumeSettings.minSmoothness;
            float roughnessFadeLength = cb._SsrRoughnessFadeEnd - roughnessFadeStart;
            cb._SsrRoughnessFadeEndTimesRcpLength = (roughnessFadeLength != 0) ? (cb._SsrRoughnessFadeEnd * (1.0f / roughnessFadeLength)) : 1;
            cb._SsrRoughnessFadeRcpLength = (roughnessFadeLength != 0) ? (1.0f / roughnessFadeLength) : 0;
            cb._SsrEdgeFadeRcpLength = Mathf.Min(1.0f / volumeSettings.screenFadeDistance.value, float.MaxValue);
            cb._ColorPyramidUvScaleAndLimitPrevFrame = HDUtils.ComputeViewportScaleAndLimit(hdCamera.historyRTHandleProperties.previousViewportSize, hdCamera.historyRTHandleProperties.previousRenderTargetSize);
            cb._SsrColorPyramidMaxMip = hdCamera.colorPyramidHistoryMipCount - 1;
            cb._SsrDepthPyramidMaxMip = depthPyramid.mipLevelCount - 1;
            if (hdCamera.isFirstFrame || hdCamera.cameraFrameCount <= 2)
                cb._SsrAccumulationAmount = 1.0f;
            else
                cb._SsrAccumulationAmount = Mathf.Pow(2, Mathf.Lerp(0.0f, -7.0f, volumeSettings.accumulationFactor.value));

            parameters.offsetBufferData = depthPyramid.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);

            return parameters;
        }

        static void RenderSSR(in RenderSSRParameters  parameters,
            HDCamera                hdCamera,
            BlueNoise               blueNoise,
            RTHandle                depthTexture,
            RTHandle                depthPyramid,
            RTHandle                normalBuffer,
            RTHandle                motionVectorsBuffer,
            RTHandle                SsrHitPointTexture,
            RTHandle                stencilBuffer,
            RTHandle                clearCoatMask,
            RTHandle                previousColorPyramid,
            RTHandle                ssrAccum,
            RTHandle                ssrLightingTexture,
            RTHandle                ssrAccumPrev,
            ComputeBuffer           coarseStencilBuffer,
            CommandBuffer           cmd,
            ScriptableRenderContext renderContext)
        {
            var cs = parameters.ssrCS;

            ScreenSpaceReflection ssrSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            if (!parameters.transparentSSR)
            {
                bool ssrNeedReset = false;
                if (ssrSettings.usedAlgorithm.value == ScreenSpaceReflectionAlgorithm.PBRAccumulation &&
                    hdCamera.currentSSRAlgorithm == ScreenSpaceReflectionAlgorithm.Approximation)
                    ssrNeedReset = true;

                hdCamera.currentSSRAlgorithm = ssrSettings.usedAlgorithm.value;

                if (ssrSettings.usedAlgorithm.value == ScreenSpaceReflectionAlgorithm.PBRAccumulation)
                {
                    CoreUtils.SetRenderTarget(cmd, ssrAccum, ClearFlag.Color, Color.clear);
                    if (ssrNeedReset || hdCamera.isFirstFrame || hdCamera.resetPostProcessingHistory)
                    {
                        CoreUtils.SetRenderTarget(cmd, ssrAccumPrev, ClearFlag.Color, Color.clear);
                    }
                }
            }

            if (!parameters.usePBRAlgo)
                cmd.EnableShaderKeyword("SSR_APPROX");
            else
                cmd.DisableShaderKeyword("SSR_APPROX");

            if (parameters.transparentSSR)
                cmd.EnableShaderKeyword("DEPTH_SOURCE_NOT_FROM_MIP_CHAIN");
            else
                cmd.DisableShaderKeyword("DEPTH_SOURCE_NOT_FROM_MIP_CHAIN");

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SsrTracing)))
            {
                // cmd.SetComputeTextureParam(cs, kernel, "_SsrDebugTexture",    m_SsrDebugTexture);
                // Bind the non mip chain if we are rendering the transparent version
                cmd.SetComputeTextureParam(cs, parameters.tracingKernel, HDShaderIDs._DepthTexture, depthTexture);
                cmd.SetComputeTextureParam(cs, parameters.tracingKernel, HDShaderIDs._CameraDepthTexture, depthPyramid);
                cmd.SetComputeTextureParam(cs, parameters.tracingKernel, HDShaderIDs._NormalBufferTexture, normalBuffer);
                cmd.SetComputeTextureParam(cs, parameters.tracingKernel, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMask);
                cmd.SetComputeTextureParam(cs, parameters.tracingKernel, HDShaderIDs._SsrHitPointTexture, SsrHitPointTexture);

                if (stencilBuffer.rt.stencilFormat == GraphicsFormat.None)  // We are accessing MSAA resolved version and not the depth stencil buffer directly.
                {
                    cmd.SetComputeTextureParam(cs, parameters.tracingKernel, HDShaderIDs._StencilTexture, stencilBuffer);
                }
                else
                {
                    cmd.SetComputeTextureParam(cs, parameters.tracingKernel, HDShaderIDs._StencilTexture, stencilBuffer, 0, RenderTextureSubElement.Stencil);
                }

                cmd.SetComputeBufferParam(cs, parameters.tracingKernel, HDShaderIDs._CoarseStencilBuffer, coarseStencilBuffer);
                cmd.SetComputeBufferParam(cs, parameters.tracingKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, parameters.offsetBufferData);

                blueNoise.BindDitheredRNGData1SPP(cmd);

                ConstantBuffer.Push(cmd, parameters.cb, cs, HDShaderIDs._ShaderVariablesScreenSpaceReflection);

                cmd.DispatchCompute(cs, parameters.tracingKernel, HDUtils.DivRoundUp(parameters.width, 8), HDUtils.DivRoundUp(parameters.height, 8), parameters.viewCount);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SsrReprojection)))
            {
                cmd.SetComputeTextureParam(cs, parameters.reprojectionKernel, HDShaderIDs._DepthTexture, depthTexture);
                cmd.SetComputeTextureParam(cs, parameters.reprojectionKernel, HDShaderIDs._CameraDepthTexture, depthPyramid);
                cmd.SetComputeTextureParam(cs, parameters.reprojectionKernel, HDShaderIDs._ColorPyramidTexture, previousColorPyramid);
                cmd.SetComputeTextureParam(cs, parameters.reprojectionKernel, HDShaderIDs._NormalBufferTexture, normalBuffer);
                cmd.SetComputeTextureParam(cs, parameters.reprojectionKernel, HDShaderIDs._SsrHitPointTexture, SsrHitPointTexture);
                cmd.SetComputeTextureParam(cs, parameters.reprojectionKernel, HDShaderIDs._SSRAccumTexture, parameters.usePBRAlgo ? ssrAccum : ssrLightingTexture);
                cmd.SetComputeTextureParam(cs, parameters.reprojectionKernel, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMask);
                cmd.SetComputeTextureParam(cs, parameters.reprojectionKernel, HDShaderIDs._CameraMotionVectorsTexture, motionVectorsBuffer);

                ConstantBuffer.Push(cmd, parameters.cb, cs, HDShaderIDs._ShaderVariablesScreenSpaceReflection);

                cmd.DispatchCompute(cs, parameters.reprojectionKernel, HDUtils.DivRoundUp(parameters.width, 8), HDUtils.DivRoundUp(parameters.height, 8), parameters.viewCount);
            }

            if (parameters.usePBRAlgo)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SsrAccumulate)))
                {
                    cmd.SetComputeTextureParam(cs, parameters.accumulateKernel, HDShaderIDs._DepthTexture, depthTexture);
                    cmd.SetComputeTextureParam(cs, parameters.accumulateKernel, HDShaderIDs._CameraDepthTexture, depthPyramid);
                    cmd.SetComputeTextureParam(cs, parameters.accumulateKernel, HDShaderIDs._NormalBufferTexture, normalBuffer);
                    cmd.SetComputeTextureParam(cs, parameters.accumulateKernel, HDShaderIDs._ColorPyramidTexture, previousColorPyramid);
                    cmd.SetComputeTextureParam(cs, parameters.accumulateKernel, HDShaderIDs._SsrHitPointTexture, SsrHitPointTexture);
                    cmd.SetComputeTextureParam(cs, parameters.accumulateKernel, HDShaderIDs._SSRAccumTexture, ssrAccum);
                    cmd.SetComputeTextureParam(cs, parameters.accumulateKernel, HDShaderIDs._SsrLightingTextureRW, ssrLightingTexture);
                    cmd.SetComputeTextureParam(cs, parameters.accumulateKernel, HDShaderIDs._SsrAccumPrev, ssrAccumPrev);
                    cmd.SetComputeTextureParam(cs, parameters.accumulateKernel, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMask);
                    cmd.SetComputeTextureParam(cs, parameters.accumulateKernel, HDShaderIDs._CameraMotionVectorsTexture, motionVectorsBuffer);

                    ConstantBuffer.Push(cmd, parameters.cb, cs, HDShaderIDs._ShaderVariablesScreenSpaceReflection);

                    cmd.DispatchCompute(cs, parameters.accumulateKernel, HDUtils.DivRoundUp(parameters.width, 8), HDUtils.DivRoundUp(parameters.height, 8), parameters.viewCount);
                }
            }
        }

        unsafe void ApplyDebugDisplaySettings(HDCamera hdCamera, CommandBuffer cmd)
        {
            // See ShaderPassForward.hlsl: for forward shaders, if DEBUG_DISPLAY is enabled and no DebugLightingMode or DebugMipMapMod
            // modes have been set, lighting is automatically skipped (To avoid some crashed due to lighting RT not set on console).
            // However debug mode like colorPickerModes and false color don't need DEBUG_DISPLAY and must work with the lighting.
            // So we will enabled DEBUG_DISPLAY independently

            bool debugDisplayEnabledOrSceneLightingDisabled = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() || CoreUtils.IsSceneLightingDisabled(hdCamera.camera);
            // Enable globally the keyword DEBUG_DISPLAY on shader that support it with multi-compile
            CoreUtils.SetKeyword(cmd, "DEBUG_DISPLAY", debugDisplayEnabledOrSceneLightingDisabled);

            // Setting this all the time due to a strange bug that either reports a (globally) bound texture as not bound or where SetGlobalTexture doesn't behave as expected.
            // As a workaround we bind it regardless of debug display. Eventually with
            cmd.SetGlobalTexture(HDShaderIDs._DebugMatCapTexture, defaultResources.textures.matcapTex);

            m_ShaderVariablesGlobalCB._GlobalTessellationFactorMultiplier = (m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.QuadOverdraw) ? 0.0f : 1.0f;

            if (debugDisplayEnabledOrSceneLightingDisabled ||
                m_CurrentDebugDisplaySettings.data.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None ||
                m_CurrentDebugDisplaySettings.IsDebugExposureModeEnabled())
            {
                // This is for texture streaming
                m_CurrentDebugDisplaySettings.UpdateMaterials();

                var lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;
                var materialDebugSettings = m_CurrentDebugDisplaySettings.data.materialDebugSettings;
                var debugAlbedo = new Vector4(lightingDebugSettings.overrideAlbedo ? 1.0f : 0.0f, lightingDebugSettings.overrideAlbedoValue.r, lightingDebugSettings.overrideAlbedoValue.g, lightingDebugSettings.overrideAlbedoValue.b);
                var debugSmoothness = new Vector4(lightingDebugSettings.overrideSmoothness ? 1.0f : 0.0f, lightingDebugSettings.overrideSmoothnessValue, 0.0f, 0.0f);
                var debugNormal = new Vector4(lightingDebugSettings.overrideNormal ? 1.0f : 0.0f, 0.0f, 0.0f, 0.0f);
                var debugAmbientOcclusion = new Vector4(lightingDebugSettings.overrideAmbientOcclusion ? 1.0f : 0.0f, lightingDebugSettings.overrideAmbientOcclusionValue, 0.0f, 0.0f);
                var debugSpecularColor = new Vector4(lightingDebugSettings.overrideSpecularColor ? 1.0f : 0.0f, lightingDebugSettings.overrideSpecularColorValue.r, lightingDebugSettings.overrideSpecularColorValue.g, lightingDebugSettings.overrideSpecularColorValue.b);
                var debugEmissiveColor = new Vector4(lightingDebugSettings.overrideEmissiveColor ? 1.0f : 0.0f, lightingDebugSettings.overrideEmissiveColorValue.r, lightingDebugSettings.overrideEmissiveColorValue.g, lightingDebugSettings.overrideEmissiveColorValue.b);
                var debugTrueMetalColor = new Vector4(materialDebugSettings.materialValidateTrueMetal ? 1.0f : 0.0f, materialDebugSettings.materialValidateTrueMetalColor.r, materialDebugSettings.materialValidateTrueMetalColor.g, materialDebugSettings.materialValidateTrueMetalColor.b);

                DebugLightingMode debugLightingMode = m_CurrentDebugDisplaySettings.GetDebugLightingMode();
                if (CoreUtils.IsSceneLightingDisabled(hdCamera.camera))
                {
                    debugLightingMode = DebugLightingMode.MatcapView;
                }

                ref var cb = ref m_ShaderVariablesDebugDisplayCB;

                var debugMaterialIndices = m_CurrentDebugDisplaySettings.GetDebugMaterialIndexes();
                for (int i = 0; i < 11; ++i)
                {
                    cb._DebugViewMaterialArray[i * 4] = (uint)debugMaterialIndices[i]; // Only x component is used.
                }
                for (int i = 0; i < 32; ++i)
                {
                    for (int j = 0; j < 4; ++j)
                        cb._DebugRenderingLayersColors[i * 4 + j] = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugRenderingLayersColors[i][j];
                }

                cb._DebugLightingMode = (int)debugLightingMode;
                cb._DebugLightLayersMask = (int)m_CurrentDebugDisplaySettings.GetDebugLightLayersMask();
                cb._DebugShadowMapMode = (int)m_CurrentDebugDisplaySettings.GetDebugShadowMapMode();
                cb._DebugMipMapMode = (int)m_CurrentDebugDisplaySettings.GetDebugMipMapMode();
                cb._DebugMipMapModeTerrainTexture = (int)m_CurrentDebugDisplaySettings.GetDebugMipMapModeTerrainTexture();
                cb._ColorPickerMode = (int)m_CurrentDebugDisplaySettings.GetDebugColorPickerMode();
                cb._DebugFullScreenMode = (int)m_CurrentDebugDisplaySettings.data.fullScreenDebugMode;
                cb._DebugProbeVolumeMode = (int)m_CurrentDebugDisplaySettings.GetProbeVolumeDebugMode();

#if UNITY_EDITOR
                cb._MatcapMixAlbedo = HDRenderPipelinePreferences.matcapViewMixAlbedo ? 1 : 0;
                cb._MatcapViewScale = HDRenderPipelinePreferences.matcapViewScale;
#else
                cb._MatcapMixAlbedo = 0;
                cb._MatcapViewScale = 1.0f;
#endif
                cb._DebugLightingAlbedo = debugAlbedo;
                cb._DebugLightingSmoothness = debugSmoothness;
                cb._DebugLightingNormal = debugNormal;
                cb._DebugLightingAmbientOcclusion = debugAmbientOcclusion;
                cb._DebugLightingSpecularColor = debugSpecularColor;
                cb._DebugLightingEmissiveColor = debugEmissiveColor;
                cb._DebugLightingMaterialValidateHighColor = materialDebugSettings.materialValidateHighColor;
                cb._DebugLightingMaterialValidateLowColor = materialDebugSettings.materialValidateLowColor;
                cb._DebugLightingMaterialValidatePureMetalColor = debugTrueMetalColor;

                cb._MousePixelCoord = HDUtils.GetMouseCoordinates(hdCamera);
                cb._MouseClickPixelCoord = HDUtils.GetMouseClickCoordinates(hdCamera);

                cb._DebugSingleShadowIndex = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowDebugUseSelection ? m_DebugSelectedLightShadowIndex : (int)m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowMapIndex;

                ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesDebugDisplayCB, HDShaderIDs._ShaderVariablesDebugDisplay);

                cmd.SetGlobalTexture(HDShaderIDs._DebugFont, defaultResources.textures.debugFontTex);
            }
        }

        static bool NeedColorPickerDebug(DebugDisplaySettings debugSettings)
        {
            return debugSettings.data.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None
                || debugSettings.data.falseColorDebugSettings.falseColor
                || debugSettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuminanceMeter;
        }

        bool NeedExposureDebugMode(DebugDisplaySettings debugSettings)
        {
            return debugSettings.data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.None;
        }

        bool NeedsFullScreenDebugMode()
        {
            bool fullScreenDebugEnabled = m_CurrentDebugDisplaySettings.data.fullScreenDebugMode != FullScreenDebugMode.None;
            bool lightingDebugEnabled = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow;

            return fullScreenDebugEnabled || lightingDebugEnabled;
        }

        struct DebugParameters
        {
            public DebugDisplaySettings debugDisplaySettings;
            public HDCamera hdCamera;

            // Overlay
            public DebugOverlay debugOverlay;

            // Full screen debug
            public bool             resolveFullScreenDebug;
            public Material         debugFullScreenMaterial;
            public int              depthPyramidMip;
            public ComputeBuffer    depthPyramidOffsets;

            // Sky
            public Texture skyReflectionTexture;
            public Material debugLatlongMaterial;

            public bool rayTracingSupported;
            public RayCountManager rayCountManager;

            // Lighting
            public LightLoopDebugOverlayParameters lightingOverlayParameters;
            public ProbeVolumeDebugOverlayParameters probeVolumeOverlayParameters;

            // Color picker
            public bool     colorPickerEnabled;
            public Material colorPickerMaterial;

            // Exposure
            public bool     exposureDebugEnabled;
            public Material debugExposureMaterial;
        }

        DebugParameters PrepareDebugParameters(HDCamera hdCamera, HDUtils.PackedMipChainInfo depthMipInfo)
        {
            var parameters = new DebugParameters();

            parameters.debugDisplaySettings = m_CurrentDebugDisplaySettings;
            parameters.hdCamera = hdCamera;

            parameters.resolveFullScreenDebug = NeedsFullScreenDebugMode() && m_FullScreenDebugPushed;
            parameters.debugFullScreenMaterial = m_DebugFullScreen;
            parameters.depthPyramidMip = (int)(parameters.debugDisplaySettings.data.fullscreenDebugMip * depthMipInfo.mipLevelCount);
            parameters.depthPyramidOffsets = depthMipInfo.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);

            parameters.skyReflectionTexture = m_SkyManager.GetSkyReflection(hdCamera);
            parameters.debugLatlongMaterial = m_DebugDisplayLatlong;
            parameters.lightingOverlayParameters = PrepareLightLoopDebugOverlayParameters();
            parameters.probeVolumeOverlayParameters = PrepareProbeVolumeOverlayParameters(m_CurrentDebugDisplaySettings.data.lightingDebugSettings);

            parameters.rayTracingSupported = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing);
            parameters.rayCountManager = m_RayCountManager;

            parameters.colorPickerEnabled = NeedColorPickerDebug(parameters.debugDisplaySettings);
            parameters.colorPickerMaterial = m_DebugColorPicker;

            parameters.exposureDebugEnabled = NeedExposureDebugMode(parameters.debugDisplaySettings);
            parameters.debugExposureMaterial = m_DebugExposure;

            float overlayRatio = m_CurrentDebugDisplaySettings.data.debugOverlayRatio;
            int overlaySize = (int)(Math.Min(hdCamera.actualHeight, hdCamera.actualWidth) * overlayRatio);
            m_DebugOverlay.StartOverlay(HDUtils.GetRuntimeDebugPanelWidth(hdCamera), hdCamera.actualHeight - overlaySize, overlaySize, hdCamera.actualWidth);
            parameters.debugOverlay = m_DebugOverlay;

            return parameters;
        }

        static void ResolveFullScreenDebug(in DebugParameters      parameters,
            MaterialPropertyBlock   mpb,
            RTHandle                inputFullScreenDebug,
            RTHandle                inputDepthPyramid,
            RTHandle                output,
            ComputeBuffer           fullscreenBuffer,
            CommandBuffer           cmd)
        {
            mpb.SetTexture(HDShaderIDs._DebugFullScreenTexture, inputFullScreenDebug);
            mpb.SetTexture(HDShaderIDs._CameraDepthTexture, inputDepthPyramid);
            mpb.SetFloat(HDShaderIDs._FullScreenDebugMode, (float)parameters.debugDisplaySettings.data.fullScreenDebugMode);
            if (parameters.debugDisplaySettings.data.enableDebugDepthRemap)
                mpb.SetVector(HDShaderIDs._FullScreenDebugDepthRemap, new Vector4(parameters.debugDisplaySettings.data.fullScreenDebugDepthRemap.x, parameters.debugDisplaySettings.data.fullScreenDebugDepthRemap.y, parameters.hdCamera.camera.nearClipPlane, parameters.hdCamera.camera.farClipPlane));
            else // Setup neutral value
                mpb.SetVector(HDShaderIDs._FullScreenDebugDepthRemap, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
            mpb.SetInt(HDShaderIDs._DebugDepthPyramidMip, parameters.depthPyramidMip);
            mpb.SetBuffer(HDShaderIDs._DebugDepthPyramidOffsets, parameters.depthPyramidOffsets);
            mpb.SetInt(HDShaderIDs._DebugContactShadowLightIndex, parameters.debugDisplaySettings.data.fullScreenContactShadowLightIndex);
            mpb.SetFloat(HDShaderIDs._TransparencyOverdrawMaxPixelCost, (float)parameters.debugDisplaySettings.data.transparencyDebugSettings.maxPixelCost);
            mpb.SetFloat(HDShaderIDs._QuadOverdrawMaxQuadCost, (float)parameters.debugDisplaySettings.data.maxQuadCost);
            mpb.SetFloat(HDShaderIDs._VertexDensityMaxPixelCost, (float)parameters.debugDisplaySettings.data.maxVertexDensity);

            if (fullscreenBuffer != null)
                cmd.SetRandomWriteTarget(1, fullscreenBuffer);

            HDUtils.DrawFullScreen(cmd, parameters.debugFullScreenMaterial, output, mpb, 0);

            if (fullscreenBuffer != null)
                cmd.ClearRandomWriteTargets();
        }

        static void ResolveColorPickerDebug(in DebugParameters  parameters,
            RTHandle            debugColorPickerBuffer,
            RTHandle            output,
            CommandBuffer       cmd)
        {
            ColorPickerDebugSettings colorPickerDebugSettings = parameters.debugDisplaySettings.data.colorPickerDebugSettings;
            FalseColorDebugSettings falseColorDebugSettings = parameters.debugDisplaySettings.data.falseColorDebugSettings;
            var falseColorThresholds = new Vector4(falseColorDebugSettings.colorThreshold0, falseColorDebugSettings.colorThreshold1, falseColorDebugSettings.colorThreshold2, falseColorDebugSettings.colorThreshold3);

            // Here we have three cases:
            // - Material debug is enabled, this is the buffer we display
            // - Otherwise we display the HDR buffer before postprocess and distortion
            // - If fullscreen debug is enabled we always use it
            parameters.colorPickerMaterial.SetTexture(HDShaderIDs._DebugColorPickerTexture, debugColorPickerBuffer);
            parameters.colorPickerMaterial.SetColor(HDShaderIDs._ColorPickerFontColor, colorPickerDebugSettings.fontColor);
            parameters.colorPickerMaterial.SetInt(HDShaderIDs._FalseColorEnabled, falseColorDebugSettings.falseColor ? 1 : 0);
            parameters.colorPickerMaterial.SetVector(HDShaderIDs._FalseColorThresholds, falseColorThresholds);
            parameters.colorPickerMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(parameters.hdCamera));
            parameters.colorPickerMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(parameters.hdCamera));

            // The material display debug perform sRGBToLinear conversion as the final blit currently hardcodes a linearToSrgb conversion. As when we read with color picker this is not done,
            // we perform it inside the color picker shader. But we shouldn't do it for HDR buffer.
            parameters.colorPickerMaterial.SetFloat(HDShaderIDs._ApplyLinearToSRGB, parameters.debugDisplaySettings.IsDebugMaterialDisplayEnabled() ? 1.0f : 0.0f);

            HDUtils.DrawFullScreen(cmd, parameters.colorPickerMaterial, output);
        }

        static void RenderExposureDebug(in DebugParameters parameters,
            RTHandle inputColorBuffer,
            RTHandle postprocessedColorBuffer,
            RTHandle currentExposure,
            RTHandle prevExposure,
            RTHandle debugExposureData,
            RTHandle output,
            HableCurve hableCurve,
            int lutSize,
            Vector4 proceduralParams1,
            Vector4 proceduralParams2,
            ComputeBuffer histogramBuffer,
            CommandBuffer cmd)
        {
            // Grab exposure parameters
            var exposureSettings = parameters.hdCamera.volumeStack.GetComponent<Exposure>();

            Vector4 exposureParams = new Vector4(exposureSettings.compensation.value + parameters.debugDisplaySettings.data.lightingDebugSettings.debugExposure, exposureSettings.limitMin.value,
                exposureSettings.limitMax.value, 0f);

            Vector4 exposureVariants = new Vector4(1.0f, (int)exposureSettings.meteringMode.value, (int)exposureSettings.adaptationMode.value, 0.0f);
            Vector2 histogramFraction = exposureSettings.histogramPercentages.value / 100.0f;
            float evRange = exposureSettings.limitMax.value - exposureSettings.limitMin.value;
            float histScale = 1.0f / Mathf.Max(1e-5f, evRange);
            float histBias = -exposureSettings.limitMin.value * histScale;
            Vector4 histogramParams = new Vector4(histScale, histBias, histogramFraction.x, histogramFraction.y);

            parameters.debugExposureMaterial.SetVector(HDShaderIDs._ProceduralMaskParams, proceduralParams1);
            parameters.debugExposureMaterial.SetVector(HDShaderIDs._ProceduralMaskParams2, proceduralParams2);

            parameters.debugExposureMaterial.SetVector(HDShaderIDs._HistogramExposureParams, histogramParams);
            parameters.debugExposureMaterial.SetVector(HDShaderIDs._Variants, exposureVariants);
            parameters.debugExposureMaterial.SetVector(HDShaderIDs._ExposureParams, exposureParams);
            parameters.debugExposureMaterial.SetVector(HDShaderIDs._ExposureParams2, new Vector4(0.0f, 0.0f, ColorUtils.lensImperfectionExposureScale, ColorUtils.s_LightMeterCalibrationConstant));
            parameters.debugExposureMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(parameters.hdCamera));
            parameters.debugExposureMaterial.SetTexture(HDShaderIDs._SourceTexture, inputColorBuffer);
            parameters.debugExposureMaterial.SetTexture(HDShaderIDs._DebugFullScreenTexture, postprocessedColorBuffer);
            parameters.debugExposureMaterial.SetTexture(HDShaderIDs._PreviousExposureTexture, prevExposure);
            parameters.debugExposureMaterial.SetTexture(HDShaderIDs._ExposureTexture, currentExposure);
            parameters.debugExposureMaterial.SetTexture(HDShaderIDs._ExposureWeightMask, exposureSettings.weightTextureMask.value);
            parameters.debugExposureMaterial.SetBuffer(HDShaderIDs._HistogramBuffer, histogramBuffer);


            int passIndex = 0;
            if (parameters.debugDisplaySettings.data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.MeteringWeighted)
            {
                passIndex = 1;
                parameters.debugExposureMaterial.SetVector(HDShaderIDs._ExposureDebugParams, new Vector4(parameters.debugDisplaySettings.data.lightingDebugSettings.displayMaskOnly ? 1 : 0, 0, 0, 0));
            }
            if (parameters.debugDisplaySettings.data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.HistogramView)
            {
                parameters.debugExposureMaterial.SetTexture(HDShaderIDs._ExposureDebugTexture, debugExposureData);
                var tonemappingSettings = parameters.hdCamera.volumeStack.GetComponent<Tonemapping>();

                bool toneMapIsEnabled = parameters.hdCamera.frameSettings.IsEnabled(FrameSettingsField.Tonemapping);
                var tonemappingMode = toneMapIsEnabled ? tonemappingSettings.mode.value : TonemappingMode.None;

                bool drawTonemapCurve = tonemappingMode != TonemappingMode.None &&
                    parameters.debugDisplaySettings.data.lightingDebugSettings.showTonemapCurveAlongHistogramView;

                bool centerAroundMiddleGrey = parameters.debugDisplaySettings.data.lightingDebugSettings.centerHistogramAroundMiddleGrey;
                parameters.debugExposureMaterial.SetVector(HDShaderIDs._ExposureDebugParams, new Vector4(drawTonemapCurve ? 1.0f : 0.0f, (int)tonemappingMode, centerAroundMiddleGrey ? 1 : 0, 0));
                if (drawTonemapCurve)
                {
                    if (tonemappingMode == TonemappingMode.Custom)
                    {
                        parameters.debugExposureMaterial.SetVector(HDShaderIDs._CustomToneCurve, hableCurve.uniforms.curve);
                        parameters.debugExposureMaterial.SetVector(HDShaderIDs._ToeSegmentA, hableCurve.uniforms.toeSegmentA);
                        parameters.debugExposureMaterial.SetVector(HDShaderIDs._ToeSegmentB, hableCurve.uniforms.toeSegmentB);
                        parameters.debugExposureMaterial.SetVector(HDShaderIDs._MidSegmentA, hableCurve.uniforms.midSegmentA);
                        parameters.debugExposureMaterial.SetVector(HDShaderIDs._MidSegmentB, hableCurve.uniforms.midSegmentB);
                        parameters.debugExposureMaterial.SetVector(HDShaderIDs._ShoSegmentA, hableCurve.uniforms.shoSegmentA);
                        parameters.debugExposureMaterial.SetVector(HDShaderIDs._ShoSegmentB, hableCurve.uniforms.shoSegmentB);
                    }
                }
                else if (tonemappingMode == TonemappingMode.External)
                {
                    parameters.debugExposureMaterial.SetTexture(HDShaderIDs._LogLut3D, tonemappingSettings.lutTexture.value);
                    parameters.debugExposureMaterial.SetVector(HDShaderIDs._LogLut3D_Params, new Vector4(1f / lutSize, lutSize - 1f, tonemappingSettings.lutContribution.value, 0f));
                }
                passIndex = 2;
            }
            if (parameters.debugDisplaySettings.data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.FinalImageHistogramView)
            {
                bool finalImageRGBHisto = parameters.debugDisplaySettings.data.lightingDebugSettings.displayFinalImageHistogramAsRGB;

                parameters.debugExposureMaterial.SetVector(HDShaderIDs._ExposureDebugParams, new Vector4(0, 0, 0, finalImageRGBHisto ? 1 : 0));

                parameters.debugExposureMaterial.SetBuffer(HDShaderIDs._FullImageHistogram, histogramBuffer);
                passIndex = 3;
            }


            HDUtils.DrawFullScreen(cmd, parameters.debugExposureMaterial, output, null, passIndex);
        }

        static void RenderSkyReflectionOverlay(in DebugParameters debugParameters, CommandBuffer cmd, MaterialPropertyBlock mpb)
        {
            var lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;

            debugParameters.debugOverlay.SetViewport(cmd);
            mpb.SetTexture(HDShaderIDs._InputCubemap, debugParameters.skyReflectionTexture);
            mpb.SetFloat(HDShaderIDs._Mipmap, lightingDebug.skyReflectionMipmap);
            mpb.SetFloat(HDShaderIDs._ApplyExposure, 1.0f);
            mpb.SetFloat(HDShaderIDs._SliceIndex, lightingDebug.cubeArraySliceIndex);
            cmd.DrawProcedural(Matrix4x4.identity, debugParameters.debugLatlongMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);
            debugParameters.debugOverlay.Next();
        }

        struct PostProcessParameters
        {
            public ShaderVariablesGlobal globalCB;

            public HDCamera         hdCamera;
            public bool             postProcessIsFinalPass;
            public bool             flipYInPostProcess;
            public BlueNoise        blueNoise;

            // After Postprocess
            public bool             useDepthBuffer;
            public RendererListDesc opaqueAfterPPDesc;
            public RendererListDesc transparentAfterPPDesc;
        }

        PostProcessParameters PreparePostProcess(CullingResults cullResults, HDCamera hdCamera)
        {
            PostProcessParameters result = new PostProcessParameters();
            result.globalCB = m_ShaderVariablesGlobalCB;
            result.hdCamera = hdCamera;
            result.postProcessIsFinalPass = HDUtils.PostProcessIsFinalPass(hdCamera);
            // Y-Flip needs to happen during the post process pass only if it's the final pass and is the regular game view
            // SceneView flip is handled by the editor internal code and GameView rendering into render textures should not be flipped in order to respect Unity texture coordinates convention
            result.flipYInPostProcess = result.postProcessIsFinalPass && (hdCamera.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY || hdCamera.isMainGameView);
            result.blueNoise = m_BlueNoise;

            result.useDepthBuffer = !hdCamera.IsTAAEnabled() && hdCamera.frameSettings.IsEnabled(FrameSettingsField.ZTestAfterPostProcessTAA);
            result.opaqueAfterPPDesc = CreateOpaqueRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque);
            result.transparentAfterPPDesc = CreateTransparentRendererListDesc(cullResults, hdCamera.camera, HDShaderPassNames.s_ForwardOnlyName, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent);

            return result;
        }

        static void UpdateOffscreenRenderingConstants(ref ShaderVariablesGlobal cb, bool enabled, uint factor)
        {
            cb._OffScreenRendering = enabled ? 1u : 0u;
            cb._OffScreenDownsampleFactor = factor;
        }

        static void RenderAfterPostProcess(PostProcessParameters   parameters,
            in RendererList         opaqueAfterPostProcessRendererList,
            in RendererList         transparentAfterPostProcessRendererList,
            ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.AfterPostProcessing)))
            {
                // Note about AfterPostProcess and TAA:
                // When TAA is enabled rendering is jittered and then resolved during the post processing pass.
                // It means that any rendering done after post processing need to disable jittering. This is what we do with hdCamera.UpdateViewConstants(false);
                // The issue is that the only available depth buffer is jittered so pixels would wobble around depth tested edges.
                // In order to avoid that we decide that objects rendered after Post processes while TAA is active will not benefit from the depth buffer so we disable it.
                parameters.hdCamera.UpdateAllViewConstants(false);
                parameters.hdCamera.UpdateShaderVariablesGlobalCB(ref parameters.globalCB);

                UpdateOffscreenRenderingConstants(ref parameters.globalCB, true, 1);
                ConstantBuffer.PushGlobal(cmd, parameters.globalCB, HDShaderIDs._ShaderVariablesGlobal);

                DrawOpaqueRendererList(renderContext, cmd, parameters.hdCamera.frameSettings, opaqueAfterPostProcessRendererList);
                // Setup off-screen transparency here
                DrawTransparentRendererList(renderContext, cmd, parameters.hdCamera.frameSettings, transparentAfterPostProcessRendererList);

                UpdateOffscreenRenderingConstants(ref parameters.globalCB, false, 1);
                ConstantBuffer.PushGlobal(cmd, parameters.globalCB, HDShaderIDs._ShaderVariablesGlobal);
            }
        }

        struct SendGeometryGraphcisBuffersParameters
        {
            public HDCamera hdCamera;
            public bool needNormalBuffer;
            public bool needDepthBuffer;
            public VFXCameraBufferTypes neededVFXBuffers;
            public HDUtils.PackedMipChainInfo packedMipChainInfo;

            public bool NeedSendBuffers()
            {
                return needNormalBuffer || needDepthBuffer || neededVFXBuffers != VFXCameraBufferTypes.None;
            }
        }

        SendGeometryGraphcisBuffersParameters PrepareSendGeometryBuffersParameters(HDCamera hdCamera, in HDUtils.PackedMipChainInfo packedMipInfo)
        {
            SendGeometryGraphcisBuffersParameters parameters = new SendGeometryGraphcisBuffersParameters();

            parameters.hdCamera = hdCamera;
            parameters.needNormalBuffer = false;
            parameters.needDepthBuffer = false;
            parameters.packedMipChainInfo = packedMipInfo;

            HDAdditionalCameraData acd = null;
            hdCamera.camera.TryGetComponent(out acd);

            HDAdditionalCameraData.BufferAccessType externalAccess = new HDAdditionalCameraData.BufferAccessType();
            if (acd != null)
                externalAccess = acd.GetBufferAccess();

            // Figure out which client systems need which buffers
            // Only VFX systems for now
            parameters.neededVFXBuffers = VFXManager.IsCameraBufferNeeded(hdCamera.camera);
            parameters.needNormalBuffer |= ((parameters.neededVFXBuffers & VFXCameraBufferTypes.Normal) != 0 || (externalAccess & HDAdditionalCameraData.BufferAccessType.Normal) != 0);
            parameters.needDepthBuffer |= ((parameters.neededVFXBuffers & VFXCameraBufferTypes.Depth) != 0 || (externalAccess & HDAdditionalCameraData.BufferAccessType.Depth) != 0 || GetIndirectDiffuseMode(hdCamera) == IndirectDiffuseMode.ScreenSpace);

            // Raytracing require both normal and depth from previous frame.
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && GetRayTracingState())
            {
                parameters.needNormalBuffer = true;
                parameters.needDepthBuffer = true;
            }

            return parameters;
        }

        static void SendGeometryGraphicsBuffers(in SendGeometryGraphcisBuffersParameters parameters,
            RTHandle mainNormalBuffer,
            RTHandle mainDepthBuffer,
            CommandBuffer cmd)
        {
            var hdCamera = parameters.hdCamera;

            Texture normalBuffer = null;
            Texture depthBuffer = null;
            Texture depthBuffer1 = null;

            // Here if needed for this particular camera, we allocate history buffers.
            // Only one is needed here because the main buffer used for rendering is separate.
            // Ideally, we should double buffer the main rendering buffer but since we don't know in advance if history is going to be needed, it would be a big waste of memory.
            if (parameters.needNormalBuffer && mainNormalBuffer.rt != null)
            {
                // local variable to avoid gcalloc caused by capture.
                var localNormalBuffer = mainNormalBuffer;
                RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
                {
                    return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: localNormalBuffer.rt.graphicsFormat, dimension: TextureXR.dimension, enableRandomWrite: localNormalBuffer.rt.enableRandomWrite, name: $"{id}_Normal History Buffer"
                    );
                }

                normalBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal) ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Normal, Allocator, 1);

                for (int i = 0; i < hdCamera.viewCount; i++)
                    cmd.CopyTexture(localNormalBuffer, i, 0, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight, normalBuffer, i, 0, 0, 0);
            }

            if (parameters.needDepthBuffer && mainDepthBuffer.rt != null)
            {
                // local variable to avoid gcalloc caused by capture.
                var localDepthBuffer = mainDepthBuffer;
                RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
                {
                    return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: localDepthBuffer.rt.graphicsFormat, dimension: TextureXR.dimension, enableRandomWrite: localDepthBuffer.rt.enableRandomWrite, name: $"{id}_Depth History Buffer");
                }

                depthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth) ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Depth, Allocator, 1);

                for (int i = 0; i < hdCamera.viewCount; i++)
                    cmd.CopyTexture(localDepthBuffer, i, 0, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight, depthBuffer, i, 0, 0, 0);

                RTHandle Allocator1(string id, int frameIndex, RTHandleSystem rtHandleSystem)
                {
                    return rtHandleSystem.Alloc(Vector2.one * 0.5f, TextureXR.slices, colorFormat: localDepthBuffer.rt.graphicsFormat, dimension: TextureXR.dimension, enableRandomWrite: localDepthBuffer.rt.enableRandomWrite, name: $"Depth History Buffer Mip 1");
                }

                depthBuffer1 = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth1) ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Depth1, Allocator1, 1);
                for (int i = 0; i < hdCamera.viewCount; i++)
                    cmd.CopyTexture(localDepthBuffer, i, 0, parameters.packedMipChainInfo.mipLevelOffsets[1].x, parameters.packedMipChainInfo.mipLevelOffsets[1].y, hdCamera.actualWidth / 2, hdCamera.actualHeight / 2, depthBuffer1, i, 0, 0, 0);
            }

            // Send buffers to client.
            // For now, only VFX systems
            if ((parameters.neededVFXBuffers & VFXCameraBufferTypes.Depth) != 0)
            {
                VFXManager.SetCameraBuffer(hdCamera.camera, VFXCameraBufferTypes.Depth, depthBuffer, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight);
            }

            if ((parameters.neededVFXBuffers & VFXCameraBufferTypes.Normal) != 0)
            {
                VFXManager.SetCameraBuffer(hdCamera.camera, VFXCameraBufferTypes.Normal, normalBuffer, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight);
            }
        }

        static void SendColorGraphicsBuffer(CommandBuffer cmd, HDCamera hdCamera)
        {
            // Figure out which client systems need which buffers
            VFXCameraBufferTypes neededVFXBuffers = VFXManager.IsCameraBufferNeeded(hdCamera.camera);

            if ((neededVFXBuffers & VFXCameraBufferTypes.Color) != 0)
            {
                var colorBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                VFXManager.SetCameraBuffer(hdCamera.camera, VFXCameraBufferTypes.Color, colorBuffer, 0, 0, hdCamera.actualWidth, hdCamera.actualHeight);
            }
        }

        /// <summary>
        /// Overrides the current camera, changing all the matrices and view parameters for the new one.
        /// It allows you to render objects from another camera, which can be useful in custom passes for example.
        /// </summary>
        internal struct OverrideCameraRendering : IDisposable
        {
            CommandBuffer   cmd;
            Camera          overrideCamera;
            HDCamera        overrideHDCamera;
            float           originalAspect;

            /// <summary>
            /// Overrides the current camera, changing all the matrices and view parameters for the new one.
            /// </summary>
            /// <param name="cmd">The current command buffer in use</param>
            /// <param name="overrideCamera">The camera that will replace the current one</param>
            /// <example>
            /// <code>
            /// using (new HDRenderPipeline.OverrideCameraRendering(cmd, overrideCamera))
            /// {
            ///     ...
            /// }
            /// </code>
            /// </example>
            public OverrideCameraRendering(CommandBuffer cmd, Camera overrideCamera)
            {
                this.cmd = cmd;
                this.overrideCamera = overrideCamera;
                this.overrideHDCamera = null;
                this.originalAspect = 0;

                if (!IsContextValid(overrideCamera))
                    return;

                var hdrp = HDRenderPipeline.currentPipeline;
                overrideHDCamera = HDCamera.GetOrCreate(overrideCamera);

                // Mark the HDCamera as persistant so it's not deleted because it's camera is disabled.
                overrideHDCamera.isPersistent = true;

                // We need to patch the pixel rect of the camera because by default the camera size is synchronized
                // with the game view and so it breaks in the scene view. Note that we can't use Camera.pixelRect here
                // because when we assign it, the change is not instantaneous and is not reflected in pixelWidth/pixelHeight.
                overrideHDCamera.OverridePixelRect(hdrp.m_CurrentHDCamera.camera.pixelRect);
                // We also sync the aspect ratio of the camera, this time using the camera instead of HDCamera.
                // This will update the projection matrix to match the aspect of the current rendering camera.
                originalAspect = overrideCamera.aspect;
                overrideCamera.aspect = (float)hdrp.m_CurrentHDCamera.camera.pixelRect.width / (float)hdrp.m_CurrentHDCamera.camera.pixelRect.height;

                // Update HDCamera datas
                overrideHDCamera.Update(overrideHDCamera.frameSettings, hdrp, hdrp.m_MSAASamples, hdrp.m_XRSystem.emptyPass, allocateHistoryBuffers: false);
                // Reset the reference size as it could have been changed by the override camera
                hdrp.m_CurrentHDCamera.SetReferenceSize();
                overrideHDCamera.UpdateShaderVariablesGlobalCB(ref hdrp.m_ShaderVariablesGlobalCB);

                ConstantBuffer.PushGlobal(cmd, hdrp.m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
            }

            bool IsContextValid(Camera overrideCamera)
            {
                var hdrp = HDRenderPipeline.currentPipeline;

                if (hdrp.m_CurrentHDCamera == null)
                {
                    Debug.LogError("OverrideCameraRendering can only be called inside the render loop !");
                    return false;
                }

                if (overrideCamera == hdrp.m_CurrentHDCamera.camera)
                    return false;

                return true;
            }

            /// <summary>
            /// Reset the camera settings to the original camera
            /// </summary>
            void IDisposable.Dispose()
            {
                if (!IsContextValid(overrideCamera))
                    return;

                overrideHDCamera.ResetPixelRect();
                overrideCamera.aspect = originalAspect;

                var hdrp = HDRenderPipeline.currentPipeline;
                // Reset the reference size as it could have been changed by the override camera
                hdrp.m_CurrentHDCamera.SetReferenceSize();
                hdrp.m_CurrentHDCamera.UpdateShaderVariablesGlobalCB(ref hdrp.m_ShaderVariablesGlobalCB);
                ConstantBuffer.PushGlobal(cmd, hdrp.m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
            }
        }

        /// <summary>
        /// Release all persistent shadow atlas.
        /// In HDRP, shadow persistent atlases are allocated per light type (area, punctual or directional) when needed but never deallocated.
        /// Calling this will force deallocation of those atlases. This can be useful between levels for example when you know that some types of lights aren't used anymore.
        /// </summary>
        public void ReleasePersistentShadowAtlases()
        {
            // TODO RENDERGRAPH remove test when we have only one code path.
            if (m_RenderGraph != null)
                m_ShadowManager.ReleaseSharedShadowAtlases(m_RenderGraph);
        }
    }
}
