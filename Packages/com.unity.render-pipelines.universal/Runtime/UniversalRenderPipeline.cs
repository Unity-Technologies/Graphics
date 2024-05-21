using System;
using Unity.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering.Universal;
#endif
using UnityEngine.Scripting.APIUpdating;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Profiling;
using static UnityEngine.Camera;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The main class for the Universal Render Pipeline (URP).
    /// </summary>
    public sealed partial class UniversalRenderPipeline : RenderPipeline
    {
        /// <summary>
        /// The shader tag used in the Universal Render Pipeline (URP)
        /// </summary>
        public const string k_ShaderTagName = "UniversalPipeline";

        // Cache camera data to avoid per-frame allocations.
        internal static class CameraMetadataCache
        {
            public class CameraMetadataCacheEntry
            {
                public string name;
                public ProfilingSampler sampler;
            }

            static Dictionary<int, CameraMetadataCacheEntry> s_MetadataCache = new();

            static readonly CameraMetadataCacheEntry k_NoAllocEntry = new() { name = "Unknown", sampler = new ProfilingSampler("Unknown") };

            public static CameraMetadataCacheEntry GetCached(Camera camera)
            {
#if UNIVERSAL_PROFILING_NO_ALLOC
                return k_NoAllocEntry;
#else
                int cameraId = camera.GetHashCode();
                if (!s_MetadataCache.TryGetValue(cameraId, out CameraMetadataCacheEntry result))
                {
                    string cameraName = camera.name; // Warning: camera.name allocates
                    result = new CameraMetadataCacheEntry
                    {
                        name = cameraName,
                        sampler = new ProfilingSampler(
                            $"{nameof(UniversalRenderPipeline)}.{nameof(RenderSingleCameraInternal)}: {cameraName}")
                    };
                    s_MetadataCache.Add(cameraId, result);
                }

                return result;
#endif
            }
        }

        internal static class Profiling
        {
            public static class Pipeline
            {
                const string k_Name = nameof(UniversalRenderPipeline);
                public static readonly ProfilingSampler initializeCameraData = new ProfilingSampler($"{k_Name}.{nameof(CreateCameraData)}");
                public static readonly ProfilingSampler initializeStackedCameraData = new ProfilingSampler($"{k_Name}.{nameof(InitializeStackedCameraData)}");
                public static readonly ProfilingSampler initializeAdditionalCameraData = new ProfilingSampler($"{k_Name}.{nameof(InitializeAdditionalCameraData)}");
                public static readonly ProfilingSampler initializeRenderingData = new ProfilingSampler($"{k_Name}.{nameof(CreateRenderingData)}");
                public static readonly ProfilingSampler initializeShadowData = new ProfilingSampler($"{k_Name}.{nameof(CreateShadowData)}");
                public static readonly ProfilingSampler initializeLightData = new ProfilingSampler($"{k_Name}.{nameof(CreateLightData)}");
                public static readonly ProfilingSampler buildAdditionalLightsShadowAtlasLayout = new ProfilingSampler($"{k_Name}.{nameof(BuildAdditionalLightsShadowAtlasLayout)}");
                public static readonly ProfilingSampler getPerObjectLightFlags = new ProfilingSampler($"{k_Name}.{nameof(GetPerObjectLightFlags)}");
                public static readonly ProfilingSampler getMainLightIndex = new ProfilingSampler($"{k_Name}.{nameof(GetMainLightIndex)}");
                public static readonly ProfilingSampler setupPerFrameShaderConstants = new ProfilingSampler($"{k_Name}.{nameof(SetupPerFrameShaderConstants)}");
                public static readonly ProfilingSampler setupPerCameraShaderConstants = new ProfilingSampler($"{k_Name}.{nameof(SetupPerCameraShaderConstants)}");

                public static class Renderer
                {
                    const string k_Name = nameof(ScriptableRenderer);
                    public static readonly ProfilingSampler setupCullingParameters = new ProfilingSampler($"{k_Name}.{nameof(ScriptableRenderer.SetupCullingParameters)}");
                    public static readonly ProfilingSampler setup = new ProfilingSampler($"{k_Name}.{nameof(ScriptableRenderer.Setup)}");
                };

                public static class Context
                {
                    const string k_Name = nameof(ScriptableRenderContext);
                    public static readonly ProfilingSampler submit = new ProfilingSampler($"{k_Name}.{nameof(ScriptableRenderContext.Submit)}");
                };
            };
        }

        /// <summary>
        /// The maximum amount of bias allowed for shadows.
        /// </summary>
        public static float maxShadowBias
        {
            get => 10.0f;
        }

        /// <summary>
        /// The minimum value allowed for render scale.
        /// </summary>
        public static float minRenderScale
        {
            get => 0.1f;
        }

        /// <summary>
        /// The maximum value allowed for render scale.
        /// </summary>
        public static float maxRenderScale
        {
            get => 2.0f;
        }

        /// <summary>
        /// The max number of iterations allowed calculating enclosing sphere.
        /// </summary>
        public static int maxNumIterationsEnclosingSphere
        {
            get => 1000;
        }

        /// <summary>
        /// The max number of lights that can be shaded per object (in the for loop in the shader).
        /// </summary>
        public static int maxPerObjectLights
        {
            get => 8;
        }

        /// <summary>
        /// The max number of additional lights that can can affect each GameObject.
        /// </summary>
        public static int maxVisibleAdditionalLights
        {
            get
            {
                // Must match: Input.hlsl, MAX_VISIBLE_LIGHTS
                bool isMobileOrMobileBuildTarget = PlatformAutoDetect.isShaderAPIMobileDefined;
                if (isMobileOrMobileBuildTarget && (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && Graphics.minOpenGLESVersion <= OpenGLESVersion.OpenGLES30))
                    return ShaderOptions.k_MaxVisibleLightCountLowEndMobile;

                // GLES can be selected as platform on Windows (not a mobile platform) but uniform buffer size so we must use a low light count.
                // WebGPU's minimal limits are based on mobile rather than desktop, so it will need to assume mobile.
                return (isMobileOrMobileBuildTarget || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU)
                    ? ShaderOptions.k_MaxVisibleLightCountMobile : ShaderOptions.k_MaxVisibleLightCountDesktop;
            }
        }

        // Match with values in Input.hlsl
        internal static int lightsPerTile => ((maxVisibleAdditionalLights + 31) / 32) * 32;
        internal static int maxZBinWords => 1024 * 4;
        internal static int maxTileWords => (maxVisibleAdditionalLights <= 32 ? 1024 : 4096) * 4;
        internal static int maxVisibleReflectionProbes => Math.Min(maxVisibleAdditionalLights, 64);

        internal const int k_DefaultRenderingLayerMask = 0x00000001;
        private readonly DebugDisplaySettingsUI m_DebugDisplaySettingsUI = new DebugDisplaySettingsUI();

        private UniversalRenderPipelineGlobalSettings m_GlobalSettings;

        internal UniversalRenderPipelineRuntimeTextures runtimeTextures { get; private set; }

        /// <summary>
        /// The default Render Pipeline Global Settings.
        /// </summary>
        public override RenderPipelineGlobalSettings defaultSettings => m_GlobalSettings;

        // flag to keep track of depth buffer requirements by any of the cameras in the stack
        internal static bool cameraStackRequiresDepthForPostprocessing = false;

        internal static RenderGraph s_RenderGraph;
        internal static RTHandleResourcePool s_RTHandlePool;

        // internal for tests
        internal static bool useRenderGraph;

        // Store locally the value on the instance due as the Render Pipeline Asset data might change before the disposal of the asset, making some APV Resources leak.
        internal bool apvIsEnabled = false;

        // Reference to the asset associated with the pipeline.
        // When a pipeline asset is switched in `GraphicsSettings`, the `UniversalRenderPipelineCore.asset` member
        // becomes unreliable for the purpose of pipeline and renderer clean-up in the `Dispose` call from
        // `RenderPipelineManager.CleanupRenderPipeline`.
        // This field provides the correct reference for the purpose of cleaning up the renderers on this pipeline
        // asset.
        private readonly UniversalRenderPipelineAsset pipelineAsset;

        /// <inheritdoc/>
        public override string ToString() => pipelineAsset?.ToString();

        /// <summary>
        /// Creates a new <c>UniversalRenderPipeline</c> instance.
        /// </summary>
        /// <param name="asset">The <c>UniversalRenderPipelineAsset</c> asset to initialize the pipeline.</param>
        /// <seealso cref="RenderPassEvent"/>
        public UniversalRenderPipeline(UniversalRenderPipelineAsset asset)
        {
            pipelineAsset = asset;

            m_GlobalSettings = UniversalRenderPipelineGlobalSettings.instance;

            runtimeTextures = GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineRuntimeTextures>();

            var shaders = GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineRuntimeShaders>();
            Blitter.Initialize(shaders.coreBlitPS, shaders.coreBlitColorAndDepthPS);

            SetSupportedRenderingFeatures(pipelineAsset);

            // Initial state of the RTHandle system.
            // We initialize to screen width/height to avoid multiple realloc that can lead to inflated memory usage (as releasing of memory is delayed).
            RTHandles.Initialize(Screen.width, Screen.height);

            // Init global shader keywords
            ShaderGlobalKeywords.InitializeShaderGlobalKeywords();

            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;

            // In QualitySettings.antiAliasing disabled state uses value 0, where in URP 1
            int qualitySettingsMsaaSampleCount = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
            bool msaaSampleCountNeedsUpdate = qualitySettingsMsaaSampleCount != asset.msaaSampleCount;

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (msaaSampleCountNeedsUpdate)
            {
                QualitySettings.antiAliasing = asset.msaaSampleCount;
            }

            var defaultVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>();
            VolumeManager.instance.Initialize(defaultVolumeProfileSettings.volumeProfile, asset.volumeProfile);

            // Configure initial XR settings
            MSAASamples msaaSamples = (MSAASamples)Mathf.Clamp(Mathf.NextPowerOfTwo(QualitySettings.antiAliasing), (int)MSAASamples.None, (int)MSAASamples.MSAA8x);
            XRSystem.SetDisplayMSAASamples(msaaSamples);
            XRSystem.SetRenderScale(asset.renderScale);

            Lightmapping.SetDelegate(lightsDelegate);

            CameraCaptureBridge.enabled = true;

            RenderingUtils.ClearSystemInfoCache();

            DecalProjector.defaultMaterial = asset.decalMaterial;

            s_RenderGraph = new RenderGraph("URPRenderGraph");
            useRenderGraph = !GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode;

#if !UNITY_EDITOR
            Debug.Log($"RenderGraph is now {(useRenderGraph ? "enabled" : "disabled")}.");
#endif

            s_RTHandlePool = new RTHandleResourcePool();

            DebugManager.instance.RefreshEditor();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_DebugDisplaySettingsUI.RegisterDebug(UniversalRenderPipelineDebugDisplaySettings.Instance);
#endif

            QualitySettings.enableLODCrossFade = asset.enableLODCrossFade;

            apvIsEnabled = asset != null && asset.lightProbeSystem == LightProbeSystem.ProbeVolumes;
            SupportedRenderingFeatures.active.overridesLightProbeSystem = apvIsEnabled;
            SupportedRenderingFeatures.active.skyOcclusion = apvIsEnabled;
            if (apvIsEnabled)
            {
                ProbeReferenceVolume.instance.Initialize(new ProbeVolumeSystemParameters
                {
                    memoryBudget = asset.probeVolumeMemoryBudget,
                    blendingMemoryBudget = asset.probeVolumeBlendingMemoryBudget,
                    shBands = asset.probeVolumeSHBands,
                    supportGPUStreaming = asset.supportProbeVolumeGPUStreaming,
                    supportDiskStreaming = asset.supportProbeVolumeDiskStreaming,
                    supportScenarios = asset.supportProbeVolumeScenarios,
                    supportScenarioBlending = asset.supportProbeVolumeScenarioBlending,
#pragma warning disable 618
                    sceneData = m_GlobalSettings.GetOrCreateAPVSceneData(),
#pragma warning restore 618
                });
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (apvIsEnabled)
            {
                ProbeReferenceVolume.instance.Cleanup();
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_DebugDisplaySettingsUI.UnregisterDebug();
#endif

            Blitter.Cleanup();

            base.Dispose(disposing);

            pipelineAsset.DestroyRenderers();

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
            ShaderData.instance.Dispose();
            XRSystem.Dispose();

            s_RenderGraph.Cleanup();
            s_RenderGraph = null;

            s_RTHandlePool.Cleanup();
            s_RTHandlePool = null;
#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
#endif
            Lightmapping.ResetDelegate();
            CameraCaptureBridge.enabled = false;

            ConstantBuffer.ReleaseAll();
            VolumeManager.instance.Deinitialize();

            DisposeAdditionalCameraData();
            AdditionalLightsShadowAtlasLayout.ClearStaticCaches();
        }

        // If the URP gets destroyed, we must clean up all the added URP specific camera data and
        // non-GC resources to avoid leaking them.
        private void DisposeAdditionalCameraData()
        {
            foreach (var c in Camera.allCameras)
            {
                if (c.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
                {
                    additionalCameraData.historyManager.Dispose();
                };
            }
        }

        readonly struct CameraRenderingScope : IDisposable
        {
            static readonly ProfilingSampler beginCameraRenderingSampler = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(BeginCameraRendering)}");
            static readonly ProfilingSampler endCameraRenderingSampler = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(EndCameraRendering)}");

            private readonly ScriptableRenderContext m_Context;
            private readonly Camera m_Camera;

            public CameraRenderingScope(ScriptableRenderContext context, Camera camera)
            {
                using (new ProfilingScope(beginCameraRenderingSampler))
                {
                    m_Context = context;
                    m_Camera = camera;

                    BeginCameraRendering(context, camera);
                }
            }

            public void Dispose()
            {
                using (new ProfilingScope(endCameraRenderingSampler))
                {
                    EndCameraRendering(m_Context, m_Camera);
                }
            }
        }

        readonly struct ContextRenderingScope : IDisposable
        {
            static readonly ProfilingSampler beginContextRenderingSampler = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(BeginContextRendering)}");
            static readonly ProfilingSampler endContextRenderingSampler = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(EndContextRendering)}");

            private readonly ScriptableRenderContext m_Context;
            private readonly List<Camera> m_Cameras;

            public ContextRenderingScope(ScriptableRenderContext context, List<Camera> cameras)
            {
                m_Context = context;
                m_Cameras = cameras;

                using (new ProfilingScope(beginContextRenderingSampler))
                {
                    BeginContextRendering(m_Context, m_Cameras);
                }
            }

            public void Dispose()
            {
                using (new ProfilingScope(endContextRenderingSampler))
                {
                    EndContextRendering(m_Context, m_Cameras);
                }
            }
        }

#if UNITY_2021_1_OR_NEWER
        /// <inheritdoc/>
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            Render(renderContext, new List<Camera>(cameras));
        }

#endif

#if UNITY_2021_1_OR_NEWER
        /// <inheritdoc/>
        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
#else
        /// <inheritdoc/>
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
#endif
        {
            SetHDRState(cameras);

#if UNITY_2021_1_OR_NEWER
            int cameraCount = cameras.Count;
#else
            int cameraCount = cameras.Length;
#endif
            // For XR, HDR and no camera cases, UI Overlay ownership must be enforced
            AdjustUIOverlayOwnership(cameraCount);

            GPUResidentDrawer.ReinitializeIfNeeded();

            // TODO: Would be better to add Profiling name hooks into RenderPipelineManager.
            // C#8 feature, only in >= 2020.2
            using var profScope = new ProfilingScope(ProfilingSampler.Get(URPProfileId.UniversalRenderTotal));

            using (new ContextRenderingScope(renderContext, cameras))
            {
                GraphicsSettings.lightsUseLinearIntensity = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                GraphicsSettings.lightsUseColorTemperature = true;
                SetupPerFrameShaderConstants();
                XRSystem.SetDisplayMSAASamples((MSAASamples)asset.msaaSampleCount);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (DebugManager.instance.isAnyDebugUIActive)
                    UniversalRenderPipelineDebugDisplaySettings.Instance.UpdateDisplayStats();

                // This is for texture streaming
                UniversalRenderPipelineDebugDisplaySettings.Instance.UpdateMaterials();
#endif

                // URP uses the camera's allowDynamicResolution flag to decide if useDynamicScale should be enabled for camera render targets.
                // However, the RTHandle system has an additional setting that controls if useDynamicScale will be set for render targets allocated via RTHandles.
                // In order to avoid issues at runtime, we must make the RTHandle system setting consistent with URP's logic. URP already synchronizes the setting
                // during initialization, but unfortunately it's possible for external code to overwrite the setting due to RTHandle state being global.
                // The best we can do to avoid errors in this situation is to ensure the state is set to the correct value every time we perform rendering.
                RTHandles.SetHardwareDynamicResolutionState(true);

                SortCameras(cameras);
#if UNITY_2021_1_OR_NEWER
                for (int i = 0; i < cameras.Count; ++i)
#else
                for (int i = 0; i < cameras.Length; ++i)
#endif
                {
                    var camera = cameras[i];
                    if (IsGameCamera(camera))
                    {
                        RenderCameraStack(renderContext, camera);
                    }
                    else
                    {
                        using (new CameraRenderingScope(renderContext, camera))
                        {
#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                        //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                        //N.B.: We aren't expecting an XR camera at this stage
                        VFX.VFXManager.PrepareCamera(camera);
#endif
                            UpdateVolumeFramework(camera, null);

                            RenderSingleCameraInternal(renderContext, camera);
                        }
                    }
                }

                s_RenderGraph.EndFrame();
                s_RTHandlePool.PurgeUnusedResources(Time.frameCount);
            }

#if ENABLE_SHADER_DEBUG_PRINT
            ShaderDebugPrintManager.instance.EndFrame();
#endif
        }

        /// <summary>
        /// Check whether RenderRequest is supported
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="data"></param>
        /// <typeparam name="RequestData"></typeparam>
        /// <returns></returns>
        protected override bool IsRenderRequestSupported<RequestData>(Camera camera, RequestData data)
        {
            if (data is StandardRequest)
                return true;
            else if (data is SingleCameraRequest)
                return true;

            return false;
        }

        /// <summary>
        /// Process a render request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="camera"></param>
        /// <param name="renderRequest"></param>
        /// <typeparam name="RequestData"></typeparam>
        protected override void ProcessRenderRequests<RequestData>(ScriptableRenderContext context, Camera camera, RequestData renderRequest)
        {
            StandardRequest standardRequest = renderRequest as StandardRequest;
            SingleCameraRequest singleRequest = renderRequest as SingleCameraRequest;

            if(standardRequest != null || singleRequest != null)
            {
                RenderTexture destination = standardRequest != null ? standardRequest.destination : singleRequest.destination;

                //don't go further if no destination texture
                if(destination == null)
                {
                    Debug.LogError("RenderRequest has no destination texture, set one before sending request");
                    return;
                }

                int mipLevel = standardRequest != null ? standardRequest.mipLevel : singleRequest.mipLevel;
                int slice = standardRequest != null ? standardRequest.slice : singleRequest.slice;
                int face = standardRequest != null ? (int)standardRequest.face : (int)singleRequest.face;

                //store data that will be changed
                var originalTarget = camera.targetTexture;

                //set data
                RenderTexture temporaryRT = null;
                RenderTextureDescriptor RTDesc = destination.descriptor;
                //need to set use default constructor of RenderTextureDescriptor which doesn't enable allowVerticalFlip which matters for cubemaps.
                if (destination.dimension == TextureDimension.Cube)
                    RTDesc = new RenderTextureDescriptor();

                RTDesc.colorFormat = destination.format;
                RTDesc.volumeDepth = 1;
                RTDesc.msaaSamples = destination.descriptor.msaaSamples;
                RTDesc.dimension = TextureDimension.Tex2D;
                RTDesc.width = destination.width / (int)Math.Pow(2, mipLevel);
                RTDesc.height = destination.height / (int)Math.Pow(2, mipLevel);
                RTDesc.width = Mathf.Max(1, RTDesc.width);
                RTDesc.height = Mathf.Max(1, RTDesc.height);

                //if mip is 0 and target is Texture2D we can immediately render to the requested destination
                if(destination.dimension != TextureDimension.Tex2D || mipLevel != 0)
                {
                    temporaryRT = RenderTexture.GetTemporary(RTDesc);
                }

                camera.targetTexture = temporaryRT ? temporaryRT : destination;

                if (standardRequest != null)
                {
                    Render(context, new Camera[] { camera });
                }
                else
                {
                    using (ListPool<Camera>.Get(out var tmp))
                    {
                        tmp.Add(camera);

                        using (new ContextRenderingScope(context, tmp))
                        using (new CameraRenderingScope(context, camera))
                        {
                            camera.gameObject.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData);
                            RenderSingleCameraInternal(context, camera, ref additionalCameraData);
                        }
                    }
                }

                if(temporaryRT)
                {
                    bool isCopySupported = false;

                    switch(destination.dimension)
                    {
                        case TextureDimension.Tex2D:
                            if((SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) != 0)
                            {
                                isCopySupported = true;
                                Graphics.CopyTexture(temporaryRT, 0, 0, destination, 0, mipLevel);
                            }
                            break;
                        case TextureDimension.Tex2DArray:
                            if((SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) != 0)
                            {
                                isCopySupported = true;
                                Graphics.CopyTexture(temporaryRT, 0, 0, destination, slice, mipLevel);
                            }
                            break;
                        case TextureDimension.Tex3D:
                            if((SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) != 0)
                            {
                                isCopySupported = true;
                                Graphics.CopyTexture(temporaryRT, 0, 0, destination, slice, mipLevel);
                            }
                            break;
                        case TextureDimension.Cube:
                            if((SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) != 0)
                            {
                                isCopySupported = true;
                                Graphics.CopyTexture(temporaryRT, 0, 0, destination, face, mipLevel);
                            }
                            break;
                        case TextureDimension.CubeArray:
                            if((SystemInfo.copyTextureSupport & CopyTextureSupport.DifferentTypes) != 0)
                            {
                                isCopySupported = true;
                                Graphics.CopyTexture(temporaryRT, 0, 0, destination, face + slice * 6, mipLevel);
                            }
                            break;
                        default:
                            break;
                    }

                    if(!isCopySupported)
                        Debug.LogError("RenderRequest cannot have destination texture of this format: " + Enum.GetName(typeof(TextureDimension), destination.dimension));
                }

                //restore data
                camera.targetTexture = originalTarget;
                Graphics.SetRenderTarget(originalTarget);
                RenderTexture.ReleaseTemporary(temporaryRT);
            }
            else
            {
                Debug.LogWarning("RenderRequest type: " + typeof(RequestData).FullName  + " is either invalid or unsupported by the current pipeline");
            }
        }

        /// <summary>
        /// Standalone camera rendering. Use this to render procedural cameras.
        /// This method doesn't call <c>BeginCameraRendering</c> and <c>EndCameraRendering</c> callbacks.
        /// </summary>
        /// <param name="context">Render context used to record commands during execution.</param>
        /// <param name="camera">Camera to render.</param>
        /// <seealso cref="ScriptableRenderContext"/>
        [Obsolete("RenderSingleCamera is obsolete, please use RenderPipeline.SubmitRenderRequest with UniversalRenderer.SingleCameraRequest as RequestData type")]
        public static void RenderSingleCamera(ScriptableRenderContext context, Camera camera)
        {
            RenderSingleCameraInternal(context, camera);
        }

        internal static void RenderSingleCameraInternal(ScriptableRenderContext context, Camera camera)
        {
            UniversalAdditionalCameraData additionalCameraData = null;
            if (IsGameCamera(camera))
                camera.gameObject.TryGetComponent(out additionalCameraData);

            RenderSingleCameraInternal(context, camera, ref additionalCameraData);
        }

        internal static void RenderSingleCameraInternal(ScriptableRenderContext context, Camera camera, ref UniversalAdditionalCameraData additionalCameraData)
        {
            if (additionalCameraData != null && additionalCameraData.renderType != CameraRenderType.Base)
            {
                Debug.LogWarning("Only Base cameras can be rendered with standalone RenderSingleCamera. Camera will be skipped.");
                return;
            }

            var frameData = GetRenderer(camera, additionalCameraData).frameData;
            var cameraData = CreateCameraData(frameData, camera, additionalCameraData, true);
            InitializeAdditionalCameraData(camera, additionalCameraData, true, cameraData);
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            if (asset.useAdaptivePerformance)
                ApplyAdaptivePerformance(cameraData);
#endif

            RenderSingleCamera(context, cameraData);
        }

        static bool TryGetCullingParameters(UniversalCameraData cameraData, out ScriptableCullingParameters cullingParams)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                cullingParams = cameraData.xr.cullingParams;

                // Sync the FOV on the camera to match the projection from the XR device
                if (!cameraData.camera.usePhysicalProperties && !XRGraphicsAutomatedTests.enabled)
                    cameraData.camera.fieldOfView = Mathf.Rad2Deg * Mathf.Atan(1.0f / cullingParams.stereoProjectionMatrix.m11) * 2.0f;

                return true;
            }
#endif

            return cameraData.camera.TryGetCullingParameters(false, out cullingParams);
        }

        /// <summary>
        /// Renders a single camera. This method will do culling, setup and execution of the renderer.
        /// </summary>
        /// <param name="context">Render context used to record commands during execution.</param>
        /// <param name="cameraData">Camera rendering data. This might contain data inherited from a base camera.</param>
        static void RenderSingleCamera(ScriptableRenderContext context, UniversalCameraData cameraData)
        {
            Camera camera = cameraData.camera;
            ScriptableRenderer renderer = cameraData.renderer;
            if (renderer == null)
            {
                Debug.LogWarning(string.Format("Trying to render {0} with an invalid renderer. Camera rendering will be skipped.", camera.name));
                return;
            }

            // Note: We are disposing frameData once this variable goes out of scope.
            using ContextContainer frameData = renderer.frameData;

            if (!TryGetCullingParameters(cameraData, out var cullingParameters))
                return;

            ScriptableRenderer.current = renderer;
#if RENDER_GRAPH_OLD_COMPILER
            s_RenderGraph.nativeRenderPassesEnabled = false;
            Debug.LogWarning("The native render pass compiler is disabled. Use this for debugging only. Mobile performance may be sub-optimal.");
#else
            s_RenderGraph.nativeRenderPassesEnabled = renderer.supportsNativeRenderPassRendergraphCompiler;
#endif
            bool isSceneViewCamera = cameraData.isSceneViewCamera;

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            // The named CommandBuffer will close its "profiling scope" on execution.
            // That will orphan ProfilingScope markers as the named CommandBuffer markers are their parents.
            // Resulting in following pattern:
            // exec(cmd.start, scope.start, cmd.end) and exec(cmd.start, scope.end, cmd.end)
            CommandBuffer cmd = CommandBufferPool.Get();

            // TODO: move skybox code from C++ to URP in order to remove the call to context.Submit() inside DrawSkyboxPass
            // Until then, we can't use nested profiling scopes with XR multipass
            CommandBuffer cmdScope = cameraData.xr.enabled ? null : cmd;

            var cameraMetadata = CameraMetadataCache.GetCached(camera);
            using (new ProfilingScope(cmdScope, cameraMetadata.sampler)) // Enqueues a "BeginSample" command into the CommandBuffer cmd
            {
                renderer.Clear(cameraData.renderType);

                using (new ProfilingScope(Profiling.Pipeline.Renderer.setupCullingParameters))
                {
                    var legacyCameraData = new CameraData(frameData);

                    renderer.OnPreCullRenderPasses(in legacyCameraData);
                    renderer.SetupCullingParameters(ref cullingParameters, ref legacyCameraData);
                }

                context.ExecuteCommandBuffer(cmd); // Send all the commands enqueued so far in the CommandBuffer cmd, to the ScriptableRenderContext context
                cmd.Clear();

                SetupPerCameraShaderConstants(cmd);

                bool supportProbeVolume = asset != null && asset.lightProbeSystem == LightProbeSystem.ProbeVolumes;
                ProbeReferenceVolume.instance.SetEnableStateFromSRP(supportProbeVolume);
                ProbeReferenceVolume.instance.SetVertexSamplingEnabled(asset.shEvalMode  == ShEvalMode.PerVertex || asset.shEvalMode  == ShEvalMode.Mixed);
                // We need to verify and flush any pending asset loading for probe volume.
                if (supportProbeVolume && ProbeReferenceVolume.instance.isInitialized)
                {
                    ProbeReferenceVolume.instance.PerformPendingOperations();
                    if (camera.cameraType != CameraType.Reflection &&
                        camera.cameraType != CameraType.Preview)
                    {
                        // TODO: Move this to one call for all cameras
                        ProbeReferenceVolume.instance.UpdateCellStreaming(cmd, camera);
                    }
                }

                // Emit scene/game view UI. The main game camera UI is always rendered, so this needs to be handled only for different camera types
                if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                    ScriptableRenderContext.EmitGeometryForCamera(camera);
#if UNITY_EDITOR
                 else if (isSceneViewCamera)
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif

                // do AdaptiveProbeVolume stuff
                if (supportProbeVolume)
                    ProbeReferenceVolume.instance.BindAPVRuntimeResources(cmd, true);

                // Must be called before culling because it emits intermediate renderers via Graphics.DrawInstanced.
                ProbeReferenceVolume.instance.RenderDebug(camera, Texture2D.whiteTexture);

                // Update camera motion tracking (prev matrices) from cameraData.
                // Called and updated only once, as the same camera can be rendered multiple times.
                // NOTE: Tracks only the current (this) camera, not shadow views or any other offscreen views.
                // NOTE: Shared between both Execute and Render (RG) paths.
                if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
                    additionalCameraData.motionVectorsPersistentData.Update(cameraData);

                // TODO: Move into the renderer. Problem: It modifies the AdditionalCameraData which is copied into RenderingData which causes value divergence for value types.
                // Update TAA persistent data based on cameraData. Most importantly resize the history render targets.
                // NOTE: Persistent data is kept over multiple frames. Its life-time differs from typical resources.
                // NOTE: Shared between both Execute and Render (RG) paths.
                if (cameraData.taaHistory != null)
                    UpdateTemporalAATargets(cameraData);

                RTHandles.SetReferenceSize(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);

                // Do NOT use cameraData after 'InitializeRenderingData'. CameraData state may diverge otherwise.
                // RenderingData takes a copy of the CameraData.
                // UniversalRenderingData needs to be created here to avoid copying cullResults.
                var data = frameData.Create<UniversalRenderingData>();
                data.cullResults = context.Cull(ref cullingParameters);

                GPUResidentDrawer.PostCullBeginCameraRendering(new RenderRequestBatcherContext { commandBuffer = cmd });

                var isForwardPlus = cameraData.renderer is UniversalRenderer { renderingModeActual: RenderingMode.ForwardPlus };

                // Initialize all the data types required for rendering.
                UniversalLightData lightData;
                UniversalShadowData shadowData;
                using (new ProfilingScope(Profiling.Pipeline.initializeRenderingData))
                {
                    CreateUniversalResourceData(frameData);
                    lightData = CreateLightData(frameData, asset, data.cullResults.visibleLights);
                    shadowData = CreateShadowData(frameData, asset, isForwardPlus);
                    CreatePostProcessingData(frameData, asset);
                    CreateRenderingData(frameData, asset, cmd, isForwardPlus, cameraData.renderer);
                }

                RenderingData legacyRenderingData = new RenderingData(frameData);
                CheckAndApplyDebugSettings(ref legacyRenderingData);

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
                if (asset.useAdaptivePerformance)
                    ApplyAdaptivePerformance(frameData);
#endif

                CreateShadowAtlasAndCullShadowCasters(lightData, shadowData, cameraData, ref data.cullResults, ref context);

                renderer.AddRenderPasses(ref legacyRenderingData);

                if (useRenderGraph)
                {
                    RecordAndExecuteRenderGraph(s_RenderGraph, context, renderer, cmd, cameraData.camera, cameraMetadata.name);
                    renderer.FinishRenderGraphRendering(cmd);
                }
                else
                {
                    // Disable obsolete warning for internal usage
                    #pragma warning disable CS0618
                    using (new ProfilingScope(Profiling.Pipeline.Renderer.setup))
                    {
                        renderer.Setup(context, ref legacyRenderingData);
                    }

                    // Timing scope inside
                    renderer.Execute(context, ref legacyRenderingData);
                    #pragma warning restore CS0618
                }
            } // When ProfilingSample goes out of scope, an "EndSample" command is enqueued into CommandBuffer cmd

            context.ExecuteCommandBuffer(cmd); // Sends to ScriptableRenderContext all the commands enqueued since cmd.Clear, i.e the "EndSample" command
            CommandBufferPool.Release(cmd);

            using (new ProfilingScope(Profiling.Pipeline.Context.submit))
            {
                // Render Graph will do the validation by itself, so this is redundant in that case
                if (!useRenderGraph && renderer.useRenderPassEnabled && !context.SubmitForRenderPassValidation())
                {
                    renderer.useRenderPassEnabled = false;
                    cmd.SetKeyword(ShaderGlobalKeywords.RenderPassEnabled, false);
                    Debug.LogWarning("Rendering command not supported inside a native RenderPass found. Falling back to non-RenderPass rendering path");
                }
                context.Submit(); // Actually execute the commands that we previously sent to the ScriptableRenderContext context
            }
            ScriptableRenderer.current = null;
        }

        private static void CreateShadowAtlasAndCullShadowCasters(UniversalLightData lightData, UniversalShadowData shadowData, UniversalCameraData cameraData, ref CullingResults cullResults, ref ScriptableRenderContext context)
        {
            if (!shadowData.supportsMainLightShadows && !shadowData.supportsAdditionalLightShadows)
                return;

            if (shadowData.supportsMainLightShadows)
                InitializeMainLightShadowResolution(shadowData);

            if (shadowData.supportsAdditionalLightShadows)
                shadowData.shadowAtlasLayout = BuildAdditionalLightsShadowAtlasLayout(lightData, shadowData, cameraData);

            shadowData.visibleLightsShadowCullingInfos = ShadowCulling.CullShadowCasters(ref context, shadowData, ref shadowData.shadowAtlasLayout, ref cullResults);
        }

        /// <summary>
        /// Renders a camera stack. This method calls RenderSingleCamera for each valid camera in the stack.
        /// The last camera resolves the final target to screen.
        /// </summary>
        /// <param name="context">Render context used to record commands during execution.</param>
        /// <param name="camera">Camera to render.</param>
        static void RenderCameraStack(ScriptableRenderContext context, Camera baseCamera)
        {
            using var profScope = new ProfilingScope(ProfilingSampler.Get(URPProfileId.RenderCameraStack));

            baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var baseCameraAdditionalData);

            // Overlay cameras will be rendered stacked while rendering base cameras
            if (baseCameraAdditionalData != null && baseCameraAdditionalData.renderType == CameraRenderType.Overlay)
                return;

            // Renderer contains a stack if it has additional data and the renderer supports stacking
            // The renderer is checked if it supports Base camera. Since Base is the only relevant type at this moment.
            var renderer = GetRenderer(baseCamera, baseCameraAdditionalData);
            bool supportsCameraStacking = renderer != null && renderer.SupportsCameraStackingType(CameraRenderType.Base);
            List<Camera> cameraStack = (supportsCameraStacking) ? baseCameraAdditionalData?.cameraStack : null;

            bool anyPostProcessingEnabled = baseCameraAdditionalData != null && baseCameraAdditionalData.renderPostProcessing;
            bool mainHdrDisplayOutputActive = HDROutputForMainDisplayIsActive();

            int rendererCount = asset.m_RendererDataList.Length;

            // We need to know the last active camera in the stack to be able to resolve
            // rendering to screen when rendering it. The last camera in the stack is not
            // necessarily the last active one as it users might disable it.
            int lastActiveOverlayCameraIndex = -1;
            if (cameraStack != null)
            {
                var baseCameraRendererType = renderer.GetType();
                bool shouldUpdateCameraStack = false;

                cameraStackRequiresDepthForPostprocessing = false;

                for (int i = 0; i < cameraStack.Count; ++i)
                {
                    Camera overlayCamera = cameraStack[i];
                    if (overlayCamera == null)
                    {
                        shouldUpdateCameraStack = true;
                        continue;
                    }

                    if (overlayCamera.isActiveAndEnabled)
                    {
                        overlayCamera.TryGetComponent<UniversalAdditionalCameraData>(out var data);
                        var overlayRenderer = GetRenderer(overlayCamera, data);

                        // Checking if the base and the overlay camera is of the same renderer type.
                        var overlayRendererType = overlayRenderer.GetType();
                        if (overlayRendererType != baseCameraRendererType)
                        {
                            Debug.LogWarning("Only cameras with compatible renderer types can be stacked. " +
                                             $"The camera: {overlayCamera.name} are using the renderer {overlayRendererType.Name}, " +
                                             $"but the base camera: {baseCamera.name} are using {baseCameraRendererType.Name}. Will skip rendering");
                            continue;
                        }

                        // Checking if they are the same renderer type but just not supporting Overlay
                        if ((overlayRenderer.SupportedCameraStackingTypes() & 1 << (int)CameraRenderType.Overlay) == 0)
                        {
                            Debug.LogWarning($"The camera: {overlayCamera.name} is using a renderer of type {renderer.GetType().Name} which does not support Overlay cameras in it's current state.");
                            continue;
                        }

                        if (data == null || data.renderType != CameraRenderType.Overlay)
                        {
                            Debug.LogWarning($"Stack can only contain Overlay cameras. The camera: {overlayCamera.name} " +
                                             $"has a type {data.renderType} that is not supported. Will skip rendering.");
                            continue;
                        }

                        cameraStackRequiresDepthForPostprocessing |= CheckPostProcessForDepth();

                        anyPostProcessingEnabled |= data.renderPostProcessing;
                        lastActiveOverlayCameraIndex = i;
                    }
                }
                if (shouldUpdateCameraStack)
                {
                    baseCameraAdditionalData.UpdateCameraStack();
                }
            }

            bool isStackedRendering = lastActiveOverlayCameraIndex != -1;

            // Prepare XR rendering
            var xrActive = false;
            var xrRendering = baseCameraAdditionalData?.allowXRRendering ?? true;
            var xrLayout = XRSystem.NewLayout();
            xrLayout.AddCamera(baseCamera, xrRendering);

            // With XR multi-pass enabled, each camera can be rendered multiple times with different parameters
            foreach ((Camera _, XRPass xrPass) in xrLayout.GetActivePasses())
            {
                var xrPassUniversal = xrPass as XRPassUniversal;
                if (xrPass.enabled)
                {
                    xrActive = true;
                    UpdateCameraStereoMatrices(baseCamera, xrPass);

                    // Apply XR display's viewport scale to URP's dynamic resolution solution
                    float xrViewportScale = XRSystem.GetRenderViewportScale();
                    ScalableBufferManager.ResizeBuffers(xrViewportScale, xrViewportScale);
                }

                bool finalOutputHDR = false;
#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                VFX.VFXCameraXRSettings cameraXRSettings;
#endif
                using (new CameraRenderingScope(context, baseCamera))
                {
                    // Update volumeframework before initializing additional camera data
                    UpdateVolumeFramework(baseCamera, baseCameraAdditionalData);

                    ContextContainer frameData = renderer.frameData;
                    UniversalCameraData baseCameraData = CreateCameraData(frameData, baseCamera,
                        baseCameraAdditionalData, !isStackedRendering);

#if ENABLE_VR && ENABLE_XR_MODULE
                    if (xrPass.enabled)
                    {
                        baseCameraData.xr = xrPass;

                        // Helper function for updating cameraData with xrPass Data
                        // Need to update XRSystem using baseCameraData to handle the case where camera position is modified in BeginCameraRendering
                        UpdateCameraData(baseCameraData, xrPass);

                        // Handle the case where camera position is modified in BeginCameraRendering
                        xrLayout.ReconfigurePass(xrPass, baseCamera);
                        XRSystemUniversal.BeginLateLatching(baseCamera, xrPassUniversal);
                    }
#endif
                    // InitializeAdditionalCameraData needs to be initialized after the cameraTargetDescriptor is set because it needs to know the
                    // msaa level of cameraTargetDescriptor and XR modifications.
                    InitializeAdditionalCameraData(baseCamera, baseCameraAdditionalData, !isStackedRendering,
                        baseCameraData);

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                    //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                    cameraXRSettings.viewTotal = baseCameraData.xr.enabled ? 2u : 1u;
                    cameraXRSettings.viewCount = baseCameraData.xr.enabled ? (uint)baseCameraData.xr.viewCount : 1u;
                    cameraXRSettings.viewOffset = (uint)baseCameraData.xr.multipassId;
                    VFX.VFXManager.PrepareCamera(baseCamera, cameraXRSettings);
#endif
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
                    if (asset.useAdaptivePerformance)
                        ApplyAdaptivePerformance(baseCameraData);
#endif
                    // update the base camera flag so that the scene depth is stored if needed by overlay cameras later in the frame
                    baseCameraData.postProcessingRequiresDepthTexture |= cameraStackRequiresDepthForPostprocessing;

                    // Check whether the camera stack final output is HDR
                    // This is equivalent of UniversalCameraData.isHDROutputActive but without necessiting the base camera to be the last camera in the stack.
                    bool hdrDisplayOutputActive = mainHdrDisplayOutputActive;
#if ENABLE_VR && ENABLE_XR_MODULE
                    // If we are rendering to xr then we need to look at the XR Display rather than the main non-xr display.
                    if (xrPass.enabled)
                        hdrDisplayOutputActive = xrPass.isHDRDisplayOutputActive;
#endif
                    finalOutputHDR =
                        asset.supportsHDR &&
                        hdrDisplayOutputActive // Check whether any HDR display is active and the render pipeline asset allows HDR rendering
                        && baseCamera.targetTexture == null &&
                        (baseCamera.cameraType == CameraType.Game ||
                         baseCamera.cameraType == CameraType.VR) // Check whether the stack outputs to a screen
                        && baseCameraData.allowHDROutput; // Check whether the base camera allows HDR output

                    // Update stack-related parameters
                    baseCameraData.stackAnyPostProcessingEnabled = anyPostProcessingEnabled;
                    baseCameraData.stackLastCameraOutputToHDR = finalOutputHDR;

                    RenderSingleCamera(context, baseCameraData);
                }

                // Late latching is not supported after this point
                if (xrPass.enabled)
                    XRSystemUniversal.EndLateLatching(baseCamera, xrPassUniversal);

                if (isStackedRendering)
                {
                    for (int i = 0; i < cameraStack.Count; ++i)
                    {
                        var overlayCamera = cameraStack[i];
                        if (!overlayCamera.isActiveAndEnabled)
                            continue;

                        overlayCamera.TryGetComponent<UniversalAdditionalCameraData>(out var overlayAdditionalCameraData);
                        // Camera is overlay and enabled
                        if (overlayAdditionalCameraData != null)
                        {
                            ContextContainer overlayFrameData = GetRenderer(overlayCamera, overlayAdditionalCameraData).frameData;
                            UniversalCameraData overlayCameraData = CreateCameraData(overlayFrameData, baseCamera, baseCameraAdditionalData, false);
#if ENABLE_VR && ENABLE_XR_MODULE
                            if (xrPass.enabled)
                            {
                                overlayCameraData.xr = xrPass;
                                UpdateCameraData(overlayCameraData, xrPass);
                            }
#endif

                            InitializeAdditionalCameraData(overlayCamera, overlayAdditionalCameraData, false, overlayCameraData);
                            overlayCameraData.camera = overlayCamera;
                            overlayCameraData.baseCamera = baseCamera;

                            UpdateCameraStereoMatrices(overlayAdditionalCameraData.camera, xrPass);

                            using (new CameraRenderingScope(context, overlayCamera))
                            {
#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                                //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                                VFX.VFXManager.PrepareCamera(overlayCamera, cameraXRSettings);
#endif
                                UpdateVolumeFramework(overlayCamera, overlayAdditionalCameraData);

                                bool lastCamera = i == lastActiveOverlayCameraIndex;
                                InitializeAdditionalCameraData(overlayCamera, overlayAdditionalCameraData, lastCamera, overlayCameraData);

                                overlayCameraData.stackAnyPostProcessingEnabled = anyPostProcessingEnabled;
                                overlayCameraData.stackLastCameraOutputToHDR = finalOutputHDR;

                                xrLayout.ReconfigurePass(overlayCameraData.xr, overlayCamera);

                                RenderSingleCamera(context, overlayCameraData);
                            }
                        }
                    }
                }
            }

            if (xrActive)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                XRSystem.RenderMirrorView(cmd, baseCamera);
                context.ExecuteCommandBuffer(cmd);
                context.Submit();
                CommandBufferPool.Release(cmd);
            }

            XRSystem.EndLayout();
        }

        // Used for updating URP cameraData data struct with XRPass data.
        static void UpdateCameraData(UniversalCameraData baseCameraData, in XRPass xr)
        {
            // Update cameraData viewport for XR
            Rect cameraRect = baseCameraData.camera.rect;
            Rect xrViewport = xr.GetViewport();
            baseCameraData.pixelRect = new Rect(cameraRect.x * xrViewport.width + xrViewport.x,
                cameraRect.y * xrViewport.height + xrViewport.y,
                cameraRect.width * xrViewport.width,
                cameraRect.height * xrViewport.height);
            Rect camPixelRect = baseCameraData.pixelRect;
            baseCameraData.pixelWidth = (int)System.Math.Round(camPixelRect.width + camPixelRect.x) - (int)System.Math.Round(camPixelRect.x);
            baseCameraData.pixelHeight = (int)System.Math.Round(camPixelRect.height + camPixelRect.y) - (int)System.Math.Round(camPixelRect.y);
            baseCameraData.aspectRatio = (float)baseCameraData.pixelWidth / (float)baseCameraData.pixelHeight;

            // Update cameraData cameraTargetDescriptor for XR. This descriptor is mainly used for configuring intermediate screen space textures
            var originalTargetDesc = baseCameraData.cameraTargetDescriptor;
            baseCameraData.cameraTargetDescriptor = xr.renderTargetDesc;
            if (baseCameraData.isHdrEnabled)
            {
                baseCameraData.cameraTargetDescriptor.graphicsFormat = originalTargetDesc.graphicsFormat;
            }
            baseCameraData.cameraTargetDescriptor.msaaSamples = originalTargetDesc.msaaSamples;

            if (baseCameraData.isDefaultViewport)
            {
                // When viewport is default, intermediate textures created with this descriptor will have dynamic resolution enabled.
                baseCameraData.cameraTargetDescriptor.useDynamicScale = true;
            }
            else
            {
                // Some effects like Vignette computes aspect ratio from width and height. We have to take viewport into consideration if it is not default viewport.
                baseCameraData.cameraTargetDescriptor.width = baseCameraData.pixelWidth;
                baseCameraData.cameraTargetDescriptor.height = baseCameraData.pixelHeight;
				baseCameraData.cameraTargetDescriptor.useDynamicScale = false;
            }
        }

        static void UpdateVolumeFramework(Camera camera, UniversalAdditionalCameraData additionalCameraData)
        {
            using var profScope = new ProfilingScope(ProfilingSampler.Get(URPProfileId.UpdateVolumeFramework));

            // We update the volume framework for:
            // * All cameras in the editor when not in playmode
            // * scene cameras
            // * cameras with update mode set to EveryFrame
            // * cameras with update mode set to UsePipelineSettings and the URP Asset set to EveryFrame
            bool shouldUpdate = camera.cameraType == CameraType.SceneView;
            shouldUpdate |= additionalCameraData != null && additionalCameraData.requiresVolumeFrameworkUpdate;

#if UNITY_EDITOR
            shouldUpdate |= Application.isPlaying == false;
#endif

            // When we have volume updates per-frame disabled...
            if (!shouldUpdate && additionalCameraData)
            {
                // If an invalid volume stack is present, destroy it
                if (additionalCameraData.volumeStack != null && !additionalCameraData.volumeStack.isValid)
                {
                    camera.DestroyVolumeStack(additionalCameraData);
                }

                // Create a local volume stack and cache the state if it's null
                if (additionalCameraData.volumeStack == null)
                {
                    camera.UpdateVolumeStack(additionalCameraData);
                }

                VolumeManager.instance.stack = additionalCameraData.volumeStack;
                return;
            }

            // When we want to update the volumes every frame...

            // We destroy the volumeStack in the additional camera data, if present, to make sure
            // it gets recreated and initialized if the update mode gets later changed to ViaScripting...
            if (additionalCameraData && additionalCameraData.volumeStack != null)
            {
                camera.DestroyVolumeStack(additionalCameraData);
            }

            // Get the mask + trigger and update the stack
            camera.GetVolumeLayerMaskAndTrigger(additionalCameraData, out LayerMask layerMask, out Transform trigger);
            VolumeManager.instance.ResetMainStack();
            VolumeManager.instance.Update(trigger, layerMask);
        }

        static bool CheckPostProcessForDepth(UniversalCameraData cameraData)
        {
            if (!cameraData.postProcessEnabled)
                return false;

            if ((cameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing || cameraData.IsTemporalAAEnabled())
                && cameraData.renderType == CameraRenderType.Base)
                return true;

            return CheckPostProcessForDepth();
        }

        static bool CheckPostProcessForDepth()
        {
            var stack = VolumeManager.instance.stack;

            if (stack.GetComponent<DepthOfField>().IsActive())
                return true;

            if (stack.GetComponent<MotionBlur>().IsActive())
                return true;

            return false;
        }

        static void SetSupportedRenderingFeatures(UniversalRenderPipelineAsset pipelineAsset)
        {
#if UNITY_EDITOR
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.None,
                defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive,
                mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive | SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly | SupportedRenderingFeatures.LightmapMixedBakeModes.Shadowmask,
                lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime,
                lightmapsModes = LightmapsMode.CombinedDirectional | LightmapsMode.NonDirectional,
                lightProbeProxyVolumes = false,
                motionVectors = true,
                receiveShadows = false,
                reflectionProbes = false,
                reflectionProbesBlendDistance = true,
                particleSystemInstancing = true,
                overridesEnableLODCrossFade = true
            };
            SceneViewDrawMode.SetupDrawMode();
#endif

            SupportedRenderingFeatures.active.supportsHDR = pipelineAsset.supportsHDR;
            SupportedRenderingFeatures.active.rendersUIOverlay = true;
        }

        static ScriptableRenderer GetRenderer(Camera camera, UniversalAdditionalCameraData additionalCameraData)
        {
            var renderer = additionalCameraData != null ? additionalCameraData.scriptableRenderer : null;
            if (renderer == null || camera.cameraType == CameraType.SceneView)
                renderer = asset.scriptableRenderer;
            return renderer;
        }

        static UniversalCameraData CreateCameraData(ContextContainer frameData, Camera camera, UniversalAdditionalCameraData additionalCameraData, bool resolveFinalTarget)
        {
            using var profScope = new ProfilingScope(Profiling.Pipeline.initializeCameraData);

            var renderer = GetRenderer(camera, additionalCameraData);
            UniversalCameraData cameraData = frameData.Create<UniversalCameraData>();
            InitializeStackedCameraData(camera, additionalCameraData, cameraData);

            cameraData.camera = camera;

            // Add reference to writable camera history to give access to injected user render passes which can produce history.
            cameraData.historyManager = additionalCameraData?.historyManager;

            ///////////////////////////////////////////////////////////////////
            // Descriptor settings                                            /
            ///////////////////////////////////////////////////////////////////

            bool rendererSupportsMSAA = renderer != null && renderer.supportedRenderingFeatures.msaa;

            int msaaSamples = 1;
            if (camera.allowMSAA && asset.msaaSampleCount > 1 && rendererSupportsMSAA)
                msaaSamples = (camera.targetTexture != null) ? camera.targetTexture.antiAliasing : asset.msaaSampleCount;

            // Use XR's MSAA if camera is XR camera. XR MSAA needs special handle here because it is not per Camera.
            // Multiple cameras could render into the same XR display and they should share the same MSAA level.
            // However it should still respect the sample count of the target texture camera is rendering to.
            if (cameraData.xrRendering && rendererSupportsMSAA && camera.targetTexture == null)
                msaaSamples = (int)XRSystem.GetDisplayMSAASamples();

            bool needsAlphaChannel = Graphics.preserveFramebufferAlpha;

            cameraData.hdrColorBufferPrecision = asset ? asset.hdrColorBufferPrecision : HDRColorBufferPrecision._32Bits;
            cameraData.cameraTargetDescriptor = CreateRenderTextureDescriptor(camera, cameraData,
                cameraData.isHdrEnabled, cameraData.hdrColorBufferPrecision, msaaSamples, needsAlphaChannel, cameraData.requiresOpaqueTexture);

            uint count = GraphicsFormatUtility.GetAlphaComponentCount(cameraData.cameraTargetDescriptor.graphicsFormat);
            cameraData.isAlphaOutputEnabled = GraphicsFormatUtility.HasAlphaChannel(cameraData.cameraTargetDescriptor.graphicsFormat);
            if (cameraData.camera.cameraType == CameraType.SceneView && CoreUtils.IsSceneFilteringEnabled())
                cameraData.isAlphaOutputEnabled = true;

            return cameraData;
        }

        /// <summary>
        /// Initialize camera data settings common for all cameras in the stack. Overlay cameras will inherit
        /// settings from base camera.
        /// </summary>
        /// <param name="baseCamera">Base camera to inherit settings from.</param>
        /// <param name="baseAdditionalCameraData">Component that contains additional base camera data.</param>
        /// <param name="cameraData">Camera data to initialize setttings.</param>
        static void InitializeStackedCameraData(Camera baseCamera, UniversalAdditionalCameraData baseAdditionalCameraData, UniversalCameraData cameraData)
        {
            using var profScope = new ProfilingScope(Profiling.Pipeline.initializeStackedCameraData);

            var settings = asset;
            cameraData.targetTexture = baseCamera.targetTexture;
            cameraData.cameraType = baseCamera.cameraType;
            bool isSceneViewCamera = cameraData.isSceneViewCamera;

            ///////////////////////////////////////////////////////////////////
            // Environment and Post-processing settings                       /
            ///////////////////////////////////////////////////////////////////
            if (isSceneViewCamera)
            {
                cameraData.volumeLayerMask = 1; // "Default"
                cameraData.volumeTrigger = null;
                cameraData.isStopNaNEnabled = false;
                cameraData.isDitheringEnabled = false;
                cameraData.antialiasing = AntialiasingMode.None;
                cameraData.antialiasingQuality = AntialiasingQuality.High;
                cameraData.xrRendering = false;
                cameraData.allowHDROutput = false;
            }
            else if (baseAdditionalCameraData != null)
            {
                cameraData.volumeLayerMask = baseAdditionalCameraData.volumeLayerMask;
                cameraData.volumeTrigger = baseAdditionalCameraData.volumeTrigger == null ? baseCamera.transform : baseAdditionalCameraData.volumeTrigger;
                cameraData.isStopNaNEnabled = baseAdditionalCameraData.stopNaN && SystemInfo.graphicsShaderLevel >= 35;
                cameraData.isDitheringEnabled = baseAdditionalCameraData.dithering;
                cameraData.antialiasing = baseAdditionalCameraData.antialiasing;
                cameraData.antialiasingQuality = baseAdditionalCameraData.antialiasingQuality;
                cameraData.xrRendering = baseAdditionalCameraData.allowXRRendering && XRSystem.displayActive;
                cameraData.allowHDROutput = baseAdditionalCameraData.allowHDROutput;
            }
            else
            {
                cameraData.volumeLayerMask = 1; // "Default"
                cameraData.volumeTrigger = null;
                cameraData.isStopNaNEnabled = false;
                cameraData.isDitheringEnabled = false;
                cameraData.antialiasing = AntialiasingMode.None;
                cameraData.antialiasingQuality = AntialiasingQuality.High;
                cameraData.xrRendering = XRSystem.displayActive;
                cameraData.allowHDROutput = true;
            }

            ///////////////////////////////////////////////////////////////////
            // Settings that control output of the camera                     /
            ///////////////////////////////////////////////////////////////////

            cameraData.isHdrEnabled = baseCamera.allowHDR && settings.supportsHDR;
            cameraData.allowHDROutput &= settings.supportsHDR;

            Rect cameraRect = baseCamera.rect;
            cameraData.pixelRect = baseCamera.pixelRect;
            cameraData.pixelWidth = baseCamera.pixelWidth;
            cameraData.pixelHeight = baseCamera.pixelHeight;
            cameraData.aspectRatio = (float)cameraData.pixelWidth / (float)cameraData.pixelHeight;
            cameraData.isDefaultViewport = (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f ||
                Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f));

            bool isScenePreviewOrReflectionCamera = cameraData.cameraType == CameraType.SceneView || cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection;

            // Discard variations lesser than kRenderScaleThreshold.
            // Scale is only enabled for gameview.
            const float kRenderScaleThreshold = 0.05f;
            bool disableRenderScale = ((Mathf.Abs(1.0f - settings.renderScale) < kRenderScaleThreshold) || isScenePreviewOrReflectionCamera);
            cameraData.renderScale = disableRenderScale ? 1.0f : settings.renderScale;

            bool enableRenderGraph =
                GraphicsSettings.TryGetRenderPipelineSettings<RenderGraphSettings>(out var renderGraphSettings) &&
                !renderGraphSettings.enableRenderCompatibilityMode;

            // Convert the upscaling filter selection from the pipeline asset into an image upscaling filter
            cameraData.upscalingFilter = ResolveUpscalingFilterSelection(new Vector2(cameraData.pixelWidth, cameraData.pixelHeight), cameraData.renderScale, settings.upscalingFilter, enableRenderGraph);

            if (cameraData.renderScale > 1.0f)
            {
                cameraData.imageScalingMode = ImageScalingMode.Downscaling;
            }
            else if ((cameraData.renderScale < 1.0f) || (!isScenePreviewOrReflectionCamera && ((cameraData.upscalingFilter == ImageUpscalingFilter.FSR) || (cameraData.upscalingFilter == ImageUpscalingFilter.STP))))
            {
                // When certain upscalers are enabled, we still consider 100% render scale an upscaling operation. (This behavior is only intended for game view cameras)
                // This allows us to run the upscaling shader passes all the time since they improve visual quality even at 100% scale.

                cameraData.imageScalingMode = ImageScalingMode.Upscaling;

                // When STP is enabled, we force temporal anti-aliasing on since it's a prerequisite.
                if (cameraData.upscalingFilter == ImageUpscalingFilter.STP)
                {
                    cameraData.antialiasing = AntialiasingMode.TemporalAntiAliasing;
                }
            }
            else
            {
                cameraData.imageScalingMode = ImageScalingMode.None;
            }

            cameraData.fsrOverrideSharpness = settings.fsrOverrideSharpness;
            cameraData.fsrSharpness = settings.fsrSharpness;

            cameraData.xr = XRSystem.emptyPass;
            XRSystem.SetRenderScale(cameraData.renderScale);

            var commonOpaqueFlags = SortingCriteria.CommonOpaque;
            var noFrontToBackOpaqueFlags = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;
            bool hasHSRGPU = SystemInfo.hasHiddenSurfaceRemovalOnGPU;
            bool canSkipFrontToBackSorting = (baseCamera.opaqueSortMode == OpaqueSortMode.Default && hasHSRGPU) || baseCamera.opaqueSortMode == OpaqueSortMode.NoDistanceSort;

            cameraData.defaultOpaqueSortFlags = canSkipFrontToBackSorting ? noFrontToBackOpaqueFlags : commonOpaqueFlags;
            cameraData.captureActions = Unity.RenderPipelines.Core.Runtime.Shared.CameraCaptureBridge.GetCachedCaptureActionsEnumerator(baseCamera);
        }

        /// <summary>
        /// Initialize settings that can be different for each camera in the stack.
        /// </summary>
        /// <param name="camera">Camera to initialize settings from.</param>
        /// <param name="additionalCameraData">Additional camera data component to initialize settings from.</param>
        /// <param name="resolveFinalTarget">True if this is the last camera in the stack and rendering should resolve to camera target.</param>
        /// <param name="cameraData">Settings to be initilized.</param>
        static void InitializeAdditionalCameraData(Camera camera, UniversalAdditionalCameraData additionalCameraData, bool resolveFinalTarget, UniversalCameraData cameraData)
        {
            using var profScope = new ProfilingScope(Profiling.Pipeline.initializeAdditionalCameraData);

            var renderer = GetRenderer(camera, additionalCameraData);
            var settings = asset;

            bool anyShadowsEnabled = settings.supportsMainLightShadows || settings.supportsAdditionalLightShadows;
            cameraData.maxShadowDistance = Mathf.Min(settings.shadowDistance, camera.farClipPlane);
            cameraData.maxShadowDistance = (anyShadowsEnabled && cameraData.maxShadowDistance >= camera.nearClipPlane) ? cameraData.maxShadowDistance : 0.0f;

            bool isSceneViewCamera = cameraData.isSceneViewCamera;
            if (isSceneViewCamera)
            {
                cameraData.renderType = CameraRenderType.Base;
                cameraData.clearDepth = true;
                cameraData.postProcessEnabled = CoreUtils.ArePostProcessesEnabled(camera);
                cameraData.requiresDepthTexture = settings.supportsCameraDepthTexture;
                cameraData.requiresOpaqueTexture = settings.supportsCameraOpaqueTexture;
                cameraData.useScreenCoordOverride = false;
                cameraData.screenSizeOverride = cameraData.pixelRect.size;
                cameraData.screenCoordScaleBias = Vector2.one;
            }
            else if (additionalCameraData != null)
            {
                cameraData.renderType = additionalCameraData.renderType;
                cameraData.clearDepth = (additionalCameraData.renderType != CameraRenderType.Base) ? additionalCameraData.clearDepth : true;
                cameraData.postProcessEnabled = additionalCameraData.renderPostProcessing;
                cameraData.maxShadowDistance = (additionalCameraData.renderShadows) ? cameraData.maxShadowDistance : 0.0f;
                cameraData.requiresDepthTexture = additionalCameraData.requiresDepthTexture;
                cameraData.requiresOpaqueTexture = additionalCameraData.requiresColorTexture;
                cameraData.useScreenCoordOverride = additionalCameraData.useScreenCoordOverride;
                cameraData.screenSizeOverride = additionalCameraData.screenSizeOverride;
                cameraData.screenCoordScaleBias = additionalCameraData.screenCoordScaleBias;
            }
            else
            {
                cameraData.renderType = CameraRenderType.Base;
                cameraData.clearDepth = true;
                cameraData.postProcessEnabled = false;
                cameraData.requiresDepthTexture = settings.supportsCameraDepthTexture;
                cameraData.requiresOpaqueTexture = settings.supportsCameraOpaqueTexture;
                cameraData.useScreenCoordOverride = false;
                cameraData.screenSizeOverride = cameraData.pixelRect.size;
                cameraData.screenCoordScaleBias = Vector2.one;
            }

            cameraData.renderer = renderer;
            cameraData.requiresDepthTexture |= isSceneViewCamera;
            cameraData.postProcessingRequiresDepthTexture = CheckPostProcessForDepth(cameraData);
            cameraData.resolveFinalTarget = resolveFinalTarget;

            // enable GPU occlusion culling in game and scene views only
            cameraData.useGPUOcclusionCulling = GPUResidentDrawer.IsInstanceOcclusionCullingEnabled()
                && renderer.supportsGPUOcclusion
                && camera.cameraType is CameraType.SceneView or CameraType.Game or CameraType.Preview;
            cameraData.requiresDepthTexture |= cameraData.useGPUOcclusionCulling;

            // Disable depth and color copy. We should add it in the renderer instead to avoid performance pitfalls
            // of camera stacking breaking render pass execution implicitly.
            bool isOverlayCamera = (cameraData.renderType == CameraRenderType.Overlay);
            if (isOverlayCamera)
            {
                cameraData.requiresOpaqueTexture = false;
            }

            // NOTE: TAA depends on XR modifications of cameraTargetDescriptor.
            if (additionalCameraData != null)
                UpdateTemporalAAData(cameraData, additionalCameraData);

            Matrix4x4 projectionMatrix = camera.projectionMatrix;

            // Overlay cameras inherit viewport from base.
            // If the viewport is different between them we might need to patch the projection to adjust aspect ratio
            // matrix to prevent squishing when rendering objects in overlay cameras.
            if (isOverlayCamera && !camera.orthographic && cameraData.pixelRect != camera.pixelRect)
            {
                // m00 = (cotangent / aspect), therefore m00 * aspect gives us cotangent.
                float cotangent = camera.projectionMatrix.m00 * camera.aspect;

                // Get new m00 by dividing by base camera aspectRatio.
                float newCotangent = cotangent / cameraData.aspectRatio;
                projectionMatrix.m00 = newCotangent;
            }

            // TAA debug settings
            // Affects the jitter set just below. Do not move.
            ApplyTaaRenderingDebugOverrides(ref cameraData.taaSettings);

            // Depends on the cameraTargetDesc, size and MSAA also XR modifications of those.
            TemporalAA.JitterFunc jitterFunc = cameraData.IsSTPEnabled() ? StpUtils.s_JitterFunc : TemporalAA.s_JitterFunc;
            Matrix4x4 jitterMat = TemporalAA.CalculateJitterMatrix(cameraData, jitterFunc);
            cameraData.SetViewProjectionAndJitterMatrix(camera.worldToCameraMatrix, projectionMatrix, jitterMat);

            cameraData.worldSpaceCameraPos = camera.transform.position;

            var backgroundColorSRGB = camera.backgroundColor;
            // Get the background color from preferences if preview camera
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.Preview && camera.clearFlags != CameraClearFlags.SolidColor)
            {
                backgroundColorSRGB = CoreRenderPipelinePreferences.previewBackgroundColor;
            }
#endif

            cameraData.backgroundColor = CoreUtils.ConvertSRGBToActiveColorSpace(backgroundColorSRGB);

            cameraData.stackAnyPostProcessingEnabled = cameraData.postProcessEnabled;
            cameraData.stackLastCameraOutputToHDR = cameraData.isHDROutputActive;

            // Apply post-processing settings to the alpha output.
            // cameraData.isAlphaOutputEnabled is set based on target alpha channel availability on create. Target can be a RenderTexture or the back-buffer.
            bool allowAlphaOutput = !cameraData.postProcessEnabled || (cameraData.postProcessEnabled && settings.allowPostProcessAlphaOutput);
            cameraData.isAlphaOutputEnabled = cameraData.isAlphaOutputEnabled && allowAlphaOutput;
        }

        static UniversalRenderingData CreateRenderingData(ContextContainer frameData, UniversalRenderPipelineAsset settings, CommandBuffer cmd, bool isForwardPlus, ScriptableRenderer renderer)
        {
            UniversalLightData universalLightData = frameData.Get<UniversalLightData>();

            UniversalRenderingData data = frameData.Get<UniversalRenderingData>();
            data.supportsDynamicBatching = settings.supportsDynamicBatching;
            data.perObjectData = GetPerObjectLightFlags(universalLightData.additionalLightsCount, isForwardPlus);

            // Render graph does not support RenderingData.commandBuffer as its execution timeline might break.
            // RenderingData.commandBuffer is available only for the old non-RG execute code path.
            if(useRenderGraph)
                data.m_CommandBuffer = null;
            else
                data.m_CommandBuffer = cmd;

            UniversalRenderer universalRenderer = renderer as UniversalRenderer;
            if (universalRenderer != null)
            {
                data.renderingMode = universalRenderer.renderingModeActual;
                data.opaqueLayerMask = universalRenderer.opaqueLayerMask;
                data.transparentLayerMask = universalRenderer.transparentLayerMask;
            }

            return data;
        }

        static UniversalShadowData CreateShadowData(ContextContainer frameData, UniversalRenderPipelineAsset urpAsset, bool isForwardPlus)
        {
            using var profScope = new ProfilingScope(Profiling.Pipeline.initializeShadowData);

            // Initial setup
            // ------------------------------------------------------
            UniversalShadowData shadowData = frameData.Create<UniversalShadowData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            m_ShadowBiasData.Clear();
            m_ShadowResolutionData.Clear();

            shadowData.shadowmapDepthBufferBits = 16;
            shadowData.mainLightShadowCascadeBorder = urpAsset.cascadeBorder;
            shadowData.mainLightShadowCascadesCount = urpAsset.shadowCascadeCount;
            shadowData.mainLightShadowCascadesSplit = GetMainLightCascadeSplit(shadowData.mainLightShadowCascadesCount, urpAsset);
            shadowData.mainLightShadowmapWidth = urpAsset.mainLightShadowmapResolution;
            shadowData.mainLightShadowmapHeight = urpAsset.mainLightShadowmapResolution;
            shadowData.additionalLightsShadowmapWidth = shadowData.additionalLightsShadowmapHeight = urpAsset.additionalLightsShadowmapResolution;

            // This will be setup in AdditionalLightsShadowCasterPass.
            shadowData.isKeywordAdditionalLightShadowsEnabled = false;
            shadowData.isKeywordSoftShadowsEnabled = false;

            // Those fields must be setup after ApplyAdaptivePerformance is called on RenderingData.
            // This is because this function can currently modify mainLightShadowmapWidth, mainLightShadowmapHeight and mainLightShadowCascadesCount.
            // All three parameters are needed to compute those fields, so their initialization is deferred to InitializeMainLightShadowResolution.
            shadowData.mainLightShadowResolution = 0;
            shadowData.mainLightRenderTargetWidth = 0;
            shadowData.mainLightRenderTargetHeight = 0;

            // Those two fields must be initialized using ShadowData, which can be modified right after this function (InitializeRenderingData) by ApplyAdaptivePerformance.
            // Their initializations is thus deferred to a later point when ShadowData is fully initialized.
            shadowData.shadowAtlasLayout = default;
            shadowData.visibleLightsShadowCullingInfos = default;

            // Setup data that requires iterating over lights
            // ------------------------------------------------------
            var mainLightIndex = lightData.mainLightIndex;
            var visibleLights = lightData.visibleLights;

            // maxShadowDistance is set to 0.0f when the Render Shadows toggle is disabled on the camera
            bool cameraRenderShadows = cameraData.maxShadowDistance > 0.0f;

            shadowData.mainLightShadowsEnabled = urpAsset.supportsMainLightShadows && urpAsset.mainLightRenderingMode == LightRenderingMode.PerPixel;
            shadowData.supportsMainLightShadows = SystemInfo.supportsShadows && shadowData.mainLightShadowsEnabled && cameraRenderShadows;

            shadowData.additionalLightShadowsEnabled = urpAsset.supportsAdditionalLightShadows && (urpAsset.additionalLightsRenderingMode == LightRenderingMode.PerPixel || isForwardPlus);
            shadowData.supportsAdditionalLightShadows = SystemInfo.supportsShadows && shadowData.additionalLightShadowsEnabled && !lightData.shadeAdditionalLightsPerVertex && cameraRenderShadows;

            // Early out if shadows are not rendered...
            if (!shadowData.supportsMainLightShadows && !shadowData.supportsAdditionalLightShadows)
                return shadowData;

            shadowData.supportsMainLightShadows &= mainLightIndex != -1
                                                   && visibleLights[mainLightIndex].light != null
                                                   && visibleLights[mainLightIndex].light.shadows != LightShadows.None;

            if (shadowData.supportsAdditionalLightShadows)
            {
                // Check if there is at least one additional light casting shadows...
                bool additionalLightsCastShadows = false;
                for (int i = 0; i < visibleLights.Length; ++i)
                {
                    if (i == mainLightIndex)
                        continue;

                    ref VisibleLight vl = ref visibleLights.UnsafeElementAtMutable(i);

                    // UniversalRP doesn't support additional directional light shadows yet
                    if (vl.lightType == LightType.Spot || vl.lightType == LightType.Point)
                    {
                        Light light = vl.light;
                        if (light == null || light.shadows == LightShadows.None)
                            continue;

                        additionalLightsCastShadows = true;
                        break;
                    }
                }
                shadowData.supportsAdditionalLightShadows &= additionalLightsCastShadows;
            }

            // Check again if it's possible to early out...
            if (!shadowData.supportsMainLightShadows && !shadowData.supportsAdditionalLightShadows)
                return shadowData;

            for (int i = 0; i < visibleLights.Length; ++i)
            {
                if (!shadowData.supportsMainLightShadows && i == mainLightIndex)
                {
                    m_ShadowBiasData.Add(Vector4.zero);
                    m_ShadowResolutionData.Add(0);
                    continue;
                }

                if (!shadowData.supportsAdditionalLightShadows && i != mainLightIndex)
                {
                    m_ShadowBiasData.Add(Vector4.zero);
                    m_ShadowResolutionData.Add(0);
                    continue;
                }

                ref VisibleLight vl = ref visibleLights.UnsafeElementAtMutable(i);
                Light light = vl.light;
                UniversalAdditionalLightData data = null;
                if (light != null)
                {
                    light.gameObject.TryGetComponent(out data);
                }

                if (data && !data.usePipelineSettings)
                    m_ShadowBiasData.Add(new Vector4(light.shadowBias, light.shadowNormalBias, 0.0f, 0.0f));
                else
                    m_ShadowBiasData.Add(new Vector4(urpAsset.shadowDepthBias, urpAsset.shadowNormalBias, 0.0f, 0.0f));

                if (data && (data.additionalLightsShadowResolutionTier == UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierCustom))
                {
                    m_ShadowResolutionData.Add((int)light.shadowResolution); // native code does not clamp light.shadowResolution between -1 and 3
                }
                else if (data && (data.additionalLightsShadowResolutionTier != UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierCustom))
                {
                    int resolutionTier = Mathf.Clamp(data.additionalLightsShadowResolutionTier, UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow, UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh);
                    m_ShadowResolutionData.Add(urpAsset.GetAdditionalLightsShadowResolution(resolutionTier));
                }
                else
                {
                    m_ShadowResolutionData.Add(urpAsset.GetAdditionalLightsShadowResolution(UniversalAdditionalLightData.AdditionalLightsShadowDefaultResolutionTier));
                }
            }

            shadowData.bias = m_ShadowBiasData;
            shadowData.resolution = m_ShadowResolutionData;
            shadowData.supportsSoftShadows = urpAsset.supportsSoftShadows && (shadowData.supportsMainLightShadows || shadowData.supportsAdditionalLightShadows);

            return shadowData;
        }

        private static Vector3 GetMainLightCascadeSplit(int mainLightShadowCascadesCount, UniversalRenderPipelineAsset urpAsset)
        {
            switch (mainLightShadowCascadesCount)
            {
                case 1:  return new Vector3(1.0f, 0.0f, 0.0f);
                case 2:  return new Vector3(urpAsset.cascade2Split, 1.0f, 0.0f);
                case 3:  return urpAsset.cascade3Split;
                default: return urpAsset.cascade4Split;
            }
        }

        static void InitializeMainLightShadowResolution(UniversalShadowData shadowData)
        {
            shadowData.mainLightShadowResolution = ShadowUtils.GetMaxTileResolutionInAtlas(shadowData.mainLightShadowmapWidth, shadowData.mainLightShadowmapHeight, shadowData.mainLightShadowCascadesCount);
            shadowData.mainLightRenderTargetWidth = shadowData.mainLightShadowmapWidth;
            shadowData.mainLightRenderTargetHeight = (shadowData.mainLightShadowCascadesCount == 2) ? shadowData.mainLightShadowmapHeight >> 1 : shadowData.mainLightShadowmapHeight;
        }

        static UniversalPostProcessingData CreatePostProcessingData(ContextContainer frameData, UniversalRenderPipelineAsset settings)
        {
            UniversalPostProcessingData postProcessingData = frameData.Create<UniversalPostProcessingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            postProcessingData.isEnabled = cameraData.stackAnyPostProcessingEnabled;

            postProcessingData.gradingMode = settings.supportsHDR
                ? settings.colorGradingMode
                : ColorGradingMode.LowDynamicRange;

            if (cameraData.stackLastCameraOutputToHDR)
                postProcessingData.gradingMode = ColorGradingMode.HighDynamicRange;

            postProcessingData.lutSize = settings.colorGradingLutSize;
            postProcessingData.useFastSRGBLinearConversion = settings.useFastSRGBLinearConversion;
            postProcessingData.supportScreenSpaceLensFlare = settings.supportScreenSpaceLensFlare;
            postProcessingData.supportDataDrivenLensFlare = settings.supportDataDrivenLensFlare;

            return postProcessingData;
        }

        static UniversalResourceData CreateUniversalResourceData(ContextContainer frameData)
        {
            return frameData.Create<UniversalResourceData>();
        }

        static UniversalLightData CreateLightData(ContextContainer frameData, UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights)
        {
            using var profScope = new ProfilingScope(Profiling.Pipeline.initializeLightData);

            UniversalLightData lightData = frameData.Create<UniversalLightData>();

            lightData.mainLightIndex = GetMainLightIndex(settings, visibleLights);

            if (settings.additionalLightsRenderingMode != LightRenderingMode.Disabled)
            {
                lightData.additionalLightsCount = Math.Min((lightData.mainLightIndex != -1) ? visibleLights.Length - 1 : visibleLights.Length, maxVisibleAdditionalLights);
                lightData.maxPerObjectAdditionalLightsCount = Math.Min(settings.maxAdditionalLightsCount, maxPerObjectLights);
            }
            else
            {
                lightData.additionalLightsCount = 0;
                lightData.maxPerObjectAdditionalLightsCount = 0;
            }

            lightData.supportsAdditionalLights = settings.additionalLightsRenderingMode != LightRenderingMode.Disabled;
            lightData.shadeAdditionalLightsPerVertex = settings.additionalLightsRenderingMode == LightRenderingMode.PerVertex;
            lightData.visibleLights = visibleLights;
            lightData.supportsMixedLighting = settings.supportsMixedLighting;
            lightData.reflectionProbeBlending = settings.reflectionProbeBlending;
            lightData.reflectionProbeBoxProjection = settings.reflectionProbeBoxProjection;
            lightData.supportsLightLayers = RenderingUtils.SupportsLightLayers(SystemInfo.graphicsDeviceType) && settings.useRenderingLayers;

            return lightData;
        }

        private static void ApplyTaaRenderingDebugOverrides(ref TemporalAA.Settings taaSettings)
        {
            var debugDisplaySettings = UniversalRenderPipelineDebugDisplaySettings.Instance;
            DebugDisplaySettingsRendering renderingSettings = debugDisplaySettings.renderingSettings;
            switch (renderingSettings.taaDebugMode)
            {
                case DebugDisplaySettingsRendering.TaaDebugMode.ShowClampedHistory:
                    taaSettings.m_FrameInfluence = 0;
                    break;

                case DebugDisplaySettingsRendering.TaaDebugMode.ShowRawFrame:
                    taaSettings.m_FrameInfluence = 1;
                    break;

                case DebugDisplaySettingsRendering.TaaDebugMode.ShowRawFrameNoJitter:
                    taaSettings.m_FrameInfluence = 1;
                    taaSettings.jitterScale = 0;
                    break;
            }
        }

        private static void UpdateTemporalAAData(UniversalCameraData cameraData, UniversalAdditionalCameraData additionalCameraData)
        {
            // Always request the TAA history data here in order to fit the existing URP structure.
            additionalCameraData.historyManager.RequestAccess<TaaHistory>();
            cameraData.taaHistory = additionalCameraData.historyManager.GetHistoryForWrite<TaaHistory>();

            if (cameraData.IsSTPEnabled())
            {
                additionalCameraData.historyManager.RequestAccess<StpHistory>();
                cameraData.stpHistory = additionalCameraData.historyManager.GetHistoryForWrite<StpHistory>();
            }

            // Update TAA settings
            ref var taaSettings = ref additionalCameraData.taaSettings;
            cameraData.taaSettings = taaSettings;

            // Decrease history clear counter. Typically clear is only 1 frame, but can be many for XR multipass eyes!
            taaSettings.resetHistoryFrames -= taaSettings.resetHistoryFrames > 0 ? 1 : 0;
        }

        private static void UpdateTemporalAATargets(UniversalCameraData cameraData)
        {
            if (cameraData.IsTemporalAAEnabled())
            {
                bool xrMultipassEnabled = false;
#if ENABLE_VR && ENABLE_XR_MODULE
                xrMultipassEnabled = cameraData.xr.enabled && !cameraData.xr.singlePassEnabled;
#endif
                bool allocation;
                if (cameraData.IsSTPEnabled())
                {
                    Debug.Assert(cameraData.stpHistory != null);

                    // When STP is active, we don't require the full set of resources needed by TAA.
                    cameraData.taaHistory.Reset();

                    allocation = cameraData.stpHistory.Update(cameraData);
                }
                else
                {
                    allocation = cameraData.taaHistory.Update(ref cameraData.cameraTargetDescriptor, xrMultipassEnabled);
                }

                // Fill new history with current frame
                // XR Multipass renders a "frame" per eye
                if (allocation)
                    cameraData.taaSettings.resetHistoryFrames += xrMultipassEnabled ? 2 : 1;
            }
            else
            {
                cameraData.taaHistory.Reset();   // TAA GPUResources is explicitly released if the feature is turned off. We could refactor this to rely on the type request and the "gc" only.

                // In the case where STP is enabled, but TAA gets disabled for various reasons, we should release the STP history resources
                if (cameraData.IsSTPEnabled())
                    cameraData.stpHistory.Reset();
            }
        }

        static void UpdateCameraStereoMatrices(Camera camera, XRPass xr)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
            {
                if (xr.singlePassEnabled)
                {
                    for (int i = 0; i < Mathf.Min(2, xr.viewCount); i++)
                    {
                        camera.SetStereoProjectionMatrix((Camera.StereoscopicEye)i, xr.GetProjMatrix(i));
                        camera.SetStereoViewMatrix((Camera.StereoscopicEye)i, xr.GetViewMatrix(i));
                    }
                }
                else
                {
                    camera.SetStereoProjectionMatrix((Camera.StereoscopicEye)xr.multipassId, xr.GetProjMatrix(0));
                    camera.SetStereoViewMatrix((Camera.StereoscopicEye)xr.multipassId, xr.GetViewMatrix(0));
                }
            }
#endif
        }

        static PerObjectData GetPerObjectLightFlags(int additionalLightsCount, bool isForwardPlus)
        {
            using var profScope = new ProfilingScope(Profiling.Pipeline.getPerObjectLightFlags);

            var configuration = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.OcclusionProbe | PerObjectData.ShadowMask;

            if (!isForwardPlus)
            {
                configuration |= PerObjectData.ReflectionProbes | PerObjectData.LightData;
            }

            if (additionalLightsCount > 0 && !isForwardPlus)
            {
                // In this case we also need per-object indices (unity_LightIndices)
                if (!RenderingUtils.useStructuredBuffer)
                    configuration |= PerObjectData.LightIndices;
            }

            return configuration;
        }

        // Main Light is always a directional light
        static int GetMainLightIndex(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights)
        {
            using var profScope = new ProfilingScope(Profiling.Pipeline.getMainLightIndex);

            int totalVisibleLights = visibleLights.Length;

            if (totalVisibleLights == 0 || settings.mainLightRenderingMode != LightRenderingMode.PerPixel)
                return -1;


            Light sunLight = RenderSettings.sun;
            int brightestDirectionalLightIndex = -1;
            float brightestLightIntensity = 0.0f;
            for (int i = 0; i < totalVisibleLights; ++i)
            {
                ref VisibleLight currVisibleLight = ref visibleLights.UnsafeElementAtMutable(i);
                Light currLight = currVisibleLight.light;

                // Particle system lights have the light property as null. We sort lights so all particles lights
                // come last. Therefore, if first light is particle light then all lights are particle lights.
                // In this case we either have no main light or already found it.
                if (currLight == null)
                    break;

                if (currVisibleLight.lightType == LightType.Directional)
                {
                    // Sun source needs be a directional light
                    if (currLight == sunLight)
                        return i;

                    // In case no sun light is present we will return the brightest directional light
                    if (currLight.intensity > brightestLightIntensity)
                    {
                        brightestLightIntensity = currLight.intensity;
                        brightestDirectionalLightIndex = i;
                    }
                }
            }

            return brightestDirectionalLightIndex;
        }

        void SetupPerFrameShaderConstants()
        {
            using var profScope = new ProfilingScope(Profiling.Pipeline.setupPerFrameShaderConstants);

            // Required for 2D Unlit Shadergraph master node as it doesn't currently support hidden properties.
            Shader.SetGlobalColor(ShaderPropertyId.rendererColor, Color.white);

            Texture2D ditheringTexture = null;
            switch (asset.lodCrossFadeDitheringType)
            {
                case LODCrossFadeDitheringType.BayerMatrix:
                    ditheringTexture = runtimeTextures.bayerMatrixTex;
                    break;
                case LODCrossFadeDitheringType.BlueNoise:
                    ditheringTexture = runtimeTextures.blueNoise64LTex;
                    break;
                default:
                    Debug.LogWarning($"This Lod Cross Fade Dithering Type is not supported: {asset.lodCrossFadeDitheringType}");
                    break;
            }

            if (ditheringTexture != null)
            {
                Shader.SetGlobalFloat(ShaderPropertyId.ditheringTextureInvSize, 1.0f / ditheringTexture.width);
                Shader.SetGlobalTexture(ShaderPropertyId.ditheringTexture, ditheringTexture);
            }
        }

        static void SetupPerCameraShaderConstants(CommandBuffer cmd)
        {
            using var profScope = new ProfilingScope(Profiling.Pipeline.setupPerCameraShaderConstants);

            // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
            SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
            Color linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
            Color glossyEnvColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
            cmd.SetGlobalVector(ShaderPropertyId.glossyEnvironmentColor, glossyEnvColor);

            // Used as fallback cubemap for reflections
            cmd.SetGlobalTexture(ShaderPropertyId.glossyEnvironmentCubeMap, ReflectionProbe.defaultTexture);
            cmd.SetGlobalVector(ShaderPropertyId.glossyEnvironmentCubeMapHDR, ReflectionProbe.defaultTextureHDRDecodeValues);

            // Ambient
            cmd.SetGlobalVector(ShaderPropertyId.ambientSkyColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientSkyColor));
            cmd.SetGlobalVector(ShaderPropertyId.ambientEquatorColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientEquatorColor));
            cmd.SetGlobalVector(ShaderPropertyId.ambientGroundColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientGroundColor));

            // Used when subtractive mode is selected
            cmd.SetGlobalVector(ShaderPropertyId.subtractiveShadowColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor));
        }

        static void CheckAndApplyDebugSettings(ref RenderingData renderingData)
        {
            var debugDisplaySettings = UniversalRenderPipelineDebugDisplaySettings.Instance;
            ref CameraData cameraData = ref renderingData.cameraData;

            if (debugDisplaySettings.AreAnySettingsActive && !cameraData.isPreviewCamera)
            {
                DebugDisplaySettingsRendering renderingSettings = debugDisplaySettings.renderingSettings;
                int msaaSamples = cameraData.cameraTargetDescriptor.msaaSamples;

                if (!renderingSettings.enableMsaa)
                    msaaSamples = 1;

                if (!renderingSettings.enableHDR)
                    cameraData.isHdrEnabled = false;

                if (!debugDisplaySettings.IsPostProcessingAllowed)
                    cameraData.postProcessEnabled = false;

                cameraData.hdrColorBufferPrecision = asset ? asset.hdrColorBufferPrecision : HDRColorBufferPrecision._32Bits;
                cameraData.cameraTargetDescriptor.graphicsFormat = MakeRenderTextureGraphicsFormat(cameraData.isHdrEnabled, cameraData.hdrColorBufferPrecision, true);
                cameraData.cameraTargetDescriptor.msaaSamples = msaaSamples;

            }
        }

        /// <summary>
        /// Returns the best supported image upscaling filter based on the provided upscaling filter selection
        /// </summary>
        /// <param name="imageSize">Size of the final image</param>
        /// <param name="renderScale">Scale being applied to the final image size</param>
        /// <param name="selection">Upscaling filter selected by the user</param>
        /// <returns>Either the original filter provided, or the best replacement available</returns>
        static ImageUpscalingFilter ResolveUpscalingFilterSelection(Vector2 imageSize, float renderScale, UpscalingFilterSelection selection, bool enableRenderGraph)
        {
            // By default we just use linear filtering since it's the most compatible choice
            ImageUpscalingFilter filter = ImageUpscalingFilter.Linear;

            // Fall back to the automatic filter if the selected filter isn't supported on the current platform or rendering environment
            if (((selection == UpscalingFilterSelection.FSR) && (!FSRUtils.IsSupported()))
                || ((selection == UpscalingFilterSelection.STP) && (!STP.IsSupported() || !enableRenderGraph))
            )
            {
                selection = UpscalingFilterSelection.Auto;
            }

            switch (selection)
            {
                case UpscalingFilterSelection.Auto:
                {
                    // The user selected "auto" for their upscaling filter so we should attempt to choose the best filter
                    // for the current situation. When the current resolution and render scale are compatible with integer
                    // scaling we use the point sampling filter. Otherwise we just use the default filter (linear).
                    float pixelScale = (1.0f / renderScale);
                    bool isIntegerScale = Mathf.Approximately((pixelScale - Mathf.Floor(pixelScale)), 0.0f);

                    if (isIntegerScale)
                    {
                        float widthScale = (imageSize.x / pixelScale);
                        float heightScale = (imageSize.y / pixelScale);

                        bool isImageCompatible = (Mathf.Approximately((widthScale - Mathf.Floor(widthScale)), 0.0f) &&
                                                  Mathf.Approximately((heightScale - Mathf.Floor(heightScale)), 0.0f));

                        if (isImageCompatible)
                        {
                            filter = ImageUpscalingFilter.Point;
                        }
                    }

                    break;
                }

                case UpscalingFilterSelection.Linear:
                {
                    // Do nothing since linear is already the default

                    break;
                }

                case UpscalingFilterSelection.Point:
                {
                    filter = ImageUpscalingFilter.Point;

                    break;
                }

                case UpscalingFilterSelection.FSR:
                {
                    filter = ImageUpscalingFilter.FSR;

                    break;
                }

                case UpscalingFilterSelection.STP:
                {
                    filter = ImageUpscalingFilter.STP;

                    break;
                }
            }

            return filter;
        }

        /// <summary>
        /// Checks if the hardware (main display and platform) and the render pipeline support HDR.
        /// </summary>
        /// <returns>True if the main display and platform support HDR and HDR output is enabled on the platform.</returns>
        internal static bool HDROutputForMainDisplayIsActive()
        {
            bool hdrOutputSupported = SystemInfo.hdrDisplaySupportFlags.HasFlag(HDRDisplaySupportFlags.Supported) && asset.supportsHDR;
            bool hdrOutputActive = HDROutputSettings.main.available && HDROutputSettings.main.active;
            return hdrOutputSupported && hdrOutputActive;
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

        // We only want to enable HDR Output for the game view once
        // since the game itself might want to control this
        internal bool enableHDROnce = true;

        /// <summary>
        /// Configures the render pipeline to render to HDR output or disables HDR output.
        /// </summary>
#if UNITY_2021_1_OR_NEWER
        void SetHDRState(List<Camera> cameras)
#else
        void SetHDRState(Camera[] cameras)
#endif
        {
            bool hdrOutputActive = HDROutputSettings.main.available && HDROutputSettings.main.active;

            // If the pipeline doesn't support HDR rendering, output to SDR.
            bool supportsSwitchingHDR = SystemInfo.hdrDisplaySupportFlags.HasFlag(HDRDisplaySupportFlags.RuntimeSwitchable);
            bool switchHDRToSDR = supportsSwitchingHDR && !asset.supportsHDR && hdrOutputActive;
            if (switchHDRToSDR)
            {
                HDROutputSettings.main.RequestHDRModeChange(false);
            }

#if UNITY_EDITOR
            bool requestedHDRModeChange = false;

            // Automatically switch to HDR in the editor if it's available
            if (supportsSwitchingHDR && asset.supportsHDR && PlayerSettings.useHDRDisplay && HDROutputSettings.main.available)
            {
#if UNITY_2021_1_OR_NEWER
                int cameraCount = cameras.Count;
#else
                int cameraCount = cameras.Length;
#endif
                if (cameraCount > 0 && cameras[0].cameraType != CameraType.Game)
                {
                    requestedHDRModeChange = hdrOutputActive;
                    HDROutputSettings.main.RequestHDRModeChange(false);
                }
                else if (enableHDROnce)
                {
                    requestedHDRModeChange = !hdrOutputActive;
                    HDROutputSettings.main.RequestHDRModeChange(true);
                    enableHDROnce = false;
                }
            }

            if (requestedHDRModeChange || switchHDRToSDR)
            {
                // Repaint scene views and game views so the HDR mode request is applied
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
#endif

            // Make sure HDR auto tonemap is off if the URP is handling it
            if (hdrOutputActive)
            {
                HDROutputSettings.main.automaticHDRTonemapping = false;
            }
        }

        internal static void GetHDROutputLuminanceParameters(HDROutputUtils.HDRDisplayInformation hdrDisplayInformation, ColorGamut hdrDisplayColorGamut, Tonemapping tonemapping, out Vector4 hdrOutputParameters)
        {
            float minNits = hdrDisplayInformation.minToneMapLuminance;
            float maxNits = hdrDisplayInformation.maxToneMapLuminance;
            float paperWhite = hdrDisplayInformation.paperWhiteNits;

            if (!tonemapping.detectPaperWhite.value)
            {
                paperWhite = tonemapping.paperWhite.value;
            }
            if (!tonemapping.detectBrightnessLimits.value)
            {
                minNits = tonemapping.minNits.value;
                maxNits = tonemapping.maxNits.value;
            }

            hdrOutputParameters = new Vector4(minNits, maxNits, paperWhite, 1f / paperWhite);
        }

        internal static void GetHDROutputGradingParameters(Tonemapping tonemapping, out Vector4 hdrOutputParameters)
        {
            int eetfMode = 0;
            float hueShift = 0.0f;

            switch (tonemapping.mode.value)
            {
                case TonemappingMode.Neutral:
                    eetfMode = (int)tonemapping.neutralHDRRangeReductionMode.value;
                    hueShift = tonemapping.hueShiftAmount.value;
                    break;

                case TonemappingMode.ACES:
                    eetfMode = (int)tonemapping.acesPreset.value;
                    break;
            }

            hdrOutputParameters = new Vector4(eetfMode, hueShift, 0.0f, 0.0f);
        }

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
        static void ApplyAdaptivePerformance(UniversalCameraData cameraData)
        {
            var noFrontToBackOpaqueFlags = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;
            if (AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipFrontToBackSorting)
                cameraData.defaultOpaqueSortFlags = noFrontToBackOpaqueFlags;

            var MaxShadowDistanceMultiplier = AdaptivePerformance.AdaptivePerformanceRenderSettings.MaxShadowDistanceMultiplier;
            cameraData.maxShadowDistance *= MaxShadowDistanceMultiplier;

            var RenderScaleMultiplier = AdaptivePerformance.AdaptivePerformanceRenderSettings.RenderScaleMultiplier;
            cameraData.renderScale *= RenderScaleMultiplier;

            // TODO
            if (!cameraData.xr.enabled)
            {
                cameraData.cameraTargetDescriptor.width = (int)(cameraData.camera.pixelWidth * cameraData.renderScale);
                cameraData.cameraTargetDescriptor.height = (int)(cameraData.camera.pixelHeight * cameraData.renderScale);
            }

            var antialiasingQualityIndex = (int)cameraData.antialiasingQuality - AdaptivePerformance.AdaptivePerformanceRenderSettings.AntiAliasingQualityBias;
            if (antialiasingQualityIndex < 0)
                cameraData.antialiasing = AntialiasingMode.None;
            cameraData.antialiasingQuality = (AntialiasingQuality)Mathf.Clamp(antialiasingQualityIndex, (int)AntialiasingQuality.Low, (int)AntialiasingQuality.High);
        }

        static void ApplyAdaptivePerformance(ContextContainer frameData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

            if (AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipDynamicBatching)
                renderingData.supportsDynamicBatching = false;

            var MainLightShadowmapResolutionMultiplier = AdaptivePerformance.AdaptivePerformanceRenderSettings.MainLightShadowmapResolutionMultiplier;
            shadowData.mainLightShadowmapWidth = (int)(shadowData.mainLightShadowmapWidth * MainLightShadowmapResolutionMultiplier);
            shadowData.mainLightShadowmapHeight = (int)(shadowData.mainLightShadowmapHeight * MainLightShadowmapResolutionMultiplier);

            var MainLightShadowCascadesCountBias = AdaptivePerformance.AdaptivePerformanceRenderSettings.MainLightShadowCascadesCountBias;
            shadowData.mainLightShadowCascadesCount = Mathf.Clamp(shadowData.mainLightShadowCascadesCount - MainLightShadowCascadesCountBias, 0, 4);

            var shadowQualityIndex = AdaptivePerformance.AdaptivePerformanceRenderSettings.ShadowQualityBias;
            for (int i = 0; i < shadowQualityIndex; i++)
            {
                if (shadowData.supportsSoftShadows)
                {
                    shadowData.supportsSoftShadows = false;
                    continue;
                }

                if (shadowData.supportsAdditionalLightShadows)
                {
                    shadowData.supportsAdditionalLightShadows = false;
                    continue;
                }

                if (shadowData.supportsMainLightShadows)
                {
                    shadowData.supportsMainLightShadows = false;
                    continue;
                }

                break;
            }

            if (AdaptivePerformance.AdaptivePerformanceRenderSettings.LutBias >= 1 && postProcessingData.lutSize == 32)
                postProcessingData.lutSize = 16;
        }

#endif

        /// <summary>
        /// Data structure describing the data for a specific render request
        /// </summary>
        public class SingleCameraRequest
        {
            /// <summary>
            /// Target texture
            /// </summary>
            public RenderTexture destination = null;

            /// <summary>
            /// Target texture mip level
            /// </summary>
            public int mipLevel = 0;

            /// <summary>
            /// Target texture cubemap face
            /// </summary>
            public CubemapFace face = CubemapFace.Unknown;

            /// <summary>
            /// Target texture slice
            /// </summary>
            public int slice = 0;
        }

        static AdditionalLightsShadowAtlasLayout BuildAdditionalLightsShadowAtlasLayout(UniversalLightData lightData, UniversalShadowData shadowData, UniversalCameraData cameraData)
        {
            using var profScope = new ProfilingScope(Profiling.Pipeline.buildAdditionalLightsShadowAtlasLayout);
            return new AdditionalLightsShadowAtlasLayout(lightData, shadowData, cameraData);
        }

        /// <summary>
        /// Enforce under specific circumstances whether URP or native engine triggers the UI Overlay rendering
        /// </summary>
        static void AdjustUIOverlayOwnership(int cameraCount)
        {
            // If rendering to XR device, we don't render Screen Space UI overlay within SRP as the overlay should not be visible in HMD eyes, only when mirroring (after SRP XR Mirror pass)
            // If there is no camera to render in URP, SS UI overlay also has to be rendered in the engine
            if (XRSystem.displayActive || cameraCount == 0)
            {
                SupportedRenderingFeatures.active.rendersUIOverlay = false;
            }
            else
            {
                // Otherwise we enforce SS UI overlay rendering in URP
                // If needed, users can still request its rendering to be after URP
                // by setting rendersUIOverlay (public API) to false in a callback added to RenderPipelineManager.beginContextRendering
                SupportedRenderingFeatures.active.rendersUIOverlay = true;
            }
        }
    }
}
