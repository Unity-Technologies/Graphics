using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct ReflectionDenoiserParameters
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
    }

    internal struct ReflectionDenoiserResources
    {
        // Input buffer
        public RTHandle depthBuffer;
        public RTHandle normalBuffer;
        public RenderTargetIdentifier clearCoatMaskBuffer;
        public RTHandle motionVectorBuffer;
        public RTHandle historyDepth;

        // Intermediate textures
        public RTHandle intermediateBuffer0;
        public RTHandle intermediateBuffer1;

        // Output buffers
        public RTHandle historySignal;
        public RTHandle historySampleCount;
        public RTHandle noisyToOutputSignal;
    }

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

        internal ReflectionDenoiserParameters PrepareReflectionDenoiserParameters(HDCamera hdCamera, float historyValidity, int maxKernelSize, bool fullResolution, bool singleReflectionBounce, bool affectSmoothSurfaces, bool zeroWeightRayMiss)
        {
            ReflectionDenoiserParameters reflDenoiserParams = new ReflectionDenoiserParameters();
            // Camera parameters
            reflDenoiserParams.texWidth = fullResolution ? hdCamera.actualWidth : (hdCamera.actualWidth / 2);
            reflDenoiserParams.texHeight = fullResolution ? hdCamera.actualHeight : (hdCamera.actualHeight / 2);
            reflDenoiserParams.viewCount = hdCamera.viewCount;

            // De-noising parameters
            reflDenoiserParams.historyValidity = historyValidity;
            reflDenoiserParams.maxKernelSize = fullResolution ? maxKernelSize : maxKernelSize / 2;
            reflDenoiserParams.historyBufferSize = new Vector2(1.0f / (float)hdCamera.historyRTHandleProperties.currentRenderTargetSize.x, 1.0f / (float)hdCamera.historyRTHandleProperties.currentRenderTargetSize.y);
            reflDenoiserParams.currentEffectResolution = new Vector4(reflDenoiserParams.texWidth, reflDenoiserParams.texHeight, 1.0f / (float)reflDenoiserParams.texWidth, 1.0f / (float)reflDenoiserParams.texHeight);
            reflDenoiserParams.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, reflDenoiserParams.texWidth, reflDenoiserParams.texHeight);
            reflDenoiserParams.affectSmoothSurfaces = affectSmoothSurfaces ? 1 : 0;
            reflDenoiserParams.singleReflectionBounce = singleReflectionBounce ? 1 : 0;
            reflDenoiserParams.rayMissInWeight = zeroWeightRayMiss ? 1 : 0;

            // Other parameters
            reflDenoiserParams.reflectionDenoiserCS = m_ReflectionDenoiserCS;
            reflDenoiserParams.temporalAccumulationKernel = fullResolution ? (zeroWeightRayMiss ? s_TemporalAccumulationFullResMissWeightedKernel : s_TemporalAccumulationFullResKernel)
                : (zeroWeightRayMiss ? s_TemporalAccumulationHalfResMissWeightedKernel : s_TemporalAccumulationHalfResKernel);
            reflDenoiserParams.copyHistoryKernel = zeroWeightRayMiss ? s_CopyHistoryMissWeightedKernel : s_CopyHistoryKernel;
            reflDenoiserParams.bilateralFilterHKernel = fullResolution ? (zeroWeightRayMiss ? s_BilateralFilterHMissWeighted_FRKernel : s_BilateralFilterH_FRKernel)
                : (zeroWeightRayMiss ? s_BilateralFilterHMissWeighted_HRKernel : s_BilateralFilterH_HRKernel);
            reflDenoiserParams.bilateralFilterVKernel = fullResolution ? (zeroWeightRayMiss ? s_BilateralFilterVMissWeighted_FRKernel : s_BilateralFilterV_FRKernel)
                : (zeroWeightRayMiss ? s_BilateralFilterVMissWeighted_HRKernel : s_BilateralFilterV_HRKernel);
            reflDenoiserParams.reflectionFilterMapping = m_ReflectionFilterMapping;

            return reflDenoiserParams;
        }

        public static void DenoiseBuffer(CommandBuffer cmd, ReflectionDenoiserParameters parameters, ReflectionDenoiserResources reflDenoiserResources)
        {
            bool bypassBilateralFilter = false;
            // Evaluate the dispatch parameters
            int tileSize = 8;
            int numTilesX = (parameters.texWidth + (tileSize - 1)) / tileSize;
            int numTilesY = (parameters.texHeight + (tileSize - 1)) / tileSize;

            // Input data
            cmd.SetComputeFloatParam(parameters.reflectionDenoiserCS, HDShaderIDs._HistoryValidity, parameters.historyValidity);
            cmd.SetComputeFloatParam(parameters.reflectionDenoiserCS, HDShaderIDs._PixelSpreadAngleTangent, parameters.pixelSpreadTangent);
            cmd.SetComputeVectorParam(parameters.reflectionDenoiserCS, HDShaderIDs._HistoryBufferSize, parameters.historyBufferSize);
            cmd.SetComputeVectorParam(parameters.reflectionDenoiserCS, HDShaderIDs._CurrentEffectResolution, parameters.currentEffectResolution);
            cmd.SetComputeIntParam(parameters.reflectionDenoiserCS, HDShaderIDs._AffectSmoothSurfaces, parameters.affectSmoothSurfaces);
            cmd.SetComputeIntParam(parameters.reflectionDenoiserCS, HDShaderIDs._SingleReflectionBounce, parameters.singleReflectionBounce);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._DenoiseInputTexture, reflDenoiserResources.noisyToOutputSignal);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._DepthTexture, reflDenoiserResources.depthBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._HistoryDepthTexture, reflDenoiserResources.historyDepth);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._NormalBufferTexture, reflDenoiserResources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._CameraMotionVectorsTexture, reflDenoiserResources.motionVectorBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._HistoryBuffer, reflDenoiserResources.historySignal);
            // TODO: may need to invalidate history if switching this
            if (parameters.rayMissInWeight == 1)
                cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._HistorySampleCountTexture, reflDenoiserResources.historySampleCount);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._SsrClearCoatMaskTexture, reflDenoiserResources.clearCoatMaskBuffer);

            // Output texture
            if (bypassBilateralFilter)
                cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._DenoiseOutputTextureRW, reflDenoiserResources.noisyToOutputSignal);
            else
                cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._DenoiseOutputTextureRW, reflDenoiserResources.intermediateBuffer0);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, HDShaderIDs._SampleCountTextureRW, reflDenoiserResources.intermediateBuffer1);

            // Do the temporal accumulation
            cmd.DispatchCompute(parameters.reflectionDenoiserCS, parameters.temporalAccumulationKernel, numTilesX, numTilesY, parameters.viewCount);

            // Copy the accumulated signal into the history buffer
            if (bypassBilateralFilter)
                cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.copyHistoryKernel, HDShaderIDs._DenoiseInputTexture, reflDenoiserResources.noisyToOutputSignal);
            else
                cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.copyHistoryKernel, HDShaderIDs._DenoiseInputTexture, reflDenoiserResources.intermediateBuffer0);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.copyHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, reflDenoiserResources.historySignal);
            //if (parameters.rayMissInWeight == 1)
            //    cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.copyHistoryKernel, HDShaderIDs._HistorySampleCountTextureRW, reflDenoiserResources.historySampleCount);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.copyHistoryKernel, HDShaderIDs._HistorySampleCountTextureRW, reflDenoiserResources.historySampleCount);
            // ...TODO: for now always set, #if def guards dont seem to work in compute shader
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.copyHistoryKernel, HDShaderIDs._SampleCountTextureRW, reflDenoiserResources.intermediateBuffer1);
            cmd.DispatchCompute(parameters.reflectionDenoiserCS, parameters.copyHistoryKernel, numTilesX, numTilesY, parameters.viewCount);

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(parameters.reflectionDenoiserCS, HDShaderIDs._DenoiserFilterRadius, parameters.maxKernelSize);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterHKernel, HDShaderIDs._DenoiseInputTexture, reflDenoiserResources.intermediateBuffer0);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterHKernel, HDShaderIDs._DepthTexture, reflDenoiserResources.depthBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterHKernel, HDShaderIDs._NormalBufferTexture, reflDenoiserResources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterHKernel, HDShaderIDs._SsrClearCoatMaskTexture, reflDenoiserResources.clearCoatMaskBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterHKernel, HDShaderIDs._DenoiseOutputTextureRW, reflDenoiserResources.intermediateBuffer1);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterHKernel, HDShaderIDs._ReflectionFilterMapping, parameters.reflectionFilterMapping);
            if (!bypassBilateralFilter)
                cmd.DispatchCompute(parameters.reflectionDenoiserCS, parameters.bilateralFilterHKernel, numTilesX, numTilesY, parameters.viewCount);

            // Vertical pass of the bilateral filter
            cmd.SetComputeIntParam(parameters.reflectionDenoiserCS, HDShaderIDs._DenoiserFilterRadius, parameters.maxKernelSize);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterVKernel, HDShaderIDs._DenoiseInputTexture, reflDenoiserResources.intermediateBuffer1);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterVKernel, HDShaderIDs._DepthTexture, reflDenoiserResources.depthBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterVKernel, HDShaderIDs._NormalBufferTexture, reflDenoiserResources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterVKernel, HDShaderIDs._SsrClearCoatMaskTexture, reflDenoiserResources.clearCoatMaskBuffer);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterVKernel, HDShaderIDs._DenoiseOutputTextureRW, reflDenoiserResources.noisyToOutputSignal);
            cmd.SetComputeTextureParam(parameters.reflectionDenoiserCS, parameters.bilateralFilterVKernel, HDShaderIDs._ReflectionFilterMapping, parameters.reflectionFilterMapping);
            if (!bypassBilateralFilter)
                cmd.DispatchCompute(parameters.reflectionDenoiserCS, parameters.bilateralFilterVKernel, numTilesX, numTilesY, parameters.viewCount);
        }

        class ReflectionDenoiserPassData
        {
            public ReflectionDenoiserParameters parameters;
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

        public TextureHandle DenoiseRTR(RenderGraph renderGraph, in ReflectionDenoiserParameters parameters, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle clearCoatMaskBuffer, TextureHandle lightingTexture, RTHandle historyBuffer,  RTHandle historySampleCountBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<ReflectionDenoiserPassData>("Denoise ray traced reflections", out var passData, ProfilingSampler.Get(HDProfileId.RaytracingReflectionFilter)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = parameters;
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
                passData.historySampleCount = (historySampleCountBuffer != null) ? builder.ReadWriteTexture(renderGraph.ImportTexture(historySampleCountBuffer)) : passData.historySampleCount;
                passData.noisyToOutputSignal = builder.ReadWriteTexture(lightingTexture);

                builder.SetRenderFunc((ReflectionDenoiserPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    ReflectionDenoiserResources rtrDenoiseResources = new ReflectionDenoiserResources();
                    rtrDenoiseResources.depthBuffer = data.depthBuffer;
                    rtrDenoiseResources.historyDepth = data.historyDepth;
                    rtrDenoiseResources.normalBuffer = data.normalBuffer;
                    rtrDenoiseResources.clearCoatMaskBuffer = data.clearCoatMaskBuffer;
                    rtrDenoiseResources.motionVectorBuffer = data.motionVectorBuffer;
                    rtrDenoiseResources.intermediateBuffer0 = data.intermediateBuffer0;
                    rtrDenoiseResources.intermediateBuffer1 = data.intermediateBuffer1;
                    rtrDenoiseResources.historySignal = data.historySignal;
                    rtrDenoiseResources.historySampleCount = data.historySampleCount;
                    rtrDenoiseResources.noisyToOutputSignal = data.noisyToOutputSignal;
                    DenoiseBuffer(ctx.cmd, data.parameters, rtrDenoiseResources);
                });

                return passData.noisyToOutputSignal;
            }
        }
    }
}
