using System.Collections.Generic;
using UnityEngine.VFX;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor.Rendering;
#endif

#if ENABLE_VIRTUALTEXTURES
using UnityEngine.Rendering.VirtualTexturing;
#endif

// Resove the ambiguity in the RendererList name (pick the in-engine version)
using RendererList = UnityEngine.Rendering.RendererList;
using RendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;


namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// High Definition Render Pipeline class.
    /// </summary>
    public partial class HDRenderPipeline : RenderPipeline
    {
        #region Global Settings
        private HDRenderPipelineGlobalSettings m_GlobalSettings;
        /// <summary>
        /// Accessor to the active Global Settings for the HD Render Pipeline.
        /// </summary>
        public override RenderPipelineGlobalSettings defaultSettings => m_GlobalSettings;

        internal static HDRenderPipelineAsset currentAsset
            => GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset hdrpAsset ? hdrpAsset : null;

        internal static HDRenderPipeline currentPipeline
            => RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp ? hdrp : null;

        internal static bool isReady => HDRenderPipeline.currentAsset != null && HDRenderPipelineGlobalSettings.instance != null;

        internal static bool pipelineSupportsRayTracing => HDRenderPipeline.currentPipeline != null && HDRenderPipeline.currentPipeline.rayTracingSupported;

#if UNITY_EDITOR
        internal static bool assetSupportsRayTracing => HDRenderPipeline.currentPipeline != null && (HDRenderPipeline.currentPipeline.m_AssetSupportsRayTracing);
#endif

        internal static bool pipelineSupportsScreenSpaceShadows => GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset hdrpAsset ? hdrpAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows : false;
        #endregion

        /// <summary>
        /// Shader Tag for the High Definition Render Pipeline.
        /// </summary>
        public const string k_ShaderTagName = "HDRenderPipeline";

        readonly HDRenderPipelineAsset m_Asset;
        internal HDRenderPipelineAsset asset { get { return m_Asset; } }
        internal HDRenderPipelineRuntimeResources defaultResources { get { return m_GlobalSettings.renderPipelineResources; } }

        internal RenderPipelineSettings currentPlatformRenderPipelineSettings { get { return m_Asset.currentPlatformRenderPipelineSettings; } }

        readonly List<RenderPipelineMaterial> m_MaterialList = new List<RenderPipelineMaterial>();


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
        public uint GetRaysPerFrame(RayCountValues rayValues) { return m_RayCountManager != null ? m_RayCountManager.GetRaysPerFrame(rayValues) : 0; }

        // Renderer Bake configuration can vary depends on if shadow mask is enabled or no
        PerObjectData m_CurrentRendererConfigurationBakedLighting;
        MaterialPropertyBlock m_CopyDepthPropertyBlock = new MaterialPropertyBlock();
        Material m_CopyDepth;
        Material m_UpsampleTransparency;
        MipGenerator m_MipGenerator;
        BlueNoise m_BlueNoise;

        IBLFilterBSDF[] m_IBLFilterArray = null;

        ComputeShader m_ScreenSpaceReflectionsCS { get { return defaultResources.shaders.screenSpaceReflectionsCS; } }
        int m_SsrTracingKernel = -1;
        int m_SsrReprojectionKernel = -1;
        int m_SsrAccumulateNoWorldSpeedRejectionBothKernel = -1;
        int m_SsrAccumulateNoWorldSpeedRejectionSurfaceKernel = -1;
        int m_SsrAccumulateNoWorldSpeedRejectionHitKernel = -1;
        int m_SsrAccumulateHardThresholdSpeedRejectionBothKernel = -1;
        int m_SsrAccumulateHardThresholdSpeedRejectionSurfaceKernel = -1;
        int m_SsrAccumulateHardThresholdSpeedRejectionHitKernel = -1;
        int m_SsrAccumulateSmoothSpeedRejectionBothKernel = -1;
        int m_SsrAccumulateSmoothSpeedRejectionSurfaceKernel = -1;
        int m_SsrAccumulateSmoothSpeedRejectionHitKernel = -1;

        int m_SsrAccumulateNoWorldSpeedRejectionBothDebugKernel = -1;
        int m_SsrAccumulateNoWorldSpeedRejectionSurfaceDebugKernel = -1;
        int m_SsrAccumulateNoWorldSpeedRejectionHitDebugKernel = -1;
        int m_SsrAccumulateHardThresholdSpeedRejectionBothDebugKernel = -1;
        int m_SsrAccumulateHardThresholdSpeedRejectionSurfaceDebugKernel = -1;
        int m_SsrAccumulateHardThresholdSpeedRejectionHitDebugKernel = -1;
        int m_SsrAccumulateSmoothSpeedRejectionBothDebugKernel = -1;
        int m_SsrAccumulateSmoothSpeedRejectionSurfaceDebugKernel = -1;
        int m_SsrAccumulateSmoothSpeedRejectionHitDebugKernel = -1;

        ComputeShader m_ClearBuffer2DCS { get { return defaultResources.shaders.clearBuffer2D; } }
        int m_ClearBuffer2DKernel = -1;

        Material m_ApplyDistortionMaterial;
        Material m_FinalBlitWithOETF;
        Material m_FinalBlitWithOETFTexArraySingleSlice;

        Material m_ClearStencilBufferMaterial;

        Material m_ErrorMaterial;

        Lazy<RTHandle> m_CustomPassColorBuffer;
        Lazy<RTHandle> m_CustomPassDepthBuffer;

        // Constant Buffers
        ShaderVariablesGlobal m_ShaderVariablesGlobalCB = new ShaderVariablesGlobal();
        ShaderVariablesXR m_ShaderVariablesXRCB = new ShaderVariablesXR();
        ShaderVariablesRaytracing m_ShaderVariablesRayTracingCB = new ShaderVariablesRaytracing();

        internal ShaderVariablesGlobal GetShaderVariablesGlobalCB() => m_ShaderVariablesGlobalCB;

        // The pass "SRPDefaultUnlit" is a fall back to legacy unlit rendering and is required to support unity 2d + unity UI that render in the scene.
        ShaderTagId[] m_ForwardAndForwardOnlyPassNames = { HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_ForwardName, HDShaderPassNames.s_SRPDefaultUnlitName, HDShaderPassNames.s_DecalMeshForwardEmissiveName };
        ShaderTagId[] m_ForwardOnlyPassNames = { HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_SRPDefaultUnlitName, HDShaderPassNames.s_DecalMeshForwardEmissiveName };

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
        ShaderTagId[] m_SinglePassName = new ShaderTagId[1];
        ShaderTagId[] m_MeshDecalsPassNames = { HDShaderPassNames.s_DBufferMeshName };
        ShaderTagId[] m_VfxDecalsPassNames = { HDShaderPassNames.s_DBufferVFXDecalName };

        RenderStateBlock m_DepthStateOpaque;
        RenderStateBlock m_DepthStateNoWrite;
        RenderStateBlock m_AlphaToMaskBlock;

        readonly List<CustomPassVolume> m_ActivePassVolumes = new List<CustomPassVolume>(6);
        readonly List<Terrain> m_ActiveTerrains = new List<Terrain>();

        // Detect when windows size is changing
        int m_MaxCameraWidth;
        int m_MaxCameraHeight;
        // Keep track of the maximum number of XR instanced views
        int m_MaxViewCount = 1;

        // Use to detect frame changes (for accurate frame count in editor, consider using hdCamera.GetCameraFrameCount)
        int m_FrameCount;

        internal GraphicsFormat GetColorBufferFormat()
        {
            if (CoreUtils.IsSceneFilteringEnabled())
                return GraphicsFormat.R16G16B16A16_SFloat;

            return m_ShouldOverrideColorBufferFormat ? m_AOVGraphicsFormat : (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.colorBufferFormat;
        }

        GraphicsFormat GetCustomBufferFormat()
            => (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.customBufferFormat;

        internal int GetDecalAtlasMipCount()
        {
            int size = Math.Max(currentPlatformRenderPipelineSettings.decalSettings.atlasWidth, currentPlatformRenderPipelineSettings.decalSettings.atlasHeight);
            return Mathf.FloorToInt(Mathf.Log(size, 2.0f)) + 1;
        }

        internal int GetCookieAtlasMipCount()
        {
            int size = (int)currentPlatformRenderPipelineSettings.lightLoopSettings.cookieAtlasSize;
            return Mathf.FloorToInt(Mathf.Log(size, 2.0f)) + 1;
        }

        internal int GetReflectionProbeMipCount()
        {
            Vector2Int cacheDim = GlobalLightLoopSettings.GetReflectionProbeTextureCacheDim(currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionProbeTexCacheSize);
            return Mathf.FloorToInt(Mathf.Log(Math.Max(cacheDim.x, cacheDim.y), 2.0f)) + 1;
        }

        internal int GetReflectionProbeArraySize()
        {
            return currentPlatformRenderPipelineSettings.lightLoopSettings.supportFabricConvolution ? 2 : 1;
        }

        internal int GetMaxScreenSpaceShadows()
        {
            return currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows ? currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots : 0;
        }

        /// <summary>
        /// Checks the hardware (main display and platform) is HDR capable and the pipeline supports it
        /// </summary>
        /// <returns>Return true if the main display and platform is HDR capable and has enabled HDR output</returns>
        internal static bool HDROutputForMainDisplayIsActive()
        {
            return SystemInfo.hdrDisplaySupportFlags.HasFlag(HDRDisplaySupportFlags.Supported) && HDROutputSettings.main.active;
        }

        /// <summary>
        /// Checks if any of the display devices we can output to are HDR capable and enabled.
        /// </summary>
        /// <returns>Return true if any of the display devices we can output HDR to have enabled HDR output</returns>
        internal static bool HDROutputForAnyDisplayIsActive()
        {
            bool hdrDisplayOutputActive = HDROutputForMainDisplayIsActive();
#if ENABLE_VR && ENABLE_XR_MODULE
            // If we are rendering to xr then we need to look at the XR Display rather than the main non-xr display.
            if (XRSystem.displayActive)
            {
                hdrDisplayOutputActive |= XRSystem.isHDRDisplayOutputActive;
            }
#endif
            return hdrDisplayOutputActive;
        }

        static bool HDROutputActiveForCameraType(HDCamera camera)
        {
            return HDROutputIsActive(camera) && camera.camera.cameraType == CameraType.Game;
        }

        static bool HDROutputIsActive(HDCamera camera)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (camera.xr.enabled)
            {
                return camera.xr.isHDRDisplayOutputActive;
            }
            else
#endif
            {
                return HDROutputForMainDisplayIsActive();
            }
        }

        /// <summary>
        /// Returns a subset of the HDROutputSettings information that can be passed around.
        /// </summary>
        /// <returns>HDRDisplayInformation for the display associated with the camera.</returns>
        static HDROutputUtils.HDRDisplayInformation HDRDisplayInformationForCamera(HDCamera camera)
        {
            HDROutputUtils.HDRDisplayInformation displayInformation;
#if ENABLE_VR && ENABLE_XR_MODULE
            // If we are rendering to xr then we need to look at the XR Display rather than the main non-xr display.
            if (camera.xr.enabled)
            {
                displayInformation = camera.xr.hdrDisplayOutputInformation;
            }
            else
#endif
            {
                HDROutputSettings displaySettings = HDROutputSettings.main;
                displayInformation = new HDROutputUtils.HDRDisplayInformation(displaySettings.maxFullFrameToneMapLuminance,
                    displaySettings.maxToneMapLuminance,
                    displaySettings.minToneMapLuminance,
                    displaySettings.paperWhiteNits);
            }

            return displayInformation;
        }

        /// <summary>
        /// Returns the current color gamut that the display associated with camera uses.
        /// </summary>
        /// <returns>The color gamut used.</returns>
        static ColorGamut HDRDisplayColorGamutForCamera(HDCamera camera)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            // If we are rendering to xr then we need to look at the XR Display rather than the main non-xr display.
            if (camera.xr.enabled)
            {
                return camera.xr.hdrDisplayOutputColorGamut;
            }
            else
#endif
            {
                return HDROutputSettings.main.displayColorGamut;
            }
        }

        // We only want to enable HDR for the game view once
        // since the game itself might what to control this
        internal bool m_enableHdrOnce = true;

        void SetHDRState(HDCamera camera)
        {
            if (camera.camera.cameraType == CameraType.Reflection) return; // Do nothing for reflection probes, they don't output to backbuffers. 
#if UNITY_EDITOR
            bool hdrInPlayerSettings = UnityEditor.PlayerSettings.useHDRDisplay;
#else
            bool hdrInPlayerSettings = true;
#endif

            if (hdrInPlayerSettings && HDROutputSettings.main.available)
            {
                if (camera.camera.cameraType != CameraType.Game)
                {
                    HDROutputSettings.main.RequestHDRModeChange(false);
                }
                else if (m_enableHdrOnce)
                {
                    HDROutputSettings.main.RequestHDRModeChange(true);
                    m_enableHdrOnce = false;
                }
            }
            // Make sure HDR auto tonemap is off
            if (HDROutputSettings.main.active)
            {
                HDROutputSettings.main.automaticHDRTonemapping = false;
            }
        }

        readonly SkyManager m_SkyManager = new SkyManager();
        internal SkyManager skyManager { get { return m_SkyManager; } }

        bool m_ValidAPI; // False by default mean we render normally, true mean we don't render anything
        bool m_IsDepthBufferCopyValid;

        private ProbeCameraCache<(Transform viewer, HDProbe probe, CubemapFace face)> m_ProbeCameraCache = new
            ProbeCameraCache<(Transform viewer, HDProbe probe, CubemapFace face)>();

        ComputeBuffer m_DepthPyramidMipLevelOffsetsBuffer = null;

        ScriptableCullingParameters frozenCullingParams;
        bool frozenCullingParamAvailable = false;
        ObjectPool<HDCullingResults> m_CullingResultsPool = new ObjectPool<HDCullingResults>((cullResult) => cullResult.Reset(), null, false);

        // RENDER GRAPH
        RenderGraph m_RenderGraph = new RenderGraph("HDRP");

        // MSAA resolve materials
        Material m_ColorResolveMaterial = null;
        Material m_MotionVectorResolve = null;

        internal Material GetMSAAColorResolveMaterial()
        {
            return m_ColorResolveMaterial;
        }

        // Flag that defines if ray tracing is supported by the current asset
        bool m_AssetSupportsRayTracing = false;

        // Flag that defines if ray tracing is supported by the current asset and platform
        // Note: this will include whether resources are available or not because we can be in a state where the asset was not included in the build and so the the resources were stripped. 
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
        /// <param name="obsolete_defaultAsset">Default HDRenderPipelineAsset. [Obsolete]</param>
        public HDRenderPipeline(HDRenderPipelineAsset asset, HDRenderPipelineAsset obsolete_defaultAsset) : this(asset)
        {
        }

        /// <summary>
        /// HDRenderPipeline constructor.
        /// </summary>
        /// <param name="asset">Source HDRenderPipelineAsset.</param>
        public HDRenderPipeline(HDRenderPipelineAsset asset)
        {
            // We need to call this after the resource initialization as we attempt to use them in checking the supported API.
            if (!CheckAPIValidity())
            {
                m_ValidAPI = false;
                return;
            }

#if UNITY_EDITOR
            m_GlobalSettings = HDRenderPipelineGlobalSettings.Ensure();
#else
            m_GlobalSettings = HDRenderPipelineGlobalSettings.instance;
#endif
            m_Asset = asset;
            HDProbeSystem.Parameters = asset.reflectionSystemParameters;

            DebugManager.instance.RefreshEditor();

            m_ValidAPI = true;

            SetRenderingFeatures();

            // Initialize lod settings with the default frame settings. This will pull LoD values from the current quality level HDRP asset if necessary.
            // This will make the LoD Group UI consistent with the scene view camera like it is for builtin pipeline.
            QualitySettings.lodBias = m_GlobalSettings.GetDefaultFrameSettings(FrameSettingsRenderType.Camera).GetResolvedLODBias(m_Asset);
            QualitySettings.maximumLODLevel = m_GlobalSettings.GetDefaultFrameSettings(FrameSettingsRenderType.Camera).GetResolvedMaximumLODLevel(m_Asset);

#if UNITY_EDITOR
            UpgradeResourcesIfNeeded();

            //In case we are loading element in the asset pipeline (occurs when library is not fully constructed) the creation of the HDRenderPipeline is done at a time we cannot access resources.
            //So in this case, the reloader would fail and the resources cannot be validated. So skip validation here.
            //The HDRenderPipeline will be reconstructed in a few frame which will fix this issue.
            if ((m_GlobalSettings.AreRuntimeResourcesCreated() == false)
                || (m_GlobalSettings.AreEditorResourcesCreated() == false)
                || (m_RayTracingSupported && !m_GlobalSettings.AreRayTracingResourcesCreated()))
                return;
            else
                m_ResourcesInitialized = true;

            m_GlobalSettings.EnsureShadersCompiled();
#endif

            // The first thing we need to do is to set the defines that depend on the render pipeline settings
            bool pipelineSupportsRayTracing = PipelineSupportsRayTracing(m_Asset.currentPlatformRenderPipelineSettings);

            m_RayTracingSupported = pipelineSupportsRayTracing && m_GlobalSettings.renderPipelineRayTracingResources != null;

            // In Editor we need to be freely available to select raytracing to create the resources, otherwise we get stuck in a situation in which we cannot create the resources, 
            // hence why the following is done only in player
#if !UNITY_EDITOR
            if (pipelineSupportsRayTracing && !m_RayTracingSupported)
            {
                Debug.LogWarning("The asset supports ray tracing but the ray tracing resources are not included in the build. This can happen if the asset currently in use was not included in any quality setting for the current platform.");
                // We need to modify the pipeline settings here because we use them to sanitize the frame settings.
                m_Asset.TurnOffRayTracing();
            }
#endif

            m_AssetSupportsRayTracing = m_Asset.currentPlatformRenderPipelineSettings.supportRayTracing;

            CheckResourcesValidity();

#if ENABLE_VIRTUALTEXTURES
            VirtualTexturingSettingsSRP settings = asset.virtualTexturingSettings;

            if (settings == null)
                settings = new VirtualTexturingSettingsSRP();

            VirtualTexturing.Streaming.SetCPUCacheSize(settings.streamingCpuCacheSizeInMegaBytes);
            VirtualTexturing.Streaming.EnableMipPreloading(settings.streamingMipPreloadTexturesPerFrame, settings.streamingPreloadMipCount);

            GPUCacheSetting[] gpuCacheSettings = new GPUCacheSetting[settings.streamingGpuCacheSettings.Count];
            for (int i = 0; i < settings.streamingGpuCacheSettings.Count; ++i)
            {
                GPUCacheSettingSRP srpSetting = settings.streamingGpuCacheSettings[i];
                gpuCacheSettings[i] = new GPUCacheSetting() { format = srpSetting.format, sizeInMegaBytes = srpSetting.sizeInMegaBytes };
            }

            VirtualTexturing.Streaming.SetGPUCacheSettings(gpuCacheSettings);

            colorMaskTransparentVel = HDShaderIDs._ColorMaskTransparentVelTwo;
            colorMaskAdditionalTarget = HDShaderIDs._ColorMaskTransparentVelOne;
#else
            colorMaskTransparentVel = HDShaderIDs._ColorMaskTransparentVelOne;
            colorMaskAdditionalTarget = HDShaderIDs._ColorMaskTransparentVelTwo;
#endif

            // Initial state of the RTHandle system.
            // We initialize to screen width/height to avoid multiple realloc that can lead to inflated memory usage (as releasing of memory is delayed).
            RTHandles.Initialize(Screen.width, Screen.height);

            XRSystem.Initialize(XRPass.CreateDefault, asset.renderPipelineResources.shaders.xrOcclusionMeshPS, asset.renderPipelineResources.shaders.xrMirrorViewPS);

            m_MipGenerator = new MipGenerator(defaultResources);
            m_BlueNoise = new BlueNoise(defaultResources);

            EncodeBC6H.DefaultInstance = EncodeBC6H.DefaultInstance ?? new EncodeBC6H(defaultResources.shaders.encodeBC6HCS);

            // Scan material list and assign it
            m_MaterialList = HDUtils.GetRenderPipelineMaterialList();

            InitializePostProcess();

            // Initialize various compute shader resources
            m_SsrTracingKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsTracing");
            m_SsrReprojectionKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsReprojection");
            m_SsrAccumulateNoWorldSpeedRejectionBothKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateNoWorldSpeedRejectionBoth");
            m_SsrAccumulateNoWorldSpeedRejectionSurfaceKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateNoWorldSpeedRejectionSourceOnly");
            m_SsrAccumulateNoWorldSpeedRejectionHitKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateNoWorldSpeedRejectionTargetOnly");
            m_SsrAccumulateHardThresholdSpeedRejectionBothKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateHardThresholdSpeedRejectionBoth");
            m_SsrAccumulateHardThresholdSpeedRejectionSurfaceKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateHardThresholdSpeedRejectionSourceOnly");
            m_SsrAccumulateHardThresholdSpeedRejectionHitKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateHardThresholdSpeedRejectionTargetOnly");
            m_SsrAccumulateSmoothSpeedRejectionBothKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateSmoothSpeedRejectionBoth");
            m_SsrAccumulateSmoothSpeedRejectionSurfaceKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateSmoothSpeedRejectionSourceOnly");
            m_SsrAccumulateSmoothSpeedRejectionHitKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateSmoothSpeedRejectionTargetOnly");

            m_SsrAccumulateNoWorldSpeedRejectionBothDebugKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateNoWorldSpeedRejectionBothDebug");
            m_SsrAccumulateNoWorldSpeedRejectionSurfaceDebugKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateNoWorldSpeedRejectionSourceOnlyDebug");
            m_SsrAccumulateNoWorldSpeedRejectionHitDebugKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateNoWorldSpeedRejectionTargetOnlyDebug");
            m_SsrAccumulateHardThresholdSpeedRejectionBothDebugKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateHardThresholdSpeedRejectionBothDebug");
            m_SsrAccumulateHardThresholdSpeedRejectionSurfaceDebugKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateHardThresholdSpeedRejectionSourceOnlyDebug");
            m_SsrAccumulateHardThresholdSpeedRejectionHitDebugKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateHardThresholdSpeedRejectionTargetOnlyDebug");
            m_SsrAccumulateSmoothSpeedRejectionBothDebugKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateSmoothSpeedRejectionBothDebug");
            m_SsrAccumulateSmoothSpeedRejectionSurfaceDebugKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateSmoothSpeedRejectionSourceOnlyDebug");
            m_SsrAccumulateSmoothSpeedRejectionHitDebugKernel = m_ScreenSpaceReflectionsCS.FindKernel("ScreenSpaceReflectionsAccumulateSmoothSpeedRejectionTargetOnlyDebug");

            m_ClearBuffer2DKernel = m_ClearBuffer2DCS.FindKernel("ClearBuffer2DMain");

            m_CopyDepth = CoreUtils.CreateEngineMaterial(defaultResources.shaders.copyDepthBufferPS);
            m_UpsampleTransparency = CoreUtils.CreateEngineMaterial(defaultResources.shaders.upsampleTransparentPS);

            m_ApplyDistortionMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.applyDistortionPS);

            m_FinalBlitWithOETF = CoreUtils.CreateEngineMaterial(defaultResources.shaders.compositeUIAndOETFApplyPS);

            if (TextureXR.useTexArray)
            {
                m_FinalBlitWithOETFTexArraySingleSlice = CoreUtils.CreateEngineMaterial(defaultResources.shaders.compositeUIAndOETFApplyPS);
                m_FinalBlitWithOETFTexArraySingleSlice.EnableKeyword("BLIT_SINGLE_SLICE");
            }

            m_ClearStencilBufferMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.clearStencilBufferPS);

            InitializeDebug();

            Blitter.Initialize(defaultResources.shaders.blitPS, defaultResources.shaders.blitColorAndDepthPS);

            m_ErrorMaterial = CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader");

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

            bool apvIsEnabled = IsAPVEnabled();
            SupportedRenderingFeatures.active.overridesLightProbeSystem = apvIsEnabled;
            if (apvIsEnabled)
            {
                var pvr = ProbeReferenceVolume.instance;
                ProbeReferenceVolume.instance.Initialize(new ProbeVolumeSystemParameters
                {
                    memoryBudget = m_Asset.currentPlatformRenderPipelineSettings.probeVolumeMemoryBudget,
                    blendingMemoryBudget = m_Asset.currentPlatformRenderPipelineSettings.probeVolumeBlendingMemoryBudget,
                    probeDebugMesh = defaultResources.assets.probeDebugSphere,
                    probeDebugShader = defaultResources.shaders.probeVolumeDebugShader,
                    offsetDebugMesh = defaultResources.assets.pyramidMesh,
                    offsetDebugShader = defaultResources.shaders.probeVolumeOffsetDebugShader,
                    scenarioBlendingShader = defaultResources.shaders.probeVolumeBlendStatesCS,
                    sceneData = m_GlobalSettings.GetOrCreateAPVSceneData(),
                    shBands = m_Asset.currentPlatformRenderPipelineSettings.probeVolumeSHBands,
                    supportsRuntimeDebug = Application.isEditor || m_GlobalSettings.supportRuntimeDebugDisplay,
                    supportStreaming = m_Asset.currentPlatformRenderPipelineSettings.supportProbeVolumeStreaming
                });
                RegisterRetrieveOfProbeVolumeExtraDataAction();
                SupportedRenderingFeatures.active.overridesLightProbeSystemWarningMessage = "This Light Probe system is not active because the pipeline uses Probe Volumes and the systems cannot co-exist.\nTo disable Probe Volumes make sure the feature is disabled in the lighting section of the active HDRP Asset.";
            }

            m_SkyManager.Build(asset, defaultResources, m_IBLFilterArray);

            InitializeVolumetricLighting();
            InitializeVolumetricClouds();
            InitializeSubsurfaceScattering();
            InitializeWaterSystem();

            m_DebugDisplaySettings.RegisterDebug();
            m_DebugDisplaySettingsUI.RegisterDebug(HDDebugDisplaySettings.Instance);
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

            m_CustomPassColorBuffer = new Lazy<RTHandle>(() => RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GetCustomBufferFormat(), enableRandomWrite: true, useDynamicScale: true, name: "CustomPassColorBuffer"));
            m_CustomPassDepthBuffer = new Lazy<RTHandle>(() => RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, colorFormat: GraphicsFormat.R32_UInt, useDynamicScale: true, name: "CustomPassDepthBuffer", depthBufferBits: DepthBits.Depth32));

            // For debugging
            MousePositionDebug.instance.Build();

            InitializeRenderStateBlocks();

            if (m_RayTracingSupported)
            {
                InitRayTracingManager();
                InitRayTracedReflections();
                InitRayTracedIndirectDiffuse();
                InitRaytracingDeferred();
                InitRecursiveRenderer();
                InitPathTracing();
                InitRayTracingAmbientOcclusion();
            }

            // Initialize the SSGI structures
            InitScreenSpaceGlobalIllumination();

            // Initialize screen space shadows
            InitializeScreenSpaceShadows();

            CameraCaptureBridge.enabled = true;

            InitializePrepass(m_Asset);
            m_ColorResolveMaterial = CoreUtils.CreateEngineMaterial(m_GlobalSettings.renderPipelineResources.shaders.colorResolvePS);
            m_MotionVectorResolve = CoreUtils.CreateEngineMaterial(m_GlobalSettings.renderPipelineResources.shaders.resolveMotionVecPS);

            CustomPassUtils.Initialize();

            LensFlareCommonSRP.Initialize();

            Hammersley.Initialize();
            DecalSystem.instance.Initialize();
        }

#if UNITY_EDITOR
        void UpgradeResourcesIfNeeded()
        {
            // Check that the serialized Resources are not broken
            m_GlobalSettings.EnsureRuntimeResources(forceReload: true);
            m_GlobalSettings.EnsureEditorResources(forceReload: true);

            // Make sure to include ray-tracing resources if at least one of the defaultAsset or quality levels needs it
            bool requiresRayTracingResources = m_Asset.currentPlatformRenderPipelineSettings.supportRayTracing;

            // Make sure to include ray-tracing resources if at least one of the quality levels needs it
            int qualityLevelCount = QualitySettings.names.Length;
            for (int i = 0; i < qualityLevelCount && !requiresRayTracingResources; ++i)
            {
                var hdrpAsset = QualitySettings.GetRenderPipelineAssetAt(i) as HDRenderPipelineAsset;
                if (hdrpAsset != null && hdrpAsset.currentPlatformRenderPipelineSettings.supportRayTracing)
                    requiresRayTracingResources = true;
            }

            // If ray tracing is not enabled we do not want to have ray tracing resources referenced
            if (requiresRayTracingResources)
                m_GlobalSettings.EnsureRayTracingResources(forceReload: true);
            else
                m_GlobalSettings.ClearRayTracingResources();
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
            Shader.globalRenderPipeline = k_ShaderTagName;

            // HD use specific GraphicsSettings
            m_PreviousLightsUseLinearIntensity = GraphicsSettings.lightsUseLinearIntensity;
            GraphicsSettings.lightsUseLinearIntensity = true;
            m_PreviousLightsUseColorTemperature = GraphicsSettings.lightsUseColorTemperature;
            GraphicsSettings.lightsUseColorTemperature = true;
            m_PreviousSRPBatcher = GraphicsSettings.useScriptableRenderPipelineBatching;
            GraphicsSettings.useScriptableRenderPipelineBatching = m_Asset.enableSRPBatcher;
#if UNITY_2020_2_OR_NEWER
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
                editableMaterialRenderQueue = false,
                enlighten = true
                ,
                overridesLODBias = true
                ,
                overridesMaximumLODLevel = true
                ,
                overridesShadowmask = true // Don't display the shadow mask UI in Quality Settings
                ,
                overridesRealtimeReflectionProbes = true // Don't display the real time reflection probes checkbox UI in Quality Settings
                ,
                autoAmbientProbeBaking = false
                ,
                autoDefaultReflectionProbeBaking = false
                ,
                #pragma warning disable 618
                enlightenLightmapper = false
                #pragma warning restore 618
                ,
                rendersUIOverlay = true,
                supportsHDR = true
            };

            Lightmapping.SetDelegate(GlobalIlluminationUtils.hdLightsDelegate);

#if UNITY_EDITOR
            // HDRP always enable baking of cookie by default
            m_PreviousEnableCookiesInLightmapper = UnityEditor.EditorSettings.enableCookiesInLightmapper;
            UnityEditor.EditorSettings.enableCookiesInLightmapper = true;

            SceneViewDrawMode.SetupDrawMode();

            if (UnityEditor.PlayerSettings.colorSpace == ColorSpace.Gamma)
            {
                Debug.LogError("High Definition Render Pipeline doesn't support Gamma mode, change to Linear mode (HDRP isn't set up properly. Go to Window > Rendering > HDRP Wizard to fix your settings).");
            }
#endif

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            m_DebugDisplaySettings.nvidiaDebugView.Reset();
#endif
            HDRenderPipeline.SetupDLSSFeature(m_GlobalSettings);
        }

        internal static void SetupDLSSFeature(HDRenderPipelineGlobalSettings globalSettings)
        {
            if (globalSettings == null)
            {
                Debug.LogError("Tried to setup DLSS with a null globalSettings object.");
                return;
            }

            if (DLSSPass.SetupFeature(globalSettings))
            {
                HDDynamicResolutionPlatformCapabilities.ActivateDLSS();
            }
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

        bool CheckResourcesValidity()
        {
            if (!(defaultResources?.shaders.defaultPS?.isSupported ?? true))
            {
                HDUtils.DisplayMessageNotification("Unable to compile Default Material based on Lit.shader. Either there is a compile error in Lit.shader or the current platform / API isn't compatible.");
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

#if UNITY_EDITOR
            UnityEditor.BuildTarget activeBuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            return HDUtils.IsSupportedBuildTargetAndDevice(activeBuildTarget, out unsupportedGraphicDevice);
#else
            return HDUtils.IsSupportedGraphicDevice(SystemInfo.graphicsDeviceType) && HDUtils.IsOperatingSystemSupported(SystemInfo.operatingSystem);
#endif
        }

        void UnsetRenderingFeatures()
        {
            Shader.globalRenderPipeline = string.Empty;

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

        void CleanupRenderGraph()
        {
            m_RenderGraph.Cleanup();
            m_RenderGraph = null;
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
#endif
            {
                m_ProbeCameraCache.Dispose();
                m_ProbeCameraCache = null;
            }
        }

        /// <summary>
        /// Disposable pattern implementation.
        /// </summary>
        /// <param name="disposing">Is disposing.</param>
        protected override void Dispose(bool disposing)
        {
            Graphics.ClearRandomWriteTargets();
            Graphics.SetRenderTarget(null);
            DisposeProbeCameraPool();

            UnsetRenderingFeatures();

            if (!m_ValidAPI)
                return;

#if UNITY_EDITOR
            if (!m_ResourcesInitialized)
                return;
#endif

            base.Dispose(disposing);

            HDLightRenderDatabase.instance.Cleanup();
            ReleaseScreenSpaceShadows();

            if (m_RayTracingSupported)
            {
                ReleaseRayTracingDeferred();
                ReleaseRayTracedIndirectDiffuse();
                ReleasePathTracing();
            }

            ReleaseRayTracingManager();
            m_DebugDisplaySettingsUI.UnregisterDebug();
            m_DebugDisplaySettings.UnregisterDebug();

            CleanupLightLoop();

            ReleaseVolumetricClouds();
            CleanupSubsurfaceScattering();
            ReleaseWaterSystem();

            // For debugging
            MousePositionDebug.instance.Cleanup();

            DecalSystem.instance.Cleanup();

            CoreUtils.SafeRelease(m_EmptyIndexBuffer);
            m_EmptyIndexBuffer = null;

            m_MaterialList.ForEach(material => material.Cleanup());

            CleanupDebug();

            Blitter.Cleanup();

            CoreUtils.Destroy(m_CopyDepth);
            CoreUtils.Destroy(m_ErrorMaterial);
            CoreUtils.Destroy(m_UpsampleTransparency);
            CoreUtils.Destroy(m_ApplyDistortionMaterial);
            CoreUtils.Destroy(m_ClearStencilBufferMaterial);
            CoreUtils.Destroy(m_FinalBlitWithOETF);
            CoreUtils.Destroy(m_FinalBlitWithOETFTexArraySingleSlice);

            XRSystem.Dispose();
            m_SkyManager.Cleanup();
            CleanupVolumetricLighting();

            for (int bsdfIdx = 0; bsdfIdx < m_IBLFilterArray.Length; ++bsdfIdx)
            {
                m_IBLFilterArray[bsdfIdx].Cleanup();
            }

            CleanupPostProcess();
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
            CustomPostProcessVolumeComponent.CleanupAllCustomPostProcesses();

            CleanupPrepass();
            CoreUtils.Destroy(m_ColorResolveMaterial);
            CoreUtils.Destroy(m_MotionVectorResolve);

            LensFlareCommonSRP.Dispose();

            CustomPassUtils.Cleanup();
#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();

            // Do not attempt to unregister SceneView FrameSettings. It is shared amongst every scene view and take only a little place.
            // For removing it, you should be sure that Dispose could never be called after the constructor of another instance of this SRP.
            // Also, at the moment, applying change to hdrpAsset cause the SRP to be Disposed and Constructed again.
            // Not always in that order.
#endif

            if (IsAPVEnabled())
            {
                ProbeReferenceVolume.instance.Cleanup();
            }

            CleanupRenderGraph();

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
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.UpdateGlobalConstantBuffers)))
            {
                UpdateShaderVariablesGlobalCB(hdCamera, cmd);
                UpdateShaderVariablesXRCB(hdCamera, cmd);
                UpdateShaderVariablesRaytracingCB(hdCamera, cmd);

                // This one is not in a constant buffer because it's only used as a parameter for some shader's render states. It's not actually used inside shader code.
                cmd.SetGlobalInt(colorMaskTransparentVel, (int)ColorWriteMask.All);
                cmd.SetGlobalInt(colorMaskAdditionalTarget, (int)ColorWriteMask.All);
            }
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
            UpdateShaderVariablesProbeVolumes(ref m_ShaderVariablesGlobalCB, hdCamera, cmd);
            UpdateShaderVariableGlobalAmbientOcclusion(ref m_ShaderVariablesGlobalCB, hdCamera);

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
            m_ShaderVariablesGlobalCB._ReflectionsMode = (int)GetReflectionsMode(hdCamera);

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
            {
                // Check if recursive rendering is enabled or not. This will control the cull of primitive
                // during the gbuffer and forward pass
                ScreenSpaceReflection settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
                bool enableRaytracedReflections = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && ScreenSpaceReflection.RayTracingActive(settings);
                m_ShaderVariablesGlobalCB._EnableRayTracedReflections = enableRaytracedReflections ? 1 : 0;
                RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
                // Here we cannot test against the light cluster as it is not build yet but for now there is no case where it shouldnt be valid
                m_ShaderVariablesGlobalCB._EnableRecursiveRayTracing = recursiveSettings.enable.value && GetRayTracingState() ? 1u : 0u;

                m_ShaderVariablesGlobalCB._SpecularOcclusionBlend = EvaluateSpecularOcclusionFlag(hdCamera);
            }
            else
            {
                m_ShaderVariablesGlobalCB._EnableRayTracedReflections = 0;
                m_ShaderVariablesGlobalCB._EnableRecursiveRayTracing = 0;
                m_ShaderVariablesGlobalCB._SpecularOcclusionBlend = 1.0f;
            }
            m_ShaderVariablesGlobalCB._ColorPyramidUvScaleAndLimitPrevFrame = HDUtils.ComputeViewportScaleAndLimit(hdCamera.historyRTHandleProperties.previousViewportSize, hdCamera.historyRTHandleProperties.previousRenderTargetSize);

            ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
        }

        void UpdateShaderVariablesXRCB(HDCamera hdCamera, CommandBuffer cmd)
        {
            hdCamera.PushBuiltinShaderConstantsXR(cmd);
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
            m_ShaderVariablesRayTracingCB._RayTracingRayBias = rayTracingSettings.rayBias.value;
            m_ShaderVariablesRayTracingCB._RayTracingDistantRayBias = rayTracingSettings.distantRayBias.value;
            m_ShaderVariablesRayTracingCB._RayCountEnabled = m_RayCountManager.RayCountIsEnabled();
            m_ShaderVariablesRayTracingCB._RaytracingCameraNearPlane = hdCamera.camera.nearClipPlane;
            m_ShaderVariablesRayTracingCB._RaytracingPixelSpreadAngle = GetPixelSpreadAngle(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);
            m_ShaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = screenSpaceReflection.minSmoothness;
            m_ShaderVariablesRayTracingCB._RaytracingReflectionSmoothnessFadeStart = screenSpaceReflection.smoothnessFadeStart;
            m_ShaderVariablesRayTracingCB._DirectionalShadowFallbackIntensity = rayTracingSettings.directionalShadowFallbackIntensity.value;
            m_ShaderVariablesRayTracingCB._RayTracingLodBias = 0;
            ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);
        }

        void ConfigureKeywords(bool enableBakeShadowMask, HDCamera hdCamera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ConfigureKeywords)))
            {
                bool enableAPV = IsAPVEnabled();

                // Globally enable (for GBuffer shader and forward lit (opaque and transparent) the keyword SHADOWS_SHADOWMASK
                CoreUtils.SetKeyword(cmd, "SHADOWS_SHADOWMASK", enableBakeShadowMask);
                // Configure material to use depends on shadow mask option
                m_CurrentRendererConfigurationBakedLighting = HDUtils.GetRendererConfiguration(enableAPV, enableBakeShadowMask);
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

                CoreUtils.SetKeyword(cmd, "PROBE_VOLUMES_OFF", !enableAPV);
                CoreUtils.SetKeyword(cmd, "PROBE_VOLUMES_L1", enableAPV && m_Asset.currentPlatformRenderPipelineSettings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL1);
                CoreUtils.SetKeyword(cmd, "PROBE_VOLUMES_L2", enableAPV && m_Asset.currentPlatformRenderPipelineSettings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2);

                // Raise the normal buffer flag only if we are in forward rendering
                CoreUtils.SetKeyword(cmd, "WRITE_NORMAL_BUFFER", hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward);

                // Raise the decal buffer flag only if we have decal enabled
                CoreUtils.SetKeyword(cmd, "WRITE_DECAL_BUFFER", hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers));

                // Raise or remove the depth msaa flag based on the frame setting
                CoreUtils.SetKeyword(cmd, "WRITE_MSAA_DEPTH", hdCamera.msaaEnabled);

                CoreUtils.SetKeyword(cmd, "SCREEN_COORD_OVERRIDE", hdCamera.frameSettings.IsEnabled(FrameSettingsField.ScreenCoordOverride));
            }
        }

        void SetupDLSSForCameraDataAndDynamicResHandler(
            in HDAdditionalCameraData hdCam,
            Camera camera,
            XRPass xrPass,
            bool cameraRequestedDynamicRes,
            ref GlobalDynamicResolutionSettings outDrsSettings)
        {
            if (hdCam == null)
                return;

            hdCam.cameraCanRenderDLSS = cameraRequestedDynamicRes
                && HDDynamicResolutionPlatformCapabilities.DLSSDetected
                && hdCam.allowDeepLearningSuperSampling
                && m_Asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.enableDLSS
                && m_Asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.enabled;

            if (m_DLSSPass != null && hdCam.cameraCanRenderDLSS)
            {
                bool useOptimalSettings = hdCam.deepLearningSuperSamplingUseCustomAttributes
                    ? hdCam.deepLearningSuperSamplingUseOptimalSettings
                    : m_Asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.DLSSUseOptimalSettings;
                m_DLSSPass.SetupDRSScaling(useOptimalSettings, camera, xrPass, ref outDrsSettings);
            }
        }

        struct RenderRequest
        {
            public struct Target
            {
                public RenderTargetIdentifier id;
                public CubemapFace face;
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
            public bool cullingResultIsShared;
        }

        private void VisitRenderRequestRecursive(List<RenderRequest> requests, List<int> visitStatus, int requestIndex, List<int> renderIndices)
        {
            if (visitStatus[requestIndex] == 1)
                throw new Exception("Cycle in render request dependencies!");
            if (visitStatus[requestIndex] != 0)
                return;

            // mark as visiting, iterate dependencies
            visitStatus[requestIndex] = 1;
            foreach (int dependsOnRequestIndex in requests[requestIndex].dependsOnRenderRequestIndices)
                VisitRenderRequestRecursive(requests, visitStatus, dependsOnRequestIndex, renderIndices);

            // dependencies are done, so mark visited and add to render order
            visitStatus[requestIndex] = 2;
            renderIndices.Add(requestIndex);
        }

        private void FlattenRenderRequestGraph(List<RenderRequest> requests, List<int> renderIndices)
        {
            using (ListPool<int>.Get(out List<int> visitStatus))
            {
                // mark everything as "not visited"
                for (int i = 0; i < requests.Count; ++i)
                    visitStatus.Add(0);

                // iterate in request order (recursively visits dependencies first)
                for (int i = 0; i < requests.Count; ++i)
                    VisitRenderRequestRecursive(requests, visitStatus, i, renderIndices);
            }
        }

        class HDCullingResults
        {
            public CullingResults cullingResults;
            public CullingResults uiCullingResults;
            public CullingResults? customPassCullingResults;
            public HDProbeCullingResults hdProbeCullingResults = new HDProbeCullingResults();
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

        bool PrepareAndCullCamera(Camera camera, XRPass xrPass, bool cameraRequestedDynamicRes,
            List<RenderRequest> renderRequests,
            ScriptableRenderContext renderContext,
            out RenderRequest renderRequest,
            CubemapFace cubemapFace = CubemapFace.Unknown)
        {
            renderRequest = default(RenderRequest);

            // Try to compute the parameters of the request or skip the request
            var skipRequest = !TryCalculateFrameParameters(
                camera,
                xrPass,
                out var additionalCameraData,
                out var hdCamera,
                out var cullingParameters);

            var cullingResults = m_CullingResultsPool.Get();

            // Note: In case of a custom render, we have false here and 'TryCull' is not executed
            bool cullingResultIsShared = false;
            if (!skipRequest)
            {

                VFXCameraXRSettings cameraXRSettings;
                cameraXRSettings.viewTotal = hdCamera.xr.enabled ? 2U : 1U;
                cameraXRSettings.viewCount = (uint)hdCamera.viewCount;
                cameraXRSettings.viewOffset = (uint)hdCamera.xr.multipassId;

                VFXManager.PrepareCamera(camera, cameraXRSettings);

                var needCulling = true;

                // In XR multipass, culling results can be shared if the pass has the same culling id
                if (xrPass.multipassId > 0)
                {
                    foreach (var req in renderRequests)
                    {
                        if (camera == req.hdCamera.camera && req.hdCamera.xr.cullingPassId == xrPass.cullingPassId)
                        {
                            m_CullingResultsPool.Release(cullingResults);
                            cullingResults = req.cullingResults;
                            cullingResultIsShared = true;
                            needCulling = false;
                            m_SkyManager.UpdateCurrentSkySettings(hdCamera);
                        }
                    }

                    // Skip request for the second pass: culling the same camera twice in a row would crash the editor/player.
                    // https://jira.unity3d.com/browse/UUM-41447
                    if (needCulling == true && m_ActiveTerrains.Count > 0)
                    {
                        Debug.LogWarning("The current XR provider does not support rendering Terrain under the XR multipass rendering mode. Please set the XR render mode to single pass or multi-view in the XR provider settings.");
                        needCulling = false;
                        skipRequest = true;
                    }
                }

                if (needCulling)
                {
                    skipRequest = !TryCull(camera, hdCamera, renderContext, m_SkyManager, cullingParameters, m_Asset, ref cullingResults);
                }
            }

            if (additionalCameraData.hasCustomRender && additionalCameraData.fullscreenPassthrough)
            {
                Debug.LogWarning("HDRP Camera custom render is not supported when Fullscreen Passthrough is enabled. Please either disable Fullscreen Passthrough in the camera settings or remove all customRender callbacks attached to this camera.");
                return false;
            }

            if (additionalCameraData != null && additionalCameraData.hasCustomRender)
            {
                skipRequest = true;
                // First prepare the global constant buffer for users (Only camera properties)
                hdCamera.UpdateShaderVariablesGlobalCB(ref m_ShaderVariablesGlobalCB);
                ConstantBuffer.PushGlobal(m_ShaderVariablesGlobalCB, HDShaderIDs._ShaderVariablesGlobal);
                // Execute custom render
                BeginCameraRendering(renderContext, camera);
                additionalCameraData.ExecuteCustomRender(renderContext, hdCamera);
            }

            if (skipRequest)
            {
                // Submit render context and free pooled resources for this request
                renderContext.Submit();
                m_CullingResultsPool.Release(cullingResults);
                UnityEngine.Rendering.RenderPipeline.EndCameraRendering(renderContext, camera);
                return false;
            }

            // Select render target
            RenderTargetIdentifier targetId = camera.targetTexture ?? new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            if (camera.targetTexture != null)
            {
                camera.targetTexture.IncrementUpdateCount(); // Necessary if the texture is used as a cookie.
            }

            // Render directly to XR render target if active
            if (hdCamera.xr.enabled)
                targetId = hdCamera.xr.renderTarget;

            hdCamera.RequestDynamicResolution(cameraRequestedDynamicRes, DynamicResolutionHandler.instance);

            // Add render request
            renderRequest = new RenderRequest
            {
                hdCamera = hdCamera,
                cullingResults = cullingResults,
                target = new RenderRequest.Target
                {
                    id = targetId,
                    face = cubemapFace
                },
                dependsOnRenderRequestIndices = ListPool<int>.Get(),
                index = renderRequests.Count,
                cameraSettings = CameraSettings.From(hdCamera),
                viewDependentProbesData = ListPool<(HDProbe.RenderData, HDProbe)>.Get(),
                cullingResultIsShared = cullingResultIsShared
                // TODO: store DecalCullResult
            };
            renderRequests.Add(renderRequest);

            return true;
        }

        void DetermineVisibleProbesForRequest(in RenderRequest request, Dictionary<HDProbe, List<(int index, float weight)>> renderRequestIndicesWhereTheProbeIsVisible)
        {
            var cullingResults = request.cullingResults;
            // Add visible probes to list
            // Reflection probes
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

                AddVisibleProbeVisibleIndexIfUpdateIsRequired(additionalReflectionData, request, renderRequestIndicesWhereTheProbeIsVisible);
            }

            // Planar probes
            for (var i = 0; i < cullingResults.hdProbeCullingResults.visibleProbes.Count; ++i)
            {
                AddVisibleProbeVisibleIndexIfUpdateIsRequired(cullingResults.hdProbeCullingResults.visibleProbes[i], request, renderRequestIndicesWhereTheProbeIsVisible);
            }
        }

        void AddVisibleProbeVisibleIndexIfUpdateIsRequired(HDProbe probe, in RenderRequest request, Dictionary<HDProbe, List<(int index, float weight)>> renderRequestIndicesWhereTheProbeIsVisible)
        {
            // Don't add it if it has already been updated this frame or not a real time probe
            // TODO: discard probes that are baked once per frame and already baked this frame
            if (!probe.requiresRealtimeUpdate)
                return;

            var viewerTransform = request.hdCamera.camera.transform;
            float visibility = HDUtils.ComputeWeightedLinearFadeDistance(probe.transform.position, viewerTransform.position, probe.weight, probe.fadeDistance);

            // Notify that we render the probe at this frame
            // Also, we need to set the probe as rendered only if we'll actually render it and this won't happen if visibility is not > 0.
            if (visibility > 0.0f)
                probe.SetIsRendered();

            if (!renderRequestIndicesWhereTheProbeIsVisible.TryGetValue(probe, out var visibleInIndices))
            {
                visibleInIndices = ListPool<(int index, float weight)>.Get();
                renderRequestIndicesWhereTheProbeIsVisible.Add(probe, visibleInIndices);
            }

            if (!visibleInIndices.Contains((request.index, visibility)))
            {
                visibleInIndices.Add((request.index, visibility));
            }
        }

        void AddHDProbeRenderRequests(
            HDProbe visibleProbe,
            Transform viewerTransform,
            List<(int index, float weight)> visibilities,
            ulong overrideSceneCullingMask,
            HDCamera hdParentCamera,
            float referenceFieldOfView,
            float referenceAspect,
            ref List<HDProbe.RenderData> renderDatas,
            List<CameraSettings> cameraSettings,
            List<CameraPositionSettings> cameraPositionSettings,
            List<CubemapFace> cameraCubemapFaces,
            List<RenderRequest> renderRequests,
            ScriptableRenderContext renderContext
        )
        {
            var renderSteps = visibleProbe.NextRenderSteps();

            var position = ProbeCapturePositionSettings.ComputeFrom(
                visibleProbe,
                viewerTransform
            );
            cameraSettings.Clear();
            cameraPositionSettings.Clear();
            cameraCubemapFaces.Clear();
            HDRenderUtilities.GenerateRenderingSettingsFor(
                visibleProbe.settings, position,
                cameraSettings, cameraPositionSettings, cameraCubemapFaces, overrideSceneCullingMask, renderSteps,
                referenceFieldOfView: referenceFieldOfView,
                referenceAspect: referenceAspect
            );

            var probeFormat = (GraphicsFormat)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionProbeFormat;

            switch (visibleProbe.type)
            {
                case ProbeSettings.ProbeType.ReflectionProbe:
                    int desiredProbeSize = (int)visibleProbe.cubeResolution;
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

            ProbeRenderSteps skippedRenderSteps = ProbeRenderSteps.None;
            for (int j = 0; j < cameraSettings.Count; ++j)
            {
                CubemapFace face = cameraCubemapFaces[j];
                var camera = m_ProbeCameraCache.GetOrCreate((viewerTransform, visibleProbe, face), Time.frameCount);

                var settingsCopy = m_Asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings;
                settingsCopy.forcedPercentage = 100.0f;
                settingsCopy.forceResolution = true;
                DynamicResolutionHandler.UpdateAndUseCamera(camera, settingsCopy);

                foreach (var terrain in m_ActiveTerrains)
                    terrain.SetKeepUnusedCameraRenderingResources(camera.GetInstanceID(), true);

                if (!camera.TryGetComponent<HDAdditionalCameraData>(out var additionalCameraData))
                {
                    additionalCameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
                    additionalCameraData.hasPersistentHistory = true;
                    additionalCameraData.clearDepth = true;
                }

                // We need to set a targetTexture with the right otherwise when setting pixelRect, it will be rescaled internally to the size of the screen
                camera.targetTexture = visibleProbe.realtimeTexture;

                // Warning: accessing Object.name generate 48B of garbage at each frame here
                // camera.name = HDUtils.ComputeProbeCameraName(visibleProbe.name, j, viewerTransform?.name);
                // Non Alloc version of ComputeProbeCameraName but without the viewerTransform name part
                camera.name = visibleProbe.probeName[j];

                camera.ApplySettings(cameraSettings[j]);
                camera.ApplySettings(cameraPositionSettings[j]);
                camera.pixelRect = new Rect(0, 0, visibleProbe.realtimeTexture.width, visibleProbe.realtimeTexture.height);

                var _cullingResults = m_CullingResultsPool.Get();
                _cullingResults.Reset();

                if (!(TryCalculateFrameParameters(
                    camera,
                    XRSystem.emptyPass,
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
                    m_CullingResultsPool.Release(_cullingResults);
                    skippedRenderSteps |= ProbeRenderStepsExt.FromCubeFace(face);
                    continue;
                }

                bool useFetchedGpuExposure = false;
                float fetchedGpuExposure = 1.0f;

                if (visibleProbe.type == ProbeSettings.ProbeType.PlanarProbe)
                {
                    //cache the resolved settings. Otherwise if we use the internal probe settings, it will be the wrong resolved result.
                    visibleProbe.ExposureControlEnabled = hdCamera.exposureControlFS;
                    if (visibleProbe.ExposureControlEnabled)
                    {
                        RTHandle exposureTexture = GetExposureTexture(hdParentCamera);
                        hdParentCamera.RequestGpuExposureValue(exposureTexture);
                        fetchedGpuExposure = hdParentCamera.GpuExposureValue();
                        visibleProbe.SetProbeExposureValue(fetchedGpuExposure);
                        additionalCameraData.deExposureMultiplier = 1.0f;

                        // If the planar is under exposure control, all the pixels will be de-exposed, for the other skies it is handeled in a shader.
                        // For the clear color, we need to do it manually here.
                        additionalCameraData.backgroundColorHDR = additionalCameraData.backgroundColorHDR * visibleProbe.ProbeExposureValue();
                    }
                    else
                    {
                        //the de-exposure multiplier must be used for anything rendering flatly, for example UI or Unlit.
                        //this will cause them to blow up, but will match the standard nomralized exposure.
                        hdParentCamera.RequestGpuDeExposureValue(GetExposureTextureHandle(hdParentCamera.currentExposureTextures.previous));
                        visibleProbe.SetProbeExposureValue(fetchedGpuExposure);
                        additionalCameraData.deExposureMultiplier = 1.0f / hdParentCamera.GpuDeExposureValue();
                    }

                    // Make sure that the volumetric cloud animation data is in sync with the parent camera.
                    useFetchedGpuExposure = true;
                }
                else
                {
                    hdCamera.realtimeReflectionProbe = (visibleProbe.mode == ProbeSettings.Mode.Realtime);
                }

                hdCamera.SetParentCamera(hdParentCamera, useFetchedGpuExposure, fetchedGpuExposure); // Used to inherit the properties of the view

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

                if (face != CubemapFace.Unknown)
                {
                    request.target = new RenderRequest.Target
                    {
                        id = visibleProbe.realtimeTextureRTH,
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

                // HACK! We render the probe until we know the ambient probe for the associated sky context is ready.
                // For one-off rendering the dynamic ambient probe will be set to black until they are not processed, leading to faulty rendering.
                // So we enqueue another rendering and then we will not set the probe texture until we have rendered with valid ambient probe.
                if (!m_SkyManager.HasSetValidAmbientProbe(hdCamera))
                {
                    skippedRenderSteps |= ProbeRenderStepsExt.FromCubeFace(face);
                }

                renderRequests.Add(request);


                foreach (var visibility in visibilities)
                    renderRequests[visibility.index].dependsOnRenderRequestIndices.Add(request.index);
            }

            // NOTE: If the probe was rendered on the very first frame, we could have some data that was used and it wasn't in a fully initialized state, which is fine on PC, but on console
            // might lead to NaNs due to lack of complete initialization. To circumvent this, we force the probe to render again only if it was rendered on the first frame. Note that the problem
            // doesn't apply if probe is enable any frame other than the very first. Also note that we are likely to be re-rendering the probe anyway due to the issue on sky ambient probe
            // (see m_SkyManager.HasSetValidAmbientProbe in this function).
            if (m_FrameCount <= 1)
            {
                // say we skipped everything, will redo next frame (handled by next block)
                skippedRenderSteps = renderSteps;
            }

            // update the render count (to update the cache) only if nothing was skipped, and ensure we repeat any skipped work next time
            if (renderSteps.HasFlag(ProbeRenderSteps.IncrementRenderCount))
            {
                if (skippedRenderSteps.IsNone())
                    visibleProbe.IncrementRealtimeRenderCount();
                else
                    skippedRenderSteps |= ProbeRenderSteps.IncrementRenderCount;
            }
            visibleProbe.RepeatRenderSteps(skippedRenderSteps);
        }

        static List<(int index, float weight)> s_TempGenerateProbeRenderRequestsList = new List<(int index, float weight)>();

        void GenerateProbeRenderRequests(
            Dictionary<HDProbe, List<(int index, float weight)>> renderRequestIndicesWhereTheProbeIsVisible,
            List<RenderRequest> renderRequests,
            List<CameraSettings> cameraSettings,
            List<CameraPositionSettings> cameraPositionSettings,
            List<CubemapFace> cameraCubemapFaces,
            ScriptableRenderContext renderContext)
        {
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

                        s_TempGenerateProbeRenderRequestsList.Clear();
                        s_TempGenerateProbeRenderRequestsList.Add(visibility);

                        AddHDProbeRenderRequests(
                            visibleProbe,
                            viewerTransform,
                            s_TempGenerateProbeRenderRequestsList,
                            HDUtils.GetSceneCullingMaskFromCamera(visibleInRenderRequest.hdCamera.camera),
                            hdParentCamera,
                            visibleInRenderRequest.hdCamera.camera.fieldOfView,
                            visibleInRenderRequest.hdCamera.camera.aspect,
                            ref renderDatas, cameraSettings, cameraPositionSettings, cameraCubemapFaces, renderRequests, renderContext
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
                    // No single parent camera for view independent probes.
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
                        AddHDProbeRenderRequests(visibleProbe, null, visibilities, 0, hdParentCamera, referenceFieldOfView: 90, referenceAspect: 1, ref renderDatas,
                            cameraSettings, cameraPositionSettings, cameraCubemapFaces, renderRequests, renderContext);
                        ListPool<HDProbe.RenderData>.Release(renderDatas);
                    }
                }
            }
        }

        void ExecuteAOVRenderRequests(in RenderRequest renderRequest, CommandBuffer cmd, ScriptableRenderContext renderContext)
        {
            // var aovRequestIndex = 0;
            foreach (var aovRequest in renderRequest.hdCamera.aovRequests)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.HDRenderPipelineRenderAOV)))
                {
                    // Before rendering the AOV, bind the correct history buffers
                    var aovHistory = renderRequest.hdCamera.GetHistoryRTHandleSystem(aovRequest);
                    renderRequest.hdCamera.BindHistoryRTHandleSystem(aovHistory);
                    cmd.SetInvertCulling(renderRequest.cameraSettings.invertFaceCulling);
                    ExecuteRenderRequest(renderRequest, renderContext, cmd, aovRequest);
                    cmd.SetInvertCulling(false);
                }
                renderContext.ExecuteCommandBuffer(cmd);
                renderContext.Submit();
                cmd.Clear();
            }
        }

        void EndRenderRequest(in RenderRequest renderRequest, CommandBuffer cmd)
        {
            // release reference because the RenderTexture might be destroyed before the camera
            if (renderRequest.clearCameraSettings)
                renderRequest.hdCamera.camera.targetTexture = null;

            ListPool<int>.Release(renderRequest.dependsOnRenderRequestIndices);
            ListPool<(HDProbe.RenderData, HDProbe)>.Release(renderRequest.viewDependentProbesData);

            // Culling results can be shared between render requests: clear only when required
            if (!renderRequest.cullingResultIsShared)
            {
                renderRequest.cullingResults.decalCullResults?.Clear();
                m_CullingResultsPool.Release(renderRequest.cullingResults);
            }
        }

#if UNITY_2021_1_OR_NEWER
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            Render(renderContext, new List<Camera>(cameras));
        }

#endif

#if UNITY_2021_1_OR_NEWER
        // Only for internal use, outside of SRP people can call Camera.Render()
        internal void InternalRender(ScriptableRenderContext renderContext, List<Camera> cameras)
        {
            Render(renderContext, cameras);
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
            // Build target can change in editor so we need to check if the target is supported
            if (!HDUtils.IsSupportedBuildTarget(UnityEditor.EditorUserBuildSettings.activeBuildTarget))
                return;

            if (!m_ResourcesInitialized)
                return;
#endif

#if UNITY_2021_1_OR_NEWER
            int cameraCount = cameras.Count;
#else
            int cameraCount = cameras.Length;
#endif

            AdjustUIOverlayOwnership(cameraCount);

            if (!m_ValidAPI || cameraCount == 0)
                return;

#if UNITY_EDITOR
            // We do not want to start rendering if HDRP global settings are not ready (m_globalSettings is null)
            // or been deleted/moved (m_globalSettings is not necessarily null)
            if (m_GlobalSettings == null || HDRenderPipelineGlobalSettings.instance == null)
            {
                m_GlobalSettings = HDRenderPipelineGlobalSettings.Ensure();
                m_GlobalSettings.EnsureShadersCompiled();
                return;
            }
#endif
            m_GlobalSettings.GetOrCreateDefaultVolume();

            if (m_GlobalSettings.lensAttenuationMode == LensAttenuationMode.ImperfectLens)
            {
                ColorUtils.s_LensAttenuation = 0.65f;
            }
            else if (m_GlobalSettings.lensAttenuationMode == LensAttenuationMode.PerfectLens)
            {
                ColorUtils.s_LensAttenuation = 0.78f;
            }

            DecalSystem.instance.StartDecalUpdateJobs();

            // This function should be called once every render (once for all camera)
            LightLoopNewRender();

#if UNITY_2021_1_OR_NEWER
            BeginContextRendering(renderContext, cameras);
#else
            BeginFrameRendering(renderContext, cameras);
#endif

            // Check if we can speed up FrameSettings process by skipping history
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
                m_ProbeCameraCache.ReleaseCamerasUnusedFor(2, Time.frameCount);
                HDCamera.CleanUnused();

            }

            if (m_Asset.currentPlatformRenderPipelineSettings.supportWater)
            {
                // Update the water surfaces
                var commandBuffer = CommandBufferPool.Get("");
                UpdateWaterSurfaces(commandBuffer);
                renderContext.ExecuteCommandBuffer(commandBuffer);
                renderContext.Submit();
                commandBuffer.Clear();
                CommandBufferPool.Release(commandBuffer);
            }


#if DEVELOPMENT_BUILD || UNITY_EDITOR

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            m_DebugDisplaySettings.nvidiaDebugView.Update();
#endif

            if (DebugManager.instance.isAnyDebugUIActive)
                m_DebugDisplaySettings.debugFrameTiming.UpdateFrameTiming();

            // This is for texture streaming
            m_DebugDisplaySettings.UpdateMaterials();
#endif

            Terrain.GetActiveTerrains(m_ActiveTerrains);

            XRSystem.singlePassAllowed = m_Asset.currentPlatformRenderPipelineSettings.xrSettings.singlePass;
            var xrLayout = XRSystem.NewLayout();

            // This syntax is awful and hostile to debugging, please don't use it...
            using (ListPool<RenderRequest>.Get(out List<RenderRequest> renderRequests))
            using (DictionaryPool<HDProbe, List<(int index, float weight)>>.Get(out Dictionary<HDProbe, List<(int index, float weight)>> renderRequestIndicesWhereTheProbeIsVisible))
            using (ListPool<CameraSettings>.Get(out List<CameraSettings> cameraSettings))
            using (ListPool<CameraPositionSettings>.Get(out List<CameraPositionSettings> cameraPositionSettings))
            using (ListPool<CubemapFace>.Get(out List<CubemapFace> cameraCubemapFaces))
            {
                // With XR multi-pass enabled, each camera can be rendered multiple times with different parameters
                foreach (var c in cameras)
                    xrLayout.AddCamera(c, HDUtils.TryGetAdditionalCameraDataOrDefault(c).xrRendering);

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

                // We avoid ticking this per camera. Every time the resolution type changes from hardware to software, this will reinvalidate all the internal resources
                // of the RTHandle system. So we just obey directly what the render pipeline quality asset says. Cameras that have DRS disabled should still pass a res percentage of %100
                // so will present rendering at native resolution. This will only pay a small cost of memory on the texture aliasing that the runtime has to keep track of.
                RTHandles.SetHardwareDynamicResolutionState(m_Asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.dynResType == DynamicResolutionType.Hardware);

                // Culling loop
                foreach ((Camera camera, XRPass xrPass) in xrLayout.GetActivePasses())
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

                    bool cameraRequestedDynamicRes = false;
                    HDAdditionalCameraData hdCam = null;
                    var drsSettings = m_Asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings;

                    #region DRS Setup
                    ////////////////////////////////
                    // setup of DRS in the camera //
                    ////////////////////////////////
                    // First step we tell the DRS handler that we will be using the scaler set by the user. Note DLSS can set a system slot in case it wants to provide
                    // the scale.
                    DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.User);
                    if (camera.TryGetComponent<HDAdditionalCameraData>(out hdCam))
                    {
                        cameraRequestedDynamicRes = hdCam.allowDynamicResolution && camera.cameraType == CameraType.Game;

                        // DRS should be disabled in case this camera will be path tracing.
                        cameraRequestedDynamicRes &= !HDCamera.GetOrCreate(camera).IsPathTracingEnabled();
                    }

                    // We now setup DLSS if its enabled. DLSS can override the drsSettings (i.e. setting a System scaler slot, and providing quality settings).
                    SetupDLSSForCameraDataAndDynamicResHandler(hdCam, camera, xrPass, cameraRequestedDynamicRes, ref drsSettings);

                    // only select the current instance for this camera. We dont pass the settings set to prevent an update.
                    // This will set a new instance in DynamicResolutionHandler.instance that is specific to this camera.
                    DynamicResolutionHandler.UpdateAndUseCamera(camera);

                    //Warning!! do not read anything off the dynResHandler, until we have called Update(). Otherwise, the handler is in the process of getting constructed.
                    var dynResHandler = DynamicResolutionHandler.instance;

                    if (hdCam != null)
                    {
                        // We are in a case where the platform does not support hw dynamic resolution, so we force the software fallback.
                        // TODO: Expose the graphics caps info on whether the platform supports hw dynamic resolution or not.
                        // Temporarily disable HW Dynamic resolution on metal until the problems we have with it are fixed
                        if (drsSettings.dynResType == DynamicResolutionType.Hardware && cameraRequestedDynamicRes && !camera.allowDynamicResolution)
                        {
                            dynResHandler.ForceSoftwareFallback();
                        }
                    }

                    // Notify the hanlder if this camera requests DRS.
                    dynResHandler.SetCurrentCameraRequest(cameraRequestedDynamicRes);
                    dynResHandler.runUpscalerFilterOnFullResolution = (hdCam != null && hdCam.cameraCanRenderDLSS) || DynamicResolutionHandler.instance.filter == DynamicResUpscaleFilter.TAAU;

                    // Finally, our configuration is prepared. Push it to the drs handler
                    dynResHandler.Update(drsSettings);
                    #endregion
                    // Start culling for all main cameras, setup RenderRequests for them and determine the list of visible realtime probes.
                    if (PrepareAndCullCamera(camera, xrPass, cameraRequestedDynamicRes, renderRequests, renderContext, out var request))
                    {
                        DetermineVisibleProbesForRequest(request, renderRequestIndicesWhereTheProbeIsVisible);
                    }
                }

                // Generate RenderRequests for all visible probes
                GenerateProbeRenderRequests(renderRequestIndicesWhereTheProbeIsVisible, renderRequests, cameraSettings, cameraPositionSettings, cameraCubemapFaces, renderContext);

                foreach (var pair in renderRequestIndicesWhereTheProbeIsVisible)
                    ListPool<(int index, float weight)>.Release(pair.Value);
                renderRequestIndicesWhereTheProbeIsVisible.Clear();

                using (ListPool<int>.Get(out List<int> renderRequestIndicesToRender))
                {
                    // Flatten the render requests graph in an array that guarantee dependency constraints
                    FlattenRenderRequestGraph(renderRequests, renderRequestIndicesToRender);

                    using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.HDRenderPipelineAllRenderRequest)))
                    {
                        // Warm up the RTHandle system so that it gets init to the maximum resolution available (avoiding to call multiple resizes
                        // that can lead to high memory spike as the memory release is delayed while the creation is immediate).
                        {
                            Vector2Int maxSize = new Vector2Int(1, 1);

                            for (int i = 0; i < renderRequestIndicesToRender.Count; ++i)
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
                            RTHandles.SetReferenceSize(maxSize.x, maxSize.y);
                        }


                        // Execute render request graph, in reverse order
                        for (int i = 0; i < renderRequestIndicesToRender.Count; ++i)
                        {
                            bool isLast = i == renderRequestIndicesToRender.Count - 1;
                            var renderRequestIndex = renderRequestIndicesToRender[i];
                            var renderRequest = renderRequests[renderRequestIndex];

                            var cmd = CommandBufferPool.Get("");

                            // The HDProbe store only one RenderData per probe, however RenderData can be view dependent (e.g. planar probes).
                            // To avoid that the render data for the wrong view is used, we previously store a copy of the render data
                            // for each viewer and we are going to set it on the probe right before said viewer is rendered.
                            foreach (var probeDataPair in renderRequest.viewDependentProbesData)
                            {
                                var probe = probeDataPair.Item2;
                                var probeRenderData = probeDataPair.Item1;
                                probe.SetRenderData(ProbeSettings.Mode.Realtime, probeRenderData);
                            }

                            // Save the camera history before rendering the AOVs
                            var cameraHistory = renderRequest.hdCamera.GetHistoryRTHandleSystem();

                            ExecuteAOVRenderRequests(renderRequest, cmd, renderContext);

                            // We are now going to render the main camera, so bind the correct HistoryRTHandleSystem (in case we previously render an AOV)
                            renderRequest.hdCamera.BindHistoryRTHandleSystem(cameraHistory);

                            using (new ProfilingScope(cmd, renderRequest.hdCamera.profilingSampler))
                            {
                                cmd.SetInvertCulling(renderRequest.cameraSettings.invertFaceCulling);
                                ExecuteRenderRequest(renderRequest, renderContext, cmd, AOVRequestData.defaultAOVRequestDataNonAlloc);
                                cmd.SetInvertCulling(false);
                            }

                            //  EndCameraRendering callback should be executed outside of any profiling scope in case user code submits the renderContext
                            EndCameraRendering(renderContext, renderRequest.hdCamera.camera);

                            EndRenderRequest(renderRequest, cmd);

                            // Render XR mirror view once all render requests have been completed
                            if (isLast && renderRequest.hdCamera.camera.cameraType == CameraType.Game && renderRequest.hdCamera.camera.targetTexture == null)
                            {
                                if (HDUtils.TryGetAdditionalCameraDataOrDefault(renderRequest.hdCamera.camera).xrRendering)
                                {
                                    XRSystem.RenderMirrorView(cmd, renderRequest.hdCamera.camera);
                                }
                            }

                            // Let's make sure to keep track of lights that will generate screen space shadows.
                            CollectScreenSpaceShadowData();

                            renderContext.ExecuteCommandBuffer(cmd);
                            CommandBufferPool.Release(cmd);
                            renderContext.Submit();
                        }
                    }
                }
            }

            // Now that all cameras have been rendered, let's make sure to keep track of update the screen space shadow data
            PropagateScreenSpaceShadowData();

            DynamicResolutionHandler.ClearSelectedCamera();

            m_RenderGraph.EndFrame();
            XRSystem.EndLayout();

#if UNITY_2021_1_OR_NEWER
            EndContextRendering(renderContext, cameras);
#else
            EndFrameRendering(renderContext, cameras);
#endif
        }

        void CollectScreenSpaceShadowData()
        {
            // For every unique light that has been registered, make sure it is kept track of
            foreach (ScreenSpaceShadowData ssShadowData in m_CurrentScreenSpaceShadowData)
            {
                if (ssShadowData.valid)
                {
                    HDAdditionalLightData currentAdditionalLightData = ssShadowData.additionalLightData;
                    m_ScreenSpaceShadowsUnion.Add(currentAdditionalLightData);
                }
            }

            if (m_CurrentSunLightAdditionalLightData != null)
            {
                m_ScreenSpaceShadowsUnion.Add(m_CurrentSunLightAdditionalLightData);
            }
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
            var uiCullingResults = renderRequest.cullingResults.uiCullingResults;
            var hdProbeCullingResults = renderRequest.cullingResults.hdProbeCullingResults;
            var decalCullingResults = renderRequest.cullingResults.decalCullResults;
            var target = renderRequest.target;

            m_FullScreenDebugPushed = false;

            // Updates RTHandle
            hdCamera.BeginRender(cmd);

            SetHDRState(hdCamera);

            if (m_RayTracingSupported)
            {
                // This call need to happen once per camera
                // TODO: This can be wasteful for "compatible" cameras.
                // We need to determine the minimum set of feature used by all the camera and build the minimum number of acceleration structures.
                using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.RaytracingBuildAccelerationStructure)))
                {
                    BuildRayTracingAccelerationStructure(hdCamera);
                }
                CullForRayTracing(cmd, hdCamera);
            }


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
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    m_DebugDisplaySettings.UpdateCameraFreezeOptions();
#endif
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

                if (m_DebugDisplaySettings.IsDebugDisplayRemovePostprocess())
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.CustomPassBufferClearDebug)))
                    {
                        if (m_CustomPassColorBuffer.IsValueCreated && m_CustomPassDepthBuffer.IsValueCreated)
                            CoreUtils.SetRenderTarget(cmd, m_CustomPassColorBuffer.Value, m_CustomPassDepthBuffer.Value, ClearFlag.All);
                        else if (m_CustomPassColorBuffer.IsValueCreated)
                            CoreUtils.SetRenderTarget(cmd, m_CustomPassColorBuffer.Value, ClearFlag.Color);
                        else if (m_CustomPassDepthBuffer.IsValueCreated)
                            CoreUtils.SetRenderTarget(cmd, m_CustomPassDepthBuffer.Value, ClearFlag.Depth);
                    }
                }

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPass))
                {
                    using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.CustomPassVolumeUpdate)))
                        CustomPassVolume.Update(hdCamera);
                }

                // Do anything we need to do upon a new frame.
                // The NewFrame must be after the VolumeManager update and before Resize because it uses properties set in NewFrame
                LightLoopNewFrame(cmd, hdCamera);

                // Apparently scissor states can leak from editor code. As it is not used currently in HDRP (apart from VR). We disable scissor at the beginning of the frame.
                cmd.DisableScissorRect();

                Resize(hdCamera);
                BeginPostProcessFrame(cmd, hdCamera, this);

                ApplyDebugDisplaySettings(hdCamera, cmd, aovRequest.isValid);

                if (DebugManager.instance.isAnyDebugUIActive)
                    m_CurrentDebugDisplaySettings.UpdateAveragedProfilerTimings();

                SetupCameraProperties(hdCamera, renderContext, cmd);

                // TODO: Find a correct place to bind these material textures
                // We have to bind the material specific global parameters in this mode
                foreach (var material in m_MaterialList)
                    material.Bind(cmd);

                // Frustum cull Local Volumetric Fog on the CPU. Can be performed as soon as the camera is set up.
                PrepareVisibleLocalVolumetricFogList(hdCamera, cmd);

                // do AdaptiveProbeVolume stuff
                // Bind AdaptiveProbeVolume resources
                if (IsAPVEnabled())
                {
                    BindAPVRuntimeResources(cmd, hdCamera);
                }

                // Note: Legacy Unity behave like this for ShadowMask
                // When you select ShadowMask in Lighting panel it recompile shaders on the fly with the SHADOW_MASK keyword.
                // However there is no C# function that we can query to know what mode have been select in Lighting Panel and it will be wrong anyway. Lighting Panel setup what will be the next bake mode. But until light is bake, it is wrong.
                // Currently to know if you need shadow mask you need to go through all visible lights (of CullResult), check the LightBakingOutput struct and look at lightmapBakeType/mixedLightingMode. If one light have shadow mask bake mode, then you need shadow mask features (i.e extra Gbuffer).
                // It mean that when we build a standalone player, if we detect a light with bake shadow mask, we generate all shader variant (with and without shadow mask) and at runtime, when a bake shadow mask light is visible, we dynamically allocate an extra GBuffer and switch the shader.
                // So the first thing to do is to go through all the light: PrepareLightsForGPU
                bool enableBakeShadowMask = PrepareLightsForGPU(cmd, hdCamera, cullingResults, hdProbeCullingResults, m_CurrentDebugDisplaySettings, aovRequest);

                UpdateGlobalConstantBuffers(hdCamera, cmd);

                // Do the same for ray tracing if allowed
                if (m_RayTracingSupported)
                {
                    m_RayCountManager.SetRayCountEnabled(m_CurrentDebugDisplaySettings.data.countRays);
                    BuildRayTracingLightData(cmd, hdCamera, m_CurrentDebugDisplaySettings);
                }

                // Configure all the keywords
                ConfigureKeywords(enableBakeShadowMask, hdCamera, cmd);

                VFXCameraXRSettings cameraXRSettings;
                cameraXRSettings.viewTotal = hdCamera.xr.enabled ? 2U : 1U;
                cameraXRSettings.viewCount = (uint)hdCamera.viewCount;
                cameraXRSettings.viewOffset = (uint)hdCamera.xr.multipassId;

                VFXManager.ProcessCameraCommand(camera, cmd, cameraXRSettings, cullingResults);

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
                FrameSettingsHistory.AggregateFrameSettings(ref currentFrameSettings, camera, additionalCameraData, m_Asset, null);
            else
                FrameSettings.AggregateFrameSettings(ref currentFrameSettings, camera, additionalCameraData, m_Asset);

            // With the Frame Settings now properly set up, we can resolve the sample budget.
            currentFrameSettings.sssResolvedSampleBudget = currentFrameSettings.GetResolvedSssSampleBudget(m_Asset);

            // If raytracing is not properly initialized, we should disable it. This can happen in the few first frame of enabling raytracing in the editor while raytrace resource are created.
            currentFrameSettings.SetEnabled(FrameSettingsField.RayTracing, currentFrameSettings.IsEnabled(FrameSettingsField.RayTracing) && m_RayTracingSupported);

            // Specific pass to simply display the content of the camera buffer if users have fill it themselves (like video player)
            if (additionalCameraData.fullscreenPassthrough)
                return false;

            // Retrieve debug display settings to init FrameSettings, unless we are a reflection and in this case we don't have debug settings apply.
            DebugDisplaySettings debugDisplaySettings = (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview) ? s_NeutralDebugDisplaySettings : m_DebugDisplaySettings;

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

            //Forcefully disable antialiasing if DLSS is enabled.
            if (additionalCameraData != null)
                currentFrameSettings.SetEnabled(FrameSettingsField.Antialiasing, currentFrameSettings.IsEnabled(FrameSettingsField.Antialiasing) && !additionalCameraData.cameraCanRenderDLSS);

            // From this point, we should only use frame settings from the camera
            hdCamera.Update(currentFrameSettings, this, xrPass);

            // Custom Render requires a proper HDCamera, so we return after the HDCamera was setup
            if (additionalCameraData != null && additionalCameraData.hasCustomRender)
                return false;

            if (hdCamera.xr.enabled)
            {
                cullingParams = hdCamera.xr.cullingParams;

                // Sync the FOV on the camera to match the projection from the XR device in order to cull shadows accurately
                if (!camera.usePhysicalProperties && !XRGraphicsAutomatedTests.enabled)
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
            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
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

            // Must be called before culling because it emits intermediate renderers via Graphics.DrawInstanced.
            if (currentPipeline.IsAPVEnabled())
            {
                ProbeReferenceVolume.instance.RenderDebug(hdCamera.camera);
            }

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
                sortingCriteria = HDUtils.k_OpaqueSortingCriteria,
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
                sortingCriteria = HDUtils.k_OpaqueSortingCriteria,
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

        void UpdateShaderVariablesGlobalDecal(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
            {
                cb._EnableDecals = 1;
                cb._DecalAtlasResolution = new Vector2(HDUtils.hdrpSettings.decalSettings.atlasWidth, HDUtils.hdrpSettings.decalSettings.atlasHeight);
            }
            else
            {
                cb._EnableDecals = 0;
            }
        }

        void RenderWireFrame(CullingResults cull, HDCamera hdCamera, RenderTargetIdentifier backbuffer, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RenderWireFrame)))
            {
                CoreUtils.SetRenderTarget(cmd, backbuffer, ClearFlag.Color, GetColorBufferClearColor(hdCamera));

                // Pass that renders the water surfaces as a wireframe (if water is enabled)
                RenderWaterAsWireFrame(cmd, hdCamera);

                var rendererListOpaque = renderContext.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_AllForwardOpaquePassNames));
                DrawOpaqueRendererList(renderContext, cmd, hdCamera.frameSettings, rendererListOpaque);

                // Render forward transparent
                var rendererListTransparent = renderContext.CreateRendererList(CreateTransparentRendererListDesc(cull, hdCamera.camera, m_AllTransparentPassNames));
                DrawTransparentRendererList(renderContext, cmd, hdCamera.frameSettings, rendererListTransparent);

                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

#if UNITY_EDITOR
                if(UnityEditor.Handles.ShouldRenderGizmos())
                {
                    renderContext.DrawGizmos(hdCamera.camera, GizmoSubset.PreImageEffects);
                    renderContext.DrawGizmos(hdCamera.camera, GizmoSubset.PostImageEffects);
                }
#endif
            }
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

        static bool NeedMotionVectorForTransparent(FrameSettings frameSettings)
        {
            // IMPORTANT NOTE: This is not checking for Transparent Motion Vectors because we need to explicitly write camera motion vectors
            // for transparent objects too, otherwise the transparent objects will look completely broken upon motion if Transparent Motion Vectors is off.
            return frameSettings.IsEnabled(FrameSettingsField.MotionVectors);
        }

        /// <summary>
        /// Release all persistent shadow atlas.
        /// In HDRP, shadow persistent atlases are allocated per light type (area, punctual or directional) when needed but never deallocated.
        /// Calling this will force deallocation of those atlases. This can be useful between levels for example when you know that some types of lights aren't used anymore.
        /// </summary>
        public void ReleasePersistentShadowAtlases()
        {
            m_ShadowManager.ReleaseSharedShadowAtlases(m_RenderGraph);
        }

        /// <summary>
        /// Enforce under specific circumstances whether HDRP or native engine triggers the Screen Space UI Overlay rendering
        /// </summary>
        static void AdjustUIOverlayOwnership(int cameraCount)
        {
            // If rendering to XR device, we don't render SS UI overlay within SRP as the overlay should not be visible in HMD eyes, only when mirroring (after SRP XR Mirror pass)
            // If there is no camera to render in HDRP, SS UI overlay has to be rendered in the engine
            if (XRSystem.displayActive || cameraCount == 0)
            {
                SupportedRenderingFeatures.active.rendersUIOverlay = false;
            }
            // When HDR is active and no XR we enforce UI overlay per camera as we want all UI to be calibrated to white paper inside a single pass
            else if (HDROutputForAnyDisplayIsActive())
            {
                SupportedRenderingFeatures.active.rendersUIOverlay = true;
            }
        }
    }
}
