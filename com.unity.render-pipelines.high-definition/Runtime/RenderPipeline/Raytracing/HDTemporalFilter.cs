using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Experimental.Rendering.HighDefinition
{
    class HDTemporalFilter
    {
        // Resources used for the denoiser
        ComputeShader m_TemporalFilterCS;

        // Required for fetching depth and normal buffers
        SharedRTManager m_SharedRTManager;

        // Temporary buffers used for the denoising
        RTHandle m_ValidationBuffer = null;

        public HDTemporalFilter()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager)
        {   
            // Keep track of the resources
            m_TemporalFilterCS = rpRTResources.temporalFilterCS;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;

            // Allocate the temporary buffers
            m_ValidationBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R8_UInt, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "ValidationBuffer");
        }

        public void Release()
        {
            RTHandles.Release(m_ValidationBuffer);
        }

        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera,
            RTHandle noisySignal, RTHandle historySignal,
            RTHandle outputSignal, 
            bool singleChannel = true, int slotIndex = -1)
        {
            // If we do not have a depth and normal history buffers, we can skip right away
            var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            var historyNormalBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
            if (historyDepthBuffer == null || historyNormalBuffer == null)
            {
                HDUtils.BlitCameraTexture(cmd, noisySignal, historySignal);
                HDUtils.BlitCameraTexture(cmd, noisySignal, outputSignal);
                return;
            }

            // Fetch texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // First of all we need to validate the history to know where we can or cannot use the history signal
            int m_KernelFilter = m_TemporalFilterCS.FindKernel("ValidateHistory");
            var historyScale = new Vector2(hdCamera.actualWidth / (float)historySignal.rt.width, hdCamera.actualHeight / (float)historySignal.rt.height);
            cmd.SetComputeVectorParam(m_TemporalFilterCS, HDShaderIDs._RTHandleScaleHistory, historyScale);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryDepthTexture, historyDepthBuffer);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryNormalBufferTexture, historyNormalBuffer);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._ValidationBufferRW, m_ValidationBuffer);
            cmd.SetComputeFloatParam(m_TemporalFilterCS, HDShaderIDs._PixelSpreadAngleTangent, HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Now that we have validated our history, let's accumulate
            m_KernelFilter = m_TemporalFilterCS.FindKernel(singleChannel ? (slotIndex == -1 ? "TemporalAccumulationSingle" : "TemporalAccumulationSingleArray") : "TemporalAccumulationColor");
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryBuffer, historySignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._ValidationBuffer, m_ValidationBuffer);
            cmd.SetComputeIntParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistorySlot, slotIndex);
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            m_KernelFilter = m_TemporalFilterCS.FindKernel(singleChannel ? (slotIndex == -1 ? "CopyHistorySingle" : "CopyHistorySingleArray") : "CopyHistoryColor");
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, outputSignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, historySignal);
            cmd.SetComputeIntParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistorySlot, slotIndex);
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
