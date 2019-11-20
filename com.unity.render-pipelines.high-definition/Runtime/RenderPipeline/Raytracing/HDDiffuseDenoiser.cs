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

        // Temporary buffers used for the denoising
        RTHandle m_IntermediateBuffer0 = null;

        public HDDiffuseDenoiser()
        {
        }

        public void Init(RenderPipelineResources rpResources, HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager)
        {
            // Keep track of the resources
            m_SimpleDenoiserCS = rpRTResources.diffuseDenoiserCS;
            m_OwenScrambleRGBA = rpResources.textures.owenScrambledRGBATex;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;

            // Allocate the temporary buffers
            m_IntermediateBuffer0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IntermediateBuffer0");
        }

        public void Release()
        {
            RTHandles.Release(m_IntermediateBuffer0);
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

            int m_KernelFilter = m_SimpleDenoiserCS.FindKernel(singleChannel ? "BilateralFilterSingle" : "BilateralFilterColor");
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledRGTexture, m_OwenScrambleRGBA);
            cmd.SetComputeFloatParam(m_SimpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, halfResolutionFilter ? m_IntermediateBuffer0 : outputSignal);
            cmd.SetComputeIntParam(m_SimpleDenoiserCS, HDShaderIDs._HalfResolutionFilter, halfResolutionFilter ? 1 : 0);
            cmd.SetComputeFloatParam(m_SimpleDenoiserCS, HDShaderIDs._PixelSpreadAngleTangent, HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            if (halfResolutionFilter)
            {
                m_KernelFilter = m_SimpleDenoiserCS.FindKernel(singleChannel ? "GatherSingle" : "GatherColor");
                cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, m_IntermediateBuffer0);
                cmd.SetComputeTextureParam(m_SimpleDenoiserCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
                cmd.DispatchCompute(m_SimpleDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
            }
        }
    }
}
