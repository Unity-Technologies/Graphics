using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // String values
        const string m_RayGenIndirectDiffuseIntegrationName = "RayGenIntegration";

        void InitRayTracedIndirectDiffuse()
        {
        }

        void ReleaseRayTracedIndirectDiffuse()
        {
        }

        bool RayTracedIndirectDiffuseState(HDCamera hdCamera)
        {
            var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            return ValidIndirectDiffuseState(hdCamera)
                    && settings.rayTracing.value
                    && hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing);
        }

        RTHandle IndirectDiffuseHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("IndirectDiffuseHistoryBuffer{0}", frameIndex));
        }
        
        void RenderRayTracedIndirectDiffuse(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // If we are not supposed to evaluate the ray traced indirect diffuse term, quit right away
            if (!RayTracedIndirectDiffuseState(hdCamera))
                return;

            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            // Based on what the asset supports, follow the volume or force the right mode.
            if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
            {
                switch (giSettings.mode.value)
                {
                    case RayTracingMode.Performance:
                    {
                        RenderIndirectDiffusePerformance(hdCamera, cmd, renderContext, frameCount);
                    }
                    break;
                    case RayTracingMode.Quality:
                    {
                        RenderIndirectDiffuseQuality(hdCamera, cmd, renderContext, frameCount);
                    }
                    break;
                }
            }
            else if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality)
            {
                RenderIndirectDiffuseQuality(hdCamera, cmd, renderContext, frameCount);
            }
            else
            {
                RenderIndirectDiffusePerformance(hdCamera, cmd, renderContext, frameCount);
            }

            PropagateIndirectDiffuseData(hdCamera, cmd, renderContext, frameCount);
        }

        void PropagateIndirectDiffuseData(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // Bind the indirect diffuse texture (for forward materials)
            BindIndirectDiffuseTexture(cmd);

            // If we are in deferred mode, we need to make sure to add the indirect diffuse (that we intentionally ignored during the GBuffer pass)
            // Note that this discards the texture/object ambient occlusion. But we consider that okay given that the ray traced indirect diffuse
            // is a physically correct evaluation of that quantity
            ComputeShader indirectDiffuseCS = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingCS;
            if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
            {
                int indirectDiffuseKernel = indirectDiffuseCS.FindKernel("IndirectDiffuseAccumulation");

                // Bind the source texture
                cmd.SetComputeTextureParam(indirectDiffuseCS, indirectDiffuseKernel, HDShaderIDs._IndirectDiffuseTexture, m_IndirectDiffuseBuffer0);

                // Bind the output texture
                cmd.SetComputeTextureParam(indirectDiffuseCS, indirectDiffuseKernel, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                cmd.SetComputeTextureParam(indirectDiffuseCS, indirectDiffuseKernel, HDShaderIDs._GBufferTextureRW[3], m_GbufferManager.GetBuffer(3));

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesX = (hdCamera.actualWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesY = (hdCamera.actualHeight + (areaTileSize - 1)) / areaTileSize;

                // Add the indirect diffuse to the GBuffer
                cmd.DispatchCompute(indirectDiffuseCS, indirectDiffuseKernel, numTilesX, numTilesY, hdCamera.viewCount);
            }

            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, m_IndirectDiffuseBuffer0, FullScreenDebugMode.ScreenSpaceGlobalIllumination);
        }

        DeferredLightingRTParameters PrepareIndirectDiffuseDeferredLightingRTParameters(HDCamera hdCamera)
        {
            DeferredLightingRTParameters deferredParameters = new DeferredLightingRTParameters();

            // Fetch the GI volume component
            var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            RayTracingSettings rTSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            // Make sure the binning buffer has the right size
            CheckBinningBuffersSize(hdCamera);

            // Generic attributes
            deferredParameters.rayBinning = true;
            deferredParameters.layerMask.value = (int)RayTracingRendererFlag.GlobalIllumination;
            deferredParameters.rayBias = rTSettings.rayBias.value;
            deferredParameters.maxRayLength = settings.rayLength.value;
            deferredParameters.clampValue = settings.clampValue.value;
            deferredParameters.includeSky = true;
            deferredParameters.diffuseLightingOnly = true;

            deferredParameters.halfResolution = false;
            deferredParameters.rayCountFlag = m_RayCountManager.RayCountIsEnabled();
            deferredParameters.rayCountType = (int)RayCountValues.DiffuseGI_Deferred;
            deferredParameters.preExpose = true;

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

        void RenderIndirectDiffusePerformance(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // Fetch the required resources
            var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            BlueNoise blueNoise = GetBlueNoiseManager();

            // Fetch all the settings
            LightCluster lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            ComputeShader indirectDiffuseCS = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingCS;

            // Request the intermediate texture we will be using
            RTHandle directionBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Direction);
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingIntegrateIndirectDiffuse)))
            {
                // Fetch the new sample kernel
                int currentKernel = indirectDiffuseCS.FindKernel(settings.fullResolution.value ? "RaytracingIndirectDiffuseFullRes" : "RaytracingIndirectDiffuseHalfRes");

                // Inject the ray-tracing sampling data
                blueNoise.BindDitheredRNGData8SPP(cmd);

                // Bind all the required textures
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

                // Bind all the required scalars
                cmd.SetGlobalFloat(HDShaderIDs._RaytracingIntensityClamp, settings.clampValue.value);

                // Bind the output buffers
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);

                // Texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                // Compute the directions
                cmd.DispatchCompute(indirectDiffuseCS, currentKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);

                // Prepare the components for the deferred lighting
                DeferredLightingRTParameters deferredParamters = PrepareIndirectDiffuseDeferredLightingRTParameters(hdCamera);
                DeferredLightingRTResources deferredResources = PrepareDeferredLightingRTResources(hdCamera, directionBuffer, m_IndirectDiffuseBuffer0);

                // Evaluate the deferred lighting
                RenderRaytracingDeferredLighting(cmd, deferredParamters, deferredResources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingFilterIndirectDiffuse)))
            {
                // Fetch the right filter to use
                int currentKernel = indirectDiffuseCS.FindKernel(settings.fullResolution.value ? "IndirectDiffuseIntegrationUpscaleFullRes" : "IndirectDiffuseIntegrationUpscaleHalfRes");

                // Inject all the parameters for the compute
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._IndirectDiffuseTexture, m_IndirectDiffuseBuffer0);
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, directionBuffer);
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._BlueNoiseTexture, blueNoise.textureArray16RGB);
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._UpscaledIndirectDiffuseTextureRW, intermediateBuffer1);
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._ScramblingTexture, m_Asset.renderPipelineResources.textures.scramblingTex);
                cmd.SetComputeIntParam(indirectDiffuseCS, HDShaderIDs._SpatialFilterRadius, settings.upscaleRadius.value);

                // Texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesXHR = (texWidth  + (areaTileSize - 1)) / areaTileSize;
                int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                // Compute the texture
                cmd.DispatchCompute(indirectDiffuseCS, currentKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);

                // Copy the data back to the right buffer
                HDUtils.BlitCameraTexture(cmd, intermediateBuffer1, m_IndirectDiffuseBuffer0);

                // Denoise if required
                if (settings.denoise.value)
                {
                    DenoiseIndirectDiffuseBuffer(hdCamera, cmd, settings);
                }
            }
        }

        void BindRayTracedIndirectDiffuseData(CommandBuffer cmd, HDCamera hdCamera
                                                    , RayTracingShader indirectDiffuseShader
                                                    , GlobalIllumination settings, LightCluster lightClusterSettings, RayTracingSettings rtSettings
                                                    , RTHandle outputLightingBuffer, RTHandle outputHitPointBuffer)
        {
            // Grab the acceleration structures and the light cluster to use
            RayTracingAccelerationStructure accelerationStructure = RequestAccelerationStructure();
            HDRaytracingLightCluster lightCluster = RequestLightCluster();
            BlueNoise blueNoise = GetBlueNoiseManager();

            // Define the shader pass to use for the indirect diffuse pass
            cmd.SetRayTracingShaderPass(indirectDiffuseShader, "IndirectDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(indirectDiffuseShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            blueNoise.BindDitheredRNGData8SPP(cmd);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtSettings.rayBias.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, settings.rayLength.value);
            cmd.SetGlobalInt(HDShaderIDs._RaytracingNumSamples, settings.sampleCount.value);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._IndirectDiffuseTextureRW, outputLightingBuffer);
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._IndirectDiffuseHitPointTextureRW, outputHitPointBuffer);
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

            // Set the indirect diffuse parameters
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingIntensityClamp, settings.clampValue.value);

            // Set ray count texture
            RayCountManager rayCountManager = GetRayCountManager();
            cmd.SetGlobalInt(HDShaderIDs._RayCountEnabled, rayCountManager.RayCountIsEnabled());
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());

            // Compute the pixel spread value
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingPixelSpreadAngle, GetPixelSpreadAngle(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));

            // LightLoop data
            lightCluster.BindLightClusterData(cmd);

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._SkyTexture, m_SkyManager.GetSkyReflection(hdCamera));

            // Set the number of bounces to 1
            cmd.SetGlobalInt(HDShaderIDs._RaytracingMaxRecursion, settings.bounceCount.value);
        }

        void RenderIndirectDiffuseQuality(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            LightCluster lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();
            RayTracingSettings rtSettings = hdCamera.volumeStack.GetComponent<RayTracingSettings>();

            // Shaders that are used
            RayTracingShader indirectDiffuseRT = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingRT;

            // Request the intermediate texture we will be using
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            // Bind all the parameters for ray tracing
            BindRayTracedIndirectDiffuseData(cmd, hdCamera, indirectDiffuseRT, giSettings, lightClusterSettings, rtSettings, m_IndirectDiffuseBuffer0, intermediateBuffer1);

            // Compute the actual resolution that is needed base on the quality
            int widthResolution = hdCamera.actualWidth;
            int heightResolution = hdCamera.actualHeight;

            // Only use the shader variant that has multi bounce if the bounce count > 1
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", giSettings.bounceCount.value > 1);

            // Run the computation
            cmd.SetGlobalInt(HDShaderIDs._RayTracingDiffuseLightingOnly, 1);
            cmd.DispatchRays(indirectDiffuseRT, m_RayGenIndirectDiffuseIntegrationName, (uint)widthResolution, (uint)heightResolution, (uint)hdCamera.viewCount);

            // Disable the keywords we do not need anymore
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", false);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingFilterIndirectDiffuse)))
            {
                if (giSettings.denoise.value)
                {
                    DenoiseIndirectDiffuseBuffer(hdCamera, cmd, giSettings);
                }
            }
        }

        void DenoiseIndirectDiffuseBuffer(HDCamera hdCamera, CommandBuffer cmd, GlobalIllumination settings)
        {
            // Grab the high frequency history buffer
            RTHandle indirectDiffuseHistoryHF = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);

            // Request the intermediate texture we will be using
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            float historyValidity = 1.0f;
#if UNITY_HDRP_DXR_TESTS_DEFINE
            if (Application.isPlaying)
                historyValidity = 0.0f;
            else
#endif
                // We need to check if something invalidated the history buffers
                historyValidity *= ValidRayTracingHistory(hdCamera) ? 1.0f : 0.0f;

            // Apply the temporal denoiser
            HDTemporalFilter temporalFilter = GetTemporalFilter();
            temporalFilter.DenoiseBuffer(cmd, hdCamera, m_IndirectDiffuseBuffer0, indirectDiffuseHistoryHF, intermediateBuffer1, singleChannel: false, historyValidity: historyValidity);

            // Apply the first pass of our denoiser
            HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();
            diffuseDenoiser.DenoiseBuffer(cmd, hdCamera, intermediateBuffer1, m_IndirectDiffuseBuffer0, settings.denoiserRadius.value, singleChannel: false, halfResolutionFilter: settings.halfResolutionDenoiser.value);

            // If the second pass is requested, do it otherwise blit
            if (settings.secondDenoiserPass.value)
            {
                // Grab the low frequency history buffer
                RTHandle indirectDiffuseHistoryLF = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);

                temporalFilter.DenoiseBuffer(cmd, hdCamera, m_IndirectDiffuseBuffer0, indirectDiffuseHistoryLF, intermediateBuffer1, singleChannel: false, historyValidity: historyValidity);
                diffuseDenoiser.DenoiseBuffer(cmd, hdCamera, intermediateBuffer1, m_IndirectDiffuseBuffer0, settings.secondDenoiserRadius.value, singleChannel: false, halfResolutionFilter: settings.halfResolutionDenoiser.value);
            }
        }
    }
}
