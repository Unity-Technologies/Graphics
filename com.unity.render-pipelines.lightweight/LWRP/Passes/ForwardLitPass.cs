using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class ForwardLitPass : ScriptableRenderPass
    {
        const int k_DepthStencilBufferBits = 32;

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

        // Opaque Copy Pass
        Material m_SamplingMaterial;
        float[] m_OpaqueScalerValues = {1.0f, 0.5f, 0.25f, 0.25f};
        int m_SampleOffsetShaderHandle;

        const string k_RenderOpaquesTag = "Render Opaques";
        const string k_RenderTransparentsTag = "Render Transparents";

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

            m_ColorFormat = RenderTextureFormat.Default;
            m_BlitMaterial = renderer.GetMaterial(MaterialHandles.Blit);
            m_ErrorMaterial = renderer.GetMaterial(MaterialHandles.Error);

            // Copy Depth Pass
            m_DepthCopyMaterial = renderer.GetMaterial(MaterialHandles.DepthCopy);

            // Copy Opaque Color Pass
            m_SamplingMaterial = renderer.GetMaterial(MaterialHandles.Sampling);
            m_SampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
        }

        public override void Setup(CommandBuffer cmd, RenderTextureDescriptor baseDescriptor, int[] colorAttachmentHandles, int depthAttachmentHandle = -1, int samples = 1)
        {
            base.Setup(cmd, baseDescriptor, colorAttachmentHandles, depthAttachmentHandle, samples);

            m_ColorFormat = baseDescriptor.colorFormat;
            if (colorAttachmentHandle != -1)
            {
                var descriptor = baseDescriptor;
                descriptor.depthBufferBits = k_DepthStencilBufferBits;  // TODO: does the color RT always need depth?
                descriptor.sRGB = true;
                descriptor.msaaSamples = samples;
                cmd.GetTemporaryRT(colorAttachmentHandle, descriptor, FilterMode.Bilinear);
            }

            if (depthAttachmentHandle != -1)
            {
                var descriptor = baseDescriptor;
                descriptor.colorFormat = RenderTextureFormat.Depth;
                descriptor.depthBufferBits = k_DepthStencilBufferBits;
                descriptor.msaaSamples = samples;
                descriptor.bindMS = samples > 1;
                cmd.GetTemporaryRT(depthAttachmentHandle, descriptor, FilterMode.Point);
            }
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            bool dynamicBatching = renderingData.supportsDynamicBatching;
            SetupShaderConstants(ref context, ref renderingData.cameraData, ref renderingData.lightData, ref renderingData.shadowData);
            RendererConfiguration rendererConfiguration = GetRendererConfiguration(renderingData.lightData.totalAdditionalLightsCount);

            if (renderingData.cameraData.isStereoEnabled)
                context.StartMultiEye(camera);

            RenderOpaques(ref context, ref cullResults, ref renderingData.cameraData, rendererConfiguration, dynamicBatching);

            if (renderingData.cameraData.postProcessEnabled &&
                renderingData.cameraData.postProcessLayer.HasOpaqueOnlyEffects(renderer.postProcessRenderContext))
                OpaquePostProcessSubPass(ref context, ref renderingData.cameraData);

            if (depthAttachmentHandle != -1)
                CopyDepthSubPass(ref context, ref renderingData.cameraData);

            if (renderingData.cameraData.requiresOpaqueTexture)
                CopyColorSubpass(ref context, ref renderingData.cameraData);

            RenderTransparents(ref context, ref cullResults, ref renderingData.cameraData, rendererConfiguration, dynamicBatching);

            if (renderingData.cameraData.postProcessEnabled)
                PostProcessPass(ref context, ref renderingData.cameraData);
            else if (!renderingData.cameraData.isOffscreenRender && colorAttachmentHandle != -1)
                FinalBlitPass(ref context, ref renderingData.cameraData);

            if (renderingData.cameraData.isStereoEnabled)
            {
                context.StopMultiEye(camera);
                context.StereoEndRender(camera);
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

        void SetupShaderConstants(ref ScriptableRenderContext context, ref CameraData cameraData, ref LightData lightData, ref ShadowData shadowData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("SetupShaderConstants");
            SetupShaderLightConstants(cmd, ref lightData);
            SetShaderKeywords(cmd, ref cameraData, ref lightData, ref shadowData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetupShaderLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            // Clear to default all light constant data
            m_MixedLightingSetup = MixedLightingSetup.None;
            for (int i = 0; i < renderer.maxVisibleLocalLights; ++i)
                renderer.InitializeLightConstants(lightData.visibleLights, -1, m_MixedLightingSetup, out m_LightPositions[i],
                    out m_LightColors[i],
                    out m_LightDistanceAttenuations[i],
                    out m_LightSpotDirections[i],
                    out m_LightSpotAttenuations[i]);

            // Main light has an optimized shader path for main light. This will benefit games that only care about a single light.
            // Lightweight pipeline also supports only a single shadow light, if available it will be the main light.
            SetupMainLightConstants(cmd, ref lightData);
            SetupAdditionalLightConstants(cmd, ref lightData);
        }

        void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
        {
            Vector4 lightPos, lightColor, lightDistanceAttenuation, lightSpotDir, lightSpotAttenuation;
            List<VisibleLight> lights = lightData.visibleLights;
            renderer.InitializeLightConstants(lightData.visibleLights, lightData.mainLightIndex, m_MixedLightingSetup, out lightPos, out lightColor, out lightDistanceAttenuation, out lightSpotDir, out lightSpotAttenuation);

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
                        renderer.InitializeLightConstants(lights, i, m_MixedLightingSetup, out m_LightPositions[localLightsCount],
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

        void SetShaderKeywords(CommandBuffer cmd, ref CameraData cameraData, ref LightData lightData, ref ShadowData shadowData)
        {
            int vertexLightsCount = lightData.totalAdditionalLightsCount - lightData.pixelAdditionalLightsCount;

            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.AdditionalLights, lightData.totalAdditionalLightsCount > 0);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.MixedLightingSubtractive, m_MixedLightingSetup == MixedLightingSetup.Subtractive);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.VertexLights, vertexLightsCount > 0);

            // TODO: We have to discuss cookie approach on LWRP.
            // CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.MainLightCookieText, mainLightIndex != -1 && LightweightUtils.IsSupportedCookieType(visibleLights[mainLightIndex].lightType) && visibleLights[mainLightIndex].light.cookie != null);

            LightShadows directionalShadowQuality = shadowData.renderedDirectionalShadowQuality;
            LightShadows localShadowQuality = shadowData.renderedLocalShadowQuality;

            // Currently shadow filtering keyword is shared between local and directional shadows.
            bool hasSoftShadows = (directionalShadowQuality == LightShadows.Soft || localShadowQuality == LightShadows.Soft) &&
                shadowData.supportsSoftShadows;

            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.DirectionalShadows, directionalShadowQuality != LightShadows.None);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.LocalShadows, localShadowQuality != LightShadows.None);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.SoftShadows, hasSoftShadows);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.CascadeShadows, shadowData.directionalLightCascadeCount > 1);

            // TODO: Remove this. legacy particles support will be removed from Unity in 2018.3. This should be a shader_feature instead with prop exposed in the Standard particles shader.
            CoreUtils.SetKeyword(cmd, "SOFTPARTICLES_ON", cameraData.requiresSoftParticles);
        }

        void SetRenderTarget(CommandBuffer cmd, RenderBufferLoadAction loadOp, RenderBufferStoreAction storeOp, ClearFlag clearFlag, Color clearColor)
        {
            if (colorAttachmentHandle != -1)
            {
                if (depthAttachmentHandle != -1)
                    SetRenderTarget(cmd, GetSurface(colorAttachmentHandle), loadOp, storeOp, GetSurface(depthAttachmentHandle), loadOp, storeOp, clearFlag, clearColor);
                else
                    SetRenderTarget(cmd, GetSurface(colorAttachmentHandle), loadOp, storeOp, clearFlag, clearColor);
            }
            else
            {
                SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, loadOp, storeOp, clearFlag, clearColor);
            }
        }

        void RenderOpaques(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, RendererConfiguration rendererConfiguration, bool dynamicBatching)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_RenderOpaquesTag);
            using (new ProfilingSample(cmd, k_RenderOpaquesTag))
            {
                Camera camera = cameraData.camera;
                ClearFlag clearFlag = GetCameraClearFlag(camera);
                SetRenderTarget(cmd, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, clearFlag, CoreUtils.ConvertSRGBToActiveColorSpace(camera.backgroundColor));

                // TODO: We need a proper way to handle multiple camera/ camera stack. Issue is: multiple cameras can share a same RT
                // (e.g, split screen games). However devs have to be dilligent with it and know when to clear/preserve color.
                // For now we make it consistent by resolving viewport with a RT until we can have a proper camera management system
                //if (colorAttachmentHandle == -1 && !cameraData.isDefaultViewport)
                //    cmd.SetViewport(camera.pixelRect);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawSettings = CreateDrawRendererSettings(camera, SortFlags.CommonOpaque, rendererConfiguration, dynamicBatching);
                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, renderer.opaqueFilterSettings);

                // Render objects that did not match any shader pass with error shader
                RenderObjectsWithError(ref context, ref cullResults, camera, renderer.opaqueFilterSettings, SortFlags.None);

                if (camera.clearFlags == CameraClearFlags.Skybox)
                    context.DrawSkybox(camera);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void RenderTransparents(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, RendererConfiguration rendererConfiguration, bool dynamicBatching)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_RenderTransparentsTag);
            using (new ProfilingSample(cmd, k_RenderTransparentsTag))
            {
                Camera camera = cameraData.camera;
                SetRenderTarget(cmd, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, ClearFlag.None, Color.black);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawSettings = CreateDrawRendererSettings(camera, SortFlags.CommonTransparent, rendererConfiguration, dynamicBatching);
                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, renderer.transparentFilterSettings);

                // Render objects that did not match any shader pass with error shader
                RenderObjectsWithError(ref context, ref cullResults, camera, renderer.transparentFilterSettings, SortFlags.None);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void FinalBlitPass(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            Material material = cameraData.isStereoEnabled ? null : m_BlitMaterial;
            RenderTargetIdentifier sourceRT = GetSurface(colorAttachmentHandle);

            CommandBuffer cmd = CommandBufferPool.Get("Final Blit Pass");
            cmd.SetGlobalTexture("_BlitTex", sourceRT);

            // We need to handle viewport on a RT. We do it by rendering a fullscreen quad + viewport
            if (!cameraData.isDefaultViewport)
            {
                SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.black);
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.SetViewport(cameraData.camera.pixelRect);
                LightweightPipeline.DrawFullScreen(cmd, material);
            }
            else
            {
                cmd.Blit(GetSurface(colorAttachmentHandle), BuiltinRenderTextureType.CameraTarget, material);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // TODO: move to postfx pass
        void PostProcessPass(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render PostProcess Effects");
            LightweightPipeline.RenderPostProcess(cmd, renderer.postProcessRenderContext, ref cameraData, m_ColorFormat, GetSurface(colorAttachmentHandle), BuiltinRenderTextureType.CameraTarget, false);
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

        void OpaquePostProcessSubPass(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render Opaque PostProcess Effects");

            RenderTargetIdentifier source = GetSurface(colorAttachmentHandle);
            LightweightPipeline.RenderPostProcess(cmd, renderer.postProcessRenderContext, ref cameraData, m_ColorFormat, source, GetSurface(colorAttachmentHandle), true);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void CopyDepthSubPass(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Depth Copy");
            RenderTargetIdentifier depthSurface = GetSurface(depthAttachmentHandle);
            RenderTargetIdentifier copyDepthSurface = GetSurface(RenderTargetHandles.DepthTexture);

            RenderTextureDescriptor descriptor = renderer.CreateRTDesc(ref cameraData);
            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = k_DepthStencilBufferBits;
            descriptor.msaaSamples = 1;
            descriptor.bindMS = false;
            cmd.GetTemporaryRT(RenderTargetHandles.DepthTexture, descriptor, FilterMode.Point);

            if (cameraData.msaaSamples > 1)
            {
                cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthNoMsaa);
                if (cameraData.msaaSamples == 4)
                {
                    cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa2);
                    cmd.EnableShaderKeyword(LightweightKeywordStrings.DepthMsaa4);
                }
                else
                {
                    cmd.EnableShaderKeyword(LightweightKeywordStrings.DepthMsaa2);
                    cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa4);
                }
                cmd.Blit(depthSurface, copyDepthSurface, m_DepthCopyMaterial);
            }
            else
            {
                cmd.EnableShaderKeyword(LightweightKeywordStrings.DepthNoMsaa);
                cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa2);
                cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa4);
                LightweightPipeline.CopyTexture(cmd, depthSurface, copyDepthSurface, m_DepthCopyMaterial);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void CopyColorSubpass(ref ScriptableRenderContext context, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Copy Opaque Color");
            Downsampling downsampling = cameraData.opaqueTextureDownsampling;
            float opaqueScaler = m_OpaqueScalerValues[(int)downsampling];

            RenderTextureDescriptor opaqueDesc = renderer.CreateRTDesc(ref cameraData, opaqueScaler);
            RenderTargetIdentifier colorRT = GetSurface(colorAttachmentHandle);
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

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
