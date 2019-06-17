using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDSimpleDenoiser
    {
#if ENABLE_RAYTRACING
        ComputeShader m_SimpleDenoiserCS;

        SharedRTManager m_SharedRTManager;

        RTHandleSystem.RTHandle m_IntermediateBuffer0 = null;
        RTHandleSystem.RTHandle m_IntermediateBuffer1 = null;

        public HDSimpleDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager)
        {
            m_SimpleDenoiserCS = rpRTResources.simpleDenoiserCS;

            m_SharedRTManager = sharedRTManager;

            m_IntermediateBuffer0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IntermediateBuffer0");
            m_IntermediateBuffer1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IntermediateBuffer1");
        }

        public void Release()
        {
            RTHandles.Release(m_IntermediateBuffer1);
            RTHandles.Release(m_IntermediateBuffer0);
        }

        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera, RTHandleSystem.RTHandle noisySignal, RTHandleSystem.RTHandle historySignal, RTHandleSystem.RTHandle outputSingal, int kernelSize, int slotIndex = -1)
        {
            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            int m_KernelFilter = 0;
            if (slotIndex < 0)
            {
                m_KernelFilter = m_SimpleDenoiserCS.FindKernel("TemporalAccumulation");
            }
            else
            {
                m_KernelFilter = m_SimpleDenoiserCS.FindKernel("TemporalAccumulationArray");
            }

            // Apply a vectorized temporal filtering pass and store it back in the denoisebuffer0 with the analytic value in the third channel
            var historyScale = new Vector2(hdCamera.actualWidth / (float)historySignal.rt.width, hdCamera.actualHeight / (float)historySignal.rt.height);
            cmd.SetComputeVectorParam(m_SimpleDenoiserCS, HDShaderIDs._RTHandleScaleHistory, historyScale);

            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._Historybuffer, historySignal);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_IntermediateBuffer0);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, 1);

            // Output the new history
            if (slotIndex < 0)
            {
                m_KernelFilter = m_SimpleDenoiserCS.FindKernel("CopyHistory");
            }
            else
            {
                m_KernelFilter = m_SimpleDenoiserCS.FindKernel("CopyHistoryArray");
            }
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer0);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, historySignal);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, 1);

            m_KernelFilter = m_SimpleDenoiserCS.FindKernel("BilateralFilterH");

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(m_SimpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer0);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, m_IntermediateBuffer1);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, 1);

            m_KernelFilter = m_SimpleDenoiserCS.FindKernel("BilateralFilterV");

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(m_SimpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer1);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSingal);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, 1);
        }
#endif
    }
}
