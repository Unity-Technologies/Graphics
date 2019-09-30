using System;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering.LWRP;
#endif
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Experimental.GlobalIllumination;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;

namespace UnityEngine.Rendering.LWRP
{
    public sealed partial class LightweightRenderPipeline : RenderPipeline
    {
        static internal class PerFrameBuffer
        {
            public static int _GlossyEnvironmentColor;
            public static int _SubtractiveShadowColor;

            public static int _Time;
            public static int _SinTime;
            public static int _CosTime;
            public static int unity_DeltaTime;
            public static int _TimeParameters;
        }

        static internal class PerCameraBuffer
        {
            // TODO: This needs to account for stereo rendering
            public static int _InvCameraViewProj;
            public static int _ScaledScreenParams;
            public static int _ScreenParams;
            public static int _WorldSpaceCameraPos;
        }

        public const string k_ShaderTagName = "LightweightPipeline";

        const string k_RenderCameraTag = "Render Camera";

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
            get => 4.0f;
        }

        // Amount of Lights that can be shaded per object (in the for loop in the shader)
        // This uses unity_4LightIndices to store an array of 4 light indices
        public static int maxPerObjectLights
        {
            get => 4;
        }

        // Light data is stored in a constant buffer (uniform array)
        // This value has to match MAX_VISIBLE_LIGHTS in Input.hlsl
        public static int maxVisibleAdditionalLights
        {
            get => 16;
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
            SetSupportedRenderingFeatures();

            PerFrameBuffer._GlossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
            PerFrameBuffer._SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

            PerFrameBuffer._Time = Shader.PropertyToID("_Time");
            PerFrameBuffer._SinTime = Shader.PropertyToID("_SinTime");
            PerFrameBuffer._CosTime = Shader.PropertyToID("_CosTime");
            PerFrameBuffer.unity_DeltaTime = Shader.PropertyToID("unity_DeltaTime");
            PerFrameBuffer._TimeParameters = Shader.PropertyToID("_TimeParameters");

            PerCameraBuffer._InvCameraViewProj = Shader.PropertyToID("_InvCameraViewProj");
            PerCameraBuffer._ScreenParams = Shader.PropertyToID("_ScreenParams");
            PerCameraBuffer._ScaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
            PerCameraBuffer._WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (QualitySettings.antiAliasing != asset.msaaSampleCount)
                QualitySettings.antiAliasing = asset.msaaSampleCount;

            Shader.globalRenderPipeline = "LightweightPipeline";

            Lightmapping.SetDelegate(lightsDelegate);

            CameraCaptureBridge.enabled = true;

            RenderingUtils.ClearSystemInfoCache();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Shader.globalRenderPipeline = "";
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
#endif

            Lightmapping.ResetDelegate();
            CameraCaptureBridge.enabled = false;
        }

        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            BeginFrameRendering(renderContext, cameras);

            GraphicsSettings.lightsUseLinearIntensity = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;
            SetupPerFrameShaderConstants();

            SortCameras(cameras);
            foreach (Camera camera in cameras)
            {
                BeginCameraRendering(renderContext, camera);

                UnityEngine.Experimental.VFX.VFXManager.ProcessCamera(camera); //Visual Effect Graph is not yet a required package but calling this method when there isn't any VisualEffect component has no effect (but needed for Camera sorting in Visual Effect Graph context)
                RenderSingleCamera(renderContext, camera);

                EndCameraRendering(renderContext, camera);
            }

            EndFrameRendering(renderContext, cameras);
        }

        public static void RenderSingleCamera(ScriptableRenderContext context, Camera camera)
        {
            if (!camera.TryGetCullingParameters(IsStereoEnabled(camera), out var cullingParameters))
                return;

            var settings = asset;
            LWRPAdditionalCameraData additionalCameraData = null;
            if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.VR)
#if UNITY_2019_2_OR_NEWER
                camera.gameObject.TryGetComponent(out additionalCameraData);
#else
                additionalCameraData = camera.gameObject.GetComponent<LWRPAdditionalCameraData>();
#endif

            InitializeCameraData(settings, camera, additionalCameraData, out var cameraData);
            SetupPerCameraShaderConstants(cameraData);

            ScriptableRenderer renderer = (additionalCameraData != null) ? additionalCameraData.scriptableRenderer : settings.scriptableRenderer;
            if (renderer == null)
            {
                Debug.LogWarning(string.Format("Trying to render {0} with an invalid renderer. Camera rendering will be skipped.", camera.name));
                return;
            }

#if UNITY_EDITOR
            string tag = camera.name;
#else
            string tag = k_RenderCameraTag;
#endif
            CommandBuffer cmd = CommandBufferPool.Get(tag);
            using (new ProfilingSample(cmd, tag))
            {
                renderer.Clear();
                renderer.SetupCullingParameters(ref cullingParameters, ref cameraData);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

#if UNITY_EDITOR

                // Emit scene view UI
                if (cameraData.isSceneViewCamera)
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif

                var cullResults = context.Cull(ref cullingParameters);
                InitializeRenderingData(settings, ref cameraData, ref cullResults, out var renderingData);

                renderer.Setup(context, ref renderingData);
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
                mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive | SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly,
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
            cameraData.isStereoEnabled = IsStereoEnabled(camera);

            int msaaSamples = 1;
            if (camera.allowMSAA && settings.msaaSampleCount > 1)
                msaaSamples = (camera.targetTexture != null) ? camera.targetTexture.antiAliasing : settings.msaaSampleCount;
            
            cameraData.isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            cameraData.isHdrEnabled = camera.allowHDR && settings.supportsHDR;
#if UNITY_2019_2_OR_NEWER
            camera.TryGetComponent(out cameraData.postProcessLayer);
#else
            cameraData.postProcessLayer = camera.GetComponent<PostProcessLayer>();
#endif
            cameraData.postProcessEnabled = cameraData.postProcessLayer != null && cameraData.postProcessLayer.isActiveAndEnabled;

            // On Android, Postprocessing v2 works with single-pass double-wide mode and is disabled for multiview
            var xrDesc = UnityEngine.XR.XRSettings.eyeTextureDesc;
            if (cameraData.isStereoEnabled && Application.isMobilePlatform && Application.platform == RuntimePlatform.Android && xrDesc.dimension == TextureDimension.Tex2DArray)
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

            bool anyShadowsEnabled = settings.supportsMainLightShadows || settings.supportsAdditionalLightShadows;
            cameraData.maxShadowDistance = Mathf.Min(settings.shadowDistance, camera.farClipPlane);
            cameraData.maxShadowDistance = (anyShadowsEnabled && cameraData.maxShadowDistance >= camera.nearClipPlane) ?
                cameraData.maxShadowDistance : 0.0f;

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

            cameraData.cameraTargetDescriptor = CreateRenderTextureDescriptor(camera, cameraData.renderScale,
                cameraData.isStereoEnabled, cameraData.isHdrEnabled, msaaSamples);
        }

        static void InitializeRenderingData(LightweightRenderPipelineAsset settings, ref CameraData cameraData, ref CullingResults cullResults,
            out RenderingData renderingData)
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
            InitializeLightData(settings, visibleLights, mainLightIndex, out renderingData.lightData);
            InitializeShadowData(settings, visibleLights, mainLightCastShadows, additionalLightsCastShadows && !renderingData.lightData.shadeAdditionalLightsPerVertex, out renderingData.shadowData);
            renderingData.supportsDynamicBatching = settings.supportsDynamicBatching;
            renderingData.perObjectData = GetPerObjectLightFlags(renderingData.lightData.additionalLightsCount);

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
                LWRPAdditionalLightData data = null;
                if (light != null)
                {
#if UNITY_2019_2_OR_NEWER
                    light.gameObject.TryGetComponent(out data);
#else
                    data = light.gameObject.GetComponent<LWRPAdditionalLightData>();
#endif
                }

                if (data && !data.usePipelineSettings)
                    m_ShadowBiasData.Add(new Vector4(light.shadowBias, light.shadowNormalBias, 0.0f, 0.0f));
                else
                    m_ShadowBiasData.Add(new Vector4(settings.shadowDepthBias, settings.shadowNormalBias, 0.0f, 0.0f));
            }

            shadowData.bias = m_ShadowBiasData;

            // Until we can have keyword stripping forcing single cascade hard shadows on gles2
            bool supportsScreenSpaceShadows = SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            shadowData.supportsMainLightShadows = SystemInfo.supportsShadows && settings.supportsMainLightShadows && mainLightCastShadows;

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

            shadowData.supportsAdditionalLightShadows = SystemInfo.supportsShadows && settings.supportsAdditionalLightShadows && additionalLightsCastShadows;
            shadowData.additionalLightsShadowmapWidth = shadowData.additionalLightsShadowmapHeight = settings.additionalLightsShadowmapResolution;
            shadowData.supportsSoftShadows = settings.supportsSoftShadows && (shadowData.supportsMainLightShadows || shadowData.supportsAdditionalLightShadows);
            shadowData.shadowmapDepthBufferBits = 16;
        }

        static void InitializeLightData(LightweightRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights, int mainLightIndex, out LightData lightData)
        {
            int maxPerObjectAdditionalLights = LightweightRenderPipeline.maxPerObjectLights;
            int maxVisibleAdditionalLights = LightweightRenderPipeline.maxVisibleAdditionalLights;

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

            lightData.shadeAdditionalLightsPerVertex = settings.additionalLightsRenderingMode == LightRenderingMode.PerVertex;
            lightData.visibleLights = visibleLights;
            lightData.supportsMixedLighting = settings.supportsMixedLighting;
        }

        static PerObjectData GetPerObjectLightFlags(int additionalLightsCount)
        {
            var configuration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightData | PerObjectData.OcclusionProbe;

            if (additionalLightsCount > 0)
                configuration |= PerObjectData.LightIndices;

            return configuration;
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

            float scaledCameraWidth = (float)cameraData.camera.pixelWidth * cameraData.renderScale;
            float scaledCameraHeight = (float)cameraData.camera.pixelHeight * cameraData.renderScale;
            Shader.SetGlobalVector(PerCameraBuffer._ScaledScreenParams, new Vector4(scaledCameraWidth, scaledCameraHeight, 1.0f + 1.0f / scaledCameraWidth, 1.0f + 1.0f / scaledCameraHeight));
            Shader.SetGlobalVector(PerCameraBuffer._WorldSpaceCameraPos, camera.transform.position);
            float cameraWidth = (float)cameraData.camera.pixelWidth;
            float cameraHeight = (float)cameraData.camera.pixelHeight;
            Shader.SetGlobalVector(PerCameraBuffer._ScreenParams, new Vector4(cameraWidth, cameraHeight, 1.0f + 1.0f / cameraWidth, 1.0f + 1.0f / cameraHeight));

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
