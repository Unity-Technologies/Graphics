using System;
using System.Collections.Generic;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.Rendering.LightweightPipeline;
#endif
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public sealed partial class LightweightPipeline : RenderPipeline
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

        private static IRendererSetup m_DefaultRendererSetup;
        private static IRendererSetup defaultRendererSetup
        {
            get
            {
                if (m_DefaultRendererSetup == null)
                    m_DefaultRendererSetup = new DefaultRendererSetup();

                return m_DefaultRendererSetup;
            }
        }

        const string k_RenderCameraTag = "Render Camera";
        CullResults m_CullResults;

        public ScriptableRenderer renderer { get; private set; }
        public PipelineSettings settings { get; private set; }

        public struct PipelineSettings
        {
            public int msaaSampleCount { get; private set; }
            public bool supportsHDR { get; private set; }
            public XRGraphicsConfig savedXRGraphicsConfig { get; private set; }
            public float renderScale { get; private set; }
            public bool supportsCameraDepthTexture { get; private set; }
            public Downsampling opaqueDownsampling { get; private set; }
            public bool supportsCameraOpaqueTexture { get; private set; }
            public bool supportsDynamicBatching { get; private set; }
            public int maxPixelLights { get; private set; }
            public bool supportsDirectionalShadows { get; private set; }
            public bool supportsSoftParticles { get; private set; }
            public bool supportsLocalShadows { get; private set; }
            public float shadowDistance { get; private set; }
            public int cascadeCount { get; private set; }
            public int directionalShadowAtlasResolution { get; private set; }
            public float cascade2Split { get; private set; }
            public Vector3 cascade4Split { get; private set; }
            public bool supportsVertexLight { get; private set; }
            public int localShadowAtlasResolution { get; private set; }
            public bool supportsSoftShadows { get; private set; }

            public static PipelineSettings Create(LightweightPipelineAsset asset)
            {
                var cache = new PipelineSettings();
                cache.msaaSampleCount = asset.msaaSampleCount;
                cache.supportsHDR = asset.supportsHDR;
                cache.savedXRGraphicsConfig = asset.savedXRGraphicsConfig;
                cache.renderScale = asset.renderScale;
                cache.supportsCameraDepthTexture = asset.supportsCameraDepthTexture;
                cache.opaqueDownsampling = asset.opaqueDownsampling;
                cache.supportsCameraOpaqueTexture = asset.supportsCameraOpaqueTexture;
                cache.supportsDynamicBatching = asset.supportsDynamicBatching;
                cache.maxPixelLights = asset.maxPixelLights;
                cache.supportsDirectionalShadows = asset.supportsDirectionalShadows;
                cache.supportsSoftParticles = asset.supportsSoftParticles;
                cache.supportsLocalShadows = asset.supportsLocalShadows;
                cache.shadowDistance = asset.shadowDistance;
                cache.cascadeCount = asset.cascadeCount;
                cache.directionalShadowAtlasResolution = asset.directionalShadowAtlasResolution;
                cache.cascade2Split = asset.cascade2Split;
                cache.cascade4Split = asset.cascade4Split;
                cache.supportsVertexLight = asset.supportsVertexLight;
                cache.localShadowAtlasResolution = asset.localShadowAtlasResolution;
                cache.supportsSoftShadows = asset.supportsSoftShadows;

                cache.savedXRGraphicsConfig.renderScale = cache.renderScale;
                cache.savedXRGraphicsConfig.viewportScale = 1.0f; // Placeholder until viewportScale is all hooked up
                // Apply any changes to XRGConfig prior to this point
                cache.savedXRGraphicsConfig.SetConfig();
                return cache;
            }
        }

        public LightweightPipeline(LightweightPipelineAsset asset)
        {
            settings = PipelineSettings.Create(asset);
            renderer = new ScriptableRenderer(asset);

            SetSupportedRenderingFeatures();
            SetSupportedShaderFeatures(asset);

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

        public override void Dispose()
        {
            base.Dispose();
            Shader.globalRenderPipeline = "";
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
#endif

            renderer.Dispose();

            Lightmapping.ResetDelegate();
        }

        public interface IBeforeCameraRender
        {
            void ExecuteBeforeCameraRender(LightweightPipeline pipelineInstance, ScriptableRenderContext context, Camera camera);
        }

        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            base.Render(context, cameras);
            BeginFrameRendering(cameras);

            GraphicsSettings.lightsUseLinearIntensity = true;
            SetupPerFrameShaderConstants();

            SortCameras(cameras);
            foreach (Camera camera in cameras)
            {
                BeginCameraRendering(camera);

                foreach (var beforeCamera in camera.GetComponents<IBeforeCameraRender>())
                    beforeCamera.ExecuteBeforeCameraRender(this, context, camera);

                RenderSingleCamera(this, context, camera, ref m_CullResults, camera.GetComponent<IRendererSetup>());
            }
        }

        public static void RenderSingleCamera(LightweightPipeline pipelineInstance, ScriptableRenderContext context, Camera camera, ref CullResults cullResults, IRendererSetup setup = null)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_RenderCameraTag);
            using (new ProfilingSample(cmd, k_RenderCameraTag))
            {
                CameraData cameraData;
                PipelineSettings settings = pipelineInstance.settings;
                ScriptableRenderer renderer = pipelineInstance.renderer;
                InitializeCameraData(settings, camera, out cameraData);
                SetupPerCameraShaderConstants(cameraData);

                ScriptableCullingParameters cullingParameters;
                if (!CullResults.GetCullingParameters(camera, cameraData.isStereoEnabled, out cullingParameters))
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
                CullResults.Cull(ref cullingParameters, context, ref cullResults);

                RenderingData renderingData;
                InitializeRenderingData(settings, ref cameraData, ref cullResults,
                    renderer.maxSupportedLocalLightsPerPass, renderer.maxSupportedVertexLights, out renderingData);

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
                reflectionProbeSupportFlags = SupportedRenderingFeatures.ReflectionProbeSupportFlags.None,
                defaultMixedLightingMode = SupportedRenderingFeatures.LightmapMixedBakeMode.Subtractive,
                supportedMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeMode.Subtractive,
                supportedLightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed,
                supportedLightmapsModes = LightmapsMode.CombinedDirectional | LightmapsMode.NonDirectional,
                rendererSupportsLightProbeProxyVolumes = false,
                rendererSupportsMotionVectors = false,
                rendererSupportsReceiveShadows = false,
                rendererSupportsReflectionProbes = true
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
            cameraData.isOffscreenRender = camera.targetTexture != null && !cameraData.isSceneViewCamera;
            cameraData.isStereoEnabled = IsStereoEnabled(camera);

            // TODO: There's currently an issue in engine side that breaks MSAA with texture2DArray.
            // for now we force msaa disabled when using texture2DArray. This fixes VR multiple and single pass instanced modes.
            if (cameraData.isStereoEnabled && XRGraphicsConfig.eyeTextureDesc.dimension == TextureDimension.Tex2DArray)
                cameraData.msaaSamples = 1;

            cameraData.isHdrEnabled = camera.allowHDR && settings.supportsHDR;

            cameraData.postProcessLayer = camera.GetComponent<PostProcessLayer>();
            cameraData.postProcessEnabled = cameraData.postProcessLayer != null && cameraData.postProcessLayer.isActiveAndEnabled;

            Rect cameraRect = camera.rect;
            cameraData.isDefaultViewport = (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f ||
                Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f));

            // If XR is enabled, use XR renderScale.
            // Discard variations lesser than kRenderScaleThreshold.
            // Scale is only enabled for gameview.
            float usedRenderScale = XRGraphicsConfig.enabled ? settings.savedXRGraphicsConfig.renderScale : settings.renderScale;
            cameraData.renderScale = (Mathf.Abs(1.0f - usedRenderScale) < kRenderScaleThreshold) ? 1.0f : usedRenderScale;
            cameraData.renderScale = (camera.cameraType == CameraType.Game) ? cameraData.renderScale : 1.0f;

            cameraData.requiresDepthTexture = settings.supportsCameraDepthTexture || cameraData.isSceneViewCamera;
            cameraData.requiresSoftParticles = settings.supportsSoftParticles;
            cameraData.requiresOpaqueTexture = settings.supportsCameraOpaqueTexture;
            cameraData.opaqueTextureDownsampling = settings.opaqueDownsampling;

            bool anyShadowsEnabled = settings.supportsDirectionalShadows || settings.supportsLocalShadows;
            cameraData.maxShadowDistance = (anyShadowsEnabled) ? settings.shadowDistance : 0.0f;

            LightweightAdditionalCameraData additionalCameraData = camera.gameObject.GetComponent<LightweightAdditionalCameraData>();
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

            var commonOpaqueFlags = SortFlags.CommonOpaque;
            var noFrontToBackOpaqueFlags = SortFlags.SortingLayer | SortFlags.RenderQueue | SortFlags.OptimizeStateChanges | SortFlags.CanvasOrder;
            bool hasHSRGPU = SystemInfo.hasHiddenSurfaceRemovalOnGPU;
            bool canSkipFrontToBackSorting = (camera.opaqueSortMode == OpaqueSortMode.Default && hasHSRGPU) || camera.opaqueSortMode == OpaqueSortMode.NoDistanceSort;

            cameraData.defaultOpaqueSortFlags = canSkipFrontToBackSorting ? noFrontToBackOpaqueFlags : commonOpaqueFlags;
        }

        static void InitializeRenderingData(PipelineSettings settings, ref CameraData cameraData, ref CullResults cullResults,
            int maxSupportedLocalLightsPerPass, int maxSupportedVertexLights,
            out RenderingData renderingData)
        {
            List<VisibleLight> visibleLights = cullResults.visibleLights;
            List<int> localLightIndices = new List<int>();

            bool hasDirectionalShadowCastingLight = false;
            bool hasLocalShadowCastingLight = false;

            if (cameraData.maxShadowDistance > 0.0f)
            {
                for (int i = 0; i < visibleLights.Count; ++i)
                {
                    Light light = visibleLights[i].light;
                    bool castShadows = light != null && light.shadows != LightShadows.None;
                    if (visibleLights[i].lightType == LightType.Directional)
                    {
                        hasDirectionalShadowCastingLight |= castShadows;
                    }
                    else
                    {
                        hasLocalShadowCastingLight |= castShadows;
                        localLightIndices.Add(i);
                    }
                }
            }

            renderingData.cullResults = cullResults;
            renderingData.cameraData = cameraData;
            InitializeLightData(settings, visibleLights, maxSupportedLocalLightsPerPass, maxSupportedVertexLights, localLightIndices, out renderingData.lightData);
            InitializeShadowData(settings, hasDirectionalShadowCastingLight, hasLocalShadowCastingLight, out renderingData.shadowData);
            renderingData.supportsDynamicBatching = settings.supportsDynamicBatching;
        }

        static void InitializeShadowData(PipelineSettings settings, bool hasDirectionalShadowCastingLight, bool hasLocalShadowCastingLight, out ShadowData shadowData)
        {
            // Until we can have keyword stripping forcing single cascade hard shadows on gles2
            bool supportsScreenSpaceShadows = SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            shadowData.renderDirectionalShadows = settings.supportsDirectionalShadows && hasDirectionalShadowCastingLight;

            // we resolve shadows in screenspace when cascades are enabled to save ALU as computing cascade index + shadowCoord on fragment is expensive
            shadowData.requiresScreenSpaceShadowResolve = shadowData.renderDirectionalShadows && supportsScreenSpaceShadows && settings.cascadeCount > 1;
            shadowData.directionalLightCascadeCount = (shadowData.requiresScreenSpaceShadowResolve) ? settings.cascadeCount : 1;
            shadowData.directionalShadowAtlasWidth = settings.directionalShadowAtlasResolution;
            shadowData.directionalShadowAtlasHeight = settings.directionalShadowAtlasResolution;

            switch (shadowData.directionalLightCascadeCount)
            {
                case 1:
                    shadowData.directionalLightCascades = new Vector3(1.0f, 0.0f, 0.0f);
                    break;

                case 2:
                    shadowData.directionalLightCascades = new Vector3(settings.cascade2Split, 1.0f, 0.0f);
                    break;

                default:
                    shadowData.directionalLightCascades = settings.cascade4Split;
                    break;
            }

            shadowData.renderLocalShadows = settings.supportsLocalShadows && hasLocalShadowCastingLight;
            shadowData.localShadowAtlasWidth = shadowData.localShadowAtlasHeight = settings.localShadowAtlasResolution;
            shadowData.supportsSoftShadows = settings.supportsSoftShadows;
            shadowData.bufferBitCount = 16;
        }

        static void InitializeLightData(PipelineSettings settings, List<VisibleLight> visibleLights,
            int maxSupportedLocalLightsPerPass, int maxSupportedVertexLights, List<int> localLightIndices, out LightData lightData)
        {
            int visibleLightsCount = Math.Min(visibleLights.Count, settings.maxPixelLights);
            lightData.mainLightIndex = GetMainLight(settings, visibleLights);

            // If we have a main light we don't shade it in the per-object light loop. We also remove it from the per-object cull list
            int mainLightPresent = (lightData.mainLightIndex >= 0) ? 1 : 0;
            int additionalPixelLightsCount = Math.Min(visibleLightsCount - mainLightPresent, maxSupportedLocalLightsPerPass);
            int vertexLightCount = (settings.supportsVertexLight) ? Math.Min(visibleLights.Count, maxSupportedLocalLightsPerPass) - additionalPixelLightsCount : 0;
            vertexLightCount = Math.Min(vertexLightCount, maxSupportedVertexLights);

            lightData.pixelAdditionalLightsCount = additionalPixelLightsCount;
            lightData.totalAdditionalLightsCount = additionalPixelLightsCount + vertexLightCount;
            lightData.visibleLights = visibleLights;
            lightData.visibleLocalLightIndices = localLightIndices;
        }

        // Main Light is always a directional light
        static int GetMainLight(PipelineSettings settings, List<VisibleLight> visibleLights)
        {
            int totalVisibleLights = visibleLights.Count;

            if (totalVisibleLights == 0 || settings.maxPixelLights == 0)
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
