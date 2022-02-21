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
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline : RenderPipeline
    {
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
                    ps = new ProfilingSampler($"{nameof(UniversalRenderPipeline)}.{nameof(RenderSingleCamera)}: {camera.name}");
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

                public static class XR
                {
                    public static readonly ProfilingSampler mirrorView = new ProfilingSampler("XR Mirror View");
                };
            };
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        internal static XRSystem m_XRSystem = new XRSystem();
#endif

        public static float maxShadowBias
        {
            get => 10.0f;
        }

        public static float minRenderScale
        {
            get => 0.1f;
        }

        public static float maxRenderScale
        {
            get => 2.0f;
        }

        // Amount of Lights that can be shaded per object (in the for loop in the shader)
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
        internal static int maxZBins => 1024 * 4;
        internal static int maxTileVec4s => 4096;

        internal const int k_DefaultRenderingLayerMask = 0x00000001;
        private readonly DebugDisplaySettingsUI m_DebugDisplaySettingsUI = new DebugDisplaySettingsUI();

        private UniversalRenderPipelineGlobalSettings m_GlobalSettings;
        public override RenderPipelineGlobalSettings defaultSettings => m_GlobalSettings;

        public UniversalRenderPipeline(UniversalRenderPipelineAsset asset)
        {
#if UNITY_EDITOR
            m_GlobalSettings = UniversalRenderPipelineGlobalSettings.Ensure();
#else
            m_GlobalSettings = UniversalRenderPipelineGlobalSettings.instance;
#endif
            SetSupportedRenderingFeatures();

            // In QualitySettings.antiAliasing disabled state uses value 0, where in URP 1
            int qualitySettingsMsaaSampleCount = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
            bool msaaSampleCountNeedsUpdate = qualitySettingsMsaaSampleCount != asset.msaaSampleCount;

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (msaaSampleCountNeedsUpdate)
            {
                QualitySettings.antiAliasing = asset.msaaSampleCount;
#if ENABLE_VR && ENABLE_XR_MODULE
                XRSystem.UpdateMSAALevel(asset.msaaSampleCount);
#endif
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            XRSystem.UpdateRenderScale(asset.renderScale);
#endif
            Shader.globalRenderPipeline = "UniversalPipeline";

            Lightmapping.SetDelegate(lightsDelegate);

            CameraCaptureBridge.enabled = true;

            RenderingUtils.ClearSystemInfoCache();

            DecalProjector.defaultMaterial = asset.decalMaterial;

            DebugManager.instance.RefreshEditor();
            m_DebugDisplaySettingsUI.RegisterDebug(DebugDisplaySettings.Instance);
        }

        protected override void Dispose(bool disposing)
        {
            m_DebugDisplaySettingsUI.UnregisterDebug();

            base.Dispose(disposing);

            Shader.globalRenderPipeline = "";
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
            ShaderData.instance.Dispose();
            DeferredShaderData.instance.Dispose();

#if ENABLE_VR && ENABLE_XR_MODULE
            m_XRSystem?.Dispose();
#endif

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
#endif
            Lightmapping.ResetDelegate();
            CameraCaptureBridge.enabled = false;
        }

#if UNITY_2021_1_OR_NEWER
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            Render(renderContext, new List<Camera>(cameras));
        }

#endif

#if UNITY_2021_1_OR_NEWER
        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
#else
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
#endif
        {
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
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;
            GraphicsSettings.defaultRenderingLayerMask = k_DefaultRenderingLayerMask;
            SetupPerFrameShaderConstants();
#if ENABLE_VR && ENABLE_XR_MODULE
            // Update XR MSAA level per frame.
            XRSystem.UpdateMSAALevel(asset.msaaSampleCount);
#endif

#if UNITY_EDITOR
            // We do not want to start rendering if URP global settings are not ready (m_globalSettings is null)
            // or been deleted/moved (m_globalSettings is not necessarily null)
            if (m_GlobalSettings == null || UniversalRenderPipelineGlobalSettings.instance == null)
            {
                m_GlobalSettings = UniversalRenderPipelineGlobalSettings.Ensure();
                if(m_GlobalSettings == null) return;
            }
#endif

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
                    using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
                    {
                        BeginCameraRendering(renderContext, camera);
                    }
#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                    //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                    VFX.VFXManager.PrepareCamera(camera);
#endif
                    UpdateVolumeFramework(camera, null);

                    RenderSingleCamera(renderContext, camera);

                    using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
                    {
                        EndCameraRendering(renderContext, camera);
                    }
                }
            }
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
        }

        /// <summary>
        /// Standalone camera rendering. Use this to render procedural cameras.
        /// This method doesn't call <c>BeginCameraRendering</c> and <c>EndCameraRendering</c> callbacks.
        /// </summary>
        /// <param name="context">Render context used to record commands during execution.</param>
        /// <param name="camera">Camera to render.</param>
        /// <seealso cref="ScriptableRenderContext"/>
        public static void RenderSingleCamera(ScriptableRenderContext context, Camera camera)
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
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            if (asset.useAdaptivePerformance)
                ApplyAdaptivePerformance(ref cameraData);
#endif
            RenderSingleCamera(context, cameraData, cameraData.postProcessEnabled);
        }

        static bool TryGetCullingParameters(CameraData cameraData, out ScriptableCullingParameters cullingParams)
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
        static void RenderSingleCamera(ScriptableRenderContext context, CameraData cameraData, bool anyPostProcessingEnabled)
        {
            Camera camera = cameraData.camera;
            var renderer = cameraData.renderer;
            if (renderer == null)
            {
                Debug.LogWarning(string.Format("Trying to render {0} with an invalid renderer. Camera rendering will be skipped.", camera.name));
                return;
            }

            if (!TryGetCullingParameters(cameraData, out var cullingParameters))
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
                {
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                }
#endif

                var cullResults = context.Cull(ref cullingParameters);
                InitializeRenderingData(asset, ref cameraData, ref cullResults, anyPostProcessingEnabled, out var renderingData);

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
                if (asset.useAdaptivePerformance)
                    ApplyAdaptivePerformance(ref renderingData);
#endif

                using (new ProfilingScope(null, Profiling.Pipeline.Renderer.setup))
                {
                    renderer.Setup(context, ref renderingData);
                }

                // Timing scope inside
                renderer.Execute(context, ref renderingData);
                CleanupLightData(ref renderingData.lightData);
            } // When ProfilingSample goes out of scope, an "EndSample" command is enqueued into CommandBuffer cmd

            cameraData.xr.EndCamera(cmd, cameraData);
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
        // Renders a camera stack. This method calls RenderSingleCamera for each valid camera in the stack.
        // The last camera resolves the final target to screen.
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

            // renderer contains a stack if it has additional data and the renderer supports stacking
            var renderer = baseCameraAdditionalData?.scriptableRenderer;
            bool supportsCameraStacking = renderer != null && renderer.supportedRenderingFeatures.cameraStacking;
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

                        if (data == null || data.renderType != CameraRenderType.Overlay)
                        {
                            Debug.LogWarning(string.Format("Stack can only contain Overlay cameras. {0} will skip rendering.", currCamera.name));
                            continue;
                        }

                        var currCameraRendererType = data?.scriptableRenderer.GetType();
                        if (currCameraRendererType != baseCameraRendererType)
                        {
                            var renderer2DType = typeof(Renderer2D);
                            if (currCameraRendererType != renderer2DType && baseCameraRendererType != renderer2DType)
                            {
                                Debug.LogWarning(string.Format("Only cameras with compatible renderer types can be stacked. {0} will skip rendering", currCamera.name));
                                continue;
                            }
                        }

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

#if ENABLE_VR && ENABLE_XR_MODULE
            var xrActive = false;
            var xrRendering = true;
            if (baseCameraAdditionalData != null)
                xrRendering = baseCameraAdditionalData.allowXRRendering;
            var xrPasses = m_XRSystem.SetupFrame(baseCamera, xrRendering);
            foreach (XRPass xrPass in xrPasses)
            {
                if (xrPass.enabled)
                {
                    xrActive = true;
                    UpdateCameraStereoMatrices(baseCamera, xrPass);
                }
#endif

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
                // XRTODO: remove isStereoEnabled in 2021.x
#pragma warning disable 0618
                baseCameraData.isStereoEnabled = xrPass.enabled;
#pragma warning restore 0618

                // Helper function for updating cameraData with xrPass Data
                m_XRSystem.UpdateCameraData(ref baseCameraData, baseCameraData.xr);
                // Need to update XRSystem using baseCameraData to handle the case where camera position is modified in BeginCameraRendering
                m_XRSystem.UpdateFromCamera(ref baseCameraData.xr, baseCameraData);
                m_XRSystem.BeginLateLatching(baseCamera, xrPass);
            }
#endif

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
            //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
            VFX.VFXManager.PrepareCamera(baseCamera);
#endif
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            if (asset.useAdaptivePerformance)
                ApplyAdaptivePerformance(ref baseCameraData);
#endif
            RenderSingleCamera(context, baseCameraData, anyPostProcessingEnabled);
            using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
            {
                EndCameraRendering(context, baseCamera);
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            m_XRSystem.EndLateLatching(baseCamera, xrPass);
#endif

            if (isStackedRendering)
            {
                for (int i = 0; i < cameraStack.Count; ++i)
                {
                    var currCamera = cameraStack[i];
                    if (!currCamera.isActiveAndEnabled)
                        continue;

                    currCamera.TryGetComponent<UniversalAdditionalCameraData>(out var currCameraData);
                    // Camera is overlay and enabled
                    if (currCameraData != null)
                    {
                        // Copy base settings from base camera data and initialize initialize remaining specific settings for this camera type.
                        CameraData overlayCameraData = baseCameraData;
                        bool lastCamera = i == lastActiveOverlayCameraIndex;

#if ENABLE_VR && ENABLE_XR_MODULE
                        UpdateCameraStereoMatrices(currCameraData.camera, xrPass);
#endif

                        using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
                        {
                            BeginCameraRendering(context, currCamera);
                        }
#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                        //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                        VFX.VFXManager.PrepareCamera(currCamera);
#endif
                        UpdateVolumeFramework(currCamera, currCameraData);
                        InitializeAdditionalCameraData(currCamera, currCameraData, lastCamera, ref overlayCameraData);
#if ENABLE_VR && ENABLE_XR_MODULE
                        if (baseCameraData.xr.enabled)
                            m_XRSystem.UpdateFromCamera(ref overlayCameraData.xr, overlayCameraData);
#endif
                        RenderSingleCamera(context, overlayCameraData, anyPostProcessingEnabled);

                        using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
                        {
                            EndCameraRendering(context, currCamera);
                        }
                    }
                }
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (baseCameraData.xr.enabled)
                baseCameraData.cameraTargetDescriptor = originalTargetDesc;
        }

        if (xrActive)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, Profiling.Pipeline.XR.mirrorView))
            {
                m_XRSystem.RenderMirrorView(cmd, baseCamera);
            }

            context.ExecuteCommandBuffer(cmd);
            context.Submit();
            CommandBufferPool.Release(cmd);
        }

        m_XRSystem.ReleaseFrame();
#endif
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

        static bool CheckPostProcessForDepth(in CameraData cameraData)
        {
            if (!cameraData.postProcessEnabled)
                return false;

            if (cameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                return true;

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
                motionVectors = false,
                receiveShadows = false,
                reflectionProbes = false,
                reflectionProbesBlendDistance = true,
                particleSystemInstancing = true
            };
            SceneViewDrawMode.SetupDrawMode();
#endif
        }

        static void InitializeCameraData(Camera camera, UniversalAdditionalCameraData additionalCameraData, bool resolveFinalTarget, out CameraData cameraData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeCameraData);

            cameraData = new CameraData();
            InitializeStackedCameraData(camera, additionalCameraData, ref cameraData);
            InitializeAdditionalCameraData(camera, additionalCameraData, resolveFinalTarget, ref cameraData);

            ///////////////////////////////////////////////////////////////////
            // Descriptor settings                                            /
            ///////////////////////////////////////////////////////////////////

            var renderer = additionalCameraData?.scriptableRenderer;
            bool rendererSupportsMSAA = renderer != null && renderer.supportedRenderingFeatures.msaa;

            int msaaSamples = 1;
            if (camera.allowMSAA && asset.msaaSampleCount > 1 && rendererSupportsMSAA)
                msaaSamples = (camera.targetTexture != null) ? camera.targetTexture.antiAliasing : asset.msaaSampleCount;
#if ENABLE_VR && ENABLE_XR_MODULE
            // Use XR's MSAA if camera is XR camera. XR MSAA needs special handle here because it is not per Camera.
            // Multiple cameras could render into the same XR display and they should share the same MSAA level.
            if (cameraData.xrRendering && rendererSupportsMSAA)
                msaaSamples = XRSystem.GetMSAALevel();
#endif

            bool needsAlphaChannel = Graphics.preserveFramebufferAlpha;
            cameraData.cameraTargetDescriptor = CreateRenderTextureDescriptor(camera, cameraData.renderScale,
                cameraData.isHdrEnabled, msaaSamples, needsAlphaChannel, cameraData.requiresOpaqueTexture);
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
#if ENABLE_VR && ENABLE_XR_MODULE
                cameraData.xrRendering = false;
#endif
            }
            else if (baseAdditionalCameraData != null)
            {
                cameraData.volumeLayerMask = baseAdditionalCameraData.volumeLayerMask;
                cameraData.volumeTrigger = baseAdditionalCameraData.volumeTrigger == null ? baseCamera.transform : baseAdditionalCameraData.volumeTrigger;
                cameraData.isStopNaNEnabled = baseAdditionalCameraData.stopNaN && SystemInfo.graphicsShaderLevel >= 35;
                cameraData.isDitheringEnabled = baseAdditionalCameraData.dithering;
                cameraData.antialiasing = baseAdditionalCameraData.antialiasing;
                cameraData.antialiasingQuality = baseAdditionalCameraData.antialiasingQuality;
#if ENABLE_VR && ENABLE_XR_MODULE
                cameraData.xrRendering = baseAdditionalCameraData.allowXRRendering && m_XRSystem.RefreshXrSdk();
#endif
            }
            else
            {
                cameraData.volumeLayerMask = 1; // "Default"
                cameraData.volumeTrigger = null;
                cameraData.isStopNaNEnabled = false;
                cameraData.isDitheringEnabled = false;
                cameraData.antialiasing = AntialiasingMode.None;
                cameraData.antialiasingQuality = AntialiasingQuality.High;
#if ENABLE_VR && ENABLE_XR_MODULE
                cameraData.xrRendering = m_XRSystem.RefreshXrSdk();
#endif
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

#if ENABLE_VR && ENABLE_XR_MODULE
            cameraData.xr = m_XRSystem.emptyPass;
            XRSystem.UpdateRenderScale(cameraData.renderScale);
#else
            cameraData.xr = XRPass.emptyPass;
#endif

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
            cameraData.camera = camera;

            bool anyShadowsEnabled = settings.supportsMainLightShadows || settings.supportsAdditionalLightShadows;
            cameraData.maxShadowDistance = Mathf.Min(settings.shadowDistance, camera.farClipPlane);
            cameraData.maxShadowDistance = (anyShadowsEnabled && cameraData.maxShadowDistance >= camera.nearClipPlane) ? cameraData.maxShadowDistance : 0.0f;

            // Getting the background color from preferences to add to the preview camera
#if UNITY_EDITOR
            if (cameraData.camera.cameraType == CameraType.Preview)
            {
                camera.backgroundColor = CoreRenderPipelinePreferences.previewBackgroundColor;
            }
#endif

            bool isSceneViewCamera = cameraData.isSceneViewCamera;
            if (isSceneViewCamera)
            {
                cameraData.renderType = CameraRenderType.Base;
                cameraData.clearDepth = true;
                cameraData.postProcessEnabled = CoreUtils.ArePostProcessesEnabled(camera);
                cameraData.requiresDepthTexture = settings.supportsCameraDepthTexture;
                cameraData.requiresOpaqueTexture = settings.supportsCameraOpaqueTexture;
                cameraData.renderer = asset.scriptableRenderer;
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
            }
            else
            {
                cameraData.renderType = CameraRenderType.Base;
                cameraData.clearDepth = true;
                cameraData.postProcessEnabled = false;
                cameraData.requiresDepthTexture = settings.supportsCameraDepthTexture;
                cameraData.requiresOpaqueTexture = settings.supportsCameraOpaqueTexture;
                cameraData.renderer = asset.scriptableRenderer;
            }

            // Disables post if GLes2
            cameraData.postProcessEnabled &= SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            cameraData.requiresDepthTexture |= isSceneViewCamera;
            cameraData.postProcessingRequiresDepthTexture |= CheckPostProcessForDepth(cameraData);
            cameraData.resolveFinalTarget = resolveFinalTarget;

            // Disable depth and color copy. We should add it in the renderer instead to avoid performance pitfalls
            // of camera stacking breaking render pass execution implicitly.
            bool isOverlayCamera = (cameraData.renderType == CameraRenderType.Overlay);
            if (isOverlayCamera)
            {
                cameraData.requiresDepthTexture = false;
                cameraData.requiresOpaqueTexture = false;
                cameraData.postProcessingRequiresDepthTexture = false;
            }

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

            cameraData.SetViewAndProjectionMatrix(camera.worldToCameraMatrix, projectionMatrix);

            cameraData.worldSpaceCameraPos = camera.transform.position;
        }

        static void InitializeRenderingData(UniversalRenderPipelineAsset settings, ref CameraData cameraData, ref CullingResults cullResults,
            bool anyPostProcessingEnabled, out RenderingData renderingData)
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

                // If additional lights are shaded per-pixel they cannot cast shadows
                if (settings.additionalLightsRenderingMode == LightRenderingMode.PerPixel)
                {
                    for (int i = 0; i < visibleLights.Length; ++i)
                    {
                        if (i == mainLightIndex)
                            continue;

                        Light light = visibleLights[i].light;

                        // UniversalRP doesn't support additional directional light shadows yet
                        if ((visibleLights[i].lightType == LightType.Spot || visibleLights[i].lightType == LightType.Point) && light != null && light.shadows != LightShadows.None)
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
            renderingData.perObjectData = GetPerObjectLightFlags(renderingData.lightData.additionalLightsCount);
            renderingData.postProcessingEnabled = anyPostProcessingEnabled;

            CheckAndApplyDebugSettings(ref renderingData);
        }

        static void InitializeShadowData(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights, bool mainLightCastShadows, bool additionalLightsCastShadows, out ShadowData shadowData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeShadowData);

            m_ShadowBiasData.Clear();
            m_ShadowResolutionData.Clear();

            for (int i = 0; i < visibleLights.Length; ++i)
            {
                Light light = visibleLights[i].light;
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

            shadowData.mainLightShadowCascadesCount = settings.shadowCascadeCount;
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
            lightData.supportsLightLayers = RenderingUtils.SupportsLightLayers(SystemInfo.graphicsDeviceType) && settings.supportsLightLayers;
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

        static PerObjectData GetPerObjectLightFlags(int additionalLightsCount)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.getPerObjectLightFlags);

            var configuration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightData | PerObjectData.OcclusionProbe | PerObjectData.ShadowMask;

            if (additionalLightsCount > 0)
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
                VisibleLight currVisibleLight = visibleLights[i];
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
        }

        static void CheckAndApplyDebugSettings(ref RenderingData renderingData)
        {
            DebugDisplaySettings debugDisplaySettings = DebugDisplaySettings.Instance;
            ref CameraData cameraData = ref renderingData.cameraData;

            if (debugDisplaySettings.AreAnySettingsActive && !cameraData.isPreviewCamera)
            {
                DebugDisplaySettingsRendering renderingSettings = debugDisplaySettings.RenderingSettings;
                int msaaSamples = cameraData.cameraTargetDescriptor.msaaSamples;

                if (!renderingSettings.enableMsaa)
                    msaaSamples = 1;

                if (!renderingSettings.enableHDR)
                    cameraData.isHdrEnabled = false;

                if (!debugDisplaySettings.IsPostProcessingAllowed)
                    cameraData.postProcessEnabled = false;

                cameraData.cameraTargetDescriptor.graphicsFormat = MakeRenderTextureGraphicsFormat(cameraData.isHdrEnabled, true);
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
    }
}
