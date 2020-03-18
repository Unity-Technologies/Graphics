using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDReflectionDenoiser
    {
        ComputeShader m_ReflectionDenoiserCS;
        Texture2D m_ReflectionFilterMapping;
        SharedRTManager m_SharedRTManager;
        static int s_TemporalAccumulationKernel;
        static int s_CopyHistoryKernel;
        static int s_BilateralFilterHKernel;
        static int s_BilateralFilterVKernel;
        RTHandle m_IntermediateBuffer0 = null;
        RTHandle m_IntermediateBuffer1 = null;

        public HDReflectionDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager)
        {
            m_ReflectionDenoiserCS = rpRTResources.reflectionDenoiserCS;
            m_ReflectionFilterMapping = rpRTResources.reflectionFilterMapping;
            m_SharedRTManager = sharedRTManager;
            
            // Fetch all the kernels we shall be using
            s_TemporalAccumulationKernel = m_ReflectionDenoiserCS.FindKernel("TemporalAccumulation");
            s_CopyHistoryKernel = m_ReflectionDenoiserCS.FindKernel("CopyHistory");
            s_BilateralFilterHKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterH");
            s_BilateralFilterVKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterV");

            m_IntermediateBuffer0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IntermediateBuffer0");
            m_IntermediateBuffer1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IntermediateBuffer1");
        }

        public void Release()
        {
            RTHandles.Release(m_IntermediateBuffer1);
            RTHandles.Release(m_IntermediateBuffer0);
        }

        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera, int maxKernelSize
                                    , RTHandle noisySignal, RTHandle historySignal
                                    , RTHandle outputSignal)
        {
            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int tileSize = 8;
            int numTilesX = (texWidth + (tileSize - 1)) / tileSize;
            int numTilesY = (texHeight + (tileSize - 1)) / tileSize;

            // Apply a vectorized temporal filtering pass and store it back in the denoisebuffer0 with the analytic value in the third channel
            var historyScale = new Vector2(hdCamera.actualWidth / (float)historySignal.rt.width, hdCamera.actualHeight / (float)historySignal.rt.height);
            cmd.SetComputeVectorParam(m_ReflectionDenoiserCS, HDShaderIDs._RTHandleScaleHistory, historyScale);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_TemporalAccumulationKernel, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_TemporalAccumulationKernel, HDShaderIDs._HistoryBuffer, historySignal);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_TemporalAccumulationKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_TemporalAccumulationKernel, HDShaderIDs._DenoiseOutputTextureRW, m_IntermediateBuffer0);
            cmd.DispatchCompute(m_ReflectionDenoiserCS, s_TemporalAccumulationKernel, numTilesX, numTilesY, hdCamera.viewCount);

            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_CopyHistoryKernel, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer0);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_CopyHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, historySignal);
            cmd.DispatchCompute(m_ReflectionDenoiserCS, s_CopyHistoryKernel, numTilesX, numTilesY, hdCamera.viewCount);

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(m_ReflectionDenoiserCS, HDShaderIDs._DenoiserFilterRadius, maxKernelSize);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer0);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, HDShaderIDs._DenoiseOutputTextureRW, m_IntermediateBuffer1);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, HDShaderIDs._ReflectionFilterMapping, m_ReflectionFilterMapping);
            cmd.DispatchCompute(m_ReflectionDenoiserCS, s_BilateralFilterHKernel, numTilesX, numTilesY, hdCamera.viewCount);

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(m_ReflectionDenoiserCS, HDShaderIDs._DenoiserFilterRadius, maxKernelSize);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer1);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
            cmd.SetComputeTextureParam(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, HDShaderIDs._ReflectionFilterMapping, m_ReflectionFilterMapping);
            cmd.DispatchCompute(m_ReflectionDenoiserCS, s_BilateralFilterVKernel, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
