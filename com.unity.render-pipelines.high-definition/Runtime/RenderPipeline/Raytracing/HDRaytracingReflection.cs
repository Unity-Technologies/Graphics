using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Tile of the side used to dispatch the various compute shader kernels
        const int rtReflectionsComputeTileSize = 8;

        // String values
        const string m_RayGenIntegrationName = "RayGenIntegration";
        const string m_RayGenIntegrationTransparentName = "RayGenIntegrationTransparent";

        // Kernels
        int m_RaytracingReflectionsFullResKernel;
        int m_RaytracingReflectionsHalfResKernel;
        int m_RaytracingReflectionsTransparentFullResKernel;
        int m_RaytracingReflectionsTransparentHalfResKernel;
        int m_ReflectionIntegrationUpscaleFullResKernel;
        int m_ReflectionIntegrationUpscaleHalfResKernel;

        void InitRayTracedReflections()
        {
            ComputeShader reflectionShaderCS = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingCS;
            ComputeShader reflectionBilateralFilterCS = m_Asset.renderPipelineRayTracingResources.reflectionBilateralFilterCS;

            // Grab all the kernels we shall be using
            m_RaytracingReflectionsFullResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsFullRes");
            m_RaytracingReflectionsHalfResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsHalfRes");
            m_RaytracingReflectionsTransparentFullResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsTransparentFullRes");
            m_RaytracingReflectionsTransparentHalfResKernel = reflectionShaderCS.FindKernel("RaytracingReflectionsTransparentHalfRes");
            m_ReflectionIntegrationUpscaleFullResKernel = reflectionBilateralFilterCS.FindKernel("ReflectionIntegrationUpscaleFullRes");
            m_ReflectionIntegrationUpscaleHalfResKernel = reflectionBilateralFilterCS.FindKernel("ReflectionIntegrationUpscaleHalfRes");
        }

        static RTHandle ReflectionHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("{0}_ReflectionHistoryBuffer{1}", viewName, frameIndex));
        }

        void ReleaseRayTracedReflections()
        {
        }

        void RenderRayTracedReflections(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount, bool transparent = false)
        {
            ScreenSpaceReflection reflectionSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            // Based on what the asset supports, follow the volume or force the right mode.
            if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
            {
                switch (reflectionSettings.mode.value)
                {
                    case RayTracingMode.Performance:
                    {
                        RenderReflectionsPerformance(hdCamera, cmd, outputTexture, renderContext, frameCount, transparent);
                    }
                    break;
                    case RayTracingMode.Quality:
                    {
                        RenderReflectionsQuality(hdCamera, cmd, outputTexture, renderContext, frameCount, transparent);
                    }
                    break;
                }
            }
            else if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality)
            {
                RenderReflectionsQuality(hdCamera, cmd, outputTexture, renderContext, frameCount, transparent);
            }
            else
            {
                RenderReflectionsPerformance(hdCamera, cmd, outputTexture, renderContext, frameCount, transparent);
            }
        }

        struct RTReflectionDirGenParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Generation parameters
            public bool fullResolution;
            public float minSmoothness;

            // Additional resources
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public int dirGenKernel;
            public ComputeShader directionGenCS;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
        }

        RTReflectionDirGenParameters PrepareRTReflectionDirGenParameters(HDCamera hdCamera, bool transparent, ScreenSpaceReflection settings)
        {
            RTReflectionDirGenParameters rtrDirGenParams = new RTReflectionDirGenParameters();

            // Set the camera parameters
            rtrDirGenParams.texWidth = hdCamera.actualWidth;
            rtrDirGenParams.texHeight = hdCamera.actualHeight;
            rtrDirGenParams.viewCount = hdCamera.viewCount;

            // Set the generation parameters
            rtrDirGenParams.fullResolution = settings.fullResolution;
            rtrDirGenParams.minSmoothness = settings.minSmoothness;

            // Grab the right kernel
            rtrDirGenParams.directionGenCS = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingCS;
            if (settings.fullResolution)
            {
                rtrDirGenParams.dirGenKernel = transparent ? m_RaytracingReflectionsTransparentFullResKernel : m_RaytracingReflectionsFullResKernel;
            }
            else
            {
                rtrDirGenParams.dirGenKernel = transparent ? m_RaytracingReflectionsTransparentHalfResKernel : m_RaytracingReflectionsHalfResKernel;
            }

            // Grab the additional parameters
            BlueNoise blueNoise = GetBlueNoiseManager();
            rtrDirGenParams.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            rtrDirGenParams.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;

            return rtrDirGenParams;
        }

        struct RTReflectionDirGenResources
        {
            // Input buffers
            public RTHandle depthBuffer;
            public RTHandle normalBuffer;
            public RTHandle stencilBuffer;
            public RenderTargetIdentifier clearCoatMaskTexture;

            // Output buffers
            public RTHandle outputBuffer;
        }

        RTReflectionDirGenResources PrepareRTReflectionDirGenResources(HDCamera hdCamera, RTHandle outputBuffer)
        {
            RTReflectionDirGenResources rtrDirGenResources = new RTReflectionDirGenResources();

            // Input buffers
            rtrDirGenResources.depthBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rtrDirGenResources.stencilBuffer = m_SharedRTManager.GetStencilBuffer();
            rtrDirGenResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            rtrDirGenResources.clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : TextureXR.GetBlackTexture();

            // Output buffers
            rtrDirGenResources.outputBuffer = outputBuffer;
            return rtrDirGenResources;
        }

        static void RTReflectionDirectionGeneration(CommandBuffer cmd, RTReflectionDirGenParameters rtrDirGenParams, RTReflectionDirGenResources rtrDirGenResources)
        {
            // TODO: check if this is required, i do not think so
            CoreUtils.SetRenderTarget(cmd, rtrDirGenResources.outputBuffer, ClearFlag.Color, clearColor: Color.black);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, rtrDirGenParams.ditheredTextureSet);

            // Bind all the required scalars to the CB
            rtrDirGenParams.shaderVariablesRayTracingCB._RaytracingReflectionMinSmoothness = rtrDirGenParams.minSmoothness;
            ConstantBuffer.PushGlobal(cmd, rtrDirGenParams.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Bind all the required textures
            cmd.SetComputeTextureParam(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, HDShaderIDs._DepthTexture, rtrDirGenResources.depthBuffer);
            cmd.SetComputeTextureParam(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, HDShaderIDs._NormalBufferTexture, rtrDirGenResources.normalBuffer);
            cmd.SetComputeTextureParam(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, HDShaderIDs._SsrClearCoatMaskTexture, rtrDirGenResources.clearCoatMaskTexture);
            cmd.SetComputeIntParam(rtrDirGenParams.directionGenCS, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);
            cmd.SetComputeTextureParam(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, HDShaderIDs._StencilTexture, rtrDirGenResources.stencilBuffer, 0, RenderTextureSubElement.Stencil);

            // Bind the output buffers
            cmd.SetComputeTextureParam(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, HDShaderIDs._RaytracingDirectionBuffer, rtrDirGenResources.outputBuffer);

            int numTilesXHR, numTilesYHR;
            if (rtrDirGenParams.fullResolution)
            {
                // Evaluate the dispatch parameters
                numTilesXHR = (rtrDirGenParams.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                numTilesYHR = (rtrDirGenParams.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            }
            else
            {
                // Evaluate the dispatch parameters
                numTilesXHR = (rtrDirGenParams.texWidth / 2 + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                numTilesYHR = (rtrDirGenParams.texHeight / 2 + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            }

            // Compute the directions
            cmd.DispatchCompute(rtrDirGenParams.directionGenCS, rtrDirGenParams.dirGenKernel, numTilesXHR, numTilesYHR, rtrDirGenParams.viewCount);
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
            deferredParameters.diffuseLightingOnly = false;
            deferredParameters.halfResolution = !settings.fullResolution;
            deferredParameters.rayCountType = (int)RayCountValues.ReflectionDeferred;

            // Camera data
            deferredParameters.width = hdCamera.actualWidth;
            deferredParameters.height = hdCamera.actualHeight;
            deferredParameters.viewCount = hdCamera.viewCount;

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
            }

            // Make a copy of the previous values that were defined in the CB
            deferredParameters.raytracingCB = m_ShaderVariablesRayTracingCB;
            // Override the ones we need to
            deferredParameters.raytracingCB._RaytracingRayMaxLength = settings.rayLength;
            deferredParameters.raytracingCB._RaytracingIntensityClamp = settings.clampValue;
            deferredParameters.raytracingCB._RaytracingIncludeSky = settings.reflectSky.value ? 1 : 0;
            deferredParameters.raytracingCB._RaytracingPreExposition = 0;
            deferredParameters.raytracingCB._RayTracingDiffuseLightingOnly = 0;

            return deferredParameters;
        }

        struct RTReflectionUpscaleParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Denoising parameters
            public int upscaleRadius;
            public bool denoise;
            public int denoiserRadius;

            // Kernels
            public int upscaleKernel;

            // Other parameters
            public Texture2DArray blueNoiseTexture;
            public ComputeShader reflectionFilterCS;
        }

        RTReflectionUpscaleParameters PrepareRTReflectionUpscaleParameters(HDCamera hdCamera, ScreenSpaceReflection settings)
        {
            RTReflectionUpscaleParameters rtrUpscaleParams = new RTReflectionUpscaleParameters();
            // Camera parameters
            rtrUpscaleParams.texWidth = hdCamera.actualWidth;
            rtrUpscaleParams.texHeight = hdCamera.actualHeight;
            rtrUpscaleParams.viewCount = hdCamera.viewCount;

            // De-noising parameters
            rtrUpscaleParams.upscaleRadius = settings.upscaleRadius;
            rtrUpscaleParams.denoise = settings.denoise;
            rtrUpscaleParams.denoiserRadius = settings.denoiserRadius;

            // Kernels
            rtrUpscaleParams.upscaleKernel = settings.fullResolution ? m_ReflectionIntegrationUpscaleFullResKernel : m_ReflectionIntegrationUpscaleHalfResKernel;

            // Other parameters
            rtrUpscaleParams.blueNoiseTexture = GetBlueNoiseManager().textureArray16RGB;
            rtrUpscaleParams.reflectionFilterCS = m_Asset.renderPipelineRayTracingResources.reflectionBilateralFilterCS;
            return rtrUpscaleParams;
        }

        struct RTReflectionUpscaleResources
        {
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;
            public RTHandle lightingTexture;
            public RTHandle hitPointTexture;
            public RTHandle outputTexture;
            public RenderTargetIdentifier clearCoatMaskTexture;
        }

        RTReflectionUpscaleResources PrepareRTReflectionUpscaleResources(HDCamera hdCamera, RTHandle lightingTexture, RTHandle hitPointTexture, RTHandle outputTexture)
        {
            RTReflectionUpscaleResources rtrUpscaleResources = new RTReflectionUpscaleResources();
            rtrUpscaleResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rtrUpscaleResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            rtrUpscaleResources.lightingTexture = lightingTexture;
            rtrUpscaleResources.hitPointTexture = hitPointTexture;
            rtrUpscaleResources.outputTexture = outputTexture;
            rtrUpscaleResources.clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : TextureXR.GetBlackTexture();
            return rtrUpscaleResources;
        }

        static void UpscaleRTReflections(CommandBuffer cmd, RTReflectionUpscaleParameters rtrUpscaleParameters, RTReflectionUpscaleResources rtrUpscaleResources)
        {
            // Inject all the parameters for the compute
            cmd.SetComputeTextureParam(rtrUpscaleParameters.reflectionFilterCS, rtrUpscaleParameters.upscaleKernel, HDShaderIDs._SsrLightingTextureRW, rtrUpscaleResources.lightingTexture);
            cmd.SetComputeTextureParam(rtrUpscaleParameters.reflectionFilterCS, rtrUpscaleParameters.upscaleKernel, HDShaderIDs._SsrHitPointTexture, rtrUpscaleResources.hitPointTexture);
            cmd.SetComputeTextureParam(rtrUpscaleParameters.reflectionFilterCS, rtrUpscaleParameters.upscaleKernel, HDShaderIDs._DepthTexture, rtrUpscaleResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(rtrUpscaleParameters.reflectionFilterCS, rtrUpscaleParameters.upscaleKernel, HDShaderIDs._NormalBufferTexture, rtrUpscaleResources.normalBuffer);
            cmd.SetComputeTextureParam(rtrUpscaleParameters.reflectionFilterCS, rtrUpscaleParameters.upscaleKernel, HDShaderIDs._BlueNoiseTexture, rtrUpscaleParameters.blueNoiseTexture);
            cmd.SetComputeTextureParam(rtrUpscaleParameters.reflectionFilterCS, rtrUpscaleParameters.upscaleKernel, HDShaderIDs._RaytracingReflectionTexture, rtrUpscaleResources.outputTexture);
            cmd.SetComputeIntParam(rtrUpscaleParameters.reflectionFilterCS, HDShaderIDs._SpatialFilterRadius, rtrUpscaleParameters.upscaleRadius);
            cmd.SetComputeIntParam(rtrUpscaleParameters.reflectionFilterCS, HDShaderIDs._RaytracingDenoiseRadius, rtrUpscaleParameters.denoise ? rtrUpscaleParameters.denoiserRadius : 0);
            cmd.SetComputeTextureParam(rtrUpscaleParameters.reflectionFilterCS, rtrUpscaleParameters.upscaleKernel, HDShaderIDs._SsrClearCoatMaskTexture, rtrUpscaleResources.clearCoatMaskTexture);

            // Compute the texture
            int numTilesXHR = (rtrUpscaleParameters.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            int numTilesYHR = (rtrUpscaleParameters.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            cmd.DispatchCompute(rtrUpscaleParameters.reflectionFilterCS, rtrUpscaleParameters.upscaleKernel, numTilesXHR, numTilesYHR, rtrUpscaleParameters.viewCount);
        }

        void RenderReflectionsPerformance(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount, bool transparent)
        {
            // Fetch the required resources
            RTHandle intermediateBuffer0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            // Fetch all the settings
            ScreenSpaceReflection settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            // Generate the signal
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingReflectionDirectionGeneration)))
            {
                // Prepare the components for the direction generation
                RTReflectionDirGenParameters rtrDirGenParameters = PrepareRTReflectionDirGenParameters(hdCamera, transparent, settings);
                RTReflectionDirGenResources rtrDirGenResousources = PrepareRTReflectionDirGenResources(hdCamera, intermediateBuffer1);
                RTReflectionDirectionGeneration(cmd, rtrDirGenParameters, rtrDirGenResousources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingReflectionEvaluation)))
            {
                // Prepare the components for the deferred lighting
                DeferredLightingRTParameters deferredParamters = PrepareReflectionDeferredLightingRTParameters(hdCamera);
                DeferredLightingRTResources deferredResources = PrepareDeferredLightingRTResources(hdCamera, intermediateBuffer1, intermediateBuffer0);
                RenderRaytracingDeferredLighting(cmd, deferredParamters, deferredResources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingReflectionUpscaleGeneration)))
            {
                // Prepare the parameters for the upscale pass
                RTReflectionUpscaleParameters rtrUpscaleParameters = PrepareRTReflectionUpscaleParameters(hdCamera, settings);
                RTReflectionUpscaleResources rtrUpscaleResources = PrepareRTReflectionUpscaleResources(hdCamera, intermediateBuffer0, intermediateBuffer1, outputTexture);
                UpscaleRTReflections(cmd, rtrUpscaleParameters, rtrUpscaleResources);
            }

            // Denoise if required
            if (settings.denoise && !transparent)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingFilterReflection)))
                {
                    // Grab the history buffer
                    RTHandle reflectionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);

                    // Prepare the parameters and the resources
                    HDReflectionDenoiser reflectionDenoiser = GetReflectionDenoiser();
                    ReflectionDenoiserParameters reflDenoiserParameters = reflectionDenoiser.PrepareReflectionDenoiserParameters(hdCamera, EvaluateHistoryValidity(hdCamera), settings.denoiserRadius, false);
                    ReflectionDenoiserResources reflectionDenoiserResources = reflectionDenoiser.PrepareReflectionDenoiserResources(hdCamera, outputTexture, reflectionHistory,
                                                    intermediateBuffer0, intermediateBuffer1);

                    // Denoise
                    HDReflectionDenoiser.DenoiseBuffer(cmd, reflDenoiserParameters, reflectionDenoiserResources);
                }
            }
        }

        struct RTRQualityRenderingParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Reflection evaluation parameters
            public float clampValue;
            public int reflectSky;
            public float rayLength;
            public int sampleCount;
            public int bounceCount;
            public bool transparent;

            // Other parameters
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public Texture skyTexture;
            public RayTracingShader reflectionShader;
        };

        RTRQualityRenderingParameters PrepareRTRQualityRenderingParameters(HDCamera hdCamera, ScreenSpaceReflection settings, bool transparent)
        {
            RTRQualityRenderingParameters rtrQualityRenderingParameters = new RTRQualityRenderingParameters();

            // Camera parameters
            rtrQualityRenderingParameters.texWidth = hdCamera.actualWidth;
            rtrQualityRenderingParameters.texHeight = hdCamera.actualHeight;
            rtrQualityRenderingParameters.viewCount = hdCamera.viewCount;

            // Reflection evaluation parameters
            rtrQualityRenderingParameters.clampValue = settings.clampValue;
            rtrQualityRenderingParameters.reflectSky = settings.reflectSky.value ? 1 : 0;
            rtrQualityRenderingParameters.rayLength = settings.rayLength;
            rtrQualityRenderingParameters.sampleCount = settings.sampleCount.value;
            rtrQualityRenderingParameters.bounceCount = settings.bounceCount.value;
            rtrQualityRenderingParameters.transparent = transparent;

            // Other parameters
            rtrQualityRenderingParameters.accelerationStructure = RequestAccelerationStructure();
            rtrQualityRenderingParameters.lightCluster = RequestLightCluster();
            BlueNoise blueNoise = GetBlueNoiseManager();
            rtrQualityRenderingParameters.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            rtrQualityRenderingParameters.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            rtrQualityRenderingParameters.skyTexture = m_SkyManager.GetSkyReflection(hdCamera);
            rtrQualityRenderingParameters.reflectionShader = m_Asset.renderPipelineRayTracingResources.reflectionRaytracingRT;

            return rtrQualityRenderingParameters;
        }

        struct RTRQualityRenderingResources
        {
            // Input texture
            public RTHandle depthBuffer;
            public RTHandle normalBuffer;
            public RTHandle stencilBuffer;
            public RenderTargetIdentifier clearCoatMaskTexture;

            // Debug texture
            public RTHandle rayCountTexture;

            // Output texture
            public RTHandle outputTexture;
        }

        RTRQualityRenderingResources PrepareRTRQualityRenderingResources(HDCamera hdCamera, RTHandle outputTexture)
        {
            RTRQualityRenderingResources rtrQualityRenderingResources = new RTRQualityRenderingResources();

            // Input texture
            rtrQualityRenderingResources.depthBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rtrQualityRenderingResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            rtrQualityRenderingResources.clearCoatMaskTexture = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? m_GbufferManager.GetBuffersRTI()[2] : TextureXR.GetBlackTexture();
            rtrQualityRenderingResources.stencilBuffer = m_SharedRTManager.GetStencilBuffer();

            // Debug texture
            RayCountManager rayCountManager = GetRayCountManager();
            rtrQualityRenderingResources.rayCountTexture = rayCountManager.GetRayCountTexture();

            // Output texture
            rtrQualityRenderingResources.outputTexture = outputTexture;
            return rtrQualityRenderingResources;
        }

        static void RenderQualityRayTracedReflections(CommandBuffer cmd, RTRQualityRenderingParameters rtrQRenderingParameters, RTRQualityRenderingResources rtrQRenderingResources)
        {
            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(rtrQRenderingParameters.reflectionShader, "IndirectDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(rtrQRenderingParameters.reflectionShader, HDShaderIDs._RaytracingAccelerationStructureName, rtrQRenderingParameters.accelerationStructure);

            // Global reflection parameters
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingIntensityClamp = rtrQRenderingParameters.clampValue;
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingIncludeSky = rtrQRenderingParameters.reflectSky;
            // Inject the ray generation data
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingRayMaxLength = rtrQRenderingParameters.rayLength;
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingNumSamples = rtrQRenderingParameters.sampleCount;
            // Set the number of bounces for reflections
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RaytracingMaxRecursion = rtrQRenderingParameters.bounceCount;
            rtrQRenderingParameters.shaderVariablesRayTracingCB._RayTracingDiffuseLightingOnly = 0;
            ConstantBuffer.PushGlobal(cmd, rtrQRenderingParameters.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, rtrQRenderingParameters.ditheredTextureSet);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._SsrLightingTextureRW, rtrQRenderingResources.outputTexture);
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._DepthTexture, rtrQRenderingResources.depthBuffer);
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._NormalBufferTexture, rtrQRenderingResources.normalBuffer);
            cmd.SetGlobalTexture(HDShaderIDs._StencilTexture, rtrQRenderingResources.stencilBuffer, RenderTextureSubElement.Stencil);
            cmd.SetRayTracingIntParams(rtrQRenderingParameters.reflectionShader, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);

            // Set ray count texture
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._RayCountTexture, rtrQRenderingResources.rayCountTexture);

            // Bind the lightLoop data
            rtrQRenderingParameters.lightCluster.BindLightClusterData(cmd);

            // Evaluate the clear coat mask texture based on the lit shader mode
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._SsrClearCoatMaskTexture, rtrQRenderingResources.clearCoatMaskTexture);

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(rtrQRenderingParameters.reflectionShader, HDShaderIDs._SkyTexture, rtrQRenderingParameters.skyTexture);

            // Only use the shader variant that has multi bounce if the bounce count > 1
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", rtrQRenderingParameters.bounceCount > 1);

            // Run the computation
            cmd.DispatchRays(rtrQRenderingParameters.reflectionShader, rtrQRenderingParameters.transparent ? m_RayGenIntegrationTransparentName : m_RayGenIntegrationName, (uint)rtrQRenderingParameters.texWidth, (uint)rtrQRenderingParameters.texHeight, (uint)rtrQRenderingParameters.viewCount);

            // Disable multi-bounce
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", false);
        }

        void RenderReflectionsQuality(HDCamera hdCamera, CommandBuffer cmd, RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount, bool transparent)
        {
            // Request the buffers we shall be using
            RTHandle intermediateBuffer0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();
            LightCluster lightClusterSettings = hdCamera.volumeStack.GetComponent<LightCluster>();

            // Do the integration
            RTRQualityRenderingParameters rtrQRenderingParameters = PrepareRTRQualityRenderingParameters(hdCamera, settings, transparent);
            RTRQualityRenderingResources rtrQRenderingResources = PrepareRTRQualityRenderingResources(hdCamera, outputTexture);
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingReflectionEvaluation)))
            {
                RenderQualityRayTracedReflections(cmd, rtrQRenderingParameters, rtrQRenderingResources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingFilterReflection)))
            {
                if (settings.denoise && !transparent)
                {
                    // Grab the history buffer
                    RTHandle reflectionHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                        ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, ReflectionHistoryBufferAllocatorFunction, 1);

                    // Prepare the parameters and the resources
                    HDReflectionDenoiser reflectionDenoiser = GetReflectionDenoiser();
                    ReflectionDenoiserParameters reflDenoiserParameters = reflectionDenoiser.PrepareReflectionDenoiserParameters(hdCamera, EvaluateHistoryValidity(hdCamera), settings.denoiserRadius, rtrQRenderingParameters.bounceCount == 1);
                    ReflectionDenoiserResources reflectionDenoiserResources = reflectionDenoiser.PrepareReflectionDenoiserResources(hdCamera, outputTexture, reflectionHistory,
                                                    intermediateBuffer0, intermediateBuffer1);
                    HDReflectionDenoiser.DenoiseBuffer(cmd, reflDenoiserParameters, reflectionDenoiserResources);
                }
            }
        }
    }
}
