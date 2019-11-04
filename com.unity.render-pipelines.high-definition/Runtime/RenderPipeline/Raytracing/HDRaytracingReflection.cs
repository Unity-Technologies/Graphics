using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Intermediate buffer for computing the effect
        RTHandle m_ReflIntermediateTexture0 = null;
        RTHandle m_ReflIntermediateTexture1 = null;

        // String values
        const string m_RayGenReflectionHalfResName = "RayGenReflectionHalfRes";
        const string m_RayGenReflectionFullResName = "RayGenReflectionFullRes";
        const string m_RayGenIntegrationName = "RayGenIntegration";

        void InitRayTracedReflections()
        {
            m_ReflIntermediateTexture0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "ReflIntermediateTexture0");
            m_ReflIntermediateTexture1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "ReflIntermediateTexture1");
        }

        static RTHandle ReflectionHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("ReflectionHistoryBuffer{0}", frameIndex));
        }

        void ReleaseRayTracedReflections()
        {
            RTHandles.Release(m_ReflIntermediateTexture1);
            RTHandles.Release(m_ReflIntermediateTexture0);
        }

        void RenderRayTracedReflections(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
            RenderPipelineSettings.RaytracingTier currentTier = m_Asset.currentPlatformRenderPipelineSettings.supportedRaytracingTier;
            switch (currentTier)
            {
                case RenderPipelineSettings.RaytracingTier.Tier1:
                {
                    RenderReflectionsT1(hdCamera, cmd, outputTexture, renderContext, frameCount);
                }
                break;
                case RenderPipelineSettings.RaytracingTier.Tier2:
                {
                    RenderReflectionsT2(hdCamera, cmd, outputTexture, renderContext, frameCount);
                }
                break;
            }
        }

        void BindRayTracedReflectionData(CommandBuffer cmd, HDCamera hdCamera, RayTracingShader reflectionShader, ScreenSpaceReflection settings, LightCluster lightClusterSettings, RayTracingSettings rtSettings)
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
            cmd.SetRayTracingIntParams(reflectionShader, HDShaderIDs._RaytracingIncludeSky, settings.reflectSky.value ? 1 : 0);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtSettings.rayBias.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, settings.rayLength.value);
            cmd.SetRayTracingIntParams(reflectionShader, HDShaderIDs._RaytracingNumSamples, settings.sampleCount.value);
            int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)m_FrameCount % 8;
            cmd.SetRayTracingIntParam(reflectionShader, HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Inject the ray-tracing sampling data
            blueNoise.BindDitheredRNGData8SPP(cmd);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._SsrLightingTextureRW, m_ReflIntermediateTexture0);
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._SsrHitPointTexture, m_ReflIntermediateTexture1);
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(reflectionShader, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

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
            var settings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>();
            RayTracingSettings rTSettings = VolumeManager.instance.stack.GetComponent<RayTracingSettings>();

            // Make sure the binning buffer has the right size
            CheckBinningBuffersSize(hdCamera);

            // Generic attributes
            deferredParameters.rayBinning = settings.rayBinning.value;
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

        public void RenderReflectionsT1(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
            // Fetch the required resources
            BlueNoise blueNoise = GetBlueNoiseManager();
            RayTracingShader reflectionShaderRT = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingRT;
            ComputeShader reflectionShaderCS = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingCS;
            ComputeShader reflectionFilter = m_Asset.renderPipelineRayTracingResources.reflectionBilateralFilterCS;

            // Fetch all the settings
            var settings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>();
            LightCluster lightClusterSettings = VolumeManager.instance.stack.GetComponent<LightCluster>();
            RayTracingSettings rtSettings = VolumeManager.instance.stack.GetComponent<RayTracingSettings>();

            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesXHR = 0, numTilesYHR = 0;
            int currentKernel = 0;
            RenderTargetIdentifier clearCoatMaskTexture;

            using (new ProfilingSample(cmd, "Ray Traced Reflection", CustomSamplerId.RaytracingIntegrateReflection.GetSampler()))
            {
                if (settings.deferredMode.value)
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

                    // Bind all the required scalars
                    cmd.SetComputeFloatParam(reflectionShaderCS, HDShaderIDs._RaytracingIntensityClamp, settings.clampValue.value);
                    cmd.SetComputeFloatParam(reflectionShaderCS, HDShaderIDs._RaytracingReflectionMinSmoothness, settings.minSmoothness.value);
                    cmd.SetComputeIntParam(reflectionShaderCS, HDShaderIDs._RaytracingIncludeSky, settings.reflectSky.value ? 1 : 0);

                    // Bind the sampling data
                    int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)m_FrameCount % 8;
                    cmd.SetComputeIntParam(reflectionShaderCS, HDShaderIDs._RaytracingFrameIndex, frameIndex);

                    // Bind the output buffers
                    cmd.SetComputeTextureParam(reflectionShaderCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, m_ReflIntermediateTexture1);

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
                    DeferredLightingRTResources deferredResources = PrepareDeferredLightingRTResources(hdCamera, m_ReflIntermediateTexture1, m_ReflIntermediateTexture0);

                    // Evaluate the deferred lighting
                    RenderRaytracingDeferredLighting(cmd, deferredParamters, deferredResources);
                }
                else
                {
                    // Bind all the required data for ray tracing
                    BindRayTracedReflectionData(cmd, hdCamera, reflectionShaderRT, settings, lightClusterSettings, rtSettings);

                    // Run the computation
                    if (settings.fullResolution.value)
                    {
                        cmd.DispatchRays(reflectionShaderRT, m_RayGenReflectionFullResName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);
                    }
                    else
                    {
                        // Run the computation
                        cmd.DispatchRays(reflectionShaderRT, m_RayGenReflectionHalfResName, (uint)(hdCamera.actualWidth / 2), (uint)(hdCamera.actualHeight / 2), (uint)hdCamera.viewCount);
                    }
                }

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
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._SsrLightingTextureRW, m_ReflIntermediateTexture0);
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._SsrHitPointTexture, m_ReflIntermediateTexture1);
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._BlueNoiseTexture, blueNoise.textureArray16RGB);
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, "_RaytracingReflectionTexture", outputTexture);
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);
                cmd.SetComputeIntParam(reflectionFilter, HDShaderIDs._SpatialFilterRadius, settings.upscaleRadius.value);
                cmd.SetComputeIntParam(reflectionFilter, HDShaderIDs._RaytracingDenoiseRadius, settings.denoise.value ? settings.denoiserRadius.value : 0);
                cmd.SetComputeFloatParam(reflectionFilter, HDShaderIDs._RaytracingReflectionMinSmoothness, settings.minSmoothness.value);

                numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                // Bind the right texture for clear coat support
                clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : TextureXR.GetBlackTexture();
                cmd.SetComputeTextureParam(reflectionFilter, currentKernel, HDShaderIDs._SsrClearCoatMaskTexture, clearCoatMaskTexture);

                // Compute the texture
                cmd.DispatchCompute(reflectionFilter, currentKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);
            }

            using (new ProfilingSample(cmd, "Filter Reflection", CustomSamplerId.RaytracingFilterReflection.GetSampler()))
            {
                if (settings.denoise.value)
                {
                    // Grab the history buffer
                    RTHandle reflectionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);

                    HDSimpleDenoiser simpleDenoiser = GetSimpleDenoiser();
                    simpleDenoiser.DenoiseBuffer(cmd, hdCamera, outputTexture, reflectionHistory, m_ReflIntermediateTexture0, settings.denoiserRadius.value, singleChannel: false);
                    HDUtils.BlitCameraTexture(cmd, m_ReflIntermediateTexture0, outputTexture);
                }
            }   
        }

        void RenderReflectionsT2(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
            // Fetch the shaders that we will be using
            ComputeShader reflectionFilter = m_Asset.renderPipelineRayTracingResources.reflectionBilateralFilterCS;
            RayTracingShader reflectionShader = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingRT;

            var settings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>();
            LightCluster lightClusterSettings = VolumeManager.instance.stack.GetComponent<LightCluster>();
            RayTracingSettings rtSettings = VolumeManager.instance.stack.GetComponent<RayTracingSettings>();

            using (new ProfilingSample(cmd, "Ray Traced Reflection", CustomSamplerId.RaytracingIntegrateReflection.GetSampler()))
            {

                // Bind all the required data for ray tracing
                BindRayTracedReflectionData(cmd, hdCamera, reflectionShader, settings, lightClusterSettings, rtSettings);

                // Only use the shader variant that has multi bounce if the bounce count > 1
                CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", settings.bounceCount.value > 1);

                // We are not in the diffuse only case
                CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", false);

                // Run the computation
                cmd.DispatchRays(reflectionShader, m_RayGenIntegrationName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, (uint)hdCamera.viewCount);

                // Disable multi-bounce
                CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", false);
            }

            using (new ProfilingSample(cmd, "Filter Reflection", CustomSamplerId.RaytracingFilterReflection.GetSampler()))
            {
                if (settings.denoise.value)
                {
                    // Grab the history buffer
                    RTHandle reflectionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);

                    HDSimpleDenoiser simpleDenoiser = GetSimpleDenoiser();
                    simpleDenoiser.DenoiseBuffer(cmd, hdCamera, m_ReflIntermediateTexture0, reflectionHistory, outputTexture, settings.denoiserRadius.value, singleChannel: false);
                }
                else
                {
                    HDUtils.BlitCameraTexture(cmd, m_ReflIntermediateTexture0, outputTexture);
                }
            }
        }
    }
}
