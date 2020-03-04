using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Experimental.Rendering.HighDefinition
{
    class HDDiffuseDenoiser
    {
        // Resources used for the denoiser
        ComputeShader m_SimpleDenoiserCS;
        Texture m_OwenScrambleRGBA;

        // Required for fetching depth and normal buffers
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;

        public HDDiffuseDenoiser()
        {
        }

        public void Init(RenderPipelineResources rpResources, HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {
            // Keep track of the resources
            m_SimpleDenoiserCS = rpRTResources.diffuseDenoiserCS;
            m_OwenScrambleRGBA = rpResources.textures.owenScrambledRGBATex;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;
        }

        public void Release()
        {
        }

        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera,
            RTHandle noisySignal,
            RTHandle outputSignal, 
            float kernelSize,
            bool singleChannel = true,
            bool halfResolutionFilter = false)
        {
            // Fetch texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Request the intermediate buffers that we need
            RTHandle intermediateBuffer0 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);

            int m_KernelFilter = m_SimpleDenoiserCS.FindKernel(singleChannel ? "BilateralFilterSingle" : "BilateralFilterColor");
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledRGTexture, m_OwenScrambleRGBA);
            cmd.SetComputeFloatParam(m_SimpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, halfResolutionFilter ? intermediateBuffer0 : outputSignal);
            cmd.SetComputeIntParam(m_SimpleDenoiserCS, HDShaderIDs._HalfResolutionFilter, halfResolutionFilter ? 1 : 0);
            cmd.SetComputeFloatParam(m_SimpleDenoiserCS, HDShaderIDs._PixelSpreadAngleTangent, HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            if (halfResolutionFilter)
            {
                m_KernelFilter = m_SimpleDenoiserCS.FindKernel(singleChannel ? "GatherSingle" : "GatherColor");
                cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, intermediateBuffer0);
                cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
                cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
            }
        }
    }
}
