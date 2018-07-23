using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.Experimental.Rendering.LightweightPipeline;
#endif
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public partial class LightweightPipeline : RenderPipeline
    {
        private static class PerFrameBuffer
        {
            public static int _GlossyEnvironmentColor;
            public static int _SubtractiveShadowColor;
        }

        public LightweightPipelineAsset pipelineAsset { get; private set; }

        private IRendererSetup defaultRendererSetup
        {
            get
            {
                if (m_DefaultRendererSetup == null)
                    m_DefaultRendererSetup = new DefaultRendererSetup();

                return m_DefaultRendererSetup;
            }
        }

        CameraComparer m_CameraComparer = new CameraComparer();

        LightweightForwardRenderer m_Renderer;
        CullResults m_CullResults;
        List<int> m_LocalLightIndices = new List<int>();

        bool m_IsCameraRendering;
        private IRendererSetup m_DefaultRendererSetup;

        public LightweightPipeline(LightweightPipelineAsset asset)
        {
            pipelineAsset = asset;

            SetSupportedRenderingFeatures();
            SetPipelineCapabilities(asset);

            PerFrameBuffer._GlossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
            PerFrameBuffer._SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

            SetupLightweightConstanstPass.PerCameraBuffer._ScaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
            m_Renderer = new LightweightForwardRenderer(asset);

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (QualitySettings.antiAliasing != pipelineAsset.msaaSampleCount)
                QualitySettings.antiAliasing = pipelineAsset.msaaSampleCount;

            Shader.globalRenderPipeline = "LightweightPipeline";
            m_IsCameraRendering = false;
        }

        public override void Dispose()
        {
            base.Dispose();
            Shader.globalRenderPipeline = "";
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
#endif

            m_Renderer.Dispose();
        }

        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (m_IsCameraRendering)
            {
                Debug.LogWarning("Nested camera rendering is forbidden. If you are calling camera.Render inside OnWillRenderObject callback, use BeginCameraRender callback instead.");
                return;
            }
            pipelineAsset.savedXRGraphicsConfig.renderScale = pipelineAsset.renderScale;
            pipelineAsset.savedXRGraphicsConfig.viewportScale = 1.0f; // Placeholder until viewportScale is all hooked up
            // Apply any changes to XRGConfig prior to this point
            pipelineAsset.savedXRGraphicsConfig.SetConfig();

            base.Render(context, cameras);
            BeginFrameRendering(cameras);

            GraphicsSettings.lightsUseLinearIntensity = true;
            SetupPerFrameShaderConstants();

            // Sort cameras array by camera depth
            Array.Sort(cameras, m_CameraComparer);

            foreach (Camera camera in cameras)
            {
                BeginCameraRendering(camera);
                string renderCameraTag = "Render " + camera.name;
                CommandBuffer cmd = CommandBufferPool.Get(renderCameraTag);
                using (new ProfilingSample(cmd, renderCameraTag))
                {
                    CameraData cameraData;
                    InitializeCameraData(camera, out cameraData);
                    SetupPerCameraShaderConstants(cameraData);

                    ScriptableCullingParameters cullingParameters;
                    if (!CullResults.GetCullingParameters(camera, cameraData.isStereoEnabled, out cullingParameters))
                    {
                        CommandBufferPool.Release(cmd);
                        continue;
                    }

                    cullingParameters.shadowDistance = Mathf.Min(cameraData.maxShadowDistance, camera.farClipPlane);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

#if UNITY_EDITOR
                    try
#endif
                    {
                        m_IsCameraRendering = true;
#if UNITY_EDITOR
                        // Emit scene view UI
                        if (cameraData.isSceneViewCamera)
                            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif
                        CullResults.Cull(ref cullingParameters, context, ref m_CullResults);
                        List<VisibleLight> visibleLights = m_CullResults.visibleLights;

                        RenderingData renderingData;
                        InitializeRenderingData(ref cameraData, visibleLights,
                            m_Renderer.maxSupportedLocalLightsPerPass, m_Renderer.maxSupportedVertexLights,
                            out renderingData);

                        var setup = cameraData.camera.GetComponent<IRendererSetup>();
                        if (setup == null)
                            setup = defaultRendererSetup;

                        setup.Setup(m_Renderer, ref context, ref m_CullResults, ref renderingData);
                        m_Renderer.Execute(ref context, ref m_CullResults, ref renderingData);
                    }
#if UNITY_EDITOR
                    catch (Exception)
                    {
                        CommandBufferPool.Release(cmd);
                        throw;
                    }
                    finally
#endif
                    {
                        m_IsCameraRendering = false;
                    }
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                context.Submit();
            }
        }

        void SetSupportedRenderingFeatures()
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
                rendererSupportsReceiveShadows = true,
                rendererSupportsReflectionProbes = true
            };
            SceneViewDrawMode.SetupDrawMode();
#endif
        }

        void InitializeCameraData(Camera camera, out CameraData cameraData)
        {
            const float kRenderScaleThreshold = 0.05f;
            cameraData.camera = camera;

            bool msaaEnabled = camera.allowMSAA && pipelineAsset.msaaSampleCount > 1;
            if (msaaEnabled)
                cameraData.msaaSamples = (camera.targetTexture != null) ? camera.targetTexture.antiAliasing : pipelineAsset.msaaSampleCount;
            else
                cameraData.msaaSamples = 1;

            cameraData.isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            cameraData.isOffscreenRender = camera.targetTexture != null && !cameraData.isSceneViewCamera;
            cameraData.isStereoEnabled = IsStereoEnabled(camera);

            // TODO: There's currently an issue in engine side that breaks MSAA with texture2DArray.
            // for now we force msaa disabled when using texture2DArray. This fixes VR multiple and single pass instanced modes.
            if (cameraData.isStereoEnabled && XRGraphicsConfig.eyeTextureDesc.dimension == TextureDimension.Tex2DArray)
                cameraData.msaaSamples = 1;

            cameraData.isHdrEnabled = camera.allowHDR && pipelineAsset.supportsHDR;

            cameraData.postProcessLayer = camera.GetComponent<PostProcessLayer>();
            cameraData.postProcessEnabled = cameraData.postProcessLayer != null && cameraData.postProcessLayer.isActiveAndEnabled;

            Rect cameraRect = camera.rect;
            cameraData.isDefaultViewport = (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f ||
                                              Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f));

            // If XR is enabled, use XR renderScale. 
            // Discard variations lesser than kRenderScaleThreshold.
            // Scale is only enabled for gameview.
            float usedRenderScale = XRGraphicsConfig.enabled ? pipelineAsset.savedXRGraphicsConfig.renderScale : pipelineAsset.renderScale;
            cameraData.renderScale = (Mathf.Abs(1.0f - usedRenderScale) < kRenderScaleThreshold) ? 1.0f : usedRenderScale;
            cameraData.renderScale = (camera.cameraType == CameraType.Game) ? cameraData.renderScale : 1.0f;

            cameraData.requiresDepthTexture = pipelineAsset.supportsCameraDepthTexture || cameraData.isSceneViewCamera;
            cameraData.requiresSoftParticles = pipelineAsset.supportsSoftParticles;
            cameraData.requiresOpaqueTexture = pipelineAsset.supportsCameraOpaqueTexture;
            cameraData.opaqueTextureDownsampling = pipelineAsset.opaqueDownsampling;

            bool anyShadowsEnabled = pipelineAsset.supportsDirectionalShadows || pipelineAsset.supportsLocalShadows;
            cameraData.maxShadowDistance = (anyShadowsEnabled) ? pipelineAsset.shadowDistance : 0.0f;

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
        }

        void InitializeRenderingData(ref CameraData cameraData, List<VisibleLight> visibleLights, int maxSupportedLocalLightsPerPass, int maxSupportedVertexLights, out RenderingData renderingData)
        {
            m_LocalLightIndices.Clear();

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
                        m_LocalLightIndices.Add(i);
                    }
                }
            }

            renderingData.cameraData = cameraData;
            InitializeLightData(visibleLights, maxSupportedLocalLightsPerPass, maxSupportedVertexLights, out renderingData.lightData);
            InitializeShadowData(hasDirectionalShadowCastingLight, hasLocalShadowCastingLight, out renderingData.shadowData);
            renderingData.supportsDynamicBatching = pipelineAsset.supportsDynamicBatching;
        }

        void InitializeShadowData(bool hasDirectionalShadowCastingLight, bool hasLocalShadowCastingLight, out ShadowData shadowData)
        {
            // Until we can have keyword stripping forcing single cascade hard shadows on gles2
            bool supportsScreenSpaceShadows = SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            shadowData.renderDirectionalShadows = pipelineAsset.supportsDirectionalShadows && hasDirectionalShadowCastingLight;

            // we resolve shadows in screenspace when cascades are enabled to save ALU as computing cascade index + shadowCoord on fragment is expensive
            shadowData.requiresScreenSpaceShadowResolve = shadowData.renderDirectionalShadows && supportsScreenSpaceShadows && pipelineAsset.cascadeCount > 1;
            shadowData.directionalLightCascadeCount = (shadowData.requiresScreenSpaceShadowResolve) ? pipelineAsset.cascadeCount : 1;
            shadowData.directionalShadowAtlasWidth = pipelineAsset.directionalShadowAtlasResolution;
            shadowData.directionalShadowAtlasHeight = pipelineAsset.directionalShadowAtlasResolution;

            switch (shadowData.directionalLightCascadeCount)
            {
                case 1:
                    shadowData.directionalLightCascades = new Vector3(1.0f, 0.0f, 0.0f);
                    break;

                case 2:
                    shadowData.directionalLightCascades = new Vector3(pipelineAsset.cascade2Split, 1.0f, 0.0f);
                    break;

                default:
                    shadowData.directionalLightCascades = pipelineAsset.cascade4Split;
                    break;
            }

            shadowData.renderLocalShadows = pipelineAsset.supportsLocalShadows && hasLocalShadowCastingLight;
            shadowData.localShadowAtlasWidth = shadowData.localShadowAtlasHeight = pipelineAsset.localShadowAtlasResolution;
            shadowData.supportsSoftShadows = pipelineAsset.supportsSoftShadows;
            shadowData.bufferBitCount = 16;

            shadowData.renderedDirectionalShadowQuality = LightShadows.None;
            shadowData.renderedLocalShadowQuality = LightShadows.None;
        }

        void InitializeLightData(List<VisibleLight> visibleLights, int maxSupportedLocalLightsPerPass, int maxSupportedVertexLights, out LightData lightData)
        {
            int visibleLightsCount = Math.Min(visibleLights.Count, pipelineAsset.maxPixelLights);
            lightData.mainLightIndex = GetMainLight(visibleLights);

            // If we have a main light we don't shade it in the per-object light loop. We also remove it from the per-object cull list
            int mainLightPresent = (lightData.mainLightIndex >= 0) ? 1 : 0;
            int additionalPixelLightsCount = Math.Min(visibleLightsCount - mainLightPresent, maxSupportedLocalLightsPerPass);
            int vertexLightCount = (pipelineAsset.supportsVertexLight) ? Math.Min(visibleLights.Count, maxSupportedLocalLightsPerPass) - additionalPixelLightsCount : 0;
            vertexLightCount = Math.Min(vertexLightCount, maxSupportedVertexLights);

            lightData.pixelAdditionalLightsCount = additionalPixelLightsCount;
            lightData.totalAdditionalLightsCount = additionalPixelLightsCount + vertexLightCount;
            lightData.visibleLights = visibleLights;
            lightData.visibleLocalLightIndices = m_LocalLightIndices;
        }

        // Main Light is always a directional light
        int GetMainLight(List<VisibleLight> visibleLights)
        {
            int totalVisibleLights = visibleLights.Count;

            if (totalVisibleLights == 0 || pipelineAsset.maxPixelLights == 0)
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

        void SetupPerFrameShaderConstants()
        {
            // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
            SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
            Color linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
            Color glossyEnvColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
            Shader.SetGlobalVector(PerFrameBuffer._GlossyEnvironmentColor, glossyEnvColor);

            // Used when subtractive mode is selected
            Shader.SetGlobalVector(PerFrameBuffer._SubtractiveShadowColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor));
        }

        void SetupPerCameraShaderConstants(CameraData cameraData)
        {
            float cameraWidth = (float)cameraData.camera.pixelWidth * cameraData.renderScale;
            float cameraHeight = (float)cameraData.camera.pixelHeight * cameraData.renderScale;
            Shader.SetGlobalVector(SetupLightweightConstanstPass.PerCameraBuffer._ScaledScreenParams, new Vector4(cameraWidth, cameraHeight, 1.0f + 1.0f / cameraWidth, 1.0f + 1.0f / cameraHeight));
        }
    }
}
