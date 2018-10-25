using System;
using System.Collections.Generic;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.Rendering.LightweightPipeline;
#endif
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Experimental.GlobalIllumination;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public interface IBeforeCameraRender
    {
        void ExecuteBeforeCameraRender(LightweightRenderPipeline pipelineInstance, ScriptableRenderContext context, Camera camera);
    }

    public sealed partial class LightweightRenderPipeline : UnityEngine.Rendering.RenderPipeline
    {
        static class PerFrameBuffer
        {
            public static int _GlossyEnvironmentColor;
            public static int _SubtractiveShadowColor;
        }

        static class PerCameraBuffer
        {
            // TODO: This needs to account for stereo rendering
            public static int _InvCameraViewProj;
            public static int _ScaledScreenParams;
        }

        private static IRendererSetup s_DefaultRendererSetup;
        private static IRendererSetup defaultRendererSetup
        {
            get
            {
                if (s_DefaultRendererSetup == null)
                    s_DefaultRendererSetup = new DefaultRendererSetup();

                return s_DefaultRendererSetup;
            }
        }

        const string k_RenderCameraTag = "Render Camera";

        public ScriptableRenderer renderer { get; private set; }
        PipelineSettings settings { get; set; }

        internal struct PipelineSettings
        {
            public bool supportsCameraDepthTexture { get; private set; }
            public bool supportsCameraOpaqueTexture { get; private set; }
            public Downsampling opaqueDownsampling { get; private set; }
            public bool supportsHDR { get; private set; }
            public int msaaSampleCount { get; private set; }
            public float renderScale { get; private set; }
            public LightRenderingMode mainLightRenderingMode { get; private set; }
            public bool supportsMainLightShadows { get; private set; }
            public int mainLightShadowmapResolution { get; private set; }
            public LightRenderingMode additionalLightsRenderingMode { get; private set; }
            public int maxAdditionalLights { get; private set; }
            public bool supportsAdditionalLightShadows { get; private set; }
            public int additionalLightsShadowmapResolution { get; private set; }
            public float shadowDistance { get; private set; }
            public int cascadeCount { get; private set; }
            public float cascade2Split { get; private set; }
            public Vector3 cascade4Split { get; private set; }
            public float shadowDepthBias { get; private set; }
            public float shadowNormalBias { get; private set; }
            public bool supportsSoftShadows { get; private set; }
            public bool supportsDynamicBatching { get; private set; }
            public bool mixedLightingSupported { get; private set; }

            public static PipelineSettings Create(LightweightRenderPipelineAsset asset)
            {
                var cache = new PipelineSettings();
                // General settings
                cache.supportsCameraDepthTexture = asset.supportsCameraDepthTexture;
                cache.supportsCameraOpaqueTexture = asset.supportsCameraOpaqueTexture;
                cache.opaqueDownsampling = asset.opaqueDownsampling;

                // Quality settings
                cache.msaaSampleCount = asset.msaaSampleCount;
                cache.supportsHDR = asset.supportsHDR;
                cache.renderScale = asset.renderScale;

                // Main directional light settings
                cache.mainLightRenderingMode = asset.mainLightRenderingMode;
                cache.supportsMainLightShadows = asset.supportsMainLightShadows;
                cache.mainLightShadowmapResolution = asset.mainLightShadowmapResolution;
                
                // Additional light settings
                cache.additionalLightsRenderingMode = asset.additionalLightsRenderingMode;
                cache.maxAdditionalLights = asset.maxAdditionalLightsCount;
                cache.supportsAdditionalLightShadows = asset.supportsAdditionalLightShadows;
                cache.additionalLightsShadowmapResolution = asset.additionalLightsShadowmapResolution;

                // Shadow settings
                cache.shadowDistance = asset.shadowDistance;
                cache.cascadeCount = asset.cascadeCount;
                cache.cascade2Split = asset.cascade2Split;
                cache.cascade4Split = asset.cascade4Split;
                cache.shadowDepthBias = asset.shadowDepthBias;
                cache.shadowNormalBias = asset.shadowNormalBias;
                cache.supportsSoftShadows = asset.supportsSoftShadows;

                // Advanced settings
                cache.supportsDynamicBatching = asset.supportsDynamicBatching;
                cache.mixedLightingSupported = asset.supportsMixedLighting;
                
                return cache;
            }
        }

        public LightweightRenderPipeline(LightweightRenderPipelineAsset asset)
        {
            settings = PipelineSettings.Create(asset);
            renderer = new ScriptableRenderer(asset);

            SetSupportedRenderingFeatures();

            PerFrameBuffer._GlossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
            PerFrameBuffer._SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

            PerCameraBuffer._InvCameraViewProj = Shader.PropertyToID("_InvCameraViewProj");
            PerCameraBuffer._ScaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
            
            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (QualitySettings.antiAliasing != settings.msaaSampleCount)
                QualitySettings.antiAliasing = settings.msaaSampleCount;

            Shader.globalRenderPipeline = "LightweightPipeline";

            Lightmapping.SetDelegate(lightsDelegate);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Shader.globalRenderPipeline = "";
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
#endif

            renderer.Dispose();

            Lightmapping.ResetDelegate();
        }

        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            if (cameras == null || cameras.Length == 0)
            {
                Debug.LogWarning("The camera list passed to the render pipeline is either null or empty.");
                return;
            }

            BeginFrameRendering(cameras);

            GraphicsSettings.lightsUseLinearIntensity = true;
            SetupPerFrameShaderConstants();

            SortCameras(cameras);
            foreach (Camera camera in cameras)
            {
                BeginCameraRendering(camera);

                foreach (var beforeCamera in camera.GetComponents<IBeforeCameraRender>())
                    beforeCamera.ExecuteBeforeCameraRender(this, renderContext, camera);

                RenderSingleCamera(this, renderContext, camera, camera.GetComponent<IRendererSetup>());
            }
        }

        public static void RenderSingleCamera(LightweightRenderPipeline pipelineInstance, ScriptableRenderContext context, Camera camera, IRendererSetup setup = null)
        {
            if (pipelineInstance == null)
            {
                Debug.LogError("Trying to render a camera with an invalid render pipeline instance.");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderCameraTag);
            using (new ProfilingSample(cmd, k_RenderCameraTag))
            {
                CameraData cameraData;
                PipelineSettings settings = pipelineInstance.settings;
                ScriptableRenderer renderer = pipelineInstance.renderer;
                InitializeCameraData(settings, camera, out cameraData);
                SetupPerCameraShaderConstants(cameraData);

                ScriptableCullingParameters cullingParameters;
                if (!camera.TryGetCullingParameters(cameraData.isStereoEnabled, out cullingParameters))
                {
                    CommandBufferPool.Release(cmd);
                    return;
                }

                cullingParameters.shadowDistance = Mathf.Min(cameraData.maxShadowDistance, camera.farClipPlane);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

#if UNITY_EDITOR

                // Emit scene view UI
                if (cameraData.isSceneViewCamera)
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif

                var cullResults = context.Cull(ref cullingParameters);

                RenderingData renderingData;
                InitializeRenderingData(settings, ref cameraData, ref cullResults,
                    renderer.maxVisibleAdditionalLights, renderer.maxPerObjectAdditionalLights, out renderingData);

                var setupToUse = setup;
                if (setupToUse == null)
                    setupToUse = defaultRendererSetup;

                renderer.Clear();
                setupToUse.Setup(renderer, ref renderingData);
                renderer.Execute(context, ref renderingData);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                context.Submit();
#if UNITY_EDITOR
                Handles.DrawGizmos(camera);
#endif
            }
        }

        static void SetSupportedRenderingFeatures()
        {
#if UNITY_EDITOR
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.None,
                defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive,
                mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive,
                lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed,
                lightmapsModes = LightmapsMode.CombinedDirectional | LightmapsMode.NonDirectional,
                lightProbeProxyVolumes = false,
                motionVectors = false,
                receiveShadows = false,
                reflectionProbes = true
            };
            SceneViewDrawMode.SetupDrawMode();
#endif
        }

        static void InitializeCameraData(PipelineSettings settings, Camera camera, out CameraData cameraData)
        {
            const float kRenderScaleThreshold = 0.05f;
            cameraData.camera = camera;

            bool msaaEnabled = camera.allowMSAA && settings.msaaSampleCount > 1;
            if (msaaEnabled)
                cameraData.msaaSamples = (camera.targetTexture != null) ? camera.targetTexture.antiAliasing : settings.msaaSampleCount;
            else
                cameraData.msaaSamples = 1;

            cameraData.isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            cameraData.isStereoEnabled = IsStereoEnabled(camera);

            cameraData.isHdrEnabled = camera.allowHDR && settings.supportsHDR;

            cameraData.postProcessLayer = camera.GetComponent<PostProcessLayer>();
            cameraData.postProcessEnabled = cameraData.postProcessLayer != null && cameraData.postProcessLayer.isActiveAndEnabled;

            Rect cameraRect = camera.rect;
            cameraData.isDefaultViewport = (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f ||
                Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f));

            // If XR is enabled, use XR renderScale.
            // Discard variations lesser than kRenderScaleThreshold.
            // Scale is only enabled for gameview.
            float usedRenderScale = XRGraphics.enabled ? XRGraphics.eyeTextureResolutionScale : settings.renderScale;
            cameraData.renderScale = (Mathf.Abs(1.0f - usedRenderScale) < kRenderScaleThreshold) ? 1.0f : usedRenderScale;
            cameraData.renderScale = (camera.cameraType == CameraType.Game) ? cameraData.renderScale : 1.0f;

            cameraData.requiresDepthTexture = settings.supportsCameraDepthTexture || cameraData.isSceneViewCamera;
            cameraData.requiresOpaqueTexture = settings.supportsCameraOpaqueTexture;
            cameraData.opaqueTextureDownsampling = settings.opaqueDownsampling;

            bool anyShadowsEnabled = settings.supportsMainLightShadows || settings.supportsAdditionalLightShadows;
            cameraData.maxShadowDistance = (anyShadowsEnabled) ? settings.shadowDistance : 0.0f;

            AdditionalCameraData additionalCameraData = camera.gameObject.GetComponent<AdditionalCameraData>();
            if (additionalCameraData != null)
            {
                cameraData.maxShadowDistance = (additionalCameraData.renderShadows) ? cameraData.maxShadowDistance : 0.0f;
                cameraData.requiresDepthTexture &= additionalCameraData.requiresDepthTexture;
                cameraData.requiresOpaqueTexture &= additionalCameraData.requiresColorTexture;
            }
            else if (!cameraData.isSceneViewCamera && camera.cameraType != CameraType.Reflection && camera.cameraType != CameraType.Preview)
            {
                cameraData.requiresDepthTexture = false;
                cameraData.requiresOpaqueTexture = false;
            }

            cameraData.requiresDepthTexture |= cameraData.postProcessEnabled;

            var commonOpaqueFlags = SortingCriteria.CommonOpaque;
            var noFrontToBackOpaqueFlags = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;
            bool hasHSRGPU = SystemInfo.hasHiddenSurfaceRemovalOnGPU;
            bool canSkipFrontToBackSorting = (camera.opaqueSortMode == OpaqueSortMode.Default && hasHSRGPU) || camera.opaqueSortMode == OpaqueSortMode.NoDistanceSort;

            cameraData.defaultOpaqueSortFlags = canSkipFrontToBackSorting ? noFrontToBackOpaqueFlags : commonOpaqueFlags;
        }

        static void InitializeRenderingData(PipelineSettings settings, ref CameraData cameraData, ref CullingResults cullResults,
            int maxVisibleAdditionalLights, int maxPerObjectAdditionalLights, out RenderingData renderingData)
        {
            var visibleLights = cullResults.visibleLights;

            int mainLightIndex = GetMainLight(settings, visibleLights);
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

                        // LWRP doesn't support additional directional lights or point light shadows yet
                        if (visibleLights[i].lightType == LightType.Spot && light != null && light.shadows != LightShadows.None)
                        {
                            additionalLightsCastShadows = true;
                            break;
                        }
                    }
                }
            }

            renderingData.cullResults = cullResults;
            renderingData.cameraData = cameraData;
            InitializeLightData(settings, visibleLights, mainLightIndex, maxVisibleAdditionalLights, maxPerObjectAdditionalLights, out renderingData.lightData);
            InitializeShadowData(settings, visibleLights, mainLightCastShadows, additionalLightsCastShadows && !renderingData.lightData.shadeAdditionalLightsPerVertex, out renderingData.shadowData);
            renderingData.supportsDynamicBatching = settings.supportsDynamicBatching;
        }

        static void InitializeShadowData(PipelineSettings settings, NativeArray<VisibleLight> visibleLights, bool mainLightCastShadows, bool additionalLightsCastShadows, out ShadowData shadowData)
        {
            m_ShadowBiasData.Clear();

            for (int i = 0; i < visibleLights.Length; ++i)
            {
                Light light = visibleLights[i].light;
                LWRPAdditionalLightData data =
                    (light != null) ? light.gameObject.GetComponent<LWRPAdditionalLightData>() : null;

                if (data && !data.usePipelineSettings)
                    m_ShadowBiasData.Add(new Vector4(light.shadowBias, light.shadowNormalBias, 0.0f, 0.0f));
                else
                    m_ShadowBiasData.Add(new Vector4(settings.shadowDepthBias, settings.shadowNormalBias, 0.0f, 0.0f));
            }

            shadowData.bias = m_ShadowBiasData;

            // Until we can have keyword stripping forcing single cascade hard shadows on gles2
            bool supportsScreenSpaceShadows = SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            shadowData.supportsMainLightShadows = settings.supportsMainLightShadows && mainLightCastShadows;

            // we resolve shadows in screenspace when cascades are enabled to save ALU as computing cascade index + shadowCoord on fragment is expensive
            shadowData.requiresScreenSpaceShadowResolve = shadowData.supportsMainLightShadows && supportsScreenSpaceShadows && settings.cascadeCount > 1;
            shadowData.mainLightShadowCascadesCount = (shadowData.requiresScreenSpaceShadowResolve) ? settings.cascadeCount : 1;
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

                default:
                    shadowData.mainLightShadowCascadesSplit = settings.cascade4Split;
                    break;
            }

            shadowData.supportsAdditionalLightShadows = settings.supportsAdditionalLightShadows && additionalLightsCastShadows;
            shadowData.additionalLightsShadowmapWidth = shadowData.additionalLightsShadowmapHeight = settings.additionalLightsShadowmapResolution;
            shadowData.supportsSoftShadows = settings.supportsSoftShadows && (shadowData.supportsMainLightShadows || shadowData.supportsAdditionalLightShadows);
            shadowData.shadowmapDepthBufferBits = 16;
        }

        static void InitializeLightData(PipelineSettings settings, NativeArray<VisibleLight> visibleLights, int mainLightIndex, int maxAdditionalLights,
            int maxPerObjectAdditionalLights, out LightData lightData)
        {
            lightData.mainLightIndex = mainLightIndex;

            if (settings.additionalLightsRenderingMode != LightRenderingMode.Disabled)
            {
                lightData.additionalLightsCount =
                    Math.Min((mainLightIndex != -1) ? visibleLights.Length - 1 : visibleLights.Length,
                        maxAdditionalLights);
                lightData.maxPerObjectAdditionalLightsCount = Math.Min(settings.maxAdditionalLights, maxPerObjectAdditionalLights);
            }
            else
        {
                lightData.additionalLightsCount = 0;
                lightData.maxPerObjectAdditionalLightsCount = 0;
            }

            lightData.shadeAdditionalLightsPerVertex = settings.additionalLightsRenderingMode == LightRenderingMode.PerVertex;
            lightData.visibleLights = visibleLights;
            lightData.supportsMixedLighting = settings.mixedLightingSupported;
        }

        // Main Light is always a directional light
        static int GetMainLight(PipelineSettings settings, NativeArray<VisibleLight> visibleLights)
        {
            int totalVisibleLights = visibleLights.Length;

            if (totalVisibleLights == 0 || settings.mainLightRenderingMode != LightRenderingMode.PerPixel)
                return -1;

            for (int i = 0; i < totalVisibleLights; ++i)
            {
                VisibleLight currLight = visibleLights[i];

                // Particle system lights have the light property as null. We sort lights so all particles lights
                // come last. Therefore, if first light is particle light then all lights are particle lights.
                // In this case we either have no main light or already found it.
                if (currLight.light == null)
                    break;

                // In case no shadow light is present we will return the brightest directional light
                if (currLight.lightType == LightType.Directional)
                    return i;
            }

            return -1;
        }

        static void SetupPerFrameShaderConstants()
        {
            // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
            SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
            Color linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
            Color glossyEnvColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
            Shader.SetGlobalVector(PerFrameBuffer._GlossyEnvironmentColor, glossyEnvColor);

            // Used when subtractive mode is selected
            Shader.SetGlobalVector(PerFrameBuffer._SubtractiveShadowColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor));
        }

        static void SetupPerCameraShaderConstants(CameraData cameraData)
        {
            Camera camera = cameraData.camera;
            float cameraWidth = (float)cameraData.camera.pixelWidth * cameraData.renderScale;
            float cameraHeight = (float)cameraData.camera.pixelHeight * cameraData.renderScale;
            Shader.SetGlobalVector(PerCameraBuffer._ScaledScreenParams, new Vector4(cameraWidth, cameraHeight, 1.0f + 1.0f / cameraWidth, 1.0f + 1.0f / cameraHeight));

            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
            Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
            Shader.SetGlobalMatrix(PerCameraBuffer._InvCameraViewProj, invViewProjMatrix);
        }

        public static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] requests, NativeArray<LightDataGI> lightsOutput) =>
        {
            LightDataGI lightData = new LightDataGI();

            for (int i = 0; i < requests.Length; i++)
            {
                Light light = requests[i];
                switch (light.type)
                {
                    case LightType.Directional:
                        DirectionalLight directionalLight = new DirectionalLight();
                        LightmapperUtils.Extract(light, ref directionalLight); lightData.Init(ref directionalLight);
                        break;
                    case LightType.Point:
                        PointLight pointLight = new PointLight();
                        LightmapperUtils.Extract(light, ref pointLight); lightData.Init(ref pointLight);
                        break;
                    case LightType.Spot:
                        SpotLight spotLight = new SpotLight();
                        LightmapperUtils.Extract(light, ref spotLight); lightData.Init(ref spotLight);
                        break;
                    case LightType.Area:
                        RectangleLight rectangleLight = new RectangleLight();
                        LightmapperUtils.Extract(light, ref rectangleLight); lightData.Init(ref rectangleLight);
                        break;
                    default:
                        lightData.InitNoBake(light.GetInstanceID());
                        break;
                }

                lightData.falloff = FalloffType.InverseSquared;
                lightsOutput[i] = lightData;
            }
        };
    }
}
