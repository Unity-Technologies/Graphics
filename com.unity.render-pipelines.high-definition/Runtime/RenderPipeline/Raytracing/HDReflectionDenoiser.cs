using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDReflectionDenoiser
    {
        ComputeShader m_ReflectionDenoiserCS;
        Texture2D m_ReflectionFilterMapping;
        int s_TemporalAccumulationFullResKernel;
        int s_TemporalAccumulationHalfResKernel;
        int s_TemporalAccumulationFullResMissWeightedKernel;
        int s_TemporalAccumulationHalfResMissWeightedKernel;
        int s_CopyHistoryKernel;
        int s_CopyHistoryMissWeightedKernel;
        int s_BilateralFilterH_FRKernel;
        int s_BilateralFilterV_FRKernel;
        int s_BilateralFilterH_HRKernel;
        int s_BilateralFilterV_HRKernel;
        int s_BilateralFilterHMissWeighted_FRKernel;
        int s_BilateralFilterVMissWeighted_FRKernel;
        int s_BilateralFilterHMissWeighted_HRKernel;
        int s_BilateralFilterVMissWeighted_HRKernel;

        public HDReflectionDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources)
        {
            m_ReflectionDenoiserCS = rpRTResources.reflectionDenoiserCS;
            m_ReflectionFilterMapping = rpRTResources.reflectionFilterMapping;

            // Fetch all the kernels we shall be using
            s_TemporalAccumulationFullResKernel = m_ReflectionDenoiserCS.FindKernel("TemporalAccumulationFullRes");
            s_TemporalAccumulationHalfResKernel = m_ReflectionDenoiserCS.FindKernel("TemporalAccumulationHalfRes");
            s_TemporalAccumulationFullResMissWeightedKernel = m_ReflectionDenoiserCS.FindKernel("TemporalAccumulationFullResMissWeighted");
            s_TemporalAccumulationHalfResMissWeightedKernel = m_ReflectionDenoiserCS.FindKernel("TemporalAccumulationHalfResMissWeighted");
            s_CopyHistoryKernel = m_ReflectionDenoiserCS.FindKernel("CopyHistory");
            s_CopyHistoryMissWeightedKernel = m_ReflectionDenoiserCS.FindKernel("CopyHistoryMissWeighted");
            s_BilateralFilterH_FRKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterH_FR");
            s_BilateralFilterV_FRKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterV_FR");
            s_BilateralFilterH_HRKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterH_HR");
            s_BilateralFilterV_HRKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterV_HR");
            s_BilateralFilterHMissWeighted_FRKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterHMissWeighted_FR");
            s_BilateralFilterVMissWeighted_FRKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterVMissWeighted_FR");
            s_BilateralFilterHMissWeighted_HRKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterHMissWeighted_HR");
            s_BilateralFilterVMissWeighted_HRKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterVMissWeighted_HR");
        }

        public void Release()
        {
        }

        class ReflectionDenoiserPassData
        {
            // Camera Properties
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // De-noising parameters
            public int maxKernelSize;
            public float historyValidity;
            // Current inverse resolution of the history buffer
            public Vector2 historyBufferSize;
            // Resolution at which the effect is rendered (Half the _Screensize if half res)
            public Vector4 currentEffectResolution;
            public float pixelSpreadTangent;
            public int affectSmoothSurfaces;
            public int singleReflectionBounce;
            public int rayMissInWeight;

            // Other parameters
            public ComputeShader reflectionDenoiserCS;
            public int temporalAccumulationKernel;
            public int copyHistoryKernel;
            public int bilateralFilterHKernel;
            public int bilateralFilterVKernel;
            public Texture2D reflectionFilterMapping;

            public TextureHandle depthBuffer;
            public TextureHandle historyDepth;
            public TextureHandle normalBuffer;
            public TextureHandle clearCoatMaskBuffer;
            public TextureHandle motionVectorBuffer;
            public TextureHandle intermediateBuffer0;
            public TextureHandle intermediateBuffer1;
            public TextureHandle historySignal;
            public TextureHandle historySampleCount;
            public TextureHandle noisyToOutputSignal;
        }

        public TextureHandle DenoiseRTR(RenderGraph renderGraph, HDCamera hdCamera, float historyValidity, int maxKernelSize, bool fullResolution, bool singleReflectionBounce, bool affectSmoothSurfaces, bool zeroWeightRayMiss,
            TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle clearCoatMaskBuffer, TextureHandle lightingTexture, RTHandle historyBuffer, RTHandle historySampleCountBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<ReflectionDenoiserPassData>("Denoise ray traced reflections", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingReflectionFilter)))
            {
                builder.EnableAsyncCompute(false);

                // Camera parameters
                passData.texWidth = fullResolution ? hdCamera.actualWidth : (hdCamera.actualWidth / 2);
                passData.texHeight = fullResolution ? hdCamera.actualHeight : (hdCamera.actualHeight / 2);
                passData.viewCount = hdCamera.viewCount;

                // De-noising parameters
                passData.historyValidity = historyValidity;
                passData.maxKernelSize = fullResolution ? maxKernelSize : maxKernelSize / 2;
                passData.historyBufferSize = new Vector2(1.0f / (float)hdCamera.historyRTHandleProperties.currentRenderTargetSize.x, 1.0f / (float)hdCamera.historyRTHandleProperties.currentRenderTargetSize.y);
                passData.currentEffectResolution = new Vector4(passData.texWidth, passData.texHeight, 1.0f / (float)passData.texWidth, 1.0f / (float)passData.texHeight);
                passData.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, passData.texWidth, passData.texHeight);
                passData.affectSmoothSurfaces = affectSmoothSurfaces ? 1 : 0;
                passData.singleReflectionBounce = singleReflectionBounce ? 1 : 0;
                passData.rayMissInWeight = zeroWeightRayMiss ? 1 : 0;

                // Other parameters
                passData.reflectionDenoiserCS = m_ReflectionDenoiserCS;

                passData.temporalAccumulationKernel = fullResolution ? (zeroWeightRayMiss ? s_TemporalAccumulationFullResMissWeightedKernel : s_TemporalAccumulationFullResKernel)
                    : (zeroWeightRayMiss ? s_TemporalAccumulationHalfResMissWeightedKernel : s_TemporalAccumulationHalfResKernel);
                passData.copyHistoryKernel = zeroWeightRayMiss ? s_CopyHistoryMissWeightedKernel : s_CopyHistoryKernel;
                passData.bilateralFilterHKernel = fullResolution ? (zeroWeightRayMiss ? s_BilateralFilterHMissWeighted_FRKernel : s_BilateralFilterH_FRKernel)
                    : (zeroWeightRayMiss ? s_BilateralFilterHMissWeighted_HRKernel : s_BilateralFilterH_HRKernel);
                passData.bilateralFilterVKernel = fullResolution ? (zeroWeightRayMiss ? s_BilateralFilterVMissWeighted_FRKernel : s_BilateralFilterV_FRKernel)
                    : (zeroWeightRayMiss ? s_BilateralFilterVMissWeighted_HRKernel : s_BilateralFilterV_HRKernel);

                passData.reflectionFilterMapping = m_ReflectionFilterMapping;

                passData.depthBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorBuffer = builder.ReadTexture(motionVectorBuffer);
                passData.clearCoatMaskBuffer = builder.ReadTexture(clearCoatMaskBuffer);
                RTHandle depthT = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
                passData.historyDepth = depthT != null ? renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth)) : renderGraph.defaultResources.blackTextureXR;
                passData.intermediateBuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "IntermediateTexture0" });
                passData.intermediateBuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "IntermediateTexture1" });
                passData.historySignal = builder.ReadWriteTexture(renderGraph.ImportTexture(historyBuffer));
                passData.historySampleCount = builder.ReadWriteTexture(renderGraph.ImportTexture(historySampleCountBuffer));
                passData.noisyToOutputSignal = builder.ReadWriteTexture(lightingTexture);

                builder.SetRenderFunc((ReflectionDenoiserPassData data, RenderGraphContext ctx) =>
                {
                    bool bypassBilateralFilter = false; // debug
                    // Evaluate the dispatch parameters
                    int tileSize = 8;
                    int numTilesX = (data.texWidth + (tileSize - 1)) / tileSize;
                    int numTilesY = (data.texHeight + (tileSize - 1)) / tileSize;

                    // Input data
                    ctx.cmd.SetComputeFloatParam(data.reflectionDenoiserCS, HDShaderIDs._HistoryValidity, data.historyValidity);
                    ctx.cmd.SetComputeFloatParam(data.reflectionDenoiserCS, HDShaderIDs._PixelSpreadAngleTangent, data.pixelSpreadTangent);
                    ctx.cmd.SetComputeVectorParam(data.reflectionDenoiserCS, HDShaderIDs._HistoryBufferSize, data.historyBufferSize);
                    ctx.cmd.SetComputeVectorParam(data.reflectionDenoiserCS, HDShaderIDs._CurrentEffectResolution, data.currentEffectResolution);
                    ctx.cmd.SetComputeIntParam(data.reflectionDenoiserCS, HDShaderIDs._AffectSmoothSurfaces, data.affectSmoothSurfaces);
                    ctx.cmd.SetComputeIntParam(data.reflectionDenoiserCS, HDShaderIDs._SingleReflectionBounce, data.singleReflectionBounce);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._DenoiseInputTexture, data.noisyToOutputSignal);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._HistoryDepthTexture, data.historyDepth);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorBuffer);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._HistoryBuffer, data.historySignal);
                    // TODO: may need to invalidate history if switching this
                    if (data.rayMissInWeight == 1)
                        ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._HistorySampleCountTexture, data.historySampleCount);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMaskBuffer);

                    // Output texture
                    if (bypassBilateralFilter)
                        ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._DenoiseOutputTextureRW, data.noisyToOutputSignal);
                    else
                        ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._DenoiseOutputTextureRW, data.intermediateBuffer0);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.temporalAccumulationKernel, HDShaderIDs._SampleCountTextureRW, data.intermediateBuffer1);

                    // Do the temporal accumulation
                    ctx.cmd.DispatchCompute(data.reflectionDenoiserCS, data.temporalAccumulationKernel, numTilesX, numTilesY, data.viewCount);

                    // Copy the accumulated signal into the history buffer
                    if (bypassBilateralFilter)
                        ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.copyHistoryKernel, HDShaderIDs._DenoiseInputTexture, data.noisyToOutputSignal);
                    else
                        ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.copyHistoryKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBuffer0);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.copyHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, data.historySignal);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.copyHistoryKernel, HDShaderIDs._HistorySampleCountTextureRW, data.historySampleCount);
                    // ...always set, #if def guards dont seem to work in compute shader
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.copyHistoryKernel, HDShaderIDs._SampleCountTextureRW, data.intermediateBuffer1);
                    ctx.cmd.DispatchCompute(data.reflectionDenoiserCS, data.copyHistoryKernel, numTilesX, numTilesY, data.viewCount);

                    // Horizontal pass of the bilateral filter
                    ctx.cmd.SetComputeIntParam(data.reflectionDenoiserCS, HDShaderIDs._DenoiserFilterRadius, data.maxKernelSize);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterHKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBuffer0);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterHKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterHKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterHKernel, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMaskBuffer);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterHKernel, HDShaderIDs._DenoiseOutputTextureRW, data.intermediateBuffer1);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterHKernel, HDShaderIDs._ReflectionFilterMapping, data.reflectionFilterMapping);
                    if (!bypassBilateralFilter)
                        ctx.cmd.DispatchCompute(data.reflectionDenoiserCS, data.bilateralFilterHKernel, numTilesX, numTilesY, data.viewCount);

                    // Vertical pass of the bilateral filter
                    ctx.cmd.SetComputeIntParam(data.reflectionDenoiserCS, HDShaderIDs._DenoiserFilterRadius, data.maxKernelSize);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterVKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBuffer1);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterVKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterVKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterVKernel, HDShaderIDs._SsrClearCoatMaskTexture, data.clearCoatMaskBuffer);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterVKernel, HDShaderIDs._DenoiseOutputTextureRW, data.noisyToOutputSignal);
                    ctx.cmd.SetComputeTextureParam(data.reflectionDenoiserCS, data.bilateralFilterVKernel, HDShaderIDs._ReflectionFilterMapping, data.reflectionFilterMapping);
                    if (!bypassBilateralFilter)
                        ctx.cmd.DispatchCompute(data.reflectionDenoiserCS, data.bilateralFilterVKernel, numTilesX, numTilesY, data.viewCount);
                });

                return passData.noisyToOutputSignal;
            }
        }
    }
}
