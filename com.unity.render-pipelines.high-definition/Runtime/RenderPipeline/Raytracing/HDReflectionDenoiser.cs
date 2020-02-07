using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDReflectionDenoiser
    {
        ComputeShader m_ReflectionDenoiserCS;
        Texture2D m_ReflectionFilterMapping;
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;
        static int s_TemporalAccumulationKernel;
        static int s_CopyHistoryKernel;
        static int s_BilateralFilterHKernel;
        static int s_BilateralFilterVKernel;

        public HDReflectionDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {
            m_ReflectionDenoiserCS = rpRTResources.reflectionDenoiserCS;
            m_ReflectionFilterMapping = rpRTResources.reflectionFilterMapping;
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;

            // Fetch all the kernels we shall be using
            s_TemporalAccumulationKernel = m_ReflectionDenoiserCS.FindKernel("TemporalAccumulation");
            s_CopyHistoryKernel = m_ReflectionDenoiserCS.FindKernel("CopyHistory");
            s_BilateralFilterHKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterH");
            s_BilateralFilterVKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterV");
        }

        public void Release()
        {
        }

        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera, int maxKernelSize
                                    , RTHandle noisySignal, RTHandle historySignal
                                    , RTHandle outputSignal
                                    , float historyValidity = 1.0f)
        {
            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int tileSize = 8;
            int numTilesX = (texWidth + (tileSize - 1)) / tileSize;
            int numTilesY = (texHeight + (tileSize - 1)) / tileSize;

            // Grab the ray traced reflection volume component
            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceReflection>();

            // Request the intermediate buffers that we need
            RTHandle intermediateBuffer0 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle intermediateBuffer1 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);

            // Apply a vectorized temporal filtering pass and store it back in the denoisebuffer0 with the analytic value in the third channel
            var historyScale = new Vector2(hdCamera.actualWidth / (float)historySignal.rt.width, hdCamera.actualHeight / (float)historySignal.rt.height);
            cmd.SetComputeVectorParam(m_ReflectionDenoiserCS, HDShaderIDs._RTHandleScaleHistory, historyScale);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_TemporalAccumulationKernel, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_TemporalAccumulationKernel, HDShaderIDs._HistoryBuffer, historySignal);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_TemporalAccumulationKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_TemporalAccumulationKernel, HDShaderIDs._DenoiseOutputTextureRW, intermediateBuffer0);
            cmd.SetComputeFloatParam(m_ReflectionDenoiserCS, HDShaderIDs._HistoryValidity, historyValidity);
            cmd.DispatchCompute(m_ReflectionDenoiserCS, s_TemporalAccumulationKernel, numTilesX, numTilesY, hdCamera.viewCount);

            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_CopyHistoryKernel, HDShaderIDs._DenoiseInputTexture, intermediateBuffer0);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_CopyHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, historySignal);
            cmd.DispatchCompute(m_ReflectionDenoiserCS, s_CopyHistoryKernel, numTilesX, numTilesY, hdCamera.viewCount);

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(m_ReflectionDenoiserCS, HDShaderIDs._DenoiserFilterRadius, maxKernelSize);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, HDShaderIDs._DenoiseInputTexture, intermediateBuffer0);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, HDShaderIDs._DenoiseOutputTextureRW, intermediateBuffer1);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, HDShaderIDs._ReflectionFilterMapping, m_ReflectionFilterMapping);
            cmd.SetComputeFloatParam(m_ReflectionDenoiserCS, HDShaderIDs._RaytracingReflectionMinSmoothness, settings.minSmoothness.value);
            cmd.DispatchCompute(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, numTilesX, numTilesY, hdCamera.viewCount);

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(m_ReflectionDenoiserCS, HDShaderIDs._DenoiserFilterRadius, maxKernelSize);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, HDShaderIDs._DenoiseInputTexture, intermediateBuffer1);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, HDShaderIDs._ReflectionFilterMapping, m_ReflectionFilterMapping);
            cmd.SetComputeFloatParam(m_ReflectionDenoiserCS, HDShaderIDs._RaytracingReflectionMinSmoothness, settings.minSmoothness.value);
            cmd.DispatchCompute(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
