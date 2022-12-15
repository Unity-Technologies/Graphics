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
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Profiling;

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

        private static class Profiling
        {
            private static Dictionary<int, ProfilingSampler> s_HashSamplerCache = new Dictionary<int, ProfilingSampler>();
            public static readonly ProfilingSampler unknownSampler = new ProfilingSampler("Unknown");

            // Specialization for camera loop to avoid allocations.
            public static ProfilingSampler TryGetOrAddCameraSampler(Camera camera)
            {
#if UNIVERSAL_PROFILING_NO_ALLOC
                return unknownSampler;
#else
                ProfilingSampler ps = null;
                int cameraId = camera.GetHashCode();
                bool exists = s_HashSamplerCache.TryGetValue(cameraId, out ps);
                if (!exists)
                {
                    // NOTE: camera.name allocates!
                    ps = new ProfilingSampler($"{nameof(UniversalRenderPipeline)}.{nameof(RenderSingleCameraInternal)}: {camera.name}");
                    s_HashSamplerCache.Add(cameraId, ps);
                }
                return ps;
#endif
            }

            public static class Pipeline
            {
                // TODO: Would be better to add Profiling name hooks into RenderPipeline.cs, requires changes outside of Universal.
#if UNITY_2021_1_OR_NEWER
                public static readonly ProfilingSampler beginContextRendering  = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(BeginContextRendering)}");
                public static readonly ProfilingSampler endContextRendering    = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(EndContextRendering)}");
#else
                public static readonly ProfilingSampler beginFrameRendering = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(BeginFrameRendering)}");
                public static readonly ProfilingSampler endFrameRendering = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(EndFrameRendering)}");
#endif
                public static readonly ProfilingSampler beginCameraRendering = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(BeginCameraRendering)}");
                public static readonly ProfilingSampler endCameraRendering = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(EndCameraRendering)}");

                const string k_Name = nameof(UniversalRenderPipeline);
                public static readonly ProfilingSampler initializeCameraData = new ProfilingSampler($"{k_Name}.{nameof(InitializeCameraData)}");
                public static readonly ProfilingSampler initializeStackedCameraData = new ProfilingSampler($"{k_Name}.{nameof(InitializeStackedCameraData)}");
                public static readonly ProfilingSampler initializeAdditionalCameraData = new ProfilingSampler($"{k_Name}.{nameof(InitializeAdditionalCameraData)}");
                public static readonly ProfilingSampler initializeRenderingData = new ProfilingSampler($"{k_Name}.{nameof(InitializeRenderingData)}");
                public static readonly ProfilingSampler initializeShadowData = new ProfilingSampler($"{k_Name}.{nameof(InitializeShadowData)}");
                public static readonly ProfilingSampler initializeLightData = new ProfilingSampler($"{k_Name}.{nameof(InitializeLightData)}");
                public static readonly ProfilingSampler getPerObjectLightFlags = new ProfilingSampler($"{k_Name}.{nameof(GetPerObjectLightFlags)}");
                public static readonly ProfilingSampler getMainLightIndex = new ProfilingSampler($"{k_Name}.{nameof(GetMainLightIndex)}");
                public static readonly ProfilingSampler setupPerFrameShaderConstants = new ProfilingSampler($"{k_Name}.{nameof(SetupPerFrameShaderConstants)}");

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
            // No support to bitfield mask and int[] in gles2. Can't index fast more than 4 lights.
            // Check Lighting.hlsl for more details.
            get => (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2) ? 4 : 8;
        }

        // These limits have to match same limits in Input.hlsl
        internal const int k_MaxVisibleAdditionalLightsMobileShaderLevelLessThan45 = 16;
        internal const int k_MaxVisibleAdditionalLightsMobile = 32;
        internal const int k_MaxVisibleAdditionalLightsNonMobile = 256;

        /// <summary>
        /// The max number of additional lights that can can affect each GameObject.
        /// </summary>
        public static int maxVisibleAdditionalLights
        {
            get
            {
                // Must match: Input.hlsl, MAX_VISIBLE_LIGHTS
                bool isMobile = GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.SHADER_API_MOBILE);
                if (isMobile && (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && Graphics.minOpenGLESVersion <= OpenGLESVersion.OpenGLES30)))
                    return k_MaxVisibleAdditionalLightsMobileShaderLevelLessThan45;

                // GLES can be selected as platform on Windows (not a mobile platform) but uniform buffer size so we must use a low light count.
                return (isMobile || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                    ? k_MaxVisibleAdditionalLightsMobile : k_MaxVisibleAdditionalLightsNonMobile;
            }
        }

        // Match with values in Input.hlsl
        internal static int lightsPerTile => ((maxVisibleAdditionalLights + 31) / 32) * 32;
        internal static int maxZBinWords => 1024 * 4;
        internal static int maxTileWords => (maxVisibleAdditionalLights <= 32 ? 1024 : 4096) * 4;

        internal const int k_DefaultRenderingLayerMask = 0x00000001;
        private readonly DebugDisplaySettingsUI m_DebugDisplaySettingsUI = new DebugDisplaySettingsUI();

        private UniversalRenderPipelineGlobalSettings m_GlobalSettings;

        /// <summary>
        /// The default Render Pipeline Global Settings.
        /// </summary>
        public override RenderPipelineGlobalSettings defaultSettings => m_GlobalSettings;

        // flag to keep track of depth buffer requirements by any of the cameras in the stack
        internal static bool cameraStackRequiresDepthForPostprocessing = false;

        internal static RenderGraph s_RenderGraph;
        private static bool useRenderGraph;

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
#if UNITY_EDITOR
            m_GlobalSettings = UniversalRenderPipelineGlobalSettings.Ensure();
#else
            m_GlobalSettings = UniversalRenderPipelineGlobalSettings.instance;
#endif
            SetSupportedRenderingFeatures();

            // Initial state of the RTHandle system.
            // We initialize to screen width/height to avoid multiple realloc that can lead to inflated memory usage (as releasing of memory is delayed).
            RTHandles.Initialize(Screen.width, Screen.height);

            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;

            // In QualitySettings.antiAliasing disabled state uses value 0, where in URP 1
            int qualitySettingsMsaaSampleCount = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
            bool msaaSampleCountNeedsUpdate = qualitySettingsMsaaSampleCount != asset.msaaSampleCount;

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (msaaSampleCountNeedsUpdate)
            {
                QualitySettings.antiAliasing = asset.msaaSampleCount;
            }


            // Configure initial XR settings
            MSAASamples msaaSamples = (MSAASamples)Mathf.Clamp(Mathf.NextPowerOfTwo(QualitySettings.antiAliasing), (int)MSAASamples.None, (int)MSAASamples.MSAA8x);
            XRSystem.SetDisplayMSAASamples(msaaSamples);
            XRSystem.SetRenderScale(asset.renderScale);

            Shader.globalRenderPipeline = k_ShaderTagName;

            Lightmapping.SetDelegate(lightsDelegate);

            CameraCaptureBridge.enabled = true;

            RenderingUtils.ClearSystemInfoCache();

            DecalProjector.defaultMaterial = asset.decalMaterial;

            s_RenderGraph = new RenderGraph("URPRenderGraph");
            useRenderGraph = false;

            DebugManager.instance.RefreshEditor();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_DebugDisplaySettingsUI.RegisterDebug(UniversalRenderPipelineDebugDisplaySettings.Instance);
#endif

            QualitySettings.enableLODCrossFade = asset.enableLODCrossFade;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_DebugDisplaySettingsUI.UnregisterDebug();
#endif

            Blitter.Cleanup();

            base.Dispose(disposing);

            pipelineAsset.DestroyRenderers();

            Shader.globalRenderPipeline = string.Empty;

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
            ShaderData.instance.Dispose();
            XRSystem.Dispose();

            s_RenderGraph.Cleanup();
            s_RenderGraph = null;

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
#endif
            Lightmapping.ResetDelegate();
            CameraCaptureBridge.enabled = false;

            DisposeAdditionalCameraData();
        }

        // If the URP gets destroyed, we must clean up all the added URP specific camera data and
        // non-GC resources to avoid leaking them.
        private void DisposeAdditionalCameraData()
        {
            foreach (var c in Camera.allCameras)
            {
                if (c.TryGetComponent<UniversalAdditionalCameraData>(out var acd))
                {
                    acd.taaPersistentData?.DeallocateTargets();
                };
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
#if RENDER_GRAPH_ENABLED
            useRenderGraph = asset.enableRenderGraph || RenderGraphGraphicsAutomatedTests.enabled;
#else
            useRenderGraph = RenderGraphGraphicsAutomatedTests.enabled;
#endif

            // TODO: Would be better to add Profiling name hooks into RenderPipelineManager.
            // C#8 feature, only in >= 2020.2
            using var profScope = new ProfilingScope(null, ProfilingSampler.Get(URPProfileId.UniversalRenderTotal));

#if UNITY_2021_1_OR_NEWER
            using (new ProfilingScope(null, Profiling.Pipeline.beginContextRendering))
            {
                BeginContextRendering(renderContext, cameras);
            }
#else
            using (new ProfilingScope(null, Profiling.Pipeline.beginFrameRendering))
            {
                BeginFrameRendering(renderContext, cameras);
            }
#endif

            GraphicsSettings.lightsUseLinearIntensity = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GraphicsSettings.lightsUseColorTemperature = true;
            GraphicsSettings.defaultRenderingLayerMask = k_DefaultRenderingLayerMask;
            SetupPerFrameShaderConstants();
            XRSystem.SetDisplayMSAASamples((MSAASamples)asset.msaaSampleCount);

#if UNITY_EDITOR
            // We do not want to start rendering if URP global settings are not ready (m_globalSettings is null)
            // or been deleted/moved (m_globalSettings is not necessarily null)
            if (m_GlobalSettings == null || UniversalRenderPipelineGlobalSettings.instance == null)
            {
                m_GlobalSettings = UniversalRenderPipelineGlobalSettings.Ensure();
                if(m_GlobalSettings == null) return;
            }
#endif

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (DebugManager.instance.isAnyDebugUIActive)
                UniversalRenderPipelineDebugDisplaySettings.Instance.UpdateDisplayStats();
#endif

            SortCameras(cameras);
#if UNITY_2021_1_OR_NEWER
            for (int i = 0; i < cameras.Count; ++i)
#else
            for (int i = 0; i < cameras.Length; ++i)
#endif
            {
                var camera = cameras[i];
                camera.allowDynamicResolution = false;
                if (IsGameCamera(camera))
                {
                    RenderCameraStack(renderContext, camera);
                }
                else
                {
                    using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
                    {
                        BeginCameraRendering(renderContext, camera);
                    }
#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                    //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                    VFX.VFXManager.PrepareCamera(camera);
#endif
                    UpdateVolumeFramework(camera, null);

                    RenderSingleCameraInternal(renderContext, camera);

                    using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
                    {
                        EndCameraRendering(renderContext, camera);
                    }
                }
            }

            s_RenderGraph.EndFrame();

#if UNITY_2021_1_OR_NEWER
            using (new ProfilingScope(null, Profiling.Pipeline.endContextRendering))
            {
                EndContextRendering(renderContext, cameras);
            }
#else
            using (new ProfilingScope(null, Profiling.Pipeline.endFrameRendering))
            {
                EndFrameRendering(renderContext, cameras);
            }
#endif

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
                int mipLevel = standardRequest != null ? standardRequest.mipLevel : singleRequest.mipLevel;
                int slice = standardRequest != null ? standardRequest.slice : singleRequest.slice;
                int face = standardRequest != null ? (int)standardRequest.face : (int)singleRequest.face;

                //store data that will be changed
                var orignalTarget = camera.targetTexture;

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
                    RenderSingleCameraInternal(context, camera);
                }

                if(temporaryRT)
                {
                    switch(destination.dimension)
                    {
                        case TextureDimension.Tex2D:
                        case TextureDimension.Tex2DArray:
                        case TextureDimension.Tex3D:
                            Graphics.CopyTexture(temporaryRT, 0, 0, destination, slice, mipLevel);
                            break;
                        case TextureDimension.Cube:
                        case TextureDimension.CubeArray:
                            Graphics.CopyTexture(temporaryRT, 0, 0, destination, face + slice * 6, mipLevel);
                            break;
                    }
                }

                //restore data
                camera.targetTexture = orignalTarget;
                Graphics.SetRenderTarget(orignalTarget);
                RenderTexture.ReleaseTemporary(temporaryRT);
            }
            else
            {
                Debug.LogWarning("The given RenderRequest type: " + typeof(RequestData).FullName  + ", is either invalid or unsupported by the current pipeline");
            }
        }

        /// <summary>
        /// Standalone camera rendering. Use this to render procedural cameras.
        /// This method doesn't call <c>BeginCameraRendering</c> and <c>EndCameraRendering</c> callbacks.
        /// </summary>
        /// <param name="context">Render context used to record commands during execution.</param>
        /// <param name="camera">Camera to render.</param>
        /// <seealso cref="ScriptableRenderContext"/>
        [Obsolete("RenderSingleCamera is obsolete, please use RenderPipeline.SubmiteRenderRequest with UniversalRenderer.SingleCameraRequest as RequestData type", false)]
        public static void RenderSingleCamera(ScriptableRenderContext context, Camera camera)
        {
            RenderSingleCameraInternal(context, camera);
        }

        internal static void RenderSingleCameraInternal(ScriptableRenderContext context, Camera camera)
        {
            UniversalAdditionalCameraData additionalCameraData = null;
            if (IsGameCamera(camera))
                camera.gameObject.TryGetComponent(out additionalCameraData);

            if (additionalCameraData != null && additionalCameraData.renderType != CameraRenderType.Base)
            {
                Debug.LogWarning("Only Base cameras can be rendered with standalone RenderSingleCamera. Camera will be skipped.");
                return;
            }

            InitializeCameraData(camera, additionalCameraData, true, out var cameraData);
            InitializeAdditionalCameraData(camera, additionalCameraData, true, ref cameraData);
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            if (asset.useAdaptivePerformance)
                ApplyAdaptivePerformance(ref cameraData);
#endif
            RenderSingleCamera(context, ref cameraData, cameraData.postProcessEnabled);
        }

        static bool TryGetCullingParameters(ref CameraData cameraData, out ScriptableCullingParameters cullingParams)
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
        /// <param name="anyPostProcessingEnabled">True if at least one camera has post-processing enabled in the stack, false otherwise.</param>
        static void RenderSingleCamera(ScriptableRenderContext context, ref CameraData cameraData, bool anyPostProcessingEnabled)
        {
            Camera camera = cameraData.camera;
            var renderer = cameraData.renderer;
            if (renderer == null)
            {
                Debug.LogWarning(string.Format("Trying to render {0} with an invalid renderer. Camera rendering will be skipped.", camera.name));
                return;
            }

            if (!TryGetCullingParameters(ref cameraData, out var cullingParameters))
                return;

            ScriptableRenderer.current = renderer;
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

            ProfilingSampler sampler = Profiling.TryGetOrAddCameraSampler(camera);
            using (new ProfilingScope(cmdScope, sampler)) // Enqueues a "BeginSample" command into the CommandBuffer cmd
            {
                renderer.Clear(cameraData.renderType);

                using (new ProfilingScope(null, Profiling.Pipeline.Renderer.setupCullingParameters))
                {
                    renderer.OnPreCullRenderPasses(in cameraData);
                    renderer.SetupCullingParameters(ref cullingParameters, ref cameraData);
                }

                context.ExecuteCommandBuffer(cmd); // Send all the commands enqueued so far in the CommandBuffer cmd, to the ScriptableRenderContext context
                cmd.Clear();

#if UNITY_EDITOR
                // Emit scene view UI
                if (isSceneViewCamera)
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                else
#endif
                if (cameraData.camera.targetTexture != null && cameraData.cameraType != CameraType.Preview)
                    ScriptableRenderContext.EmitGeometryForCamera(camera);

                var cullResults = context.Cull(ref cullingParameters);
                InitializeRenderingData(asset, ref cameraData, ref cullResults, anyPostProcessingEnabled, cmd, out var renderingData);
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
                if (asset.useAdaptivePerformance)
                    ApplyAdaptivePerformance(ref renderingData);
#endif

                // Update camera motion tracking (prev matrices) from cameraData.
                // Called and updated only once, as the same camera can be rendered multiple times.
                // NOTE: Tracks only the current (this) camera, not shadow views or any other offscreen views.
                // NOTE: Shared between both Execute and Render (RG) paths.
                if(camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
                    additionalCameraData.motionVectorsPersistentData.Update(ref cameraData);

                // Update TAA persistent data based on cameraData. Most importantly resize the history render targets.
                // NOTE: Persistent data is kept over multiple frames. Its life-time differs from typical resources.
                // NOTE: Shared between both Execute and Render (RG) paths.
                if (cameraData.taaPersistentData != null)
                    UpdateTemporalAATargets(ref cameraData);

                RTHandles.SetReferenceSize(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);

                renderer.AddRenderPasses(ref renderingData);

                if (useRenderGraph)
                {
                    RecordAndExecuteRenderGraph(s_RenderGraph, context, ref renderingData);
                    renderer.FinishRenderGraphRendering(context, ref renderingData);
                }
                else
                {
                    using (new ProfilingScope(null, Profiling.Pipeline.Renderer.setup))
                        renderer.Setup(context, ref renderingData);

                    // Timing scope inside
                    renderer.Execute(context, ref renderingData);
                }

                CleanupLightData(ref renderingData.lightData);
            } // When ProfilingSample goes out of scope, an "EndSample" command is enqueued into CommandBuffer cmd

            context.ExecuteCommandBuffer(cmd); // Sends to ScriptableRenderContext all the commands enqueued since cmd.Clear, i.e the "EndSample" command
            CommandBufferPool.Release(cmd);

            using (new ProfilingScope(null, Profiling.Pipeline.Context.submit))
            {
                if (renderer.useRenderPassEnabled && !context.SubmitForRenderPassValidation())
                {
                    renderer.useRenderPassEnabled = false;
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.RenderPassEnabled, false);
                    Debug.LogWarning("Rendering command not supported inside a native RenderPass found. Falling back to non-RenderPass rendering path");
                }
                context.Submit(); // Actually execute the commands that we previously sent to the ScriptableRenderContext context
            }

            ScriptableRenderer.current = null;
        }

        /// <summary>
        /// Renders a camera stack. This method calls RenderSingleCamera for each valid camera in the stack.
        /// The last camera resolves the final target to screen.
        /// </summary>
        /// <param name="context">Render context used to record commands during execution.</param>
        /// <param name="camera">Camera to render.</param>
        static void RenderCameraStack(ScriptableRenderContext context, Camera baseCamera)
        {
            using var profScope = new ProfilingScope(null, ProfilingSampler.Get(URPProfileId.RenderCameraStack));

            baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var baseCameraAdditionalData);

            // Overlay cameras will be rendered stacked while rendering base cameras
            if (baseCameraAdditionalData != null && baseCameraAdditionalData.renderType == CameraRenderType.Overlay)
                return;

            // Renderer contains a stack if it has additional data and the renderer supports stacking
            // The renderer is checked if it supports Base camera. Since Base is the only relevant type at this moment.
            var renderer = baseCameraAdditionalData?.scriptableRenderer;
            bool supportsCameraStacking = renderer != null && renderer.SupportsCameraStackingType(CameraRenderType.Base);
            List<Camera> cameraStack = (supportsCameraStacking) ? baseCameraAdditionalData?.cameraStack : null;

            bool anyPostProcessingEnabled = baseCameraAdditionalData != null && baseCameraAdditionalData.renderPostProcessing;

            // We need to know the last active camera in the stack to be able to resolve
            // rendering to screen when rendering it. The last camera in the stack is not
            // necessarily the last active one as it users might disable it.
            int lastActiveOverlayCameraIndex = -1;
            if (cameraStack != null)
            {
                var baseCameraRendererType = baseCameraAdditionalData?.scriptableRenderer.GetType();
                bool shouldUpdateCameraStack = false;

                cameraStackRequiresDepthForPostprocessing = false;

                for (int i = 0; i < cameraStack.Count; ++i)
                {
                    Camera currCamera = cameraStack[i];
                    if (currCamera == null)
                    {
                        shouldUpdateCameraStack = true;
                        continue;
                    }

                    if (currCamera.isActiveAndEnabled)
                    {
                        currCamera.TryGetComponent<UniversalAdditionalCameraData>(out var data);

                        // Checking if the base and the overlay camera is of the same renderer type.
                        var currCameraRendererType = data?.scriptableRenderer.GetType();
                        if (currCameraRendererType != baseCameraRendererType)
                        {
                            Debug.LogWarning("Only cameras with compatible renderer types can be stacked. " +
                                             $"The camera: {currCamera.name} are using the renderer {currCameraRendererType.Name}, " +
                                             $"but the base camera: {baseCamera.name} are using {baseCameraRendererType.Name}. Will skip rendering");
                            continue;
                        }

                        var overlayRenderer = data.scriptableRenderer;
                        // Checking if they are the same renderer type but just not supporting Overlay
                        if ((overlayRenderer.SupportedCameraStackingTypes() & 1 << (int)CameraRenderType.Overlay) == 0)
                        {
                            Debug.LogWarning($"The camera: {currCamera.name} is using a renderer of type {renderer.GetType().Name} which does not support Overlay cameras in it's current state.");
                            continue;
                        }

                        if (data == null || data.renderType != CameraRenderType.Overlay)
                        {
                            Debug.LogWarning($"Stack can only contain Overlay cameras. The camera: {currCamera.name} " +
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

            // Post-processing not supported in GLES2.
            anyPostProcessingEnabled &= SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            bool isStackedRendering = lastActiveOverlayCameraIndex != -1;

            // Prepare XR rendering
            var xrActive = false;
            var xrRendering = baseCameraAdditionalData?.allowXRRendering ?? true;
            var xrLayout = XRSystem.NewLayout();
            xrLayout.AddCamera(baseCamera, xrRendering);

            // With XR multi-pass enabled, each camera can be rendered multiple times with different parameters
            foreach ((Camera _, XRPass xrPass) in xrLayout.GetActivePasses())
            {
                if (xrPass.enabled)
                {
                    xrActive = true;
                    UpdateCameraStereoMatrices(baseCamera, xrPass);
                }


                using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
                {
                    BeginCameraRendering(context, baseCamera);
                }
                // Update volumeframework before initializing additional camera data
                UpdateVolumeFramework(baseCamera, baseCameraAdditionalData);
                InitializeCameraData(baseCamera, baseCameraAdditionalData, !isStackedRendering, out var baseCameraData);
                RenderTextureDescriptor originalTargetDesc = baseCameraData.cameraTargetDescriptor;

#if ENABLE_VR && ENABLE_XR_MODULE
                if (xrPass.enabled)
                {
                    baseCameraData.xr = xrPass;

                    // Helper function for updating cameraData with xrPass Data
                    // Need to update XRSystem using baseCameraData to handle the case where camera position is modified in BeginCameraRendering
                    UpdateCameraData(ref baseCameraData, baseCameraData.xr);

                    // Handle the case where camera position is modified in BeginCameraRendering
                    xrLayout.ReconfigurePass(baseCameraData.xr, baseCamera);
                    XRSystemUniversal.BeginLateLatching(baseCamera, baseCameraData.xrUniversal);
                }
#endif
                // InitializeAdditionalCameraData needs to be initialized after the cameraTargetDescriptor is set because it needs to know the
                // msaa level of cameraTargetDescriptor and XR modifications.
                InitializeAdditionalCameraData(baseCamera, baseCameraAdditionalData, !isStackedRendering, ref baseCameraData);

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                VFX.VFXManager.PrepareCamera(baseCamera);
#endif
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
                if (asset.useAdaptivePerformance)
                    ApplyAdaptivePerformance(ref baseCameraData);
#endif
                // update the base camera flag so that the scene depth is stored if needed by overlay cameras later in the frame
                baseCameraData.postProcessingRequiresDepthTexture |= cameraStackRequiresDepthForPostprocessing;

                RenderSingleCamera(context, ref baseCameraData, anyPostProcessingEnabled);
                using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
                {
                    EndCameraRendering(context, baseCamera);
                }

                // Late latching is not supported after this point
                if (baseCameraData.xr.enabled)
                    XRSystemUniversal.EndLateLatching(baseCamera, baseCameraData.xrUniversal);

                if (isStackedRendering)
                {
                    for (int i = 0; i < cameraStack.Count; ++i)
                    {
                        var currCamera = cameraStack[i];
                        if (!currCamera.isActiveAndEnabled)
                            continue;

                        currCamera.TryGetComponent<UniversalAdditionalCameraData>(out var currAdditionalCameraData);
                        // Camera is overlay and enabled
                        if (currAdditionalCameraData != null)
                        {
                            // Copy base settings from base camera data and initialize initialize remaining specific settings for this camera type.
                            CameraData overlayCameraData = baseCameraData;
                            overlayCameraData.camera = currCamera;
                            overlayCameraData.baseCamera = baseCamera;

                            UpdateCameraStereoMatrices(currAdditionalCameraData.camera, xrPass);

                            using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
                            {
                                BeginCameraRendering(context, currCamera);
                            }
#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                            //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                            VFX.VFXManager.PrepareCamera(currCamera);
#endif
                            UpdateVolumeFramework(currCamera, currAdditionalCameraData);

                            bool lastCamera = i == lastActiveOverlayCameraIndex;
                            InitializeAdditionalCameraData(currCamera, currAdditionalCameraData, lastCamera, ref overlayCameraData);

                            xrLayout.ReconfigurePass(overlayCameraData.xr, currCamera);

                            RenderSingleCamera(context, ref overlayCameraData, anyPostProcessingEnabled);

                            using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
                            {
                                EndCameraRendering(context, currCamera);
                            }
                        }
                    }
                }

                if (baseCameraData.xr.enabled)
                    baseCameraData.cameraTargetDescriptor = originalTargetDesc;
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
        static void UpdateCameraData(ref CameraData baseCameraData, in XRPass xr)
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

            bool isDefaultXRViewport = (!(Math.Abs(xrViewport.x) > 0.0f || Math.Abs(xrViewport.y) > 0.0f ||
                Math.Abs(xrViewport.width) < xr.renderTargetDesc.width ||
                Math.Abs(xrViewport.height) < xr.renderTargetDesc.height));
            baseCameraData.isDefaultViewport = baseCameraData.isDefaultViewport && isDefaultXRViewport;

            // Update cameraData cameraTargetDescriptor for XR. This descriptor is mainly used for configuring intermediate screen space textures
            var originalTargetDesc = baseCameraData.cameraTargetDescriptor;
            baseCameraData.cameraTargetDescriptor = xr.renderTargetDesc;
            if (baseCameraData.isHdrEnabled)
            {
                baseCameraData.cameraTargetDescriptor.graphicsFormat = originalTargetDesc.graphicsFormat;
            }
            baseCameraData.cameraTargetDescriptor.msaaSamples = originalTargetDesc.msaaSamples;
            baseCameraData.cameraTargetDescriptor.width = baseCameraData.pixelWidth;
            baseCameraData.cameraTargetDescriptor.height = baseCameraData.pixelHeight;
        }

        static void UpdateVolumeFramework(Camera camera, UniversalAdditionalCameraData additionalCameraData)
        {
            using var profScope = new ProfilingScope(null, ProfilingSampler.Get(URPProfileId.UpdateVolumeFramework));

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

        static bool CheckPostProcessForDepth(ref CameraData cameraData)
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

        static void SetSupportedRenderingFeatures()
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
        }

        static void InitializeCameraData(Camera camera, UniversalAdditionalCameraData additionalCameraData, bool resolveFinalTarget, out CameraData cameraData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeCameraData);

            cameraData = new CameraData();
            InitializeStackedCameraData(camera, additionalCameraData, ref cameraData);

            cameraData.camera = camera;

            ///////////////////////////////////////////////////////////////////
            // Descriptor settings                                            /
            ///////////////////////////////////////////////////////////////////

            var renderer = additionalCameraData?.scriptableRenderer;
            bool rendererSupportsMSAA = renderer != null && renderer.supportedRenderingFeatures.msaa;

            int msaaSamples = 1;
            if (camera.allowMSAA && asset.msaaSampleCount > 1 && rendererSupportsMSAA)
                msaaSamples = (camera.targetTexture != null) ? camera.targetTexture.antiAliasing : asset.msaaSampleCount;

            // Use XR's MSAA if camera is XR camera. XR MSAA needs special handle here because it is not per Camera.
            // Multiple cameras could render into the same XR display and they should share the same MSAA level.
            if (cameraData.xrRendering && rendererSupportsMSAA)
                msaaSamples = (int)XRSystem.GetDisplayMSAASamples();

            bool needsAlphaChannel = Graphics.preserveFramebufferAlpha;

            // Render scale is not intended to affect the scene view so override the scale to 1.0 when it's rendered.
            bool isSceneViewCamera = (camera.cameraType == CameraType.SceneView);
            float renderScale = isSceneViewCamera ? 1.0f : cameraData.renderScale;

            cameraData.hdrColorBufferPrecision = asset ? asset.hdrColorBufferPrecision : HDRColorBufferPrecision._32Bits;
            cameraData.cameraTargetDescriptor = CreateRenderTextureDescriptor(camera, renderScale,
                cameraData.isHdrEnabled, cameraData.hdrColorBufferPrecision, msaaSamples, needsAlphaChannel, cameraData.requiresOpaqueTexture);
        }

        /// <summary>
        /// Initialize camera data settings common for all cameras in the stack. Overlay cameras will inherit
        /// settings from base camera.
        /// </summary>
        /// <param name="baseCamera">Base camera to inherit settings from.</param>
        /// <param name="baseAdditionalCameraData">Component that contains additional base camera data.</param>
        /// <param name="cameraData">Camera data to initialize setttings.</param>
        static void InitializeStackedCameraData(Camera baseCamera, UniversalAdditionalCameraData baseAdditionalCameraData, ref CameraData cameraData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeStackedCameraData);

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
            }

            ///////////////////////////////////////////////////////////////////
            // Settings that control output of the camera                     /
            ///////////////////////////////////////////////////////////////////

            cameraData.isHdrEnabled = baseCamera.allowHDR && settings.supportsHDR;

            Rect cameraRect = baseCamera.rect;
            cameraData.pixelRect = baseCamera.pixelRect;
            cameraData.pixelWidth = baseCamera.pixelWidth;
            cameraData.pixelHeight = baseCamera.pixelHeight;
            cameraData.aspectRatio = (float)cameraData.pixelWidth / (float)cameraData.pixelHeight;
            cameraData.isDefaultViewport = (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f ||
                Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f));

            // Discard variations lesser than kRenderScaleThreshold.
            // Scale is only enabled for gameview.
            const float kRenderScaleThreshold = 0.05f;
            cameraData.renderScale = (Mathf.Abs(1.0f - settings.renderScale) < kRenderScaleThreshold) ? 1.0f : settings.renderScale;

            // Convert the upscaling filter selection from the pipeline asset into an image upscaling filter
            cameraData.upscalingFilter = ResolveUpscalingFilterSelection(new Vector2(cameraData.pixelWidth, cameraData.pixelHeight), cameraData.renderScale, settings.upscalingFilter);

            if (cameraData.renderScale > 1.0f)
            {
                cameraData.imageScalingMode = ImageScalingMode.Downscaling;
            }
            else if ((cameraData.renderScale < 1.0f) || (cameraData.upscalingFilter == ImageUpscalingFilter.FSR))
            {
                // When FSR is enabled, we still consider 100% render scale an upscaling operation.
                // This allows us to run the FSR shader passes all the time since they improve visual quality even at 100% scale.

                cameraData.imageScalingMode = ImageScalingMode.Upscaling;
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
            cameraData.captureActions = CameraCaptureBridge.GetCaptureActions(baseCamera);
        }

        /// <summary>
        /// Initialize settings that can be different for each camera in the stack.
        /// </summary>
        /// <param name="camera">Camera to initialize settings from.</param>
        /// <param name="additionalCameraData">Additional camera data component to initialize settings from.</param>
        /// <param name="resolveFinalTarget">True if this is the last camera in the stack and rendering should resolve to camera target.</param>
        /// <param name="cameraData">Settings to be initilized.</param>
        static void InitializeAdditionalCameraData(Camera camera, UniversalAdditionalCameraData additionalCameraData, bool resolveFinalTarget, ref CameraData cameraData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeAdditionalCameraData);

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
                cameraData.renderer = asset.scriptableRenderer;
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
                cameraData.renderer = additionalCameraData.scriptableRenderer;
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
                cameraData.renderer = asset.scriptableRenderer;
                cameraData.useScreenCoordOverride = false;
                cameraData.screenSizeOverride = cameraData.pixelRect.size;
                cameraData.screenCoordScaleBias = Vector2.one;
            }

            // Disables post if GLes2
            cameraData.postProcessEnabled &= SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            cameraData.requiresDepthTexture |= isSceneViewCamera;
            cameraData.postProcessingRequiresDepthTexture = CheckPostProcessForDepth(ref cameraData);
            cameraData.resolveFinalTarget = resolveFinalTarget;

            // Disable depth and color copy. We should add it in the renderer instead to avoid performance pitfalls
            // of camera stacking breaking render pass execution implicitly.
            bool isOverlayCamera = (cameraData.renderType == CameraRenderType.Overlay);
            if (isOverlayCamera)
            {
                cameraData.requiresOpaqueTexture = false;
            }

#if URP_EXPERIMENTAL_TAA_ENABLE
            // NOTE: depends on XR modifications of cameraTargetDescriptor.
            if (additionalCameraData != null)
            {
                // Initialize shared TAA target desc.
                ref var desc = ref cameraData.cameraTargetDescriptor;
                cameraData.taaPersistentData = additionalCameraData.taaPersistentData;
                cameraData.taaPersistentData.Init(desc.width, desc.height, desc.volumeDepth, desc.graphicsFormat, desc.vrUsage, desc.dimension);

                ref var taaSettings = ref additionalCameraData.taaSettings;
                cameraData.taaSettings = taaSettings;

                // Decrease history clear counter. Typically clear is only 1 frame, but can be many for XR multipass eyes!
                taaSettings.resetHistoryFrames -= taaSettings.resetHistoryFrames > 0 ? 1 : 0;
            }
#endif

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

            // Depends on the cameraTargetDesc, size and MSAA also XR modifications of those.
            Matrix4x4 jitterMat = TemporalAA.CalculateJitterMatrix(ref cameraData);
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
        }

        static void InitializeRenderingData(UniversalRenderPipelineAsset settings, ref CameraData cameraData, ref CullingResults cullResults,
            bool anyPostProcessingEnabled, CommandBuffer cmd, out RenderingData renderingData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeRenderingData);

            var visibleLights = cullResults.visibleLights;

            int mainLightIndex = GetMainLightIndex(settings, visibleLights);
            bool mainLightCastShadows = false;
            bool additionalLightsCastShadows = false;

            if (cameraData.maxShadowDistance > 0.0f)
            {
                mainLightCastShadows = (mainLightIndex != -1 && visibleLights[mainLightIndex].light != null &&
                    visibleLights[mainLightIndex].light.shadows != LightShadows.None);

                // If additional lights are shaded per-vertex they cannot cast shadows
                if (settings.additionalLightsRenderingMode == LightRenderingMode.PerPixel)
                {
                    for (int i = 0; i < visibleLights.Length; ++i)
                    {
                        if (i == mainLightIndex)
                            continue;

                        ref VisibleLight vl = ref visibleLights.UnsafeElementAtMutable(i);
                        Light light = vl.light;

                        // UniversalRP doesn't support additional directional light shadows yet
                        if ((vl.lightType == LightType.Spot || vl.lightType == LightType.Point) && light != null && light.shadows != LightShadows.None)
                        {
                            additionalLightsCastShadows = true;
                            break;
                        }
                    }
                }
            }

            renderingData.cullResults = cullResults;
            renderingData.cameraData = cameraData;
            InitializeLightData(settings, visibleLights, mainLightIndex, out renderingData.lightData);
            InitializeShadowData(settings, visibleLights, mainLightCastShadows, additionalLightsCastShadows && !renderingData.lightData.shadeAdditionalLightsPerVertex, out renderingData.shadowData);
            InitializePostProcessingData(settings, out renderingData.postProcessingData);
            renderingData.supportsDynamicBatching = settings.supportsDynamicBatching;
            renderingData.perObjectData = GetPerObjectLightFlags(renderingData.lightData.additionalLightsCount, ((settings.scriptableRendererData as UniversalRendererData)?.renderingMode ?? RenderingMode.Forward) == RenderingMode.ForwardPlus);
            renderingData.postProcessingEnabled = anyPostProcessingEnabled;
            renderingData.commandBuffer = cmd;

            CheckAndApplyDebugSettings(ref renderingData);
        }

        static void InitializeShadowData(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights, bool mainLightCastShadows, bool additionalLightsCastShadows, out ShadowData shadowData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeShadowData);

            m_ShadowBiasData.Clear();
            m_ShadowResolutionData.Clear();

            for (int i = 0; i < visibleLights.Length; ++i)
            {
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
                    m_ShadowBiasData.Add(new Vector4(settings.shadowDepthBias, settings.shadowNormalBias, 0.0f, 0.0f));

                if (data && (data.additionalLightsShadowResolutionTier == UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierCustom))
                {
                    m_ShadowResolutionData.Add((int)light.shadowResolution); // native code does not clamp light.shadowResolution between -1 and 3
                }
                else if (data && (data.additionalLightsShadowResolutionTier != UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierCustom))
                {
                    int resolutionTier = Mathf.Clamp(data.additionalLightsShadowResolutionTier, UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow, UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh);
                    m_ShadowResolutionData.Add(settings.GetAdditionalLightsShadowResolution(resolutionTier));
                }
                else
                {
                    m_ShadowResolutionData.Add(settings.GetAdditionalLightsShadowResolution(UniversalAdditionalLightData.AdditionalLightsShadowDefaultResolutionTier));
                }
            }

            shadowData.bias = m_ShadowBiasData;
            shadowData.resolution = m_ShadowResolutionData;
            shadowData.supportsMainLightShadows = SystemInfo.supportsShadows && settings.supportsMainLightShadows && mainLightCastShadows;

            // We no longer use screen space shadows in URP.
            // This change allows us to have particles & transparent objects receive shadows.
#pragma warning disable 0618
            shadowData.requiresScreenSpaceShadowResolve = false;
#pragma warning restore 0618

            // On GLES2 we strip the cascade keywords from the lighting shaders, so for consistency we force disable the cascades here too
            shadowData.mainLightShadowCascadesCount = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 ? 1 : settings.shadowCascadeCount;
            shadowData.mainLightShadowmapWidth = settings.mainLightShadowmapResolution;
            shadowData.mainLightShadowmapHeight = settings.mainLightShadowmapResolution;

            switch (shadowData.mainLightShadowCascadesCount)
            {
                case 1:
                    shadowData.mainLightShadowCascadesSplit = new Vector3(1.0f, 0.0f, 0.0f);
                    break;

                case 2:
                    shadowData.mainLightShadowCascadesSplit = new Vector3(settings.cascade2Split, 1.0f, 0.0f);
                    break;

                case 3:
                    shadowData.mainLightShadowCascadesSplit = new Vector3(settings.cascade3Split.x, settings.cascade3Split.y, 0.0f);
                    break;

                default:
                    shadowData.mainLightShadowCascadesSplit = settings.cascade4Split;
                    break;
            }

            shadowData.mainLightShadowCascadeBorder = settings.cascadeBorder;

            shadowData.supportsAdditionalLightShadows = SystemInfo.supportsShadows && settings.supportsAdditionalLightShadows && additionalLightsCastShadows;
            shadowData.additionalLightsShadowmapWidth = shadowData.additionalLightsShadowmapHeight = settings.additionalLightsShadowmapResolution;
            shadowData.supportsSoftShadows = settings.supportsSoftShadows && (shadowData.supportsMainLightShadows || shadowData.supportsAdditionalLightShadows);
            shadowData.shadowmapDepthBufferBits = 16;

            // This will be setup in AdditionalLightsShadowCasterPass.
            shadowData.isKeywordAdditionalLightShadowsEnabled = false;
            shadowData.isKeywordSoftShadowsEnabled = false;
        }

        static void InitializePostProcessingData(UniversalRenderPipelineAsset settings, out PostProcessingData postProcessingData)
        {
            postProcessingData.gradingMode = settings.supportsHDR
                ? settings.colorGradingMode
                : ColorGradingMode.LowDynamicRange;

            postProcessingData.lutSize = settings.colorGradingLutSize;
            postProcessingData.useFastSRGBLinearConversion = settings.useFastSRGBLinearConversion;
        }

        static void InitializeLightData(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights, int mainLightIndex, out LightData lightData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeLightData);

            int maxPerObjectAdditionalLights = UniversalRenderPipeline.maxPerObjectLights;
            int maxVisibleAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;

            lightData.mainLightIndex = mainLightIndex;

            if (settings.additionalLightsRenderingMode != LightRenderingMode.Disabled)
            {
                lightData.additionalLightsCount =
                    Math.Min((mainLightIndex != -1) ? visibleLights.Length - 1 : visibleLights.Length,
                        maxVisibleAdditionalLights);
                lightData.maxPerObjectAdditionalLightsCount = Math.Min(settings.maxAdditionalLightsCount, maxPerObjectAdditionalLights);
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
            lightData.originalIndices = new NativeArray<int>(visibleLights.Length, Allocator.Temp);
            for (var i = 0; i < lightData.originalIndices.Length; i++)
            {
                lightData.originalIndices[i] = i;
            }
        }

        static void CleanupLightData(ref LightData lightData)
        {
            lightData.originalIndices.Dispose();
        }

        private static void UpdateTemporalAATargets(ref CameraData cameraData)
        {
            if (cameraData.IsTemporalAAEnabled())
            {
                bool xrMultipassEnabled = false;
#if ENABLE_VR && ENABLE_XR_MODULE
                xrMultipassEnabled = cameraData.xr.enabled && !cameraData.xr.singlePassEnabled;
#endif
                bool allocation = cameraData.taaPersistentData.AllocateTargets(xrMultipassEnabled);

                // Fill new history with current frame
                // XR Multipass renders a "frame" per eye
                if(allocation)
                    cameraData.taaSettings.resetHistoryFrames += xrMultipassEnabled ? 2 : 1;
            }
            else
                cameraData.taaPersistentData.DeallocateTargets();
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

        static PerObjectData GetPerObjectLightFlags(int additionalLightsCount, bool clustering)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.getPerObjectLightFlags);

            var configuration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightData | PerObjectData.OcclusionProbe | PerObjectData.ShadowMask;

            if (additionalLightsCount > 0 && !clustering)
            {
                configuration |= PerObjectData.LightData;

                // In this case we also need per-object indices (unity_LightIndices)
                if (!RenderingUtils.useStructuredBuffer)
                    configuration |= PerObjectData.LightIndices;
            }

            return configuration;
        }

        // Main Light is always a directional light
        static int GetMainLightIndex(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.getMainLightIndex);

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

        static void SetupPerFrameShaderConstants()
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.setupPerFrameShaderConstants);

            // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
            SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
            Color linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
            Color glossyEnvColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
            Shader.SetGlobalVector(ShaderPropertyId.glossyEnvironmentColor, glossyEnvColor);

            // Used as fallback cubemap for reflections
            Shader.SetGlobalVector(ShaderPropertyId.glossyEnvironmentCubeMapHDR, ReflectionProbe.defaultTextureHDRDecodeValues);
            Shader.SetGlobalTexture(ShaderPropertyId.glossyEnvironmentCubeMap, ReflectionProbe.defaultTexture);

            // Ambient
            Shader.SetGlobalVector(ShaderPropertyId.ambientSkyColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientSkyColor));
            Shader.SetGlobalVector(ShaderPropertyId.ambientEquatorColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientEquatorColor));
            Shader.SetGlobalVector(ShaderPropertyId.ambientGroundColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientGroundColor));

            // Used when subtractive mode is selected
            Shader.SetGlobalVector(ShaderPropertyId.subtractiveShadowColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor));

            // Required for 2D Unlit Shadergraph master node as it doesn't currently support hidden properties.
            Shader.SetGlobalColor(ShaderPropertyId.rendererColor, Color.white);

            if (asset.lodCrossFadeDitheringType == LODCrossFadeDitheringType.BayerMatrix)
            {
                Shader.SetGlobalFloat(ShaderPropertyId.ditheringTextureInvSize, 1.0f / asset.textures.bayerMatrixTex.width);
                Shader.SetGlobalTexture(ShaderPropertyId.ditheringTexture, asset.textures.bayerMatrixTex);
            }
            else if (asset.lodCrossFadeDitheringType == LODCrossFadeDitheringType.BlueNoise)
            {
                Shader.SetGlobalFloat(ShaderPropertyId.ditheringTextureInvSize, 1.0f / asset.textures.blueNoise64LTex.width);
                Shader.SetGlobalTexture(ShaderPropertyId.ditheringTexture, asset.textures.blueNoise64LTex);
            }
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
        static ImageUpscalingFilter ResolveUpscalingFilterSelection(Vector2 imageSize, float renderScale, UpscalingFilterSelection selection)
        {
            // By default we just use linear filtering since it's the most compatible choice
            ImageUpscalingFilter filter = ImageUpscalingFilter.Linear;

            // Fall back to the automatic filter if FSR was selected, but isn't supported on the current platform
            if ((selection == UpscalingFilterSelection.FSR) && !FSRUtils.IsSupported())
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
            }

            return filter;
        }

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
        static void ApplyAdaptivePerformance(ref CameraData cameraData)
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

        static void ApplyAdaptivePerformance(ref RenderingData renderingData)
        {
            if (AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipDynamicBatching)
                renderingData.supportsDynamicBatching = false;

            var MainLightShadowmapResolutionMultiplier = AdaptivePerformance.AdaptivePerformanceRenderSettings.MainLightShadowmapResolutionMultiplier;
            renderingData.shadowData.mainLightShadowmapWidth = (int)(renderingData.shadowData.mainLightShadowmapWidth * MainLightShadowmapResolutionMultiplier);
            renderingData.shadowData.mainLightShadowmapHeight = (int)(renderingData.shadowData.mainLightShadowmapHeight * MainLightShadowmapResolutionMultiplier);

            var MainLightShadowCascadesCountBias = AdaptivePerformance.AdaptivePerformanceRenderSettings.MainLightShadowCascadesCountBias;
            renderingData.shadowData.mainLightShadowCascadesCount = Mathf.Clamp(renderingData.shadowData.mainLightShadowCascadesCount - MainLightShadowCascadesCountBias, 0, 4);

            var shadowQualityIndex = AdaptivePerformance.AdaptivePerformanceRenderSettings.ShadowQualityBias;
            for (int i = 0; i < shadowQualityIndex; i++)
            {
                if (renderingData.shadowData.supportsSoftShadows)
                {
                    renderingData.shadowData.supportsSoftShadows = false;
                    continue;
                }

                if (renderingData.shadowData.supportsAdditionalLightShadows)
                {
                    renderingData.shadowData.supportsAdditionalLightShadows = false;
                    continue;
                }

                if (renderingData.shadowData.supportsMainLightShadows)
                {
                    renderingData.shadowData.supportsMainLightShadows = false;
                    continue;
                }

                break;
            }

            if (AdaptivePerformance.AdaptivePerformanceRenderSettings.LutBias >= 1 && renderingData.postProcessingData.lutSize == 32)
                renderingData.postProcessingData.lutSize = 16;
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
    }
}
