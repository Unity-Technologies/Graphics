using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDDiffuseShadowDenoiser
    {
        // Reference to other HDRP components
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;

        // The resources quired by this component
        ComputeShader m_ShadowDenoiser;
        Texture3D m_ShadowFilterMapping;

        // Kernels that we are using
        int m_BilateralFilterHSingleDirectionalKernel;
        int m_BilateralFilterVSingleDirectionalKernel;

        int m_BilateralFilterHColorDirectionalKernel;
        int m_BilateralFilterVColorDirectionalKernel;

        int m_BilateralFilterHSingleSphereKernel;
        int m_BilateralFilterVSingleSphereKernel;

        public HDDiffuseShadowDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;

            m_ShadowDenoiser = rpRTResources.diffuseShadowDenoiserCS;
            m_ShadowFilterMapping = rpRTResources.shadowFilterMapping;

            m_BilateralFilterHSingleDirectionalKernel = m_ShadowDenoiser.FindKernel("BilateralFilterHSingleDirectional");
            m_BilateralFilterVSingleDirectionalKernel = m_ShadowDenoiser.FindKernel("BilateralFilterVSingleDirectional");

            m_BilateralFilterHColorDirectionalKernel = m_ShadowDenoiser.FindKernel("BilateralFilterHColorDirectional");
            m_BilateralFilterVColorDirectionalKernel =  m_ShadowDenoiser.FindKernel("BilateralFilterVColorDirectional");

            m_BilateralFilterHSingleSphereKernel = m_ShadowDenoiser.FindKernel("BilateralFilterHSingleSphere");
            m_BilateralFilterVSingleSphereKernel = m_ShadowDenoiser.FindKernel("BilateralFilterVSingleSphere");
        }

        public void Release()
        {
        }

        public void DenoiseBufferDirectional(CommandBuffer cmd, HDCamera hdCamera,
                                    RTHandle noisySignal, RTHandle distanceSignal, RTHandle outputSignal,
                                    int kernelSize, Vector3 lightDir, float angularDiameter, bool singleChannel = true)
        {
            // Request the intermediate buffer we need
            RTHandle intermediateBuffer0 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA3);

            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Convert the angular diameter of the directional light to radians (from degrees)
            float lightAngle = angularDiameter * Mathf.PI / 180.0f;

            // Horizontal pass of the bilateral filter
            int m_KernelFilter = singleChannel ? m_BilateralFilterHSingleDirectionalKernel : m_BilateralFilterHColorDirectionalKernel;

            // Bind input uniforms
            cmd.SetComputeFloatParam(m_ShadowDenoiser, HDShaderIDs._DirectionalLightAngle, lightAngle);
            cmd.SetComputeIntParam(m_ShadowDenoiser, HDShaderIDs._DenoiserFilterRadius, kernelSize);

            // Bind Input Textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DistanceTexture, distanceSignal);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._ShadowFilterMapping, m_ShadowFilterMapping);

            // Bind output textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, intermediateBuffer0);

            // Do the Horizontal pass
            cmd.DispatchCompute(m_ShadowDenoiser, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Vertical pass of the bilateral filter
            m_KernelFilter = singleChannel ? m_BilateralFilterVSingleDirectionalKernel : m_BilateralFilterVColorDirectionalKernel;

            // Bind input uniforms
            cmd.SetComputeIntParam(m_ShadowDenoiser, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeFloatParam(m_ShadowDenoiser, HDShaderIDs._DirectionalLightAngle, lightAngle);

            // Bind Input Textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, intermediateBuffer0);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DistanceTexture, distanceSignal);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._ShadowFilterMapping, m_ShadowFilterMapping);

            // Bind output textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);

            // Do the Vertical pass
            cmd.DispatchCompute(m_ShadowDenoiser, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }

        public void DenoiseBufferSphere(CommandBuffer cmd, HDCamera hdCamera,
                            RTHandle noisySignal, RTHandle distanceSignal, RTHandle outputSignal,
                            int kernelSize, Vector3 lightPosition, float lightRadius)
        {
            // Request the intermediate buffers that we need
            RTHandle intermediateBuffer0 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA3);

            // Texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Bind input uniforms
            cmd.SetComputeIntParam(m_ShadowDenoiser, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeVectorParam(m_ShadowDenoiser, HDShaderIDs._SphereLightPosition, lightPosition);
            cmd.SetComputeFloatParam(m_ShadowDenoiser, HDShaderIDs._SphereLightRadius, lightRadius);

            // Bind Input Textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterHSingleSphereKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterHSingleSphereKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterHSingleSphereKernel, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterHSingleSphereKernel, HDShaderIDs._DistanceTexture, distanceSignal);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterHSingleSphereKernel, HDShaderIDs._ShadowFilterMapping, m_ShadowFilterMapping);

            // Bind output textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterHSingleSphereKernel, HDShaderIDs._DenoiseOutputTextureRW, intermediateBuffer0);

            // Do the Horizontal pass
            cmd.DispatchCompute(m_ShadowDenoiser, m_BilateralFilterHSingleSphereKernel, numTilesX, numTilesY, hdCamera.viewCount);

            // Bind input uniforms
            cmd.SetComputeIntParam(m_ShadowDenoiser, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeVectorParam(m_ShadowDenoiser, HDShaderIDs._SphereLightPosition, lightPosition);
            cmd.SetComputeFloatParam(m_ShadowDenoiser, HDShaderIDs._SphereLightRadius, lightRadius);

            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterVSingleSphereKernel, HDShaderIDs._DenoiseInputTexture, intermediateBuffer0);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterVSingleSphereKernel, HDShaderIDs._DistanceTexture, distanceSignal);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterVSingleSphereKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterVSingleSphereKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterVSingleSphereKernel, HDShaderIDs._ShadowFilterMapping, m_ShadowFilterMapping);

            // Bind output textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_BilateralFilterVSingleSphereKernel, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);

            // Do the Vertical pass
            cmd.DispatchCompute(m_ShadowDenoiser, m_BilateralFilterVSingleSphereKernel, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
