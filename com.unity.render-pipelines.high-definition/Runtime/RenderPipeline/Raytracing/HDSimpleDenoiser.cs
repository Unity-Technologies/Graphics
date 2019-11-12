using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDSimpleDenoiser
    {
        ComputeShader m_SimpleDenoiserCS;

        SharedRTManager m_SharedRTManager;

        RTHandle m_IntermediateBuffer0 = null;
        RTHandle m_IntermediateBuffer1 = null;

        public HDSimpleDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager)
        {
            m_SimpleDenoiserCS = rpRTResources.simpleDenoiserCS;

            m_SharedRTManager = sharedRTManager;

            m_IntermediateBuffer0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IntermediateBuffer0");
            m_IntermediateBuffer1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IntermediateBuffer1");
        }

        public void Release()
        {
            RTHandles.Release(m_IntermediateBuffer1);
            RTHandles.Release(m_IntermediateBuffer0);
        }

        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera, RTHandle noisySignal, RTHandle historySignal, RTHandle outputSignal, int kernelSize, bool singleChannel = true, int slotIndex = -1)
        {
            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            int m_KernelFilter = 0;

            if (singleChannel)
            {
               if (slotIndex < 0)
                {
                    m_KernelFilter = m_SimpleDenoiserCS.FindKernel("TemporalAccumulationSingle");
                }
                else
                {
                    m_KernelFilter = m_SimpleDenoiserCS.FindKernel("TemporalAccumulationSingleArray");
                }
            }
            else
            {
                m_KernelFilter = m_SimpleDenoiserCS.FindKernel("TemporalAccumulationColor");
            }


            // Apply a vectorized temporal filtering pass and store it back in the denoisebuffer0 with the analytic value in the third channel
            var historyScale = new Vector2(hdCamera.actualWidth / (float)historySignal.rt.width, hdCamera.actualHeight / (float)historySignal.rt.height);
            cmd.SetComputeVectorParam(m_SimpleDenoiserCS, HDShaderIDs._RTHandleScaleHistory, historyScale);

            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._HistoryBuffer, historySignal);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_IntermediateBuffer0);
            cmd.SetComputeIntParam(m_SimpleDenoiserCS, HDShaderIDs._DenoisingHistorySlot, slotIndex);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Output the new history
            if (slotIndex < 0)
            {
                m_KernelFilter = m_SimpleDenoiserCS.FindKernel(singleChannel ? "CopyHistorySingle" : "CopyHistoryColor");
            }
            else
            {
                m_KernelFilter = m_SimpleDenoiserCS.FindKernel(singleChannel ? "CopyHistorySingleArray" : "CopyHistoryColorArray");
            }
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer0);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, historySignal);
            cmd.SetComputeIntParam(m_SimpleDenoiserCS, HDShaderIDs._DenoisingHistorySlot, slotIndex);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            m_KernelFilter = m_SimpleDenoiserCS.FindKernel(singleChannel ? "BilateralFilterHSingle" : "BilateralFilterHColor");

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(m_SimpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer0);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_IntermediateBuffer1);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            m_KernelFilter = m_SimpleDenoiserCS.FindKernel(singleChannel ? "BilateralFilterVSingle" : "BilateralFilterVColor");

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(m_SimpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer1);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }

        public void DenoiseBufferNoHistory(CommandBuffer cmd, HDCamera hdCamera, RTHandle noisySignal, RTHandle outputSignal, int kernelSize, bool singleChannel = true)
        {
            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            int m_KernelFilter = m_SimpleDenoiserCS.FindKernel(singleChannel ? "BilateralFilterHSingle" : "BilateralFilterHColor");

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(m_SimpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_IntermediateBuffer0);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            m_KernelFilter = m_SimpleDenoiserCS.FindKernel(singleChannel ? "BilateralFilterVSingle" : "BilateralFilterVColor");

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(m_SimpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer0);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
