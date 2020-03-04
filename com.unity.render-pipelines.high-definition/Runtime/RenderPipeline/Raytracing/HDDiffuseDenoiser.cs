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

        int m_BilateralFilterSingleKernel;
        int m_BilateralFilterColorKernel;
        int m_BilateralFilterSingleHalfResKernel;
        int m_BilateralFilterColorHalfResKernel;

        public HDDiffuseDenoiser()
        {
        }

        public void Init(RenderPipelineResources rpResources, HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {
            // Keep track of the resources
            m_SimpleDenoiserCS = rpResources.shaders.diffuseDenoiserCS;
            m_OwenScrambleRGBA = rpResources.textures.owenScrambledRGBATex;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;

            // Fetch the kernels that we need
            m_BilateralFilterSingleKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterSingle");
            m_BilateralFilterColorKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterColor");
            m_BilateralFilterSingleHalfResKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterSingleHalfRes");
            m_BilateralFilterColorHalfResKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterColorHalfRes");
        }

        public void Release()
        {
        }

        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera,
            RTHandle noisySignal,
            RTHandle outputSignal, 
            float kernelSize,
            bool singleChannel = true,
            bool halfResolution = false)
        {
            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX, numTilesY;
            if (halfResolution)
            {
                // Fetch texture dimensions
                int texWidth = hdCamera.actualWidth / 2;
                int texHeight = hdCamera.actualHeight / 2;
                numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;
            }
            else
            {
                // Fetch texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;
                numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;
            }

            // Request the intermediate buffers that we need
            RTHandle intermediateBuffer0 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);

            int m_KernelFilter;
            if (halfResolution)
                m_KernelFilter = singleChannel ? m_BilateralFilterSingleHalfResKernel : m_BilateralFilterColorHalfResKernel;
            else
                m_KernelFilter = singleChannel ? m_BilateralFilterSingleKernel : m_BilateralFilterColorKernel;

            // Bind the input constants
            cmd.SetComputeFloatParam(m_SimpleDenoiserCS, HDShaderIDs._PixelSpreadAngleTangent, HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.SetComputeFloatParam(m_SimpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, kernelSize);

            // Bind the input buffers
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._OwenScrambledRGTexture, m_OwenScrambleRGBA);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

            // Bind the output buffer
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);

            // Apply the diffuse denoiser
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }

        public void BilateralFilter(CommandBuffer cmd, HDCamera hdCamera,
            RTHandle inputSignal,
            RTHandle outputSignal)
        {
            // Fetch texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            int m_KernelFilter = m_SimpleDenoiserCS.FindKernel("GatherColor");
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, inputSignal);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
