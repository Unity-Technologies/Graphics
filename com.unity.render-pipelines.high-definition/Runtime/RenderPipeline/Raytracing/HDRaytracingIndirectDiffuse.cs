using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Buffers used for the evaluation
        RTHandle m_IndirectDiffuseBuffer = null;
        RTHandle m_IndirectDiffuseBuffer1 = null;

        // String values
        const string m_RayGenIndirectDiffuseIntegrationName = "RayGenIntegration";
        const string m_RayGenIndirectDiffuseFullResName = "RayGenFullRes";
        const string m_MissIndirectDiffuseName = "MissShaderIndirectDiffuse";
        const string m_ClosestHitIndirectDiffuseName = "ClosestHitMain";

        void InitIndirectDiffuse()
        {
            if (m_Asset.currentPlatformRenderPipelineSettings.supportSSGI)
            {
                m_IndirectDiffuseBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IndirectDiffuseBuffer");
                m_IndirectDiffuseBuffer1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IndirectDiffuseBuffer");
            }
        }

        void ReleaseIndirectDiffuse()
        {
            if (m_IndirectDiffuseBuffer != null)
            {
                RTHandles.Release(m_IndirectDiffuseBuffer1);
                RTHandles.Release(m_IndirectDiffuseBuffer);
            }
        }

        void BindIndirectDiffuseTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._IndirectDiffuseTexture, m_IndirectDiffuseBuffer);
        }

        RTHandle IndirectDiffuseHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("IndirectDiffuseHistoryBuffer{0}", frameIndex));
        }

        RTHandle GetIndirectDiffuseTexture()
        {
            return m_IndirectDiffuseBuffer;
        }

        bool ValidIndirectDiffuseState(HDCamera hdCamera)
        {
            var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            return m_Asset.currentPlatformRenderPipelineSettings.supportSSGI && hdCamera.camera.cameraType != CameraType.Reflection && settings.enable.value;
        }

        bool RayTracedIndirectDiffuseState(HDCamera hdCamera)
        {
            // First thing to check is this effect evaluated?
            var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            return hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)  && settings.rayTracing.value && hdCamera.camera.cameraType != CameraType.Reflection;
        }

        void RenderIndirectDiffuse(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // If we are not supposed to evaluate the indirect diffuse term, quit right away
            if (!ValidIndirectDiffuseState(hdCamera))
                return;

            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            if (RayTracedIndirectDiffuseState(hdCamera))
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
            else
            {
                RenderScreenSpaceIndirectDiffuse(hdCamera, cmd, renderContext, frameCount);
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
            ComputeShader indirectDiffuseCS = m_Asset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS;
            if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
            {
                int indirectDiffuseKernel = indirectDiffuseCS.FindKernel("IndirectDiffuseAccumulation");

                // Bind the source texture
                cmd.SetComputeTextureParam(indirectDiffuseCS, indirectDiffuseKernel, HDShaderIDs._IndirectDiffuseTexture, m_IndirectDiffuseBuffer);

                // Bind the output texture
                cmd.SetComputeTextureParam(indirectDiffuseCS, indirectDiffuseKernel, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                cmd.SetComputeTextureParam(indirectDiffuseCS, indirectDiffuseKernel, HDShaderIDs._GBufferTextureRW[3], m_GbufferManager.GetBuffer(3));
                cmd.SetComputeVectorParam(indirectDiffuseCS, HDShaderIDs._IndirectLightingMultiplier, new Vector4(hdCamera.volumeStack.GetComponent<IndirectLightingController>().indirectDiffuseIntensity.value, 0, 0, 0));

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesX = (hdCamera.actualWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesY = (hdCamera.actualHeight + (areaTileSize - 1)) / areaTileSize;

                // Add the indirect diffuse to the GBuffer
                cmd.DispatchCompute(indirectDiffuseCS, indirectDiffuseKernel, numTilesX, numTilesY, hdCamera.viewCount);
            }

            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, m_IndirectDiffuseBuffer, FullScreenDebugMode.RayTracedGlobalIllumination);
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
                cmd.SetComputeFloatParam(indirectDiffuseCS, HDShaderIDs._RaytracingIntensityClamp, settings.clampValue.value);

                // Bind the sampling data
                int frameIndex = RayTracingFrameIndex(hdCamera);
                cmd.SetComputeIntParam(indirectDiffuseCS, HDShaderIDs._RaytracingFrameIndex, frameIndex);

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
                DeferredLightingRTResources deferredResources = PrepareDeferredLightingRTResources(hdCamera, directionBuffer, m_IndirectDiffuseBuffer);

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
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._IndirectDiffuseTexture, m_IndirectDiffuseBuffer);
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
                HDUtils.BlitCameraTexture(cmd, intermediateBuffer1, m_IndirectDiffuseBuffer);

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
            cmd.SetRayTracingIntParams(indirectDiffuseShader, HDShaderIDs._RaytracingNumSamples, settings.sampleCount.value);
            int frameIndex = RayTracingFrameIndex(hdCamera);
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._IndirectDiffuseTextureRW, outputLightingBuffer);
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._IndirectDiffuseHitPointTextureRW, outputHitPointBuffer);
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

            // Set the indirect diffuse parameters
            cmd.SetRayTracingFloatParams(indirectDiffuseShader, HDShaderIDs._RaytracingIntensityClamp, settings.clampValue.value);

            // Set ray count texture
            RayCountManager rayCountManager = GetRayCountManager();
            cmd.SetRayTracingIntParam(indirectDiffuseShader, HDShaderIDs._RayCountEnabled, rayCountManager.RayCountIsEnabled());
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._RayCountTexture, rayCountManager.GetRayCountTexture());

            // Compute the pixel spread value
            cmd.SetRayTracingFloatParam(indirectDiffuseShader, HDShaderIDs._RaytracingPixelSpreadAngle, GetPixelSpreadAngle(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));

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
            BindRayTracedIndirectDiffuseData(cmd, hdCamera, indirectDiffuseRT, giSettings, lightClusterSettings, rtSettings, m_IndirectDiffuseBuffer, intermediateBuffer1);

            // Compute the actual resolution that is needed base on the quality
            int widthResolution = hdCamera.actualWidth;
            int heightResolution = hdCamera.actualHeight;

            // Only use the shader variant that has multi bounce if the bounce count > 1
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", giSettings.bounceCount.value > 1);
            // Run the computation
            CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", true);

            cmd.DispatchRays(indirectDiffuseRT, m_RayGenIndirectDiffuseIntegrationName, (uint)widthResolution, (uint)heightResolution, (uint)hdCamera.viewCount);

            // Disable the keywords we do not need anymore
            CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", false);
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
            temporalFilter.DenoiseBuffer(cmd, hdCamera, m_IndirectDiffuseBuffer, indirectDiffuseHistoryHF, intermediateBuffer1, singleChannel: false, historyValidity: historyValidity);

            // Apply the first pass of our denoiser
            HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();
            diffuseDenoiser.DenoiseBuffer(cmd, hdCamera, intermediateBuffer1, m_IndirectDiffuseBuffer, settings.denoiserRadius.value, singleChannel: false, halfResolution: settings.halfResolutionDenoiser.value);

            // If the second pass is requested, do it otherwise blit
            if (settings.secondDenoiserPass.value)
            {
                // Grab the low frequency history buffer
                RTHandle indirectDiffuseHistoryLF = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);

                temporalFilter.DenoiseBuffer(cmd, hdCamera, m_IndirectDiffuseBuffer, indirectDiffuseHistoryLF, intermediateBuffer1, singleChannel: false, historyValidity: historyValidity);
                diffuseDenoiser.DenoiseBuffer(cmd, hdCamera, intermediateBuffer1, m_IndirectDiffuseBuffer, settings.secondDenoiserRadius.value, singleChannel: false, halfResolution: settings.halfResolutionDenoiser.value);
            }
        }

        void DenoiseScreenSpaceIndirectDiffuseBuffer(HDCamera hdCamera, CommandBuffer cmd, GlobalIllumination settings, RTHandle colorBuffer)
        {
            RTHandle indirectDiffuseHistory0 = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuse0)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuse0, IndirectDiffuseHistoryBufferAllocatorFunction, 1);

            // Request the intermediate texture we will be using
            RTHandle intermediateBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA4);

            SSGIDenoiser ssgiDenoiser = GetSSGIDenoiser();

            ssgiDenoiser.Denoise(cmd, hdCamera,
                colorBuffer, intermediateBuffer, 
                halfResolution: !settings.fullResolution.value);
            //HDUtils.BlitCameraTexture(cmd, intermediateBuffer, colorBuffer);

            /*
            // Apply the first pass of our denoiser
            HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();
            diffuseDenoiser.DenoiseBuffer2(cmd, hdCamera, colorBuffer, intermediateBuffer, settings.denoiserRadius.value, singleChannel: false, halfRes: !settings.fullResolution.value);
            */
            /*
            if (settings.secondDenoiserPass.value)
            {
                diffuseDenoiser.BilateralFilter(cmd, hdCamera, intermediateBuffer, colorBuffer);
            }
            else
            {
                HDUtils.BlitCameraTexture(cmd, intermediateBuffer, colorBuffer);
            }
            */
            /*
            temporalFilter.DenoiseBufferSH(cmd, hdCamera,
                colorBuffer,
                directionBuffer,
                indirectDiffuseHistory0,
                indirectDiffuseHistory1,
                indirectDiffuseHistory2,
                indirectDiffuseHistory3,
                intermediateBuffer,
                historyValidity: historyValidity);
                */
        }

        void RenderScreenSpaceIndirectDiffuse(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            BlueNoise blueNoise = GetBlueNoiseManager();
            ComputeShader ssGICS = m_Asset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS;
            ComputeShader bilateralUpsampleCS = m_Asset.renderPipelineResources.shaders.bilateralUpsampleCS;

            int texWidth, texHeight;
            if (giSettings.fullResolution.value)
            {
                texWidth = hdCamera.actualWidth;
                texHeight = hdCamera.actualHeight;
            }
            else
            {
                texWidth = hdCamera.actualWidth / 2;
                texHeight = hdCamera.actualHeight / 2;
            }

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingIntegrateIndirectDiffuse)))
            {
                // Fetch the trace kernel
                int currentKernel = ssGICS.FindKernel(giSettings.fullResolution.value ? "TraceGlobalIllumination" : "TraceGlobalIlluminationHalf");

                // Inject the frame index
                int frameIndex = RayTracingFrameIndex(hdCamera);
                cmd.SetComputeIntParam(ssGICS, HDShaderIDs._RaytracingFrameIndex, frameIndex);

                // Inject the ray-tracing sampling data
                blueNoise.BindDitheredRNGData1SPP(cmd);

                var info = m_SharedRTManager.GetDepthBufferMipChainInfo();
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._IndirectDiffuseHitPointTextureRW, m_IndirectDiffuseBuffer1);
                cmd.SetComputeBufferParam(ssGICS, currentKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));
                cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, m_TileAndClusterData.lightList);

                float n = hdCamera.camera.nearClipPlane;
                float f = hdCamera.camera.farClipPlane;
                float thickness = giSettings.depthBufferThickness.value;
                float thicknessScale = 1.0f / (1.0f + thickness);
                float thicknessBias = -n / (f - n) * (thickness * thicknessScale);
                cmd.SetComputeFloatParam(ssGICS, HDShaderIDs._IndirectDiffuseThicknessScale, thicknessScale);
                cmd.SetComputeFloatParam(ssGICS, HDShaderIDs._IndirectDiffuseThicknessBias, thicknessBias);
                cmd.SetComputeIntParam(ssGICS, HDShaderIDs._IndirectDiffuseSteps, giSettings.raySteps.value);
                cmd.SetComputeFloatParam(ssGICS, "_IndirectDiffuseMaximalRadius", giSettings.maximalRadius.value);
                cmd.SetComputeIntParam(ssGICS, "_IndirectDiffuseProbeFallbackFlag", giSettings.useProbeAsFallback.value ? 1 : 0);
                cmd.SetComputeIntParam(ssGICS, "_IndirectDiffuseProbeFallbackBias", giSettings.probeFallbackBias.value);
                cmd.SetComputeIntParam(ssGICS, "_IndirectDiffusePyramidBias", giSettings.pyramidBias.value);
                cmd.SetComputeBufferParam(ssGICS, currentKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));
                cmd.DispatchCompute(ssGICS, currentKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);

                var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);

                currentKernel = ssGICS.FindKernel(giSettings.fullResolution.value ? "ReprojectGlobalIllumination" : "ReprojectGlobalIlluminationHalf");
                var previousColorPyramid = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._IndirectDiffuseHitPointTexture, m_IndirectDiffuseBuffer1);
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._IndirectDiffuseTextureRW, m_IndirectDiffuseBuffer);
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._ColorPyramidTexture, previousColorPyramid != null ? previousColorPyramid : TextureXR.GetBlackTexture());
                Vector4 colorPyramidUVScaleAndLimit = HDUtils.ComputeUvScaleAndLimit(hdCamera.historyRTHandleProperties.previousViewportSize, hdCamera.historyRTHandleProperties.previousRenderTargetSize);
                cmd.SetComputeVectorParam(ssGICS, HDShaderIDs._ColorPyramidUvScaleAndLimitPrevFrame, colorPyramidUVScaleAndLimit);
                cmd.SetComputeFloatParam(ssGICS, HDShaderIDs._RaytracingIntensityClamp, giSettings.clampValue.value);
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._HistoryDepthTexture, historyDepthBuffer != null ? historyDepthBuffer : TextureXR.GetBlackTexture());
                cmd.SetComputeBufferParam(ssGICS, currentKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));
                cmd.DispatchCompute(ssGICS, currentKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);

                if (giSettings.denoise.value)
                {
                    DenoiseScreenSpaceIndirectDiffuseBuffer(hdCamera, cmd, giSettings, m_IndirectDiffuseBuffer);
                }

                if (!giSettings.fullResolution.value)
                {
                    // Re-evaluate the dispatch parameters 
                    numTilesXHR = (hdCamera.actualWidth + (areaTileSize - 1)) / areaTileSize;
                    numTilesYHR = (hdCamera.actualHeight + (areaTileSize - 1)) / areaTileSize;

                    currentKernel = bilateralUpsampleCS.FindKernel("BilateralUpSampleColor");
                    cmd.SetComputeTextureParam(bilateralUpsampleCS, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
                    cmd.SetComputeTextureParam(bilateralUpsampleCS, currentKernel, "_LowResolutionTexture", m_IndirectDiffuseBuffer);
                    cmd.SetComputeTextureParam(bilateralUpsampleCS, currentKernel, "_OutputUpscaledTexture", m_IndirectDiffuseBuffer1);
                    cmd.SetComputeBufferParam(bilateralUpsampleCS, currentKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));
                    cmd.DispatchCompute(bilateralUpsampleCS, currentKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);
                    HDUtils.BlitCameraTexture(cmd, m_IndirectDiffuseBuffer1, m_IndirectDiffuseBuffer);
                }

            }
        }
    }
}
