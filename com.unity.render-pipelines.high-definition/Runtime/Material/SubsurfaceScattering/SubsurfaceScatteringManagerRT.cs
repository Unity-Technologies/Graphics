using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // The set of kernels we will be using
        int m_SSSClearTextureKernel;
        int m_RaytracingDiffuseDeferredKernel;
        int m_CombineSubSurfaceKernel;
        int m_CombineSubSurfaceWithGIKernel;

        // Ray gen shader name for ray tracing
        const int s_sssTileSize = 8;
        const string m_RayGenSubSurfaceShaderName = "RayGenSubSurface";

        // History buffer for ray tracing
        static RTHandle SubSurfaceHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("{0}_SubSurfaceHistoryBuffer{1}", viewName, frameIndex));
        }

        void InitializeSubsurfaceScatteringRT()
        {
            ComputeShader rayTracingSubSurfaceCS = m_Asset.renderPipelineRayTracingResources.subSurfaceRayTracingCS;
            ComputeShader deferredRayTracingCS = m_Asset.renderPipelineRayTracingResources.deferredRaytracingCS;

            m_SSSClearTextureKernel = rayTracingSubSurfaceCS.FindKernel("ClearTexture");
            m_RaytracingDiffuseDeferredKernel = deferredRayTracingCS.FindKernel("RaytracingDiffuseDeferred");
            m_CombineSubSurfaceKernel = rayTracingSubSurfaceCS.FindKernel("BlendSubSurfaceData");
            m_CombineSubSurfaceWithGIKernel = rayTracingSubSurfaceCS.FindKernel("BlendSubSurfaceDataWithGI");
        }

        void CleanupSubsurfaceScatteringRT()
        {

        }

        struct SSSRayTracingParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public int sampleCount;

            // Required kernels
            public int clearTextureKernel;
            public int rtDeferredLightingKernel;

            // other required parameters
            public RayTracingShader rayTracingSubSurfaceRT;
            public ComputeShader rayTracingSubSurfaceCS;
            public ComputeShader deferredRayTracingCS;
            public RayTracingAccelerationStructure accelerationStructure;
            public HDRaytracingLightCluster lightCluster;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
        }

        SSSRayTracingParameters PrepareSSSRayTracingParameters(HDCamera hdCamera, SubSurfaceScattering subSurfaceScattering)
        {
            SSSRayTracingParameters sssrtParams = new SSSRayTracingParameters();

            // Camera parameters
            sssrtParams.texWidth = hdCamera.actualWidth;
            sssrtParams.texHeight = hdCamera.actualHeight;
            sssrtParams.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            sssrtParams.sampleCount = subSurfaceScattering.sampleCount.value;

            // Required kernels
            sssrtParams.clearTextureKernel = m_SSSClearTextureKernel;
            sssrtParams.rtDeferredLightingKernel = m_RaytracingDiffuseDeferredKernel;

            // other required parameters
            sssrtParams.rayTracingSubSurfaceRT = m_Asset.renderPipelineRayTracingResources.subSurfaceRayTracingRT;
            sssrtParams.rayTracingSubSurfaceCS = m_Asset.renderPipelineRayTracingResources.subSurfaceRayTracingCS;
            sssrtParams.deferredRayTracingCS = m_Asset.renderPipelineRayTracingResources.deferredRaytracingCS;
            sssrtParams.accelerationStructure = RequestAccelerationStructure();
            sssrtParams.lightCluster = RequestLightCluster();
            sssrtParams.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            BlueNoise blueNoise = GetBlueNoiseManager();
            sssrtParams.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

            return sssrtParams;
        }

        struct SSSRayTracingResources
        {
            // Input buffers
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;
            public RTHandle sssColor;

            // Intermediate buffers
            public RTHandle intermediateBuffer0;
            public RTHandle intermediateBuffer1;
            public RTHandle intermediateBuffer2;
            public RTHandle intermediateBuffer3;
            public RTHandle directionBuffer;

            // Output Buffers
            public RTHandle outputBuffer;
        }

        SSSRayTracingResources PrepareSSSRayTracingResources(RTHandle sssColor,
                                                            RTHandle intermediateBuffer0, RTHandle intermediateBuffer1,
                                                            RTHandle intermediateBuffer2, RTHandle intermediateBuffer3,
                                                            RTHandle directionBuffer, RTHandle outputBuffer)
        {
            SSSRayTracingResources sssrtResources = new SSSRayTracingResources();

            // Input buffers
            sssrtResources.depthStencilBuffer = sharedRTManager.GetDepthStencilBuffer();
            sssrtResources.normalBuffer = sharedRTManager.GetNormalBuffer();
            sssrtResources.sssColor = sssColor;

            // Intermediate buffers
            sssrtResources.intermediateBuffer0 = intermediateBuffer0;
            sssrtResources.intermediateBuffer1 = intermediateBuffer1;
            sssrtResources.intermediateBuffer2 = intermediateBuffer2;
            sssrtResources.intermediateBuffer3 = intermediateBuffer3;
            sssrtResources.directionBuffer = directionBuffer;

            // Output Buffers
            sssrtResources.outputBuffer = outputBuffer;
            return sssrtResources;
        }

        static void ExecuteRTSubsurfaceScattering(CommandBuffer cmd, SSSRayTracingParameters sssrtParams, SSSRayTracingResources sssrtResources)
        {
            // Evaluate the dispatch parameters
            int numTilesXHR = (sssrtParams.texWidth + (s_sssTileSize - 1)) / s_sssTileSize;
            int numTilesYHR = (sssrtParams.texHeight + (s_sssTileSize - 1)) / s_sssTileSize;

            // Clear the integration texture first
            cmd.SetComputeTextureParam(sssrtParams.rayTracingSubSurfaceCS, sssrtParams.clearTextureKernel, HDShaderIDs._DiffuseLightingTextureRW, sssrtResources.outputBuffer);
            cmd.DispatchCompute(sssrtParams.rayTracingSubSurfaceCS, sssrtParams.clearTextureKernel, numTilesXHR, numTilesYHR, sssrtParams.viewCount);

            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(sssrtParams.rayTracingSubSurfaceRT, "SubSurfaceDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(sssrtParams.rayTracingSubSurfaceRT, HDShaderIDs._RaytracingAccelerationStructureName, sssrtParams.accelerationStructure);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, sssrtParams.ditheredTextureSet);

            // For every sample that we need to process
            for (int sampleIndex = 0; sampleIndex < sssrtParams.sampleCount; ++sampleIndex)
            {
                // Inject the ray generation data
                sssrtParams.shaderVariablesRayTracingCB._RaytracingNumSamples = sssrtParams.sampleCount;
                sssrtParams.shaderVariablesRayTracingCB._RaytracingSampleIndex = sampleIndex;
                ConstantBuffer.PushGlobal(cmd, sssrtParams.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                // Bind the input textures for ray generation
                cmd.SetRayTracingTextureParam(sssrtParams.rayTracingSubSurfaceRT, HDShaderIDs._DepthTexture, sssrtResources.depthStencilBuffer);
                cmd.SetRayTracingTextureParam(sssrtParams.rayTracingSubSurfaceRT, HDShaderIDs._NormalBufferTexture, sssrtResources.normalBuffer);
                cmd.SetRayTracingTextureParam(sssrtParams.rayTracingSubSurfaceRT, HDShaderIDs._SSSBufferTexture, sssrtResources.sssColor);
                cmd.SetGlobalTexture(HDShaderIDs._StencilTexture, sssrtResources.depthStencilBuffer, RenderTextureSubElement.Stencil);

                // Set the output textures
                cmd.SetRayTracingTextureParam(sssrtParams.rayTracingSubSurfaceRT, HDShaderIDs._ThroughputTextureRW, sssrtResources.intermediateBuffer0);
                cmd.SetRayTracingTextureParam(sssrtParams.rayTracingSubSurfaceRT, HDShaderIDs._NormalTextureRW, sssrtResources.intermediateBuffer1);
                cmd.SetRayTracingTextureParam(sssrtParams.rayTracingSubSurfaceRT, HDShaderIDs._PositionTextureRW, sssrtResources.intermediateBuffer2);
                cmd.SetRayTracingTextureParam(sssrtParams.rayTracingSubSurfaceRT, HDShaderIDs._DiffuseLightingTextureRW, sssrtResources.intermediateBuffer3);
                cmd.SetRayTracingTextureParam(sssrtParams.rayTracingSubSurfaceRT, HDShaderIDs._DirectionTextureRW, sssrtResources.directionBuffer);

                // Run the computation
                cmd.DispatchRays(sssrtParams.rayTracingSubSurfaceRT, m_RayGenSubSurfaceShaderName, (uint)sssrtParams.texWidth, (uint)sssrtParams.texHeight, (uint)sssrtParams.viewCount);

                // Now let's do the deferred shading pass on the samples
                // Bind the lightLoop data
                sssrtParams.lightCluster.BindLightClusterData(cmd);

                // Bind the input textures
                cmd.SetComputeTextureParam(sssrtParams.deferredRayTracingCS, sssrtParams.rtDeferredLightingKernel, HDShaderIDs._DepthTexture, sssrtResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(sssrtParams.deferredRayTracingCS, sssrtParams.rtDeferredLightingKernel, HDShaderIDs._ThroughputTextureRW, sssrtResources.intermediateBuffer0);
                cmd.SetComputeTextureParam(sssrtParams.deferredRayTracingCS, sssrtParams.rtDeferredLightingKernel, HDShaderIDs._NormalTextureRW, sssrtResources.intermediateBuffer1);
                cmd.SetComputeTextureParam(sssrtParams.deferredRayTracingCS, sssrtParams.rtDeferredLightingKernel, HDShaderIDs._PositionTextureRW, sssrtResources.intermediateBuffer2);
                cmd.SetComputeTextureParam(sssrtParams.deferredRayTracingCS, sssrtParams.rtDeferredLightingKernel, HDShaderIDs._DirectionTextureRW, sssrtResources.directionBuffer);
                cmd.SetComputeTextureParam(sssrtParams.deferredRayTracingCS, sssrtParams.rtDeferredLightingKernel, HDShaderIDs._DiffuseLightingTextureRW, sssrtResources.intermediateBuffer3);

                // Bind the output texture (it is used for accumulation read and write)
                cmd.SetComputeTextureParam(sssrtParams.deferredRayTracingCS, sssrtParams.rtDeferredLightingKernel, HDShaderIDs._RaytracingLitBufferRW, sssrtResources.outputBuffer);

                // Compute the Lighting
                cmd.DispatchCompute(sssrtParams.deferredRayTracingCS, sssrtParams.rtDeferredLightingKernel, numTilesXHR, numTilesYHR, sssrtParams.viewCount);
            }
        }

        struct SSSCombineParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Generation parameters
            public bool validSSGI;

            // Required kernels
            public int combineSSSKernel;

            // other required parameters
            public ComputeShader rayTracingSubSurfaceCS;
            public Material combineLightingMat;
        }

        SSSCombineParameters PrepareSSSCombineParameters(HDCamera hdCamera)
        {
            SSSCombineParameters ssscParams = new SSSCombineParameters();

            // Camera parameters
            ssscParams.texWidth = hdCamera.actualWidth;
            ssscParams.texHeight = hdCamera.actualHeight;
            ssscParams.viewCount = hdCamera.viewCount;

            // Generation parameters
            ssscParams.validSSGI = GetIndirectDiffuseMode(hdCamera) != IndirectDiffuseMode.Off;

            // Required kernels
            ssscParams.combineSSSKernel = ssscParams.validSSGI ? m_CombineSubSurfaceWithGIKernel : m_CombineSubSurfaceKernel;

            // Other parameters
            ssscParams.rayTracingSubSurfaceCS = m_Asset.renderPipelineRayTracingResources.subSurfaceRayTracingCS;
            ssscParams.combineLightingMat = m_CombineLightingPass;

            return ssscParams;
        }

        struct SSSCombineResources
        {
            // Input buffers
            public RTHandle depthStencilBuffer;
            public RTHandle sssColor;
            public RTHandle ssgiBuffer;
            public RTHandle diffuseLightingBuffer;
            public RTHandle subsurfaceBuffer;

            // Output Buffers
            public RTHandle outputColorBuffer;
        }

        SSSCombineResources PrepareSSSCombineResources(RTHandle sssColor, RTHandle colorBufferRT, RTHandle diffuseLightingBuffer, RTHandle sssBuffer, bool validSSGI)
        {
            SSSCombineResources ssscResources = new SSSCombineResources();

            // Input buffers
            ssscResources.depthStencilBuffer = sharedRTManager.GetDepthStencilBuffer();
            ssscResources.sssColor = sssColor;
            ssscResources.ssgiBuffer = validSSGI ? m_IndirectDiffuseBuffer0 : TextureXR.GetBlackTexture();
            ssscResources.diffuseLightingBuffer = diffuseLightingBuffer;
            ssscResources.subsurfaceBuffer = sssBuffer;

            // Output Buffers
            ssscResources.outputColorBuffer = colorBufferRT;

            return ssscResources;
        }

        static void ExecuteCombineSubsurfaceScattering(CommandBuffer cmd, SSSCombineParameters ssscParams, SSSCombineResources ssscResources)
        {
            // Evaluate the dispatch parameters
            int numTilesXHR = (ssscParams.texWidth + (s_sssTileSize - 1)) / s_sssTileSize;
            int numTilesYHR = (ssscParams.texHeight + (s_sssTileSize - 1)) / s_sssTileSize;

            cmd.SetComputeTextureParam(ssscParams.rayTracingSubSurfaceCS, ssscParams.combineSSSKernel, HDShaderIDs._SubSurfaceLightingBuffer, ssscResources.subsurfaceBuffer);
            cmd.SetComputeTextureParam(ssscParams.rayTracingSubSurfaceCS, ssscParams.combineSSSKernel, HDShaderIDs._DiffuseLightingTextureRW, ssscResources.diffuseLightingBuffer);
            cmd.SetComputeTextureParam(ssscParams.rayTracingSubSurfaceCS, ssscParams.combineSSSKernel, HDShaderIDs._SSSBufferTexture, ssscResources.sssColor);
            if (ssscParams.validSSGI)
                cmd.SetComputeTextureParam(ssscParams.rayTracingSubSurfaceCS, ssscParams.combineSSSKernel, HDShaderIDs._IndirectDiffuseLightingBuffer, ssscResources.ssgiBuffer);
            cmd.DispatchCompute(ssscParams.rayTracingSubSurfaceCS, ssscParams.combineSSSKernel, numTilesXHR, numTilesYHR, ssscParams.viewCount);

            // Combine it with the rest of the lighting
            ssscParams.combineLightingMat.SetTexture(HDShaderIDs._IrradianceSource, ssscResources.diffuseLightingBuffer);
            HDUtils.DrawFullScreen(cmd, ssscParams.combineLightingMat, ssscResources.outputColorBuffer, ssscResources.depthStencilBuffer, shaderPassId: 1);
        }

        RTHandle RequestRayTracedSSSHistoryTexture(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RayTracedSubSurface)
                    ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RayTracedSubSurface, SubSurfaceHistoryBufferAllocatorFunction, 1);
        }

        void RenderSubsurfaceScatteringRT(HDCamera hdCamera, CommandBuffer cmd, RTHandle colorBufferRT,
            RTHandle diffuseBufferRT, RTHandle depthStencilBufferRT, RTHandle normalBuffer)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SubsurfaceScattering)))
            {
                // Grab the SSS params
                var settings = hdCamera.volumeStack.GetComponent<SubSurfaceScattering>();

                // Fetch all the intermediate buffers that we need (too much of them to be fair)
                RTHandle intermediateBuffer0 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
                RTHandle intermediateBuffer1 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);
                RTHandle intermediateBuffer2 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA2);
                RTHandle intermediateBuffer3 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA3);
                RTHandle intermediateBuffer4 = GetRayTracingBuffer(InternalRayTracingBuffers.RGBA4);
                RTHandle directionBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.Direction);

                // Evaluate the lighting for the samples that we need to
                SSSRayTracingParameters sssrtParams = PrepareSSSRayTracingParameters(hdCamera, settings);
                SSSRayTracingResources sssrtResources = PrepareSSSRayTracingResources(m_SSSColor,
                                                                                        intermediateBuffer0, intermediateBuffer1,
                                                                                        intermediateBuffer2, intermediateBuffer3, directionBuffer,
                                                                                        intermediateBuffer4);
                ExecuteRTSubsurfaceScattering(cmd, sssrtParams, sssrtResources);

                // Grab the history buffer
                RTHandle subsurfaceHistory = RequestRayTracedSSSHistoryTexture(hdCamera);

                // Check if we need to invalidate the history
                float historyValidity = EvaluateHistoryValidity(hdCamera);

                // Apply temporal filtering to the signal
                HDTemporalFilter temporalFilter = GetTemporalFilter();
                TemporalFilterParameters tfParameters = temporalFilter.PrepareTemporalFilterParameters(hdCamera, false, historyValidity);
                RTHandle validationBuffer = GetRayTracingBuffer(InternalRayTracingBuffers.R0);
                TemporalFilterResources tfResources = temporalFilter.PrepareTemporalFilterResources(hdCamera, validationBuffer, intermediateBuffer4, subsurfaceHistory, intermediateBuffer0);
                HDTemporalFilter.DenoiseBuffer(cmd, tfParameters, tfResources);

                // Combine the result with the rest of the lighting
                SSSCombineParameters ssscParams = PrepareSSSCombineParameters(hdCamera);
                SSSCombineResources ssscResources = PrepareSSSCombineResources(m_SSSColor, colorBufferRT, diffuseBufferRT, intermediateBuffer0, ssscParams.validSSGI);
                ExecuteCombineSubsurfaceScattering(cmd, ssscParams, ssscResources);

                // Push this version of the texture for debug
                PushFullScreenDebugTexture(hdCamera, cmd, diffuseBufferRT, FullScreenDebugMode.RayTracedSubSurface);
            }
        }

        class TraceRTSSSPassData
        {
            public SSSRayTracingParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle sssColor;
            public TextureHandle intermediateBuffer0;
            public TextureHandle intermediateBuffer1;
            public TextureHandle intermediateBuffer2;
            public TextureHandle intermediateBuffer3;
            public TextureHandle directionBuffer;
            public TextureHandle outputBuffer;
        }

        TextureHandle TraceRTSSS(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthStencilBuffer, TextureHandle normalBuffer, TextureHandle sssColor, TextureHandle ssgiBuffer, TextureHandle diffuseLightingBuffer, TextureHandle colorBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering))
            {
                return colorBuffer;
            }

            using (var builder = renderGraph.AddRenderPass<TraceRTSSSPassData>("Composing the result of RTSSS", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingSSSTrace)))
            {
                builder.EnableAsyncCompute(false);

                // Grab the SSS params
                var settings = hdCamera.volumeStack.GetComponent<SubSurfaceScattering>();
                passData.parameters = PrepareSSSRayTracingParameters(hdCamera, settings);
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.sssColor = builder.ReadTexture(sssColor);
                passData.intermediateBuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Texture 0" });
                passData.intermediateBuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Texture 1" });
                passData.intermediateBuffer2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Texture 2" });
                passData.intermediateBuffer3 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Texture 3" });
                passData.directionBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Distance buffer" });
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Ray Traced SSS" }));

                builder.SetRenderFunc(
                (TraceRTSSSPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    SSSRayTracingResources ssstResources = new SSSRayTracingResources();
                    ssstResources.depthStencilBuffer = data.depthStencilBuffer;
                    ssstResources.normalBuffer = data.normalBuffer;
                    ssstResources.sssColor = data.sssColor;
                    ssstResources.intermediateBuffer0 = data.intermediateBuffer0;
                    ssstResources.intermediateBuffer1 = data.intermediateBuffer1;
                    ssstResources.intermediateBuffer2 = data.intermediateBuffer2;
                    ssstResources.intermediateBuffer3 = data.intermediateBuffer3;
                    ssstResources.directionBuffer = data.directionBuffer;
                    ssstResources.outputBuffer = data.outputBuffer;
                    ExecuteRTSubsurfaceScattering(ctx.cmd, data.parameters, ssstResources);
                });

                return passData.outputBuffer;
            }
        }

        TextureHandle DenoiseRTSSS(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle rayTracedSSS, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorBuffer)
        {
            // Evaluate the history's validity
            float historyValidity = HDRenderPipeline.EvaluateHistoryValidity(hdCamera);

            // Run the temporal denoiser
            HDTemporalFilter temporalFilter = GetTemporalFilter();
            TemporalFilterParameters tfParameters = temporalFilter.PrepareTemporalFilterParameters(hdCamera, false, historyValidity);
            TextureHandle historyBuffer = renderGraph.ImportTexture(RequestRayTracedSSSHistoryTexture(hdCamera));
            return temporalFilter.Denoise(renderGraph, hdCamera, tfParameters, rayTracedSSS, historyBuffer, depthPyramid, normalBuffer, motionVectorBuffer);
        }

        class ComposeRTSSSPassData
        {
            public SSSCombineParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle sssColor;
            public TextureHandle ssgiBuffer;
            public TextureHandle diffuseLightingBuffer;
            public TextureHandle subsurfaceBuffer;
            public TextureHandle colorBuffer;
        }

        TextureHandle CombineRTSSS(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle rayTracedSSS, TextureHandle depthStencilBuffer, TextureHandle sssColor, TextureHandle ssgiBuffer, TextureHandle diffuseLightingBuffer, TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<ComposeRTSSSPassData>("Composing the result of RTSSS", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingSSSCompose)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = PrepareSSSCombineParameters(hdCamera);
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthStencilBuffer, DepthAccess.Read);
                passData.sssColor = builder.ReadTexture(sssColor);
                passData.ssgiBuffer = passData.parameters.validSSGI ? builder.ReadTexture(ssgiBuffer) : renderGraph.defaultResources.blackTextureXR;
                passData.diffuseLightingBuffer = builder.ReadTexture(diffuseLightingBuffer);
                passData.subsurfaceBuffer = builder.ReadTexture(rayTracedSSS);
                passData.colorBuffer = builder.ReadWriteTexture(colorBuffer);

                builder.SetRenderFunc(
                (ComposeRTSSSPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    SSSCombineResources ssscResources = new SSSCombineResources();
                    ssscResources.depthStencilBuffer = data.depthStencilBuffer;
                    ssscResources.sssColor = data.sssColor;
                    ssscResources.ssgiBuffer = data.ssgiBuffer;
                    ssscResources.diffuseLightingBuffer = data.diffuseLightingBuffer;
                    ssscResources.subsurfaceBuffer = data.subsurfaceBuffer;
                    ssscResources.outputColorBuffer = data.colorBuffer;
                    ExecuteCombineSubsurfaceScattering(ctx.cmd, data.parameters, ssscResources);
                });

                return passData.colorBuffer;
            }
        }

        TextureHandle RenderSubsurfaceScatteringRT(RenderGraph renderGraph, HDCamera hdCamera,
                                    TextureHandle depthStencilBuffer, TextureHandle normalBuffer, TextureHandle colorBuffer,
                                    TextureHandle sssColor, TextureHandle diffuseBuffer, TextureHandle motionVectorsBuffer, TextureHandle ssgiBuffer)
        {
            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.RaytracingSSS)))
            {
                // Trace the signal
                TextureHandle rtsssResult = TraceRTSSS(renderGraph, hdCamera, depthStencilBuffer, normalBuffer, sssColor, ssgiBuffer, diffuseBuffer, colorBuffer);

                // Denoise the result
                rtsssResult = DenoiseRTSSS(renderGraph, hdCamera, rtsssResult, depthStencilBuffer, normalBuffer, motionVectorsBuffer);

                // Push this version of the texture for debug
                PushFullScreenDebugTexture(renderGraph, rtsssResult, FullScreenDebugMode.RayTracedSubSurface);

                // Compose it
                rtsssResult = CombineRTSSS(renderGraph, hdCamera, rtsssResult, depthStencilBuffer, sssColor, ssgiBuffer, diffuseBuffer, colorBuffer);

                // Return the result
                return rtsssResult;
            }
        }
    }
}
