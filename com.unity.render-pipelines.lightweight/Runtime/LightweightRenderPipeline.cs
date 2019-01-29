using System;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering.LWRP;
#endif
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering.LWRP;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;

namespace UnityEngine.Rendering.LWRP
{
    public interface IBeforeCameraRender
    {
        void ExecuteBeforeCameraRender(LightweightRenderPipeline pipelineInstance, ScriptableRenderContext context, Camera camera);
    }

    public sealed partial class LightweightRenderPipeline : RenderPipeline
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

        public const string k_ShaderTagName = "LightweightPipeline";

        const string k_RenderCameraTag = "Render Camera";

        public ScriptableRenderer renderer { get; private set; }

        public static float maxShadowBias
        {
            get { return 10.0f; }
        }

        public static float minRenderScale
        {
            get { return 0.1f; }
        }

        public static float maxRenderScale
        {
            get { return 4.0f; }
        }

        public static int maxPerObjectLightCount
        {
            get { return 4; }
        }

        public static LightweightRenderPipelineAsset asset
        {
            get
            {
                return GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
            }
        }

        public LightweightRenderPipeline(LightweightRenderPipelineAsset asset)
        {
            renderer = new ScriptableRenderer();

            SetSupportedRenderingFeatures();

            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;

            PerFrameBuffer._GlossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
            PerFrameBuffer._SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

            PerCameraBuffer._InvCameraViewProj = Shader.PropertyToID("_InvCameraViewProj");
            PerCameraBuffer._ScaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (QualitySettings.antiAliasing != asset.msaaSampleCount)
                QualitySettings.antiAliasing = asset.msaaSampleCount;

            Shader.globalRenderPipeline = "LightweightPipeline";

            Lightmapping.SetDelegate(lightsDelegate);

            CameraCaptureBridge.enabled = true;
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
            CameraCaptureBridge.enabled = false;
        }

        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            BeginFrameRendering(cameras);

            GraphicsSettings.lightsUseLinearIntensity = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            SetupPerFrameShaderConstants();

            SortCameras(cameras);
            foreach (Camera camera in cameras)
            {
                BeginCameraRendering(camera);

                foreach (var beforeCamera in camera.GetComponents<IBeforeCameraRender>())
                    beforeCamera.ExecuteBeforeCameraRender(this, renderContext, camera);

                RenderSingleCamera(this, renderContext, camera);
            }
        }

        public static void RenderSingleCamera(LightweightRenderPipeline pipelineInstance, ScriptableRenderContext context, Camera camera)
        {
            if (pipelineInstance == null)
            {
                Debug.LogError("Trying to render a camera with an invalid render pipeline instance.");
                return;
            }

            if (!camera.TryGetCullingParameters(IsStereoEnabled(camera), out var cullingParameters))
                return;

            CommandBuffer cmd = CommandBufferPool.Get(k_RenderCameraTag);
            using (new ProfilingSample(cmd, k_RenderCameraTag))
            {
                ScriptableRenderer renderer = pipelineInstance.renderer;
                var settings = asset;
                LWRPAdditionalCameraData additionalCameraData = camera.gameObject.GetComponent<LWRPAdditionalCameraData>();
                InitializeCameraData(settings, camera, additionalCameraData, out var cameraData);
                SetupPerCameraShaderConstants(cameraData);

                // TODO: PerObjectCulling also affect reflection probes. Enabling it for now.
                // if (asset.additionalLightsRenderingMode == LightRenderingMode.Disabled ||
                //     asset.maxAdditionalLightsCount == 0)
                // {
                //     cullingParameters.cullingOptions |= CullingOptions.DisablePerObjectCulling;
                // }

                cullingParameters.shadowDistance = Mathf.Min(cameraData.maxShadowDistance, camera.farClipPlane);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

#if UNITY_EDITOR

                // Emit scene view UI
                if (cameraData.isSceneViewCamera)
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif

                var cullResults = context.Cull(ref cullingParameters);

                InitializeRenderingData(settings, ref cameraData, ref cullResults,
                    renderer.maxVisibleAdditionalLights, renderer.maxPerObjectAdditionalLights, out var renderingData);

                renderer.Clear();

                IRendererSetup rendererSetup = (additionalCameraData != null) ? additionalCameraData.rendererSetup : settings.rendererSetup;
                rendererSetup.Setup(renderer, ref renderingData);
                renderer.Execute(context, ref renderingData);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            context.Submit();
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

        static void InitializeCameraData(LightweightRenderPipelineAsset settings, Camera camera, LWRPAdditionalCameraData additionalCameraData, out CameraData cameraData)
        {
            const float kRenderScaleThreshold = 0.05f;
            cameraData.camera = camera;

            bool msaaEnabled = camera.allowMSAA && settings.msaaSampleCount > 1;
            if (msaaEnabled)
                cameraData.msaaSamples = (camera.targetTexture != null) ? camera.targetTexture.antiAliasing : settings.msaaSampleCount;
            else
                cameraData.msaaSamples = 1;

            if (Camera.main == camera && camera.cameraType == CameraType.Game && camera.targetTexture == null)
            {
                // There's no exposed API to control how a backbuffer is created with MSAA
                // By settings antiAliasing we match what the amount of samples in camera data with backbuffer
                // We only do this for the main camera and this only takes effect in the beginning of next frame.
                // This settings should not be changed on a frame basis so that's fine.
                QualitySettings.antiAliasing = cameraData.msaaSamples;
            }


            cameraData.isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            cameraData.isStereoEnabled = IsStereoEnabled(camera);

            cameraData.isHdrEnabled = camera.allowHDR && settings.supportsHDR;

            cameraData.postProcessLayer = camera.GetComponent<PostProcessLayer>();
            cameraData.postProcessEnabled = cameraData.postProcessLayer != null && cameraData.postProcessLayer.isActiveAndEnabled;

            // Disables postprocessing in mobile VR. It's stable on mobile yet.
            if (cameraData.isStereoEnabled && Application.isMobilePlatform)
                cameraData.postProcessEnabled = false;

            Rect cameraRect = camera.rect;
            cameraData.isDefaultViewport = (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f ||
                Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f));

            // If XR is enabled, use XR renderScale.
            // Discard variations lesser than kRenderScaleThreshold.
            // Scale is only enabled for gameview.
            float usedRenderScale = XRGraphics.enabled ? XRGraphics.eyeTextureResolutionScale : settings.renderScale;
            cameraData.renderScale = (Mathf.Abs(1.0f - usedRenderScale) < kRenderScaleThreshold) ? 1.0f : usedRenderScale;
            cameraData.renderScale = (camera.cameraType == CameraType.Game) ? cameraData.renderScale : 1.0f;

            cameraData.opaqueTextureDownsampling = settings.opaqueDownsampling;

            bool anyShadowsEnabled = settings.supportsMainLightShadows || settings.supportsAdditionalLightShadows;
            cameraData.maxShadowDistance = (anyShadowsEnabled) ? settings.shadowDistance : 0.0f;
            
            if (additionalCameraData != null)
            {
                cameraData.maxShadowDistance = (additionalCameraData.renderShadows) ? cameraData.maxShadowDistance : 0.0f;
                cameraData.requiresDepthTexture = additionalCameraData.requiresDepthTexture;
                cameraData.requiresOpaqueTexture = additionalCameraData.requiresColorTexture;
            }
            else
            {
                cameraData.requiresDepthTexture = settings.supportsCameraDepthTexture;
                cameraData.requiresOpaqueTexture = settings.supportsCameraOpaqueTexture;
            }

            cameraData.requiresDepthTexture |= cameraData.isSceneViewCamera || cameraData.postProcessEnabled;

            var commonOpaqueFlags = SortingCriteria.CommonOpaque;
            var noFrontToBackOpaqueFlags = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;
            bool hasHSRGPU = SystemInfo.hasHiddenSurfaceRemovalOnGPU;
            bool canSkipFrontToBackSorting = (camera.opaqueSortMode == OpaqueSortMode.Default && hasHSRGPU) || camera.opaqueSortMode == OpaqueSortMode.NoDistanceSort;

            cameraData.defaultOpaqueSortFlags = canSkipFrontToBackSorting ? noFrontToBackOpaqueFlags : commonOpaqueFlags;
            cameraData.captureActions = CameraCaptureBridge.GetCaptureActions(camera);
        }

        static void InitializeRenderingData(LightweightRenderPipelineAsset settings, ref CameraData cameraData, ref CullingResults cullResults,
            int maxVisibleAdditionalLights, int maxPerObjectAdditionalLights, out RenderingData renderingData)
        {
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

            bool platformNeedsToKillAlpha = Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.tvOS;
            renderingData.killAlphaInFinalBlit = !Graphics.preserveFramebufferAlpha && platformNeedsToKillAlpha;
        }

        static void InitializeShadowData(LightweightRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights, bool mainLightCastShadows, bool additionalLightsCastShadows, out ShadowData shadowData)
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
            shadowData.requiresScreenSpaceShadowResolve = shadowData.supportsMainLightShadows && supportsScreenSpaceShadows && settings.shadowCascadeOption != ShadowCascadesOption.NoCascades;

            int shadowCascadesCount;
            switch (settings.shadowCascadeOption)
            {
                case ShadowCascadesOption.FourCascades:
                    shadowCascadesCount = 4;
                    break;

                case ShadowCascadesOption.TwoCascades:
                    shadowCascadesCount = 2;
                    break;

                default:
                    shadowCascadesCount = 1;
                    break;
            }

            shadowData.mainLightShadowCascadesCount = (shadowData.requiresScreenSpaceShadowResolve) ? shadowCascadesCount : 1;
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

        static void InitializeLightData(LightweightRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights, int mainLightIndex, int maxAdditionalLights,
            int maxPerObjectAdditionalLights, out LightData lightData)
        {
            lightData.mainLightIndex = mainLightIndex;

            if (settings.additionalLightsRenderingMode != LightRenderingMode.Disabled)
            {
                lightData.additionalLightsCount =
                    Math.Min((mainLightIndex != -1) ? visibleLights.Length - 1 : visibleLights.Length,
                        maxAdditionalLights);
                lightData.maxPerObjectAdditionalLightsCount = Math.Min(settings.maxAdditionalLightsCount, maxPerObjectAdditionalLights);
            }
            else
        {
                lightData.additionalLightsCount = 0;
                lightData.maxPerObjectAdditionalLightsCount = 0;
            }

            lightData.shadeAdditionalLightsPerVertex = settings.additionalLightsRenderingMode == LightRenderingMode.PerVertex;
            lightData.visibleLights = visibleLights;
            lightData.supportsMixedLighting = settings.supportsMixedLighting;
        }

        // Main Light is always a directional light
        static int GetMainLightIndex(LightweightRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights)
        {
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

                if (currLight == sunLight)
                    return i;

                // In case no shadow light is present we will return the brightest directional light
                if (currVisibleLight.lightType == LightType.Directional && currLight.intensity > brightestLightIntensity)
                {
                    brightestLightIntensity = currLight.intensity;
                    brightestDirectionalLightIndex = i;
                }
            }

            return brightestDirectionalLightIndex;
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

        static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] requests, NativeArray<LightDataGI> lightsOutput) =>
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
