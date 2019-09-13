using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
#if ENABLE_RAYTRACING
    public partial class HDRenderPipeline
    {
        // Buffers used for the evaluation
        RTHandle m_IDIntermediateBuffer0 = null;
        RTHandle m_IDIntermediateBuffer1 = null;

        // String values
        const string m_RayGenIndirectDiffuseIntegrationName = "RayGenIntegration";
        const string m_RayGenIndirectDiffuseFullResName = "RayGenFullRes";
        const string m_MissIndirectDiffuseName = "MissShaderIndirectDiffuse";
        const string m_ClosestHitIndirectDiffuseName = "ClosestHitMain";

        public void InitRayTracedIndirectDiffuse()
        {
            m_IDIntermediateBuffer0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IndirectDiffuseBuffer");
            m_IDIntermediateBuffer1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IndirectDiffuseDenoiseBuffer");
        }

        public void ReleaseRayTracedIndirectDiffuse()
        {
            RTHandles.Release(m_IDIntermediateBuffer1);
            RTHandles.Release(m_IDIntermediateBuffer0);
        }

        void BindIndirectDiffuseTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._IndirectDiffuseTexture, m_IDIntermediateBuffer0);
        }

        static RTHandle IndirectDiffuseHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("IndirectDiffuseHistoryBuffer{0}", frameIndex));
        }

        public RTHandle GetIndirectDiffuseTexture()
        {
            return m_IDIntermediateBuffer0;
        }

        public bool ValidIndirectDiffuseState(HDCamera hdCamera)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            HDRaytracingEnvironment rtEnvironment = m_RayTracingManager.CurrentEnvironment();
            var settings = VolumeManager.instance.stack.GetComponent<GlobalIllumination>();
            return !(!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) || rtEnvironment == null || !settings.rayTracing.value);
        }

        public void RenderIndirectDiffuse(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // If we are not supposed to evaluate the indirect diffuse term, quit right away
            if (!ValidIndirectDiffuseState(hdCamera))
                return;
            
            RenderPipelineSettings.RaytracingTier currentTier = m_Asset.currentPlatformRenderPipelineSettings.supportedRaytracingTier;
            switch (currentTier)
            {
                case RenderPipelineSettings.RaytracingTier.Tier1:
                    {
                        RenderIndirectDiffuseT1(hdCamera, cmd, renderContext, frameCount);
                    }
                    break;
                case RenderPipelineSettings.RaytracingTier.Tier2:
                    {
                        RenderIndirectDiffuseT2(hdCamera, cmd, renderContext, frameCount);
                    }
                    break;
            }

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
                cmd.SetComputeTextureParam(indirectDiffuseCS, indirectDiffuseKernel, HDShaderIDs._IndirectDiffuseTexture, m_IDIntermediateBuffer0);

                // Bind the output texture
                cmd.SetComputeTextureParam(indirectDiffuseCS, indirectDiffuseKernel, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                cmd.SetComputeTextureParam(indirectDiffuseCS, indirectDiffuseKernel, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesX = (hdCamera.actualWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesY = (hdCamera.actualHeight + (areaTileSize - 1)) / areaTileSize;

                // Add the indirect diffuse to the GBuffer
                cmd.DispatchCompute(indirectDiffuseCS, indirectDiffuseKernel, numTilesX, numTilesY, 1);
            }

            (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, m_IDIntermediateBuffer0, FullScreenDebugMode.IndirectDiffuse);
        }

        DeferredLightingRTParameters PrepareIndirectDiffuseDeferredLightingRTParameters(HDCamera hdCamera, HDRaytracingEnvironment rtEnv)
        {
            DeferredLightingRTParameters deferredParameters = new DeferredLightingRTParameters();

            // Fetch the GI volume component
            var settings = VolumeManager.instance.stack.GetComponent<GlobalIllumination>();

            // Make sure the binning buffer has the right size
            CheckBinningBuffersSize(hdCamera);

            // Generic attributes
            deferredParameters.rayBinning = settings.rayBinning.value;
            deferredParameters.layerMask = rtEnv.indirectDiffuseLayerMask;
            deferredParameters.maxRayLength = settings.rayLength.value;
            deferredParameters.clampValue = settings.clampValue.value;
            deferredParameters.includeSky = true;
            deferredParameters.diffuseLightingOnly = true;
            deferredParameters.halfResolution = false;
            deferredParameters.rtEnv = rtEnv;
            deferredParameters.rayCountFlag = m_RayTracingManager.rayCountManager.RayCountIsEnabled();
            deferredParameters.preExpose = true;

            // Camera data
            deferredParameters.width = hdCamera.actualWidth;
            deferredParameters.height = hdCamera.actualHeight;
            deferredParameters.fov = hdCamera.camera.fieldOfView;


            // Compute buffers
            deferredParameters.rayBinResult = m_RayBinResult;
            deferredParameters.rayBinSizeResult = m_RayBinSizeResult;
            deferredParameters.accelerationStructure = m_RayTracingManager.RequestAccelerationStructure(rtEnv.indirectDiffuseLayerMask);
            deferredParameters.lightCluster = m_RayTracingManager.RequestLightCluster(rtEnv.indirectDiffuseLayerMask);

            // Shaders
            deferredParameters.gBufferRaytracingRT = m_Asset.renderPipelineRayTracingResources.gBufferRaytracingRT;
            deferredParameters.deferredRaytracingCS = m_Asset.renderPipelineRayTracingResources.deferredRaytracingCS;
            deferredParameters.rayBinningCS = m_Asset.renderPipelineRayTracingResources.rayBinningCS;

            return deferredParameters;
        }

        public void RenderIndirectDiffuseT1(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // Fetch the required resources
            HDRaytracingEnvironment rtEnvironment = m_RayTracingManager.CurrentEnvironment();
            var settings = VolumeManager.instance.stack.GetComponent<GlobalIllumination>();
            BlueNoise blueNoise = m_RayTracingManager.GetBlueNoiseManager();

            // Fetch all the settings
            LightCluster lightClusterSettings = VolumeManager.instance.stack.GetComponent<LightCluster>();

            if (settings.deferredMode.value)
            {
                ComputeShader indirectDiffuseCS = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingCS;

                // Fetch the new sample kernel
                int currentKernel = indirectDiffuseCS.FindKernel("RaytracingIndirectDiffuseFullRes");

                // Inject the ray-tracing sampling data
                blueNoise.BindDitheredRNGData8SPP(cmd);
            
                // Bind all the required textures
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

                // Bind all the required scalars
                cmd.SetComputeFloatParam(indirectDiffuseCS, HDShaderIDs._RaytracingIntensityClamp, settings.clampValue.value);

                // Bind the sampling data
                int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)m_FrameCount % 8;
                cmd.SetComputeIntParam(indirectDiffuseCS, HDShaderIDs._RaytracingFrameIndex, frameIndex);

                // Bind the output buffers
                cmd.SetComputeTextureParam(indirectDiffuseCS, currentKernel, HDShaderIDs._RaytracingDirectionBuffer, m_RaytracingDirectionBuffer);

                // Texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;

                // Evaluate the dispatch parameters
                int areaTileSize = 8;
                int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                // Compute the directions
                cmd.DispatchCompute(indirectDiffuseCS, currentKernel, numTilesXHR, numTilesYHR, 1);

                // Prepare the components for the deferred lighting
                DeferredLightingRTParameters deferredParamters = PrepareIndirectDiffuseDeferredLightingRTParameters(hdCamera, rtEnvironment);
                DeferredLightingRTResources deferredResources = PrepareDeferredLightingRTResources(m_RaytracingDirectionBuffer, m_IDIntermediateBuffer0);

                // Evaluate the deferred lighting
                RenderRaytracingDeferredLighting(cmd, deferredParamters, deferredResources);
            }
            else
            {
                RayTracingShader indirectDiffuseRT = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingRT;

                BindRayTracedIndirectDiffuseData(cmd, hdCamera, rtEnvironment, indirectDiffuseRT, settings, lightClusterSettings);

                // Run the computation
                CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", true);
                CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", false);

                // Run the computation
                cmd.DispatchRays(indirectDiffuseRT, m_RayGenIndirectDiffuseFullResName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);

                CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", false);
            }

            using (new ProfilingSample(cmd, "Filter Indirect Diffuse", CustomSamplerId.RaytracingFilterIndirectDiffuse.GetSampler()))
            {
                if (settings.denoise.value)
                {
                    DenoiseIndirectDiffuseBuffer(hdCamera, cmd, settings);
                }
            }
        }

        public void BindRayTracedIndirectDiffuseData(CommandBuffer cmd, HDCamera hdCamera, HDRaytracingEnvironment rtEnvironment, RayTracingShader indirectDiffuseShader, GlobalIllumination settings, LightCluster lightClusterSettings)
        {
            // Grab the acceleration structures and the light cluster to use
            RayTracingAccelerationStructure accelerationStructure = m_RayTracingManager.RequestAccelerationStructure(rtEnvironment.indirectDiffuseLayerMask);
            HDRaytracingLightCluster lightCluster = m_RayTracingManager.RequestLightCluster(rtEnvironment.indirectDiffuseLayerMask);
            BlueNoise blueNoise = m_RayTracingManager.GetBlueNoiseManager();

            // Define the shader pass to use for the indirect diffuse pass
            cmd.SetRayTracingShaderPass(indirectDiffuseShader, "IndirectDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(indirectDiffuseShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            blueNoise.BindDitheredRNGData8SPP(cmd);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtEnvironment.rayBias);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, settings.rayLength.value);
            cmd.SetRayTracingIntParams(indirectDiffuseShader, HDShaderIDs._RaytracingNumSamples, settings.sampleCount.value);
            int frameIndex = hdCamera.IsTAAEnabled() ? hdCamera.taaFrameIndex : (int)m_FrameCount % 8;
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameIndex);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._IndirectDiffuseTextureRW, m_IDIntermediateBuffer0);
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._IndirectDiffuseHitPointTextureRW, m_IDIntermediateBuffer1);
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

            // Set the indirect diffuse parameters
            cmd.SetRayTracingFloatParams(indirectDiffuseShader, HDShaderIDs._RaytracingIntensityClamp, settings.clampValue.value);

            // Set ray count texture
            cmd.SetRayTracingIntParam(indirectDiffuseShader, HDShaderIDs._RayCountEnabled, m_RayTracingManager.rayCountManager.RayCountIsEnabled());
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._RayCountTexture, m_RayTracingManager.rayCountManager.GetRayCountTexture());

            // Compute the pixel spread value
            float pixelSpreadAngle = Mathf.Atan(2.0f * Mathf.Tan(hdCamera.camera.fieldOfView * Mathf.PI / 360.0f) / Mathf.Min(hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.SetRayTracingFloatParam(indirectDiffuseShader, HDShaderIDs._RaytracingPixelSpreadAngle, pixelSpreadAngle);

            // LightLoop data
            lightCluster.BindLightClusterData(cmd);

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(indirectDiffuseShader, HDShaderIDs._SkyTexture, m_SkyManager.skyReflection);

            // Set the number of bounces to 1
            cmd.SetGlobalInt(HDShaderIDs._RaytracingMaxRecursion, settings.bounceCount.value);
        }

        public void RenderIndirectDiffuseT2(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            HDRaytracingEnvironment rtEnvironment = m_RayTracingManager.CurrentEnvironment();
            var settings = VolumeManager.instance.stack.GetComponent<GlobalIllumination>();

            // Shaders that are used
            RayTracingShader indirectDiffuseRT = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingRT;

            var lightClusterSettings = VolumeManager.instance.stack.GetComponent<LightCluster>();

            BindRayTracedIndirectDiffuseData(cmd, hdCamera, rtEnvironment, indirectDiffuseRT, settings, lightClusterSettings);

            // Compute the actual resolution that is needed base on the quality
            int widthResolution = hdCamera.actualWidth;
            int heightResolution = hdCamera.actualHeight;

            // Only use the shader variant that has multi bounce if the bounce count > 1
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", settings.bounceCount.value > 1);
            // Run the computation
            CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", true);

            cmd.DispatchRays(indirectDiffuseRT, m_RayGenIndirectDiffuseIntegrationName, (uint)widthResolution, (uint)heightResolution, 1);

            // Disable the keywords we do not need anymore
            CoreUtils.SetKeyword(cmd, "DIFFUSE_LIGHTING_ONLY", false);
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", false);

            using (new ProfilingSample(cmd, "Filter Indirect Diffuse", CustomSamplerId.RaytracingFilterIndirectDiffuse.GetSampler()))
            {
                if (settings.denoise.value)
                {
                    DenoiseIndirectDiffuseBuffer(hdCamera, cmd, settings);
                }
            }
        }

        public void DenoiseIndirectDiffuseBuffer(HDCamera hdCamera, CommandBuffer cmd, GlobalIllumination settings)
        {
            // Grab the high frequency history buffer
            RTHandle indirectDiffuseHistoryHF = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);

            // Apply the temporal denoiser
            HDTemporalFilter temporalFilter = m_RayTracingManager.GetTemporalFilter();
            temporalFilter.DenoiseBuffer(cmd, hdCamera, m_IDIntermediateBuffer0, indirectDiffuseHistoryHF, m_IDIntermediateBuffer1, singleChannel: false);

            // Apply the first pass of our denoiser
            HDDiffuseDenoiser diffuseDenoiser = m_RayTracingManager.GetDiffuseDenoiser();
            diffuseDenoiser.DenoiseBuffer(cmd, hdCamera, m_IDIntermediateBuffer1, m_IDIntermediateBuffer0, settings.denoiserRadius.value, singleChannel: false, halfResolutionFilter: settings.halfResolutionDenoiser.value);
            
            // If the second pass is requested, do it otherwise blit
            if (settings.secondDenoiserPass.value)
            {
                // Grab the low frequency history buffer
                RTHandle indirectDiffuseHistoryLF = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);

                temporalFilter.DenoiseBuffer(cmd, hdCamera, m_IDIntermediateBuffer0, indirectDiffuseHistoryLF, m_IDIntermediateBuffer1, singleChannel: false);
                diffuseDenoiser.DenoiseBuffer(cmd, hdCamera, m_IDIntermediateBuffer1 , m_IDIntermediateBuffer0, settings.secondDenoiserRadius.value, singleChannel: false, halfResolutionFilter: settings.halfResolutionDenoiser.value);
            }
            else
            {
                HDUtils.BlitCameraTexture(cmd, m_IDIntermediateBuffer1, m_IDIntermediateBuffer0);
            }
        }
    }
#endif
}
