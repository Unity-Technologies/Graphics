using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.Experimental.Rendering.LightweightPipeline;
#endif
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public partial class LightweightPipeline : RenderPipeline
    {
        public LightweightPipelineAsset pipelineAsset { get; private set; }

        CameraComparer m_CameraComparer = new CameraComparer();

        LightweightForwardRenderer m_Renderer;
        CullResults m_CullResults;
        List<int> m_LocalLightIndices = new List<int>();

        bool m_IsCameraRendering;

        public LightweightPipeline(LightweightPipelineAsset asset)
        {
            pipelineAsset = asset;

            SetSupportedRenderingFeatures();
            SetPipelineCapabilities(asset);

            PerFrameBuffer._GlossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
            PerFrameBuffer._SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

            PerCameraBuffer._ScaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
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
                    m_IsCameraRendering = true;

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
                    // Emit scene view UI
                    if (cameraData.isSceneViewCamera)
                        ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
#endif
                    CullResults.Cull(ref cullingParameters, context, ref m_CullResults);
                    List<VisibleLight> visibleLights = m_CullResults.visibleLights;

                    RenderingData renderingData;
                    InitializeRenderingData(ref cameraData, visibleLights, m_Renderer.maxSupportedLocalLightsPerPass, m_Renderer.maxSupportedVertexLights, out renderingData);
                    m_Renderer.Setup(ref context, ref m_CullResults, ref renderingData);
                    m_Renderer.Execute(ref context, ref m_CullResults, ref renderingData);

                    m_IsCameraRendering = false;
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                context.Submit();
            }
        }

        public static void RenderPostProcess(CommandBuffer cmd, PostProcessRenderContext context, ref CameraData cameraData, RenderTextureFormat colorFormat, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool opaqueOnly)
        {
            context.Reset();
            context.camera = cameraData.camera;
            context.source = source;
            context.sourceFormat = colorFormat;
            context.destination = dest;
            context.command = cmd;
            context.flip = cameraData.camera.targetTexture == null;

            if (opaqueOnly)
                cameraData.postProcessLayer.RenderOpaqueOnly(context);
            else
                cameraData.postProcessLayer.Render(context);
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
            cameraData.isHdrEnabled = camera.allowHDR && pipelineAsset.supportsHDR;

            cameraData.postProcessLayer = camera.GetComponent<PostProcessLayer>();
            cameraData.postProcessEnabled = cameraData.postProcessLayer != null && cameraData.postProcessLayer.isActiveAndEnabled;

            // PostProcess for VR is not working atm. Disable it for now.
            cameraData.postProcessEnabled &= !cameraData.isStereoEnabled;

            Rect cameraRect = camera.rect;
            cameraData.isDefaultViewport = (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f ||
                                              Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f));

            // Discard variations lesser than kRenderScaleThreshold.
            // Scale is only enabled for gameview
            // XR has it's own scaling mechanism.
            cameraData.renderScale = (Mathf.Abs(1.0f - pipelineAsset.renderScale) < kRenderScaleThreshold) ? 1.0f : pipelineAsset.renderScale;
            cameraData.renderScale = (camera.cameraType == CameraType.Game && !cameraData.isStereoEnabled) ? cameraData.renderScale : 1.0f;

            cameraData.requiresDepthTexture = pipelineAsset.supportsCameraDepthTexture || cameraData.postProcessEnabled || cameraData.isSceneViewCamera;
            cameraData.requiresSoftParticles = pipelineAsset.supportsSoftParticles;
            cameraData.requiresOpaqueTexture = pipelineAsset.supportsCameraOpaqueTexture;
            cameraData.opaqueTextureDownsampling = pipelineAsset.opaqueDownsampling;

            bool anyShadowsEnabled = pipelineAsset.supportsDirectionalShadows || pipelineAsset.supportsLocalShadows;
            cameraData.maxShadowDistance = (anyShadowsEnabled) ? pipelineAsset.shadowDistance : 0.0f;
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
                    bool castShadows = visibleLights[i].light.shadows != LightShadows.None;
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
            shadowData.requiresScreenSpaceShadowResolve = shadowData.renderDirectionalShadows && supportsScreenSpaceShadows;
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
            float cameraHeight = (float)cameraData.camera.pixelWidth * cameraData.renderScale;
            Shader.SetGlobalVector(PerCameraBuffer._ScaledScreenParams, new Vector4(cameraWidth, cameraHeight, 1.0f + 1.0f / cameraWidth, 1.0f + 1.0f / cameraHeight));
        }

        bool IsStereoEnabled(Camera camera)
        {
#if !UNITY_SWITCH
            bool isSceneViewCamera = camera.cameraType == CameraType.SceneView;
            return XRSettings.isDeviceActive && !isSceneViewCamera && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
#else
            return false;
#endif
        }
    }
}
