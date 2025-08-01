using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

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
            ComputeShader rayTracingSubSurfaceCS = rayTracingResources.subSurfaceRayTracingCS;
            ComputeShader deferredRayTracingCS = rayTracingResources.deferredRayTracingCS;

            m_SSSClearTextureKernel = rayTracingSubSurfaceCS.FindKernel("ClearTexture");
            m_RaytracingDiffuseDeferredKernel = deferredRayTracingCS.FindKernel("RaytracingDiffuseDeferred");
            m_CombineSubSurfaceKernel = rayTracingSubSurfaceCS.FindKernel("BlendSubSurfaceData");
            m_CombineSubSurfaceWithGIKernel = rayTracingSubSurfaceCS.FindKernel("BlendSubSurfaceDataWithGI");
        }

        void CleanupSubsurfaceScatteringRT()
        {
        }

        RTHandle RequestRayTracedSSSHistoryTexture(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RayTracedSubSurface)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RayTracedSubSurface, SubSurfaceHistoryBufferAllocatorFunction, 1);
        }

        class TraceRTSSSPassData
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

        TextureHandle TraceRTSSS(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthStencilBuffer, TextureHandle normalBuffer, TextureHandle sssColor, TextureHandle ssgiBuffer, TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddUnsafePass<TraceRTSSSPassData>("Composing the result of RTSSS", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingSSSTrace)))
            {
                // Grab the SSS params
                var settings = hdCamera.volumeStack.GetComponent<SubSurfaceScattering>();
                // Camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.sampleCount = settings.sampleCount.value;

                // Required kernels
                passData.clearTextureKernel = m_SSSClearTextureKernel;
                passData.rtDeferredLightingKernel = m_RaytracingDiffuseDeferredKernel;

                // other required parameters
                passData.rayTracingSubSurfaceRT = rayTracingResources.subSurfaceRayTracingRT;
                passData.rayTracingSubSurfaceCS = rayTracingResources.subSurfaceRayTracingCS;
                passData.deferredRayTracingCS = rayTracingResources.deferredRayTracingCS;
                passData.accelerationStructure = RequestAccelerationStructure(hdCamera);
                passData.lightCluster = RequestLightCluster();
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
                passData.ditheredTextureSet = GetBlueNoiseManager().DitheredTextureSet8SPP();

                passData.depthStencilBuffer = depthStencilBuffer;
                builder.UseTexture(passData.depthStencilBuffer, AccessFlags.Read);
                passData.normalBuffer = normalBuffer;
                builder.UseTexture(passData.normalBuffer, AccessFlags.Read);
                passData.sssColor = sssColor;
                builder.UseTexture(passData.sssColor, AccessFlags.Read);
                passData.intermediateBuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Texture 0" });
                passData.intermediateBuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Texture 1" });
                passData.intermediateBuffer2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Texture 2" });
                passData.intermediateBuffer3 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Texture 3" });
                passData.directionBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Distance buffer" });
                passData.outputBuffer = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Ray Traced SSS" });
                builder.UseTexture(passData.outputBuffer, AccessFlags.Write);

                builder.SetRenderFunc(
                    (TraceRTSSSPassData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        // Evaluate the dispatch parameters
                        int numTilesXHR = (data.texWidth + (s_sssTileSize - 1)) / s_sssTileSize;
                        int numTilesYHR = (data.texHeight + (s_sssTileSize - 1)) / s_sssTileSize;

                        // Clear the integration texture first
                        natCmd.SetComputeTextureParam(data.rayTracingSubSurfaceCS, data.clearTextureKernel, HDShaderIDs._DiffuseLightingTextureRW, data.outputBuffer);
                        natCmd.DispatchCompute(data.rayTracingSubSurfaceCS, data.clearTextureKernel, numTilesXHR, numTilesYHR, data.viewCount);

                        // Define the shader pass to use for the reflection pass
                        natCmd.SetRayTracingShaderPass(data.rayTracingSubSurfaceRT, "SubSurfaceDXR");

                        // Set the acceleration structure for the pass
                        natCmd.SetRayTracingAccelerationStructure(data.rayTracingSubSurfaceRT, HDShaderIDs._RaytracingAccelerationStructureName, data.accelerationStructure);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(natCmd, data.ditheredTextureSet);

                        // For every sample that we need to process
                        for (int sampleIndex = 0; sampleIndex < data.sampleCount; ++sampleIndex)
                        {
                            // Inject the ray generation data
                            data.shaderVariablesRayTracingCB._RaytracingNumSamples = data.sampleCount;
                            data.shaderVariablesRayTracingCB._RaytracingSampleIndex = sampleIndex;
                            data.shaderVariablesRayTracingCB._RayTracingAmbientProbeDimmer = 1.0f;
                            ConstantBuffer.PushGlobal(natCmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                            // Bind the input textures for ray generation
                            natCmd.SetRayTracingTextureParam(data.rayTracingSubSurfaceRT, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            natCmd.SetRayTracingTextureParam(data.rayTracingSubSurfaceRT, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                            natCmd.SetRayTracingTextureParam(data.rayTracingSubSurfaceRT, HDShaderIDs._SSSBufferTexture, data.sssColor);
                            natCmd.SetGlobalTexture(HDShaderIDs._StencilTexture, data.depthStencilBuffer, RenderTextureSubElement.Stencil);

                            // Set the output textures
                            natCmd.SetRayTracingTextureParam(data.rayTracingSubSurfaceRT, HDShaderIDs._ThroughputTextureRW, data.intermediateBuffer0);
                            natCmd.SetRayTracingTextureParam(data.rayTracingSubSurfaceRT, HDShaderIDs._NormalTextureRW, data.intermediateBuffer1);
                            natCmd.SetRayTracingTextureParam(data.rayTracingSubSurfaceRT, HDShaderIDs._PositionTextureRW, data.intermediateBuffer2);
                            natCmd.SetRayTracingTextureParam(data.rayTracingSubSurfaceRT, HDShaderIDs._DiffuseLightingTextureRW, data.intermediateBuffer3);
                            natCmd.SetRayTracingTextureParam(data.rayTracingSubSurfaceRT, HDShaderIDs._DirectionTextureRW, data.directionBuffer);

                            // Run the computation
                            natCmd.DispatchRays(data.rayTracingSubSurfaceRT, m_RayGenSubSurfaceShaderName, (uint)data.texWidth, (uint)data.texHeight, (uint)data.viewCount, null);

                            // Now let's do the deferred shading pass on the samples
                            // Bind the lightLoop data
                            data.lightCluster.BindLightClusterData(natCmd);

                            // Bind the input textures
                            natCmd.SetComputeTextureParam(data.deferredRayTracingCS, data.rtDeferredLightingKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            natCmd.SetComputeTextureParam(data.deferredRayTracingCS, data.rtDeferredLightingKernel, HDShaderIDs._ThroughputTextureRW, data.intermediateBuffer0);
                            natCmd.SetComputeTextureParam(data.deferredRayTracingCS, data.rtDeferredLightingKernel, HDShaderIDs._NormalTextureRW, data.intermediateBuffer1);
                            natCmd.SetComputeTextureParam(data.deferredRayTracingCS, data.rtDeferredLightingKernel, HDShaderIDs._PositionTextureRW, data.intermediateBuffer2);
                            natCmd.SetComputeTextureParam(data.deferredRayTracingCS, data.rtDeferredLightingKernel, HDShaderIDs._DirectionTextureRW, data.directionBuffer);
                            natCmd.SetComputeTextureParam(data.deferredRayTracingCS, data.rtDeferredLightingKernel, HDShaderIDs._DiffuseLightingTextureRW, data.intermediateBuffer3);

                            // Bind the output texture (it is used for accumulation read and write)
                            natCmd.SetComputeTextureParam(data.deferredRayTracingCS, data.rtDeferredLightingKernel, HDShaderIDs._RaytracingLitBufferRW, data.outputBuffer);

                            // Compute the Lighting
                            natCmd.DispatchCompute(data.deferredRayTracingCS, data.rtDeferredLightingKernel, numTilesXHR, numTilesYHR, data.viewCount);
                        }
                    });

                return passData.outputBuffer;
            }
        }

        TextureHandle DenoiseRTSSS(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle rayTracedSSS, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationTexture)
        {
            // Evaluate the history's validity
            float historyValidity = HDRenderPipeline.EvaluateHistoryValidity(hdCamera);

            // Run the temporal denoiser
            TextureHandle historyBuffer = renderGraph.ImportTexture(RequestRayTracedSSSHistoryTexture(hdCamera));
            HDTemporalFilter.TemporalFilterParameters filterParams;
            filterParams.singleChannel = false;
            filterParams.historyValidity = historyValidity;
            filterParams.occluderMotionRejection = false;
            filterParams.receiverMotionRejection = true;
            filterParams.exposureControl = false;
            filterParams.resolutionMultiplier = 1.0f;
            filterParams.historyResolutionMultiplier = 1.0f;

            return GetTemporalFilter().Denoise(renderGraph, hdCamera, filterParams,
                rayTracedSSS, renderGraph.defaultResources.blackTextureXR, historyBuffer,
                depthPyramid, normalBuffer, motionVectorBuffer, historyValidationTexture);
        }

        class ComposeRTSSSPassData
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

            public TextureHandle depthStencilBuffer;
            public TextureHandle sssColor;
            public TextureHandle ssgiBuffer;
            public TextureHandle diffuseLightingBuffer;
            public TextureHandle subsurfaceBuffer;
            public TextureHandle colorBuffer;
        }

        TextureHandle CombineRTSSS(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle rayTracedSSS, TextureHandle depthStencilBuffer, TextureHandle sssColor, TextureHandle ssgiBuffer, TextureHandle diffuseLightingBuffer, TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddUnsafePass<ComposeRTSSSPassData>("Composing the result of RTSSS", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingSSSCompose)))
            {
                // Camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Generation parameters
                passData.validSSGI = GetIndirectDiffuseMode(hdCamera) != IndirectDiffuseMode.Off;

                // Required kernels
                passData.combineSSSKernel = passData.validSSGI ? m_CombineSubSurfaceWithGIKernel : m_CombineSubSurfaceKernel;

                // Other parameters
                passData.rayTracingSubSurfaceCS = rayTracingResources.subSurfaceRayTracingCS;
                passData.combineLightingMat = m_CombineLightingPass;

                passData.depthStencilBuffer = depthStencilBuffer;
                builder.SetRenderAttachmentDepth(depthStencilBuffer, AccessFlags.Read);
                passData.sssColor = sssColor;
                builder.UseTexture(passData.sssColor, AccessFlags.Read);
                if (passData.validSSGI)
                    passData.ssgiBuffer = ssgiBuffer;
                else
                    passData.ssgiBuffer = renderGraph.defaultResources.blackTextureXR;
                builder.UseTexture(passData.ssgiBuffer, AccessFlags.Read);

                passData.diffuseLightingBuffer = diffuseLightingBuffer;
                builder.UseTexture(passData.diffuseLightingBuffer, AccessFlags.Read);
                passData.subsurfaceBuffer = rayTracedSSS;
                builder.UseTexture(passData.subsurfaceBuffer, AccessFlags.Read);
                passData.colorBuffer = colorBuffer;
                builder.UseTexture(passData.colorBuffer, AccessFlags.ReadWrite);

                builder.SetRenderFunc(
                    (ComposeRTSSSPassData data, UnsafeGraphContext ctx) =>
                    {
                        var natCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                        // Evaluate the dispatch parameters
                        int numTilesXHR = (data.texWidth + (s_sssTileSize - 1)) / s_sssTileSize;
                        int numTilesYHR = (data.texHeight + (s_sssTileSize - 1)) / s_sssTileSize;

                        natCmd.SetComputeTextureParam(data.rayTracingSubSurfaceCS, data.combineSSSKernel, HDShaderIDs._SubSurfaceLightingBuffer, data.subsurfaceBuffer);
                        natCmd.SetComputeTextureParam(data.rayTracingSubSurfaceCS, data.combineSSSKernel, HDShaderIDs._DiffuseLightingTextureRW, data.diffuseLightingBuffer);
                        natCmd.SetComputeTextureParam(data.rayTracingSubSurfaceCS, data.combineSSSKernel, HDShaderIDs._SSSBufferTexture, data.sssColor);
                        if (data.validSSGI)
                            natCmd.SetComputeTextureParam(data.rayTracingSubSurfaceCS, data.combineSSSKernel, HDShaderIDs._IndirectDiffuseLightingBuffer, data.ssgiBuffer);
                        natCmd.DispatchCompute(data.rayTracingSubSurfaceCS, data.combineSSSKernel, numTilesXHR, numTilesYHR, data.viewCount);

                        // Combine it with the rest of the lighting
                        data.combineLightingMat.SetTexture(HDShaderIDs._IrradianceSource, data.diffuseLightingBuffer);
                        HDUtils.DrawFullScreen(natCmd, data.combineLightingMat, data.colorBuffer, data.depthStencilBuffer, shaderPassId: 1);
                    });

                return passData.colorBuffer;
            }
        }

        TextureHandle RenderSubsurfaceScatteringRT(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthStencilBuffer, TextureHandle normalBuffer, TextureHandle colorBuffer,
            TextureHandle sssColor, TextureHandle diffuseBuffer, TextureHandle motionVectorsBuffer, TextureHandle historyValidationTexture, TextureHandle ssgiBuffer)
        {
            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.RaytracingSSS)))
            {
                // Trace the signal
                TextureHandle rtsssResult = TraceRTSSS(renderGraph, hdCamera, depthStencilBuffer, normalBuffer, sssColor, ssgiBuffer, colorBuffer);

                // Denoise the result
                rtsssResult = DenoiseRTSSS(renderGraph, hdCamera, rtsssResult, depthStencilBuffer, normalBuffer, motionVectorsBuffer, historyValidationTexture);

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
