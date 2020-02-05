using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDDiffuseShadowDenoiser
    {
        // Reference to other HDRP components
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;

        ComputeShader m_ShadowDenoiser;
        Texture3D m_ShadowFilterMapping;

        public HDDiffuseShadowDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;

            m_ShadowDenoiser = rpRTResources.diffuseShadowDenoiserCS;
            m_ShadowFilterMapping = rpRTResources.shadowFilterMapping;
        }

        public void Release()
        {
        }

        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera,
                                    RTHandle noisySignal, RTHandle distanceSignal, RTHandle outputSignal,
                                    int kernelSize, Vector3 lightDir, float radius, bool singleChannel = true)
        {
            // Request the intermediate buffers that we need
            RTHandle intermediateBuffer0 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);

            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Horizontal pass of the bilateral filter
            int m_KernelFilter = m_ShadowDenoiser.FindKernel(singleChannel ? "BilateralFilterHSingle" : "BilateralFilterHColor");
            cmd.SetComputeIntParam(m_ShadowDenoiser, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseInputDistanceTexture, distanceSignal);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._ShadowFilterMapping, m_ShadowFilterMapping);
            cmd.SetComputeVectorParam(m_ShadowDenoiser, "_LightDirection", lightDir);
            cmd.SetComputeFloatParam(m_ShadowDenoiser, "_LightRadius", radius);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, intermediateBuffer0);
            cmd.DispatchCompute(m_ShadowDenoiser, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);


            // Horizontal pass of the bilateral filter
            m_KernelFilter = m_ShadowDenoiser.FindKernel(singleChannel ? "BilateralFilterVSingle" : "BilateralFilterVColor");
            cmd.SetComputeIntParam(m_ShadowDenoiser, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, intermediateBuffer0);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseInputDistanceTexture, distanceSignal);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._ShadowFilterMapping, m_ShadowFilterMapping);
            cmd.SetComputeVectorParam(m_ShadowDenoiser, "_LightDirection", lightDir);
            cmd.SetComputeFloatParam(m_ShadowDenoiser, "_LightRadius", radius);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
            cmd.DispatchCompute(m_ShadowDenoiser, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
