using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
            deferredParameters.raytracingCB._RaytracingRayMaxLength = settings.rayLength;
            deferredParameters.raytracingCB._RaytracingIntensityClamp = settings.clampValue;
            deferredParameters.raytracingCB._RaytracingIncludeSky = 1;
            deferredParameters.raytracingCB._RaytracingPreExposition = 1;
            deferredParameters.raytracingCB._RayTracingDiffuseLightingOnly = 1;

            return deferredParameters;
        }

        class DirGenRTGIPassData
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

            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle outputBuffer;
        }

        TextureHandle DirGenRTGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination settings, TextureHandle depthPyramid, TextureHandle normalBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<DirGenRTGIPassData>("Generating the rays for RTGI", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseDirectionGeneration)))
            {
                builder.EnableAsyncCompute(false);

                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Set the generation parameters
                passData.fullResolution = settings.fullResolution;

                // Grab the right kernel
                passData.directionGenCS = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingCS;
                passData.dirGenKernel = settings.fullResolution ? m_RaytracingIndirectDiffuseFullResKernel : m_RaytracingIndirectDiffuseHalfResKernel;

                // Grab the additional parameters
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;

                passData.depthStencilBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "GI Ray Directions" }));

                builder.SetRenderFunc(
                    (DirGenRTGIPassData data, RenderGraphContext ctx) =>
                    {
                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.ditheredTextureSet);

                        // Bind all the required textures
                        ctx.cmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                        // Bind the output buffers
                        ctx.cmd.SetComputeTextureParam(data.directionGenCS, data.dirGenKernel, HDShaderIDs._RaytracingDirectionBuffer, data.outputBuffer);

                        int numTilesXHR, numTilesYHR;
                        if (data.fullResolution)
                        {
                            // Evaluate the dispatch parameters
                            numTilesXHR = (data.texWidth + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                            numTilesYHR = (data.texHeight + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                        }
                        else
                        {
                            // Evaluate the dispatch parameters
                            numTilesXHR = (data.texWidth / 2 + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                            numTilesYHR = (data.texHeight / 2 + (rtReflectionsComputeTileSize - 1)) / rtReflectionsComputeTileSize;
                        }

                        // Compute the directions
                        ctx.cmd.DispatchCompute(data.directionGenCS, data.dirGenKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });

                return passData.outputBuffer;
            }
        }

        class UpscaleRTGIPassData
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

            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle indirectDiffuseBuffer;
            public TextureHandle directionBuffer;
            public TextureHandle outputBuffer;
        }

        TextureHandle UpscaleRTGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination settings,
            TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle indirectDiffuseBuffer, TextureHandle directionBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<UpscaleRTGIPassData>("Upscale the RTGI result", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseUpscale)))
            {
                builder.EnableAsyncCompute(false);

                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Set the generation parameters
                passData.upscaleRadius = settings.upscaleRadius;

                // Grab the right kernel
                passData.upscaleCS = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingCS;
                passData.upscaleKernel = settings.fullResolution ? m_IndirectDiffuseUpscaleFullResKernel : m_IndirectDiffuseUpscaleHalfResKernel;

                // Grab the additional parameters
                passData.blueNoiseTexture = GetBlueNoiseManager().textureArray16RGB;
                passData.scramblingTexture = m_Asset.renderPipelineResources.textures.scramblingTex;

                passData.depthBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.indirectDiffuseBuffer = builder.ReadTexture(indirectDiffuseBuffer);
                passData.directionBuffer = builder.ReadTexture(directionBuffer);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Reflection Ray Indirect Diffuse" }));

                builder.SetRenderFunc(
                    (UpscaleRTGIPassData data, RenderGraphContext ctx) =>
                    {
                        // Inject all the parameters for the compute
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._IndirectDiffuseTexture, data.indirectDiffuseBuffer);
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._RaytracingDirectionBuffer, data.directionBuffer);
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._BlueNoiseTexture, data.blueNoiseTexture);
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._ScramblingTexture, data.scramblingTexture);
                        ctx.cmd.SetComputeIntParam(data.upscaleCS, HDShaderIDs._SpatialFilterRadius, data.upscaleRadius);

                        // Output buffer
                        ctx.cmd.SetComputeTextureParam(data.upscaleCS, data.upscaleKernel, HDShaderIDs._UpscaledIndirectDiffuseTextureRW, data.outputBuffer);

                        // Texture dimensions
                        int texWidth = data.texWidth;
                        int texHeight = data.texHeight;

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Compute the texture
                        ctx.cmd.DispatchCompute(data.upscaleCS, data.upscaleKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });

                return passData.outputBuffer;
            }
        }

        class AdjustRTGIWeightPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Additional resources
            public int adjustWeightKernel;
            public ComputeShader adjustWeightCS;

            public TextureHandle depthPyramid;
            public TextureHandle stencilBuffer;
            public TextureHandle indirectDiffuseBuffer;
        }

        TextureHandle AdjustRTGIWeight(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle indirectDiffuseBuffer, TextureHandle depthPyramid, TextureHandle stencilBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<AdjustRTGIWeightPassData>("Adjust the RTGI weight", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseAdjustWeight)))
            {
                builder.EnableAsyncCompute(false);

                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Grab the right kernel
                passData.adjustWeightCS = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingCS;
                passData.adjustWeightKernel = m_AdjustIndirectDiffuseWeightKernel;

                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                passData.indirectDiffuseBuffer = builder.ReadWriteTexture(indirectDiffuseBuffer);

                builder.SetRenderFunc(
                    (AdjustRTGIWeightPassData data, RenderGraphContext ctx) =>
                    {
                        // Input data
                        ctx.cmd.SetComputeTextureParam(data.adjustWeightCS, data.adjustWeightKernel, HDShaderIDs._DepthTexture, data.depthPyramid);
                        ctx.cmd.SetComputeTextureParam(data.adjustWeightCS, data.adjustWeightKernel, HDShaderIDs._StencilTexture, data.stencilBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeIntParams(data.adjustWeightCS, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);

                        // In/Output buffer
                        ctx.cmd.SetComputeTextureParam(data.adjustWeightCS, data.adjustWeightKernel, HDShaderIDs._IndirectDiffuseTextureRW, data.indirectDiffuseBuffer);

                        // Texture dimensions
                        int texWidth = data.texWidth;
                        int texHeight = data.texHeight;

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Compute the texture
                        ctx.cmd.DispatchCompute(data.adjustWeightCS, data.adjustWeightKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });

                return passData.indirectDiffuseBuffer;
            }
        }

        static RTHandle RequestRayTracedIndirectDiffuseHistoryTexture(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF,
                ReflectionHistoryBufferAllocatorFunction, 1);
        }

        TextureHandle RenderIndirectDiffusePerformance(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle historyValidationTexture,
            TextureHandle rayCountTexture, Texture skyTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            // Pointer to the final result
            TextureHandle rtgiResult;

            // Fetch all the settings
            GlobalIllumination settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            TextureHandle directionBuffer = DirGenRTGI(renderGraph, hdCamera, settings, depthPyramid, normalBuffer);

            DeferredLightingRTParameters deferredParamters = PrepareIndirectDiffuseDeferredLightingRTParameters(hdCamera);
            TextureHandle lightingBuffer = DeferredLightingRT(renderGraph, in deferredParamters, directionBuffer, depthPyramid, normalBuffer, skyTexture, rayCountTexture);

            rtgiResult = UpscaleRTGI(renderGraph, hdCamera, settings, depthPyramid, normalBuffer, lightingBuffer, directionBuffer);

            // Denoise if required
            rtgiResult = DenoiseRTGI(renderGraph, hdCamera, rtgiResult, depthPyramid, normalBuffer, motionVectors, historyValidationTexture);

            // Adjust the weight
            rtgiResult = AdjustRTGIWeight(renderGraph, hdCamera, rtgiResult, depthPyramid, stencilBuffer);

            return rtgiResult;
        }

        class TraceQualityRTGIPassData
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

            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle rayCountTexture;
            public TextureHandle outputBuffer;
        }

        TextureHandle QualityRTGI(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle rayCountTexture)
        {
            using (var builder = renderGraph.AddRenderPass<TraceQualityRTGIPassData>("Quality RT Indirect Diffuse", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingIndirectDiffuseEvaluation)))
            {
                builder.EnableAsyncCompute(false);

                var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.rayLength = settings.rayLength;
                passData.sampleCount = settings.sampleCount.value;
                passData.clampValue = settings.clampValue;
                passData.bounceCount = settings.bounceCount.value;

                // Grab the additional parameters
                passData.indirectDiffuseRT = m_Asset.renderPipelineRayTracingResources.indirectDiffuseRaytracingRT;
                passData.accelerationStructure = RequestAccelerationStructure();
                passData.lightCluster = RequestLightCluster();
                passData.skyTexture = m_SkyManager.GetSkyReflection(hdCamera);
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();

                passData.depthBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.rayCountTexture = builder.ReadWriteTexture(rayCountTexture);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Ray Traced Indirect Diffuse" }));

                builder.SetRenderFunc(
                    (TraceQualityRTGIPassData data, RenderGraphContext ctx) =>
                    {
                        // Define the shader pass to use for the indirect diffuse pass
                        ctx.cmd.SetRayTracingShaderPass(data.indirectDiffuseRT, "IndirectDXR");

                        // Set the acceleration structure for the pass
                        ctx.cmd.SetRayTracingAccelerationStructure(data.indirectDiffuseRT, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.ditheredTextureSet);

                        // Set the data for the ray generation
                        ctx.cmd.SetRayTracingTextureParam(data.indirectDiffuseRT, HDShaderIDs._IndirectDiffuseTextureRW, data.outputBuffer);
                        ctx.cmd.SetRayTracingTextureParam(data.indirectDiffuseRT, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetRayTracingTextureParam(data.indirectDiffuseRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);

                        // Set ray count texture
                        ctx.cmd.SetRayTracingTextureParam(data.indirectDiffuseRT, HDShaderIDs._RayCountTexture, data.rayCountTexture);

                        // LightLoop data
                        data.lightCluster.BindLightClusterData(ctx.cmd);

                        // Set the data for the ray miss
                        ctx.cmd.SetRayTracingTextureParam(data.indirectDiffuseRT, HDShaderIDs._SkyTexture, data.skyTexture);

                        // Update global constant buffer
                        data.shaderVariablesRayTracingCB._RaytracingIntensityClamp = data.clampValue;
                        data.shaderVariablesRayTracingCB._RaytracingIncludeSky = 1;
                        data.shaderVariablesRayTracingCB._RaytracingRayMaxLength = data.rayLength;
                        data.shaderVariablesRayTracingCB._RaytracingNumSamples = data.sampleCount;
                        data.shaderVariablesRayTracingCB._RaytracingMaxRecursion = data.bounceCount;
                        data.shaderVariablesRayTracingCB._RayTracingDiffuseLightingOnly = 1;
                        ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Only use the shader variant that has multi bounce if the bounce count > 1
                        CoreUtils.SetKeyword(ctx.cmd, "MULTI_BOUNCE_INDIRECT", data.bounceCount > 1);

                        // Run the computation
                        ctx.cmd.DispatchRays(data.indirectDiffuseRT, m_RayGenIndirectDiffuseIntegrationName, (uint)data.texWidth, (uint)data.texHeight, (uint)data.viewCount);

                        // Disable the keywords we do not need anymore
                        CoreUtils.SetKeyword(ctx.cmd, "MULTI_BOUNCE_INDIRECT", false);
                    });

                return passData.outputBuffer;
            }
        }

        TextureHandle RenderIndirectDiffuseQuality(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle historyValidationTexture,
            TextureHandle rayCountTexture, Texture skyTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            // Evaluate the signal
            TextureHandle  rtgiResult = QualityRTGI(renderGraph, hdCamera, depthPyramid, normalBuffer, rayCountTexture);

            // Denoise if required
            rtgiResult = DenoiseRTGI(renderGraph, hdCamera, rtgiResult, depthPyramid, normalBuffer, motionVectors, historyValidationTexture);

            // Adjust the weight
            rtgiResult = AdjustRTGIWeight(renderGraph, hdCamera, rtgiResult, depthPyramid, stencilBuffer);

            return rtgiResult;
        }

        static RTHandle RequestIndirectDiffuseHistoryTextureHF(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF,
                IndirectDiffuseHistoryBufferAllocatorFunction, 1);
        }

        static RTHandle RequestIndirectDiffuseHistoryTextureLF(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF,
                IndirectDiffuseHistoryBufferAllocatorFunction, 1);
        }

        TextureHandle DenoiseRTGI(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle rtGIBuffer, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationTexture)
        {
            var giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            if (giSettings.denoise)
            {
                // Evaluate the history's validity
                float historyValidity0 = EvaluateIndirectDiffuseHistoryValidity0(hdCamera, giSettings.fullResolution, true);

                HDTemporalFilter temporalFilter = GetTemporalFilter();
                HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();

                // Run the temporal denoiser
                TextureHandle historyBufferHF = renderGraph.ImportTexture(RequestIndirectDiffuseHistoryTextureHF(hdCamera));
                TextureHandle denoisedRTGI = temporalFilter.Denoise(renderGraph, hdCamera, singleChannel: false, historyValidity0, rtGIBuffer, renderGraph.defaultResources.blackTextureXR, historyBufferHF, depthPyramid, normalBuffer, motionVectorBuffer, historyValidationTexture);

                // Apply the diffuse denoiser
                rtGIBuffer = diffuseDenoiser.Denoise(renderGraph, hdCamera, singleChannel: false, kernelSize: giSettings.denoiserRadius, halfResolutionFilter: giSettings.halfResolutionDenoiser, jitterFilter: giSettings.secondDenoiserPass, denoisedRTGI, depthPyramid, normalBuffer, rtGIBuffer);

                // If the second pass is requested, do it otherwise blit
                if (giSettings.secondDenoiserPass)
                {
                    float historyValidity1 = EvaluateIndirectDiffuseHistoryValidity1(hdCamera, giSettings.fullResolution, true);

                    // Run the temporal denoiser
                    TextureHandle historyBufferLF = renderGraph.ImportTexture(RequestIndirectDiffuseHistoryTextureLF(hdCamera));
                    denoisedRTGI = temporalFilter.Denoise(renderGraph, hdCamera, singleChannel: false, historyValidity1, rtGIBuffer, renderGraph.defaultResources.blackTextureXR, historyBufferLF, depthPyramid, normalBuffer, motionVectorBuffer, historyValidationTexture);

                    // Apply the diffuse denoiser
                    rtGIBuffer = diffuseDenoiser.Denoise(renderGraph, hdCamera, singleChannel: false, kernelSize: giSettings.denoiserRadius * 0.5f, halfResolutionFilter: giSettings.halfResolutionDenoiser, jitterFilter: false, denoisedRTGI, depthPyramid, normalBuffer, rtGIBuffer);

                    // Propagate the history validity for the second buffer
                    PropagateIndirectDiffuseHistoryValidity1(hdCamera, giSettings.fullResolution, true);
                }

                // Propagate the history validity for the first buffer
                PropagateIndirectDiffuseHistoryValidity0(hdCamera, giSettings.fullResolution, true);

                return rtGIBuffer;
            }
            else
                return rtGIBuffer;
        }

        TextureHandle RenderRayTracedIndirectDiffuse(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle historyValidationTexture,
            Texture skyTexture, TextureHandle rayCountTexture,
            ShaderVariablesRaytracing shaderVariablesRaytracing)
        {
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            TextureHandle rtreflResult;
            bool qualityMode = false;

            // Based on what the asset supports, follow the volume or force the right mode.
            if (m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
                qualityMode = giSettings.mode.value == RayTracingMode.Quality;
            else
                qualityMode = m_Asset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality;


            if (qualityMode)
                rtreflResult = RenderIndirectDiffuseQuality(renderGraph, hdCamera,
                    depthPyramid, stencilBuffer, normalBuffer, motionVectors, historyValidationTexture,
                    rayCountTexture, skyTexture,
                    shaderVariablesRaytracing);
            else
                rtreflResult = RenderIndirectDiffusePerformance(renderGraph, hdCamera,
                    depthPyramid, stencilBuffer, normalBuffer, motionVectors, historyValidationTexture,
                    rayCountTexture, skyTexture,
                    shaderVariablesRaytracing);

            return rtreflResult;
        }
    }
}
