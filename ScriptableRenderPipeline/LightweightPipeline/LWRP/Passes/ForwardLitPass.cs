using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class ForwardLitPass : ScriptableRenderPass
    {
        const int k_DepthStencilBufferBits = 32;
        Vector4 k_DefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 k_DefaultLightColor = Color.black;
        Vector4 k_DefaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        Vector4 k_DefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 k_DefaultLightSpotAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);

        Vector4[] m_LightPositions;
        Vector4[] m_LightColors;
        Vector4[] m_LightDistanceAttenuations;
        Vector4[] m_LightSpotDirections;
        Vector4[] m_LightSpotAttenuations;

        RenderTextureFormat m_ColorFormat;
        MixedLightingSetup m_MixedLightingSetup;
        Material m_BlitMaterial;
        Material m_ErrorMaterial;

        // Depth Copy Pass
        Material m_DepthCopyMaterial;
        const string k_MSAADepthKeyword = "_MSAA_DEPTH";
        int m_SampleCountShaderHandle;

        // Opaque Copy Pass
        Material m_SamplingMaterial;
        float[] m_OpaqueScalerValues = {1.0f, 0.5f, 0.25f, 0.25f};
        int m_SampleOffsetShaderHandle;

        List<ShaderPassName> m_LegacyShaderPassNames;

        public ForwardLitPass(LightweightForwardRenderer renderer) : base(renderer)
        {
            RegisterShaderPassName("LightweightForward");
            RegisterShaderPassName("SRPDefaultUnlit");

            m_LegacyShaderPassNames = new List<ShaderPassName>();
            m_LegacyShaderPassNames.Add(new ShaderPassName("Always"));
            m_LegacyShaderPassNames.Add(new ShaderPassName("ForwardBase"));
            m_LegacyShaderPassNames.Add(new ShaderPassName("PrepassBase"));
            m_LegacyShaderPassNames.Add(new ShaderPassName("Vertex"));
            m_LegacyShaderPassNames.Add(new ShaderPassName("VertexLMRGBM"));
            m_LegacyShaderPassNames.Add(new ShaderPassName("VertexLM"));

            PerCameraBuffer._MainLightPosition = Shader.PropertyToID("_MainLightPosition");
            PerCameraBuffer._MainLightColor = Shader.PropertyToID("_MainLightColor");
            PerCameraBuffer._MainLightCookie = Shader.PropertyToID("_MainLightCookie");
            PerCameraBuffer._WorldToLight = Shader.PropertyToID("_WorldToLight");
            PerCameraBuffer._AdditionalLightCount = Shader.PropertyToID("_AdditionalLightCount");
            PerCameraBuffer._AdditionalLightPosition = Shader.PropertyToID("_AdditionalLightPosition");
            PerCameraBuffer._AdditionalLightColor = Shader.PropertyToID("_AdditionalLightColor");
            PerCameraBuffer._AdditionalLightDistanceAttenuation = Shader.PropertyToID("_AdditionalLightDistanceAttenuation");
            PerCameraBuffer._AdditionalLightSpotDir = Shader.PropertyToID("_AdditionalLightSpotDir");
            PerCameraBuffer._AdditionalLightSpotAttenuation = Shader.PropertyToID("_AdditionalLightSpotAttenuation");
            PerCameraBuffer._LightIndexBuffer = Shader.PropertyToID("_LightIndexBuffer");

            int maxVisibleLocalLights = renderer.maxVisibleLocalLights;
            m_LightPositions = new Vector4[maxVisibleLocalLights];
            m_LightColors = new Vector4[maxVisibleLocalLights];
            m_LightDistanceAttenuations = new Vector4[maxVisibleLocalLights];
            m_LightSpotDirections = new Vector4[maxVisibleLocalLights];
            m_LightSpotAttenuations = new Vector4[maxVisibleLocalLights];

            // TODO: HDR
            m_ColorFormat = RenderTextureFormat.Default;
            m_BlitMaterial = renderer.GetMaterial(MaterialHandles.Blit);
            m_ErrorMaterial = renderer.GetMaterial(MaterialHandles.Error);

            // Copy Depth Pass
            m_DepthCopyMaterial = renderer.GetMaterial(MaterialHandles.DepthCopy);
            m_SampleCountShaderHandle = Shader.PropertyToID("_SampleCount");

            // Copy Opaque Color Pass
            m_SamplingMaterial = renderer.GetMaterial(MaterialHandles.Sampling);
            m_SampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
        }

        public override void Setup(CommandBuffer cmd, RenderTextureDescriptor baseDescriptor, int samples)
        {
            if (colorHandles[0] != -1)
            {
                var descriptor = baseDescriptor;
                descriptor.colorFormat = m_ColorFormat;
                descriptor.depthBufferBits = k_DepthStencilBufferBits;  // TODO: does the color RT always need depth?
                descriptor.sRGB = true;
                descriptor.msaaSamples = samples;
                descriptor.enableRandomWrite = false;
                cmd.GetTemporaryRT(colorHandles[0], descriptor, FilterMode.Bilinear);
            }

            if (depthHandle != -1)
            {
                var descriptor = baseDescriptor;
                descriptor.colorFormat = RenderTextureFormat.Depth;
                descriptor.depthBufferBits = k_DepthStencilBufferBits;
                descriptor.msaaSamples = samples;
                descriptor.bindMS = samples > 1;
                cmd.GetTemporaryRT(colorHandles[1], descriptor, FilterMode.Point);
            }
            m_Disposed = false;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, ref LightData lightData)
        {
            Camera camera = cameraData.camera;
            SetupShaderConstants(ref context, ref cameraData, ref lightData);
            RendererConfiguration rendererConfiguration = GetRendererConfiguration(lightData.totalAdditionalLightsCount);

            if (cameraData.isStereoEnabled)
                context.StartMultiEye(camera);

            RenderOpaques(ref context, ref cullResults, ref cameraData, rendererConfiguration);

            if (cameraData.postProcessEnabled &&
                cameraData.postProcessLayer.HasOpaqueOnlyEffects(renderer.postProcessRenderContext))
                OpaquePostProcessPass(ref context, ref cameraData);

            if (cameraData.requiresDepthTexture)
                CopyDepthPass(ref context, ref cameraData);

            if (cameraData.requiresOpaqueTexture)
                OpaqueTexturePass(ref context, ref cameraData);

            RenderTransparents(ref context, ref cullResults, ref cameraData, rendererConfiguration);

            if (cameraData.postProcessEnabled)
                PostProcessPass(ref context, ref cameraData);

            // No blit to backbuffer if camera is offscreen render.
            if (!cameraData.isOffscreenRender && !cameraData.postProcessEnabled && colorHandles[0] != -1)
                FinalBlitPass(ref context, ref cameraData);

            if (cameraData.isStereoEnabled)
            {
                context.StopMultiEye(cameraData.camera);
                context.StereoEndRender(cameraData.camera);
            }
        }

        public override void Dispose(CommandBuffer cmd)
        {
            if (!m_Disposed)
            {
                for (int i = 0; i < colorHandles.Length; ++i)
                {
                    if (colorHandles[i] != -1)
                        cmd.ReleaseTemporaryRT(colorHandles[i]);
                }

                if (depthHandle != -1)
                    cmd.ReleaseTemporaryRT(depthHandle);
                m_Disposed = true;
            }
        }

        RendererConfiguration GetRendererConfiguration(int localLightsCount)
        {
            RendererConfiguration configuration = RendererConfiguration.PerObjectReflectionProbes | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe;
            if (localLightsCount > 0)
            {
                if (renderer.useComputeBufferForPerObjectLightIndices)
                    configuration |= RendererConfiguration.ProvideLightIndices;
                else
                    configuration |= RendererConfiguration.PerObjectLightIndices8;
            }

            return configuration;
        }

        void SetupShaderConstants(ref ScriptableRenderContext context, ref CameraData cameraData, ref LightData lightData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("SetupShaderConstants");
            SetupShaderLightConstants(cmd, ref lightData);
            SetShaderKeywords(cmd, ref cameraData, ref lightData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetupShaderLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            // Clear to default all light constant data
            for (int i = 0; i < renderer.maxVisibleLocalLights; ++i)
                InitializeLightConstants(lightData.visibleLights, -1, out m_LightPositions[i],
                    out m_LightColors[i],
                    out m_LightDistanceAttenuations[i],
                    out m_LightSpotDirections[i],
                    out m_LightSpotAttenuations[i]);

            m_MixedLightingSetup = MixedLightingSetup.None;

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Lightweight pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref lightData);
            SetupAdditionalLightConstants(cmd, ref lightData);
        }

        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightDistanceAttenuation, lightSpotDir, lightSpotAttenuation;
            List<VisibleLight> lights = lightData.visibleLights;
            InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, out lightPos, out lightColor, out lightDistanceAttenuation, out lightSpotDir, out lightSpotAttenuation);

            if (lightData.mainLightIndex >= 0)
            {
                VisibleLight mainLight = lights[lightData.mainLightIndex];
                Light mainLightRef = mainLight.light;

                if (LightweightPipeline.IsSupportedCookieType(mainLight.lightType) && mainLightRef.cookie != null)
                {
                    Matrix4x4 lightCookieMatrix;
                    LightweightPipeline.GetLightCookieMatrix(mainLight, out lightCookieMatrix);
                    cmd.SetGlobalTexture(PerCameraBuffer._MainLightCookie, mainLightRef.cookie);
                    cmd.SetGlobalMatrix(PerCameraBuffer._WorldToLight, lightCookieMatrix);
                }
            }

            cmd.SetGlobalVector(PerCameraBuffer._MainLightPosition, new Vector4(lightPos.x, lightPos.y, lightPos.z, lightDistanceAttenuation.w));
            cmd.SetGlobalVector(PerCameraBuffer._MainLightColor, lightColor);
        }

        void SetupAdditionalLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            int maxVisibleLocalLights = renderer.maxVisibleLocalLights;
            List<VisibleLight> lights = lightData.visibleLights;
            if (lightData.totalAdditionalLightsCount > 0)
            {
                int localLightsCount = 0;
                for (int i = 0; i < lights.Count && localLightsCount < maxVisibleLocalLights; ++i)
                {
                    VisibleLight light = lights[i];
                    if (light.lightType != LightType.Directional)
                    {
                        InitializeLightConstants(lights, i, out m_LightPositions[localLightsCount],
                            out m_LightColors[localLightsCount],
                            out m_LightDistanceAttenuations[localLightsCount],
                            out m_LightSpotDirections[localLightsCount],
                            out m_LightSpotAttenuations[localLightsCount]);
                        localLightsCount++;
                    }
                }

                cmd.SetGlobalVector(PerCameraBuffer._AdditionalLightCount, new Vector4(lightData.pixelAdditionalLightsCount,
                        lightData.totalAdditionalLightsCount, 0.0f, 0.0f));

                // if not using a compute buffer, engine will set indices in 2 vec4 constants
                // unity_4LightIndices0 and unity_4LightIndices1
                if (renderer.perObjectLightIndices != null)
                    cmd.SetGlobalBuffer("_LightIndexBuffer", renderer.perObjectLightIndices);
            }
            else
            {
                cmd.SetGlobalVector(PerCameraBuffer._AdditionalLightCount, Vector4.zero);
            }

            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightPosition, m_LightPositions);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightColor, m_LightColors);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightDistanceAttenuation, m_LightDistanceAttenuations);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightSpotDir, m_LightSpotDirections);
            cmd.SetGlobalVectorArray(PerCameraBuffer._AdditionalLightSpotAttenuation, m_LightSpotAttenuations);
        }

        ClearFlag GetCameraClearFlag(Camera camera)
        {
            ClearFlag clearFlag = ClearFlag.None;
            CameraClearFlags cameraClearFlags = camera.clearFlags;
            if (cameraClearFlags != CameraClearFlags.Nothing)
            {
                clearFlag |= ClearFlag.Depth;
                if (cameraClearFlags == CameraClearFlags.Color || cameraClearFlags == CameraClearFlags.Skybox)
                    clearFlag |= ClearFlag.Color;
            }

            return clearFlag;
        }

        void SetShaderKeywords(CommandBuffer cmd, ref CameraData cameraData, ref LightData lightData)
        {
            int vertexLightsCount = lightData.totalAdditionalLightsCount - lightData.pixelAdditionalLightsCount;

            CoreUtils.SetKeyword(cmd, LightweightKeywords.AdditionalLightsText, lightData.totalAdditionalLightsCount > 0);
            CoreUtils.SetKeyword(cmd, LightweightKeywords.MixedLightingSubtractiveText, m_MixedLightingSetup == MixedLightingSetup.Subtractive);
            CoreUtils.SetKeyword(cmd, LightweightKeywords.VertexLightsText, vertexLightsCount > 0);

            // TODO: We have to discuss cookie approach on LWRP.
            // CoreUtils.SetKeyword(cmd, LightweightKeywords.MainLightCookieText, mainLightIndex != -1 && LightweightUtils.IsSupportedCookieType(visibleLights[mainLightIndex].lightType) && visibleLights[mainLightIndex].light.cookie != null);

            LightShadows directionalShadowQuality = lightData.shadowData.renderedDirectionalShadowQuality;
            LightShadows localShadowQuality = lightData.shadowData.renderedLocalShadowQuality;

            // Currently shadow filtering keyword is shared between local and directional shadows.
            bool hasSoftShadows = (directionalShadowQuality == LightShadows.Soft || localShadowQuality == LightShadows.Soft) &&
                lightData.shadowData.supportsSoftShadows;

            CoreUtils.SetKeyword(cmd, LightweightKeywords.DirectionalShadowsText, directionalShadowQuality != LightShadows.None);
            CoreUtils.SetKeyword(cmd, LightweightKeywords.LocalShadowsText, localShadowQuality != LightShadows.None);
            CoreUtils.SetKeyword(cmd, LightweightKeywords.SoftShadowsText, hasSoftShadows);

            // TODO: Remove this. legacy particles support will be removed from Unity in 2018.3. This should be a shader_feature instead with prop exposed in the Standard particles shader.
            CoreUtils.SetKeyword(cmd, "SOFTPARTICLES_ON", cameraData.requiresSoftParticles);
        }

        void SetRenderTarget(CommandBuffer cmd, ClearFlag clearFlag)
        {
            if (colorHandles[0] != -1)
            {
                if (depthHandle != -1)
                    CoreUtils.SetRenderTarget(cmd, GetSurface(colorHandles[0]), GetSurface(depthHandle), clearFlag);
                else
                    CoreUtils.SetRenderTarget(cmd, GetSurface(colorHandles[0]), clearFlag);
            }
            else
            {
                CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, clearFlag);
            }
        }

        void RenderOpaques(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, RendererConfiguration settings)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render Opaques");
            Camera camera = cameraData.camera;

            ClearFlag clearFlag = GetCameraClearFlag(camera);
            SetRenderTarget(cmd, clearFlag);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // If rendering to an intermediate RT we resolve viewport on blit due to offset not being supported
            // while rendering to a RT.
            if (colorHandles[0] == -1 && cameraData.isDefaultViewport)
                cmd.SetViewport(camera.pixelRect);

            var opaqueDrawSettings = new DrawRendererSettings(camera, m_ShaderPassNames[0]);
            for (int i = 1; i < m_ShaderPassNames.Count; ++i)
                opaqueDrawSettings.SetShaderPassName(1, m_ShaderPassNames[1]);
            opaqueDrawSettings.sorting.flags = SortFlags.CommonOpaque;
            opaqueDrawSettings.rendererConfiguration = settings;

            var opaqueFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque
            };

            context.DrawRenderers(cullResults.visibleRenderers, ref opaqueDrawSettings, opaqueFilterSettings);

            // Render objects that did not match any shader pass with error shader
            RenderObjectsWithError(ref context, ref cullResults, camera, opaqueFilterSettings, SortFlags.None);

            if (camera.clearFlags == CameraClearFlags.Skybox)
                context.DrawSkybox(camera);
        }

        void RenderTransparents(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, RendererConfiguration config)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render Opaques");
            Camera camera = cameraData.camera;
            SetRenderTarget(cmd, ClearFlag.None);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            var transparentSettings = new DrawRendererSettings(camera, m_ShaderPassNames[0]);
            for (int i = 1; i < m_ShaderPassNames.Count; ++i)
                transparentSettings.SetShaderPassName(1, m_ShaderPassNames[1]);
            transparentSettings.sorting.flags = SortFlags.CommonTransparent;
            transparentSettings.rendererConfiguration = config;

            var transparentFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.transparent
            };

            context.DrawRenderers(cullResults.visibleRenderers, ref transparentSettings, transparentFilterSettings);

            // Render objects that did not match any shader pass with error shader
            RenderObjectsWithError(ref context, ref cullResults, camera, transparentFilterSettings, SortFlags.None);
        }

        void FinalBlitPass(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            var cmd = CommandBufferPool.Get("Final Blit Pass");
            if (cameraData.isStereoEnabled && XRSettings.eyeTextureDesc.dimension == TextureDimension.Tex2DArray)
                cmd.Blit(GetSurface(colorHandles[0]), BuiltinRenderTextureType.CameraTarget);
            else
                LightweightPipeline.Blit(cmd, ref cameraData, GetSurface(colorHandles[0]), BuiltinRenderTextureType.CameraTarget, cameraData.isStereoEnabled ? null : m_BlitMaterial);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void InitializeLightConstants(List<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightDistanceAttenuation, out Vector4 lightSpotDir,
            out Vector4 lightSpotAttenuation)
        {
            lightPos = k_DefaultLightPosition;
            lightColor = k_DefaultLightColor;
            lightDistanceAttenuation = k_DefaultLightSpotAttenuation;
            lightSpotDir = k_DefaultLightSpotDirection;
            lightSpotAttenuation = k_DefaultLightAttenuation;

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            if (lightData.lightType == LightType.Directional)
            {
                Vector4 dir = -lightData.localToWorld.GetColumn(2);
                lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            }
            else
            {
                Vector4 pos = lightData.localToWorld.GetColumn(3);
                lightPos = new Vector4(pos.x, pos.y, pos.z, 1.0f);
            }

            // VisibleLight.finalColor already returns color in active color space
            lightColor = lightData.finalColor;

            // Directional Light attenuation is initialize so distance attenuation always be 1.0
            if (lightData.lightType != LightType.Directional)
            {
                // Light attenuation in lightweight matches the unity vanilla one.
                // attenuation = 1.0 / 1.0 + distanceToLightSqr * quadraticAttenuation
                // then a smooth factor is applied to linearly fade attenuation to light range
                // the attenuation smooth factor starts having effect at 80% of light range
                // smoothFactor = (lightRangeSqr - distanceToLightSqr) / (lightRangeSqr - fadeStartDistanceSqr)
                // We rewrite smoothFactor to be able to pre compute the constant terms below and apply the smooth factor
                // with one MAD instruction
                // smoothFactor =  distanceSqr * (1.0 / (fadeDistanceSqr - lightRangeSqr)) + (-lightRangeSqr / (fadeDistanceSqr - lightRangeSqr)
                //                 distanceSqr *           oneOverFadeRangeSqr             +              lightRangeSqrOverFadeRangeSqr
                float lightRangeSqr = lightData.range * lightData.range;
                float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                float quadAtten = 25.0f / lightRangeSqr;
                lightDistanceAttenuation = new Vector4(quadAtten, oneOverFadeRangeSqr, lightRangeSqrOverFadeRangeSqr, 1.0f);
            }

            if (lightData.lightType == LightType.Spot)
            {
                Vector4 dir = lightData.localToWorld.GetColumn(2);
                lightSpotDir = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                // Spot Attenuation with a linear falloff can be defined as
                // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
                // This can be rewritten as
                // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
                // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
                // If we precompute the terms in a MAD instruction
                float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * lightData.spotAngle * 0.5f);
                // We neeed to do a null check for particle lights
                // This should be changed in the future
                // Particle lights will use an inline function
                float cosInnerAngle;
                if (lightData.light != null)
                    cosInnerAngle = Mathf.Cos(LightmapperUtils.ExtractInnerCone(lightData.light) * 0.5f);
                else
                    cosInnerAngle = Mathf.Cos((2.0f * Mathf.Atan(Mathf.Tan(lightData.spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f)) * 0.5f);
                float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
                float invAngleRange = 1.0f / smoothAngleRange;
                float add = -cosOuterAngle * invAngleRange;
                lightSpotAttenuation = new Vector4(invAngleRange, add, 0.0f);
            }

            Light light = lightData.light;

            // TODO: Add support to shadow mask
            if (light != null && light.bakingOutput.mixedLightingMode == MixedLightingMode.Subtractive && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
            {
                if (m_MixedLightingSetup == MixedLightingSetup.None && lightData.light.shadows != LightShadows.None)
                {
                    m_MixedLightingSetup = MixedLightingSetup.Subtractive;
                    lightDistanceAttenuation.w = 0.0f;
                }
            }
        }

        void AfterOpaque(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            //if (m_RequireDepthTexture)
            //{
            //    CommandBuffer cmd = CommandBufferPool.Get("After Opaque");

            //    bool setRenderTarget = false;
            //    RenderTargetIdentifier depthRT = m_DepthRT;

            //    // TODO: There's currently an issue in the PostFX stack that has a one frame delay when an effect is enabled/disabled
            //    // when an effect is disabled, HasOpaqueOnlyEffects returns true in the first frame, however inside render the effect
            //    // state is update, causing RenderPostProcess here to not blit to FinalColorRT. Until the next frame the RT will have garbage.
            //    if (CoreUtils.HasFlag(config, FrameRenderingConfiguration.BeforeTransparentPostProcess))
            //    {
            //        // When only have one effect in the stack we blit to a work RT then blit it back to active color RT.
            //        // This seems like an extra blit but it saves us a depth copy/blit which has some corner cases like msaa depth resolve.
            //        if (m_RequireCopyColor)
            //        {
            //            RenderPostProcess(cmd, m_CurrCameraColorRT, m_CopyColorRT, true);
            //            cmd.Blit(m_CopyColorRT, m_CurrCameraColorRT);
            //        }
            //        else
            //            RenderPostProcess(cmd, m_CurrCameraColorRT, m_CurrCameraColorRT, true);

            //        setRenderTarget = true;
            //        SetRenderTarget(cmd, m_CurrCameraColorRT, m_DepthRT);
            //    }

            //    if (CoreUtils.HasFlag(config, FrameRenderingConfiguration.DepthCopy))
            //    {
            //        bool forceBlit = false;
            //        if (m_MSAASamples > 1)
            //        {
            //            cmd.SetGlobalFloat(m_SampleCount, (float)m_MSAASamples);
            //            cmd.EnableShaderKeyword(kMSAADepthKeyword);
            //            forceBlit = true;
            //        }
            //        else
            //            cmd.DisableShaderKeyword(kMSAADepthKeyword);

            //        CopyTexture(cmd, m_DepthRT, m_CopyDepth, m_CopyDepthMaterial, forceBlit);
            //        depthRT = m_CopyDepth;
            //        setRenderTarget = true;
            //        cmd.SetGlobalTexture(RenderTargetHandle.Depth, m_CopyDepth);
            //    }

            //    if (setRenderTarget)
            //        SetRenderTarget(cmd, m_CurrCameraColorRT, depthRT);
            //    context.ExecuteCommandBuffer(cmd);
            //    CommandBufferPool.Release(cmd);
            //}
        }

        // TODO: move to postfx pass
        void PostProcessPass(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render PostProcess Effects");
            LightweightPipeline.RenderPostProcess(cmd, renderer.postProcessRenderContext, ref cameraData, m_ColorFormat, GetSurface(colorHandles[0]), BuiltinRenderTextureType.CameraTarget, false);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        void RenderObjectsWithError(ref ScriptableRenderContext context, ref CullResults cullResults, Camera camera, FilterRenderersSettings filterSettings, SortFlags sortFlags)
        {
            if (m_ErrorMaterial != null)
            {
                DrawRendererSettings errorSettings = new DrawRendererSettings(camera, m_LegacyShaderPassNames[0]);
                for (int i = 1; i < m_LegacyShaderPassNames.Count; ++i)
                    errorSettings.SetShaderPassName(i, m_LegacyShaderPassNames[i]);

                errorSettings.sorting.flags = sortFlags;
                errorSettings.rendererConfiguration = RendererConfiguration.None;
                errorSettings.SetOverrideMaterial(m_ErrorMaterial, 0);
                context.DrawRenderers(cullResults.visibleRenderers, ref errorSettings, filterSettings);
            }
        }

        void OpaquePostProcessPass(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render Opaque PostProcess Effects");

            RenderTargetIdentifier source;
            // If have a single opaque postfx we need to create a temp texture as otherwise source and target
            // would be the same
            if (cameraData.postProcessLayer.sortedBundles[PostProcessEvent.BeforeTransparent].Count == 1)
            {
                // TODO:
                // cmd.GetTemporaryRT();
                source = GetSurface(colorHandles[0]);
            }
            else
                source = GetSurface(colorHandles[0]);

            LightweightPipeline.RenderPostProcess(cmd, renderer.postProcessRenderContext, ref cameraData, m_ColorFormat, source, GetSurface(colorHandles[0]), true);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void CopyDepthPass(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Depth Copy");
            if (depthHandle == RenderTargetHandles.DepthMS)
            {
                cmd.SetGlobalFloat(m_SampleCountShaderHandle, cameraData.msaaSamples);
                cmd.EnableShaderKeyword(k_MSAADepthKeyword);
                cmd.Blit(GetSurface(RenderTargetHandles.DepthMS), GetSurface(RenderTargetHandles.Depth), m_DepthCopyMaterial);
            }
            else
            {
                // TODO:
                //cmd.Blit(GetSurface(RenderTargetHandle.Depth), depthCopy, m_DepthCopyMaterial);
            }
            cmd.DisableShaderKeyword(k_MSAADepthKeyword);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            //        bool forceBlit = false;
            //        if (m_MSAASamples > 1)
            //        {
            //            cmd.SetGlobalFloat(m_SampleCount, (float)m_MSAASamples);
            //            cmd.EnableShaderKeyword(kMSAADepthKeyword);
            //            forceBlit = true;
            //        }
            //        else
            //            cmd.DisableShaderKeyword(kMSAADepthKeyword);

            //        CopyTexture(cmd, m_DepthRT, m_CopyDepth, m_CopyDepthMaterial, forceBlit);
            //        depthRT = m_CopyDepth;
            //        setRenderTarget = true;
            //        cmd.SetGlobalTexture(RenderTargetHandle.Depth, m_CopyDepth);
        }

        void OpaqueTexturePass(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Copy Opaque Color");
            Downsampling downsampling = cameraData.opaqueTextureDownsampling;
            float opaqueScaler = m_OpaqueScalerValues[(int)downsampling];

            RenderTextureDescriptor opaqueDesc = renderer.CreateRTDesc(ref cameraData, opaqueScaler);
            RenderTargetIdentifier colorRT = GetSurface(colorHandles[0]);
            RenderTargetIdentifier opaqueColorRT = GetSurface(RenderTargetHandles.OpaqueColor);

            cmd.GetTemporaryRT(RenderTargetHandles.OpaqueColor, opaqueDesc, cameraData.opaqueTextureDownsampling == Downsampling.None ? FilterMode.Point : FilterMode.Bilinear);
            switch (downsampling)
            {
                case Downsampling.None:
                    cmd.Blit(colorRT, opaqueColorRT);
                    break;
                case Downsampling._2xBilinear:
                    cmd.Blit(colorRT, opaqueColorRT);
                    break;
                case Downsampling._4xBox:
                    m_SamplingMaterial.SetFloat(m_SampleOffsetShaderHandle, 2);
                    cmd.Blit(colorRT, opaqueColorRT, m_SamplingMaterial, 0);
                    break;
                case Downsampling._4xBilinear:
                    cmd.Blit(colorRT, opaqueColorRT);
                    break;
            }
            //SetRenderTarget(cmd, m_CurrCameraColorRT, m_DepthRT);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
