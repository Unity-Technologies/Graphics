using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // String values
        const string m_RayGenReflectionHalfResName = "RayGenReflectionHalfRes";
        const string m_RayGenReflectionFullResName = "RayGenReflectionFullRes";
        const string m_RayGenIntegrationName = "RayGenIntegration";

        void InitRayTracedReflections()
        {
        }

        static RTHandle ReflectionHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("ReflectionHistoryBuffer{0}", frameIndex));
        }

        void ReleaseRayTracedReflections()
        {
        }

        void RenderRayTracedReflections(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
            ScreenSpaceReflection reflectionSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            switch (reflectionSettings.mode.value)
            {
                case RayTracingMode.Performance:
                {
                    RenderReflectionsPerformance(hdCamera, cmd, outputTexture, renderContext, frameCount);
                }
                break;
                case RayTracingMode.Quality:
                {
                    RenderReflectionsQuality(hdCamera, cmd, outputTexture, renderContext, frameCount);
                }
                break;
            }
        }

        void BindRayTracedReflectionData(CommandBuffer cmd, HDCamera hdCamera, RayTracingShader reflectionShader, ScreenSpaceReflection settings, LightCluster lightClusterSettings, RayTracingSettings rtSettings
                                        , RTHandle outputLightingBuffer, RTHandle outputHitPointBuffer)
        {
            // Grab the acceleration structures and the light cluster to use
            RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
            HDRaytracingLightCluster lightCluster = RequestLightCluster();
            BlueNoise blueNoise = GetBlueNoiseManager();

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(reflectionShader, "IndirectDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(reflectionShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Global reflection parameters
            cmd.SetRayTracingFloatParams(reflectionShader, HDShaderIDs._RaytracingIntensityClamp, settings.clampValue.value);
            cmd.SetRayTracingFloatParams(reflectionShader, HDShaderIDs._RaytracingReflectionMinSmoothness, settings.minSmoothness.value);
            cmd.SetRayTracingFloatParams(reflectionShader, HDShaderIDs._RaytracingReflectionSmoothnessFadeStart, settings.smoothnessFadeStart.value);
            cmd.SetRayTracingIntParams(reflectionShader, HDShaderIDs._RaytracingIncludeSky, settings.reflectSky.value ? 1 : 0);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtSettings.rayBias.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, settings.rayLength.value);
            cmd.SetRayTracingIntParams(reflectionShader, HDShaderIDs._RaytracingNumSamples, settings.sampleCount.value);
            int frameIndex = RayTracingFrameIndex(hdCamera);
            cmd.SetRayTracingIntParam(reflectionShader, HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Inject the ray-tracing sampling data
            blueNoise.BindDitheredRNGData8SPP(cmd);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._SsrLightingTextureRW, outputLightingBuffer);
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._SsrHitPointTexture, outputHitPointBuffer);
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetGlobalTexture(HDShaderIDs._StencilTexture, sharedRTManager.GetDepthStencilBuffer(), RenderTextureSubElement.Stencil);
            cmd.SetRayTracingIntParams(reflectionShader, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);
            
            // Set ray count tex
            RayCountManager rayCountManager = GetRayCountManager();
            cmd.SetRayTracingIntParam(reflectionShader, HDShaderIDs._RayCountEnabled, rayCountManager.RayCountIsEnabled());
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());

            // Compute the pixel spread value
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingPixelSpreadAngle, GetPixelSpreadAngle(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));

            // Bind the lightLoop data
            lightCluster.BindLightClusterData(cmd);

            // Note: Just in case, we rebind the directional light data (in case they were not)
            cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, m_LightLoopLightData.directionalLightData);
            cmd.SetGlobalInt(HDShaderIDs._DirectionalLightCount, m_lightList.directionalLights.Count);

            // Evaluate the clear coat mask texture based on the lit shader mode
            RenderTargetIdentifier clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : TextureXR.GetBlackTexture();
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMaskTexture);

            // Set the number of bounces for reflections
            cmd.SetGlobalInt(HDShaderIDs._RaytracingMaxRecursion, settings.bounceCount.value);

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._SkyTexture, m_SkyManager.GetSkyReflection(hdCamera));
        }

        DeferredLightingRTParameters PrepareReflectionDeferredLightingRTParameters(HDCamera hdCamera)
        {
            DeferredLightingRTParameters deferredParameters = new DeferredLightingRTParameters();

            // Fetch the GI volume component
            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            RayTracingSettings rTSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            // Make sure the binning buffer has the right size
            CheckBinningBuffersSize(hdCamera);

            // Generic attributes
            deferredParameters.rayBinning = true;
            deferredParameters.layerMask.value = (int)RayTracingRendererFlag.Reflection;
            deferredParameters.rayBias = rTSettings.rayBias.value;
            deferredParameters.maxRayLength = settings.rayLength.value;
            deferredParameters.clampValue = settings.clampValue.value;
            deferredParameters.includeSky = settings.reflectSky.value;
            deferredParameters.diffuseLightingOnly = false;
            deferredParameters.halfResolution = !settings.fullResolution.value;
            deferredParameters.rayCountFlag = m_RayCountManager.RayCountIsEnabled();
            deferredParameters.rayCountType = (int)RayCountValues.ReflectionDeferred;
            deferredParameters.preExpose = false;

            // Camera data
            deferredParameters.width = hdCamera.actualWidth;
            deferredParameters.height = hdCamera.actualHeight;
            deferredParameters.viewCount = hdCamera.viewCount;
            deferredParameters.fov = hdCamera.camera.fieldOfView;

            // Compute buffers
            deferredParameters.rayBinResult = m_RayBinResult;
            deferredParameters.rayBinSizeResult = m_RayBinSizeResult;
            deferredParameters.accelerationStructure = RequestAccelerationStructure();
            deferredParameters.lightCluster = RequestLightCluster();

            // Shaders
            deferredParameters.gBufferRaytracingRT = m_Asset.renderPipelineRayTracingResources.gBufferRaytracingRT;
            deferredParameters.deferredRaytracingCS = m_Asset.renderPipelineRayTracingResources.deferredRaytracingCS;
            deferredParameters.rayBinningCS = m_Asset.renderPipelineRayTracingResources.rayBinningCS;

            // XRTODO: add ray binning support for single-pass
            if (deferredParameters.viewCount > 1 && deferredParameters.rayBinning)
            {
                deferredParameters.rayBinning = false;
                Debug.LogWarning("Ray binning is not supported with XR single-pass rendering!");
            }

            return deferredParameters;
        }

        void RenderReflectionsPerformance(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
            // Fetch the required resources
            BlueNoise blueNoise = GetBlueNoiseManager();
            RayTracingShader reflectionShaderRT = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingRT;
            ComputeShader reflectionShaderCS = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingCS;
            ComputeShader reflectionFilter = m_Asset.renderPipelineRayTracingResources.reflectionBilateralFilterCS;
            RTHandle intermediateBuffer0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            // Fetch all the settings
            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            LightCluster lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesXHR = 0, numTilesYHR = 0;
            int currentKernel = 0;
            RenderTargetIdentifier clearCoatMaskTexture;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingIntegrateReflection)))
            {
                // Fetch the new sample kernel
                currentKernel = reflectionShaderCS.FindKernel(settings.fullResolution.value ? "RaytracingReflectionsFullRes" : "RaytracingReflectionsHalfRes");

                // Inject the ray-tracing sampling data
                blueNoise.BindDitheredRNGData8SPP(cmd);

                // Bind all the required textures
                cmd.SetComputeTextureParam(reflectionShaderCS, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(reflectionShaderCS, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : TextureXR.GetBlackTexture();
                cmd.SetComputeTextureParam(reflectionShaderCS, currentKernel, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMaskTexture);
                cmd.SetComputeIntParam(reflectionShaderCS, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);
                cmd.SetComputeTextureParam(reflectionShaderCS, currentKernel, HDShaderIDs._StencilTexture, m_SharedRTManager.GetDepthStencilBuffer(), 0, RenderTextureSubElement.Stencil);

                // Bind all the required scalars
                cmd.SetComputeFloatParam(reflectionShaderCS, HDShaderIDs._RaytracingIntensityClamp, settings.clampValue.value);
                cmd.SetComputeFloatParam(reflectionShaderCS, HDShaderIDs._RaytracingReflectionMinSmoothness, settings.minSmoothness.value);
                cmd.SetComputeIntParam(reflectionShaderCS, HDShaderIDs._RaytracingIncludeSky, settings.reflectSky.value ? 1 : 0);

                // Bind the sampling data
                int frameIndex = RayTracingFrameIndex(hdCamera);
                cmd.SetComputeIntParam(reflectionShaderCS, HDShaderIDs._RaytracingFrameIndex, frameIndex);

                // Bind the output buffers
                cmd.SetComputeTextureParam(reflectionShaderCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, intermediateBuffer1);

                if (settings.fullResolution.value)
                {
                    // Evaluate the dispatch parameters
                    numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                    numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;
                }
                else
                {
                    // Evaluate the dispatch parameters
                    numTilesXHR = (texWidth / 2 + (areaTileSize - 1)) / areaTileSize;
                    numTilesYHR = (texHeight / 2 + (areaTileSize - 1)) / areaTileSize;
                }

                // Compute the directions
                cmd.DispatchCompute(reflectionShaderCS, currentKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);

                // Prepare the components for the deferred lighting
                DeferredLightingRTParameters deferredParamters = PrepareReflectionDeferredLightingRTParameters(hdCamera);
                DeferredLightingRTResources deferredResources = PrepareDeferredLightingRTResources(hdCamera, intermediateBuffer1, intermediateBuffer0);

                // Evaluate the deferred lighting
                RenderRaytracingDeferredLighting(cmd, deferredParamters, deferredResources);

                // Fetch the right filter to use
                if (settings.fullResolution.value)
                {
                    currentKernel = reflectionFilter.FindKernel("ReflectionIntegrationUpscaleFullRes");
                }
                else
                {
                    currentKernel = reflectionFilter.FindKernel("ReflectionIntegrationUpscaleHalfRes");
                }

                // Inject all the parameters for the compute
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._SsrLightingTextureRW, intermediateBuffer0);
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._SsrHitPointTexture, intermediateBuffer1);
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._BlueNoiseTexture, blueNoise.textureArray16RGB);
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, "_RaytracingReflectionTexture", outputTexture);
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);
                cmd.SetComputeIntParam(reflectionFilter, HDShaderIDs._SpatialFilterRadius, settings.upscaleRadius.value);
                cmd.SetComputeIntParam(reflectionFilter, HDShaderIDs._RaytracingDenoiseRadius, settings.denoise.value ? settings.denoiserRadius.value : 0);
                cmd.SetComputeFloatParam(reflectionFilter, HDShaderIDs._RaytracingReflectionMinSmoothness, settings.minSmoothness.value);
                cmd.SetComputeFloatParam(reflectionFilter, HDShaderIDs._RaytracingReflectionSmoothnessFadeStart, settings.smoothnessFadeStart.value);

                numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                // Bind the right texture for clear coat support
                clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : TextureXR.GetBlackTexture();
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMaskTexture);

                // Compute the texture
                cmd.DispatchCompute(reflectionFilter, currentKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingFilterReflection)))
            {
                if (settings.denoise.value)
                {
                    // Grab the history buffer
                    RTHandle reflectionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);

                    float historyValidity = 1.0f;
#if UNITY_HDRP_DXR_TESTS_DEFINE
                    if (Application.isPlaying)
                        historyValidity = 0.0f;
                    else
#endif
                        // We need to check if something invalidated the history buffers
                        historyValidity *= ValidRayTracingHistory(hdCamera) ? 1.0f : 0.0f;

                    HDReflectionDenoiser reflectionDenoiser = GetReflectionDenoiser();
                    reflectionDenoiser.DenoiseBuffer(cmd, hdCamera, settings.denoiserRadius.value, outputTexture, reflectionHistory, intermediateBuffer0, historyValidity: historyValidity);
                    HDUtils.BlitCameraTexture(cmd, intermediateBuffer0, outputTexture);
                }
            }
        }

        void RenderReflectionsQuality(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
            // Fetch the shaders that we will be using
            ComputeShader reflectionFilter = m_Asset.renderPipelineRayTracingResources.reflectionBilateralFilterCS;
            RayTracingShader reflectionShader = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingRT;

            // Request the buffers we shall be using
            RTHandle intermediateBuffer0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            LightCluster lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingIntegrateReflection)))
            {
                // Bind all the required data for ray tracing
                BindRayTracedReflectionData(cmd, hdCamera, reflectionShader, settings, lightClusterSettings, rtSettings, intermediateBuffer0, intermediateBuffer1);

                // Only use the shader variant that has multi bounce if the bounce count > 1
                CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", settings.bounceCount.value > 1);

                // We are not in the diffuse only case
                CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", false);

                // Run the computation
                cmd.DispatchRays(reflectionShader, m_RayGenIntegrationName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);

                // Disable multi-bounce
                CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", false);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingFilterReflection)))
            {
                if (settings.denoise.value)
                {
                    // Grab the history buffer
                    RTHandle reflectionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);

                    float historyValidity = 1.0f;
#if UNITY_HDRP_DXR_TESTS_DEFINE
                    if (Application.isPlaying)
                        historyValidity = 0.0f;
                    else
#endif
                        // We need to check if something invalidated the history buffers
                        historyValidity *= ValidRayTracingHistory(hdCamera) ? 1.0f : 0.0f;

                    HDReflectionDenoiser reflectionDenoiser = GetReflectionDenoiser();
                    reflectionDenoiser.DenoiseBuffer(cmd, hdCamera, settings.denoiserRadius.value, intermediateBuffer0, reflectionHistory, outputTexture, historyValidity: historyValidity);
                }
                else
                {
                    HDUtils.BlitCameraTexture(cmd, intermediateBuffer0, outputTexture);
                }
            }
        }
    }
}
