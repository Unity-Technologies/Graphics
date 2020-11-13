using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // String values
        const string m_RayGenIndirectDiffuseIntegrationName = "RayGenIntegration";

        // Kernels
        int m_RaytracingIndirectDiffuseFullResKernel;
        int m_RaytracingIndirectDiffuseHalfResKernel;
        int m_IndirectDiffuseUpscaleFullResKernel;
        int m_IndirectDiffuseUpscaleHalfResKernel;
        int m_AdjustIndirectDiffuseWeightKernel;

        void InitRayTracedIndirectDiffuse()
        {
            ComputeShader indirectDiffuseShaderCS = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingCS;

            // Grab all the kernels we shall be using
            m_RaytracingIndirectDiffuseFullResKernel = indirectDiffuseShaderCS.FindKernel("RaytracingIndirectDiffuseFullRes");
            m_RaytracingIndirectDiffuseHalfResKernel = indirectDiffuseShaderCS.FindKernel("RaytracingIndirectDiffuseHalfRes");
            m_IndirectDiffuseUpscaleFullResKernel = indirectDiffuseShaderCS.FindKernel("IndirectDiffuseIntegrationUpscaleFullRes");
            m_IndirectDiffuseUpscaleHalfResKernel = indirectDiffuseShaderCS.FindKernel("IndirectDiffuseIntegrationUpscaleHalfRes");
            m_AdjustIndirectDiffuseWeightKernel = indirectDiffuseShaderCS.FindKernel("AdjustIndirectDiffuseWeight");
        }

        void ReleaseRayTracedIndirectDiffuse()
        {
        }

        static RTHandle IndirectDiffuseHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("{0}_IndirectDiffuseHistoryBuffer{1}", viewName, frameIndex));
        }
        
        void RenderRayTracedIndirectDiffuse(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
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

            // Bind the indirect diffuse texture (for the lighting pass)
            BindIndirectDiffuseTexture(cmd);

            // Bind for debugging
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
            deferredParameters.diffuseLightingOnly = true;

            deferredParameters.halfResolution = !settings.fullResolution;
            deferredParameters.rayCountType = (int)RayCountValues.DiffuseGI_Deferred;

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
            deferredParameters.raytracingCB._RaytracingIntensityClamp = settings.clampValue;
            deferredParameters.raytracingCB._RaytracingPreExposition = 1;
            deferredParameters.raytracingCB._RaytracingIncludeSky = 1;
            deferredParameters.raytracingCB._RaytracingPreExposition = 1;
            deferredParameters.raytracingCB._RaytracingRayMaxLength = settings.rayLength;
            deferredParameters.raytracingCB._RayTracingDiffuseLightingOnly = 1;

            return deferredParameters;
        }

        struct RTIndirectDiffuseDirGenParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Generation parameters
            public bool fullResolution;

            // Additional resources
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public int dirGenKernel;
            public ComputeShader directionGenCS;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
        }

        RTIndirectDiffuseDirGenParameters PrepareRTIndirectDiffuseDirGenParameters(HDCamera hdCamera, GlobalIllumination settings)
        {
            RTIndirectDiffuseDirGenParameters rtidDirGenParams = new RTIndirectDiffuseDirGenParameters();

            // Set the camera parameters
            rtidDirGenParams.texWidth = hdCamera.actualWidth;
            rtidDirGenParams.texHeight = hdCamera.actualHeight;
            rtidDirGenParams.viewCount = hdCamera.viewCount;

            // Set the generation parameters
            rtidDirGenParams.fullResolution = settings.fullResolution;

            // Grab the right kernel
            rtidDirGenParams.directionGenCS = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingCS;
            rtidDirGenParams.dirGenKernel = settings.fullResolution ? m_RaytracingIndirectDiffuseFullResKernel : m_RaytracingIndirectDiffuseHalfResKernel;

            // Grab the additional parameters
            BlueNoise blueNoise = GetBlueNoiseManager();
            rtidDirGenParams.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            rtidDirGenParams.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;

            return rtidDirGenParams;
        }

        struct RTIndirectDiffuseDirGenResources
        {
            // Input buffers
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;

            // Output buffers
            public RTHandle outputBuffer;
        }

        RTIndirectDiffuseDirGenResources PrepareRTIndirectDiffuseDirGenResources(HDCamera hdCamera, RTHandle outputBuffer)
        {
            RTIndirectDiffuseDirGenResources rtidDirGenResources = new RTIndirectDiffuseDirGenResources();

            // Input buffers
            rtidDirGenResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rtidDirGenResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();

            // Output buffers
            rtidDirGenResources.outputBuffer = outputBuffer;

            return rtidDirGenResources;
        }

        static void RTIndirectDiffuseDirGen(CommandBuffer cmd, RTIndirectDiffuseDirGenParameters rtidDirGenParams, RTIndirectDiffuseDirGenResources rtidDirGenResources)
        {
            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, rtidDirGenParams.ditheredTextureSet);

            // Bind all the required textures
            cmd.SetComputeTextureParam(rtidDirGenParams.directionGenCS, rtidDirGenParams.dirGenKernel, HDShaderIDs._DepthTexture, rtidDirGenResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(rtidDirGenParams.directionGenCS, rtidDirGenParams.dirGenKernel, HDShaderIDs._NormalBufferTexture, rtidDirGenResources.normalBuffer);

            // Bind the output buffers
            cmd.SetComputeTextureParam(rtidDirGenParams.directionGenCS, rtidDirGenParams.dirGenKernel, HDShaderIDs._RaytracingDirectionBuffer, rtidDirGenResources.outputBuffer);

            int numTilesXHR, numTilesYHR;
            if (rtidDirGenParams.fullResolution)
            {
                // Evaluate the dispatch parameters
                numTilesXHR = (rtidDirGenParams.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                numTilesYHR = (rtidDirGenParams.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            }
            else
            {
                // Evaluate the dispatch parameters
                numTilesXHR = (rtidDirGenParams.texWidth / 2 + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                numTilesYHR = (rtidDirGenParams.texHeight / 2 + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
            }

            // Compute the directions
            cmd.DispatchCompute(rtidDirGenParams.directionGenCS, rtidDirGenParams.dirGenKernel, numTilesXHR, numTilesYHR, rtidDirGenParams.viewCount);
        }

        struct RTIndirectDiffuseUpscaleParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Generation parameters
            public int upscaleRadius;

            // Additional resources
            public Texture2DArray blueNoiseTexture;
            public Texture2D scramblingTexture;
            public int upscaleKernel;
            public ComputeShader upscaleCS;
        }

        RTIndirectDiffuseUpscaleParameters PrepareRTIndirectDiffuseUpscaleParameters(HDCamera hdCamera, GlobalIllumination settings)
        {
            RTIndirectDiffuseUpscaleParameters rtidUpscaleParams = new RTIndirectDiffuseUpscaleParameters();

            // Set the camera parameters
            rtidUpscaleParams.texWidth = hdCamera.actualWidth;
            rtidUpscaleParams.texHeight = hdCamera.actualHeight;
            rtidUpscaleParams.viewCount = hdCamera.viewCount;

            // Set the generation parameters
            rtidUpscaleParams.upscaleRadius = settings.upscaleRadius;

            // Grab the right kernel
            rtidUpscaleParams.upscaleCS = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingCS;
            rtidUpscaleParams.upscaleKernel = settings.fullResolution ? m_IndirectDiffuseUpscaleFullResKernel : m_IndirectDiffuseUpscaleHalfResKernel;

            // Grab the additional parameters
            BlueNoise blueNoise = GetBlueNoiseManager();
            rtidUpscaleParams.blueNoiseTexture = blueNoise.textureArray16RGB;
            rtidUpscaleParams.scramblingTexture = m_Asset.renderPipelineResources.textures.scramblingTex;

            return rtidUpscaleParams;
        }

        struct RTIndirectDiffuseUpscaleResources
        {
            // Input buffers
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;
            public RTHandle indirectDiffuseBuffer;
            public RTHandle directionBuffer;

            // Output buffers
            public RTHandle outputBuffer;
        }

        RTIndirectDiffuseUpscaleResources PrepareRTIndirectDiffuseUpscaleResources(HDCamera hdCamera, RTHandle indirectDiffuseBuffer, RTHandle directionBuffer, RTHandle outputBuffer)
        {
            RTIndirectDiffuseUpscaleResources rtidUpscaleResources = new RTIndirectDiffuseUpscaleResources();

            // Input buffers
            rtidUpscaleResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            rtidUpscaleResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            rtidUpscaleResources.indirectDiffuseBuffer = indirectDiffuseBuffer;
            rtidUpscaleResources.directionBuffer = directionBuffer;

            // Output buffers
            rtidUpscaleResources.outputBuffer = outputBuffer;

            return rtidUpscaleResources;
        }

        static void RTIndirectDiffuseUpscale(CommandBuffer cmd, RTIndirectDiffuseUpscaleParameters rtidUpscaleParams, RTIndirectDiffuseUpscaleResources rtidUpscaleResources)
        {
            // Inject all the parameters for the compute
            cmd.SetComputeTextureParam(rtidUpscaleParams.upscaleCS, rtidUpscaleParams.upscaleKernel, HDShaderIDs._DepthTexture, rtidUpscaleResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(rtidUpscaleParams.upscaleCS, rtidUpscaleParams.upscaleKernel, HDShaderIDs._NormalBufferTexture, rtidUpscaleResources.normalBuffer);
            cmd.SetComputeTextureParam(rtidUpscaleParams.upscaleCS, rtidUpscaleParams.upscaleKernel, HDShaderIDs._IndirectDiffuseTexture, rtidUpscaleResources.indirectDiffuseBuffer);
            cmd.SetComputeTextureParam(rtidUpscaleParams.upscaleCS, rtidUpscaleParams.upscaleKernel, HDShaderIDs._RaytracingDirectionBuffer, rtidUpscaleResources.directionBuffer);
            cmd.SetComputeTextureParam(rtidUpscaleParams.upscaleCS, rtidUpscaleParams.upscaleKernel, HDShaderIDs._BlueNoiseTexture, rtidUpscaleParams.blueNoiseTexture);
            cmd.SetComputeTextureParam(rtidUpscaleParams.upscaleCS, rtidUpscaleParams.upscaleKernel, HDShaderIDs._ScramblingTexture, rtidUpscaleParams.scramblingTexture);
            cmd.SetComputeIntParam(rtidUpscaleParams.upscaleCS, HDShaderIDs._SpatialFilterRadius, rtidUpscaleParams.upscaleRadius);

            // Output buffer
            cmd.SetComputeTextureParam(rtidUpscaleParams.upscaleCS, rtidUpscaleParams.upscaleKernel, HDShaderIDs._UpscaledIndirectDiffuseTextureRW, rtidUpscaleResources.outputBuffer);

            // Texture dimensions
            int texWidth = rtidUpscaleParams.texWidth;
            int texHeight = rtidUpscaleParams.texHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Compute the texture
            cmd.DispatchCompute(rtidUpscaleParams.upscaleCS, rtidUpscaleParams.upscaleKernel, numTilesXHR, numTilesYHR, rtidUpscaleParams.viewCount);
        }

        struct AdjustRTIDWeightParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Additional resources
            public int adjustWeightKernel;
            public ComputeShader adjustWeightCS;
        }

        AdjustRTIDWeightParameters PrepareAdjustRTIDWeightParametersParameters(HDCamera hdCamera)
        {
            AdjustRTIDWeightParameters parameters = new AdjustRTIDWeightParameters();

            // Set the camera parameters
            parameters.texWidth = hdCamera.actualWidth;
            parameters.texHeight = hdCamera.actualHeight;
            parameters.viewCount = hdCamera.viewCount;

            // Grab the right kernel
            parameters.adjustWeightCS = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingCS;
            parameters.adjustWeightKernel = m_AdjustIndirectDiffuseWeightKernel;

            return parameters;
        }
    
        static void AdjustRTIDWeight(CommandBuffer cmd, AdjustRTIDWeightParameters parameters, RTHandle indirectDiffuseTexture, RTHandle depthPyramid, RTHandle stencilBuffer)
        {
            // Input data
            cmd.SetComputeTextureParam(parameters.adjustWeightCS, parameters.adjustWeightKernel, HDShaderIDs._DepthTexture, depthPyramid);
            cmd.SetComputeTextureParam(parameters.adjustWeightCS, parameters.adjustWeightKernel, HDShaderIDs._StencilTexture, stencilBuffer, 0, RenderTextureSubElement.Stencil);
            cmd.SetComputeIntParams(parameters.adjustWeightCS, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);

            // In/Output buffer
            cmd.SetComputeTextureParam(parameters.adjustWeightCS, parameters.adjustWeightKernel, HDShaderIDs._IndirectDiffuseTextureRW, indirectDiffuseTexture);

            // Texture dimensions
            int texWidth = parameters.texWidth;
            int texHeight = parameters.texHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Compute the texture
            cmd.DispatchCompute(parameters.adjustWeightCS, parameters.adjustWeightKernel, numTilesXHR, numTilesYHR, parameters.viewCount);
        }

        void RenderIndirectDiffusePerformance(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // Fetch the required resources
            var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            // Request the intermediate texture we will be using
            RTHandle directionBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Direction);
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseDirectionGeneration)))
            {
                // Prepare the components for the direction generation
                RTIndirectDiffuseDirGenParameters rtidDirGenParameters = PrepareRTIndirectDiffuseDirGenParameters(hdCamera, settings);
                RTIndirectDiffuseDirGenResources rtidDirGenResousources = PrepareRTIndirectDiffuseDirGenResources(hdCamera, directionBuffer);
                RTIndirectDiffuseDirGen(cmd, rtidDirGenParameters, rtidDirGenResousources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseEvaluation)))
            {
                // Prepare the components for the deferred lighting
                DeferredLightingRTParameters deferredParamters = PrepareIndirectDiffuseDeferredLightingRTParameters(hdCamera);
                DeferredLightingRTResources deferredResources = PrepareDeferredLightingRTResources(hdCamera, directionBuffer, intermediateBuffer1);
                RenderRaytracingDeferredLighting(cmd, deferredParamters, deferredResources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseUpscale)))
            {
                // Upscale the indirect diffuse buffer
                RTIndirectDiffuseUpscaleParameters rtidUpscaleParameters = PrepareRTIndirectDiffuseUpscaleParameters(hdCamera, settings);
                RTIndirectDiffuseUpscaleResources rtidUpscaleResources = PrepareRTIndirectDiffuseUpscaleResources(hdCamera, intermediateBuffer1, directionBuffer, m_IndirectDiffuseBuffer0);
                RTIndirectDiffuseUpscale(cmd, rtidUpscaleParameters, rtidUpscaleResources);
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingFilterIndirectDiffuse)))
            {
                // Denoise if required
                if (settings.denoise)
                {
                    DenoiseIndirectDiffuseBuffer(hdCamera, cmd, settings);
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseAdjustWeight)))
            {
                // Upscale the indirect diffuse buffer
                AdjustRTIDWeightParameters artidParamters = PrepareAdjustRTIDWeightParametersParameters(hdCamera);
                AdjustRTIDWeight(cmd, artidParamters, m_IndirectDiffuseBuffer0, m_SharedRTManager.GetDepthStencilBuffer(), m_SharedRTManager.GetStencilBuffer());
            }
        }

        struct QualityRTIndirectDiffuseParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public float rayLength;
            public int sampleCount;
            public float clampValue;
            public int bounceCount;

            // Other parameters
            public RayTracingShader indirectDiffuseRT;
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;
            public Texture skyTexture;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
        }

        QualityRTIndirectDiffuseParameters PrepareQualityRTIndirectDiffuseParameters(HDCamera hdCamera, GlobalIllumination settings)
        {
            QualityRTIndirectDiffuseParameters qrtidParams = new QualityRTIndirectDiffuseParameters();

            // Set the camera parameters
            qrtidParams.texWidth = hdCamera.actualWidth;
            qrtidParams.texHeight = hdCamera.actualHeight;
            qrtidParams.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            qrtidParams.rayLength = settings.rayLength;
            qrtidParams.sampleCount = settings.sampleCount.value;
            qrtidParams.clampValue = settings.clampValue;
            qrtidParams.bounceCount = settings.bounceCount.value;

            // Grab the additional parameters
            qrtidParams.indirectDiffuseRT = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingRT;
            qrtidParams.accelerationStructure = RequestAccelerationStructure();
            qrtidParams.lightCluster = RequestLightCluster();
            qrtidParams.skyTexture = m_SkyManager.GetSkyReflection(hdCamera);
            qrtidParams.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            BlueNoise blueNoise = GetBlueNoiseManager();
            qrtidParams.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            return qrtidParams;
        }

        struct QualityRTIndirectDiffuseResources
        {
            // Input buffer
            public RTHandle depthBuffer;
            public RTHandle normalBuffer;

            // Debug buffer
            public RTHandle rayCountTexture;

            // Ouput buffer
            public RTHandle outputBuffer;
        }

        QualityRTIndirectDiffuseResources PrepareQualityRTIndirectDiffuseResources(RTHandle outputBuffer)
        {
            QualityRTIndirectDiffuseResources qualityrtidResources = new QualityRTIndirectDiffuseResources();

            // Input buffers
            qualityrtidResources.depthBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            qualityrtidResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();

            // Debug buffer
            RayCountManager rayCountManager = GetRayCountManager();
            qualityrtidResources.rayCountTexture = rayCountManager.GetRayCountTexture();

            // Output buffers
            qualityrtidResources.outputBuffer = outputBuffer;
            return qualityrtidResources;
        }

        static void RenderQualityRayTracedIndirectDiffuse(CommandBuffer cmd, QualityRTIndirectDiffuseParameters qrtidParameters, QualityRTIndirectDiffuseResources qrtidResources)
        {
            // Define the shader pass to use for the indirect diffuse pass
            cmd.SetRayTracingShaderPass(qrtidParameters.indirectDiffuseRT, "IndirectDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(qrtidParameters.indirectDiffuseRT, HDShaderIDs._RaytracingAccelerationStructureName, qrtidParameters.accelerationStructure);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, qrtidParameters.ditheredTextureSet);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(qrtidParameters.indirectDiffuseRT, HDShaderIDs._IndirectDiffuseTextureRW, qrtidResources.outputBuffer);
            cmd.SetRayTracingTextureParam(qrtidParameters.indirectDiffuseRT, HDShaderIDs._DepthTexture, qrtidResources.depthBuffer);
            cmd.SetRayTracingTextureParam(qrtidParameters.indirectDiffuseRT, HDShaderIDs._NormalBufferTexture, qrtidResources.normalBuffer);

            // Set ray count texture
            cmd.SetRayTracingTextureParam(qrtidParameters.indirectDiffuseRT, HDShaderIDs._RayCountTexture, qrtidResources.rayCountTexture);

            // LightLoop data
            qrtidParameters.lightCluster.BindLightClusterData(cmd);

            // Set the data for the ray miss
            cmd.SetRayTracingTextureParam(qrtidParameters.indirectDiffuseRT, HDShaderIDs._SkyTexture, qrtidParameters.skyTexture);

            // Update global constant buffer
            qrtidParameters.shaderVariablesRayTracingCB._RaytracingIntensityClamp = qrtidParameters.clampValue;
            qrtidParameters.shaderVariablesRayTracingCB._RaytracingIncludeSky = 1;
            qrtidParameters.shaderVariablesRayTracingCB._RaytracingRayMaxLength = qrtidParameters.rayLength;
            qrtidParameters.shaderVariablesRayTracingCB._RaytracingNumSamples = qrtidParameters.sampleCount;
            qrtidParameters.shaderVariablesRayTracingCB._RaytracingMaxRecursion = qrtidParameters.bounceCount;
            qrtidParameters.shaderVariablesRayTracingCB._RayTracingDiffuseLightingOnly = 1;
            ConstantBuffer.PushGlobal(cmd, qrtidParameters.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Only use the shader variant that has multi bounce if the bounce count > 1
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", qrtidParameters.bounceCount > 1);

            // Run the computation
            cmd.DispatchRays(qrtidParameters.indirectDiffuseRT, m_RayGenIndirectDiffuseIntegrationName, (uint)qrtidParameters.texWidth, (uint)qrtidParameters.texHeight, (uint)qrtidParameters.viewCount);

            // Disable the keywords we do not need anymore
            CoreUtils.SetKeyword(cmd, "MULTI_BOUNCE_INDIRECT", false);
        }

        void RenderIndirectDiffuseQuality(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseEvaluation)))
            {
                // Evaluate the signal
                QualityRTIndirectDiffuseParameters qrtidParameters = PrepareQualityRTIndirectDiffuseParameters(hdCamera, giSettings);
                QualityRTIndirectDiffuseResources qrtidResources = PrepareQualityRTIndirectDiffuseResources(m_IndirectDiffuseBuffer0);
                RenderQualityRayTracedIndirectDiffuse(cmd, qrtidParameters, qrtidResources);
            }
           
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingFilterIndirectDiffuse)))
            {
                if (giSettings.denoise)
                {
                    DenoiseIndirectDiffuseBuffer(hdCamera, cmd, giSettings);
                }
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseAdjustWeight)))
            {
                // Upscale the indirect diffuse buffer
                AdjustRTIDWeightParameters artidParamters = PrepareAdjustRTIDWeightParametersParameters(hdCamera);
                AdjustRTIDWeight(cmd, artidParamters, m_IndirectDiffuseBuffer0, m_SharedRTManager.GetDepthStencilBuffer(), m_SharedRTManager.GetStencilBuffer());
            }
        }

        void DenoiseIndirectDiffuseBuffer(HDCamera hdCamera, CommandBuffer cmd, GlobalIllumination settings)
        {
            // Grab the high frequency history buffer
            RTHandle indirectDiffuseHistoryHF = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);
            // Request the intermediate textures we will be using
            RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);
            RTHandle validationBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.R0);
            // Evaluate the history validity
            float historyValidity0 = EvaluateIndirectDiffuseHistoryValidity0(hdCamera, settings.fullResolution, true);
            // Grab the temporal denoiser
            HDTemporalFilter temporalFilter = GetTemporalFilter();

            // Temporal denoising
            TemporalFilterParameters tfParameters = temporalFilter.PrepareTemporalFilterParameters(hdCamera, false, historyValidity0);
            TemporalFilterResources tfResources = temporalFilter.PrepareTemporalFilterResources(hdCamera, validationBuffer, m_IndirectDiffuseBuffer0, indirectDiffuseHistoryHF, intermediateBuffer1);
            HDTemporalFilter.DenoiseBuffer(cmd, tfParameters, tfResources);

            // Apply the first pass of our denoiser
            HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();
            DiffuseDenoiserParameters ddParams = diffuseDenoiser.PrepareDiffuseDenoiserParameters(hdCamera, false, settings.denoiserRadius, settings.halfResolutionDenoiser, settings.secondDenoiserPass);
            RTHandle intermediateBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            DiffuseDenoiserResources ddResources = diffuseDenoiser.PrepareDiffuseDenoiserResources(intermediateBuffer1, intermediateBuffer, m_IndirectDiffuseBuffer0);
            HDDiffuseDenoiser.DenoiseBuffer(cmd, ddParams, ddResources);

            // If the second pass is requested, do it otherwise blit
            if (settings.secondDenoiserPass)
            {
                float historyValidity1 = EvaluateIndirectDiffuseHistoryValidity1(hdCamera, settings.fullResolution, true);

                // Grab the low frequency history buffer
                RTHandle indirectDiffuseHistoryLF = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);

                // Run the temporal denoiser
                tfParameters = temporalFilter.PrepareTemporalFilterParameters(hdCamera, false, historyValidity1);
                tfResources = temporalFilter.PrepareTemporalFilterResources(hdCamera, validationBuffer, m_IndirectDiffuseBuffer0, indirectDiffuseHistoryLF, intermediateBuffer1);
                HDTemporalFilter.DenoiseBuffer(cmd, tfParameters, tfResources);

                // Run the spatial denoiser
                ddParams = diffuseDenoiser.PrepareDiffuseDenoiserParameters(hdCamera, false, settings.denoiserRadius * 0.5f, settings.halfResolutionDenoiser, false);
                ddResources = diffuseDenoiser.PrepareDiffuseDenoiserResources(intermediateBuffer1, intermediateBuffer, m_IndirectDiffuseBuffer0);
                HDDiffuseDenoiser.DenoiseBuffer(cmd, ddParams, ddResources);

                PropagateIndirectDiffuseHistoryValidity1(hdCamera, settings.fullResolution, true);
            }

            // Propagate the history
            PropagateIndirectDiffuseHistoryValidity0(hdCamera, settings.fullResolution, true);
        }
    }
}
