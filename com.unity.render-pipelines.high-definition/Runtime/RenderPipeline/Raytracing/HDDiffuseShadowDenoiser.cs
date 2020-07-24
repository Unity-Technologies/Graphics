namespace UnityEngine.Rendering.HighDefinition
{
    class HDDiffuseShadowDenoiser
    {
        // Reference to other HDRP components
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;

        // The resources quired by this component
        ComputeShader m_ShadowDenoiser;

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
            float cameraFOV = hdCamera.camera.fieldOfView * Mathf.PI / 180.0f;

            // Horizontal pass of the bilateral filter
            int m_KernelFilter = singleChannel ? m_BilateralFilterHSingleDirectionalKernel : m_BilateralFilterHColorDirectionalKernel;

            // Bind input uniforms
            cmd.SetComputeFloatParam(m_ShadowDenoiser, HDShaderIDs._DirectionalLightAngle, lightAngle);
            cmd.SetComputeIntParam(m_ShadowDenoiser, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeFloatParam(m_ShadowDenoiser, HDShaderIDs._CameraFOV, cameraFOV);

            // Bind Input Textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DistanceTexture, distanceSignal);

            // Bind output textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, intermediateBuffer0);

            // Do the Horizontal pass
            cmd.DispatchCompute(m_ShadowDenoiser, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Vertical pass of the bilateral filter
            m_KernelFilter = singleChannel ? m_BilateralFilterVSingleDirectionalKernel : m_BilateralFilterVColorDirectionalKernel;

            // Bind input uniforms
            cmd.SetComputeIntParam(m_ShadowDenoiser, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeFloatParam(m_ShadowDenoiser, HDShaderIDs._DirectionalLightAngle, lightAngle);
            cmd.SetComputeFloatParam(m_ShadowDenoiser, HDShaderIDs._CameraFOV, cameraFOV);

            // Bind Input Textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, intermediateBuffer0);
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DistanceTexture, distanceSignal);

            // Bind output textures
            cmd.SetComputeTextureParam(m_ShadowDenoiser, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);

            // Do the Vertical pass
            cmd.DispatchCompute(m_ShadowDenoiser, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }

        struct DiffuseShadowSphereDenoiserParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public Vector3 lightPosition;
            public float lightRadius;
            public float cameraFov;
            public int kernelSize;

            // Kernels
            public int bilateralHKernel;
            public int bilateralVKernel;

            // Other parameters
            public ComputeShader diffuseShadowDenoiserCS;
        }

        DiffuseShadowSphereDenoiserParameters PrepareDiffuseShadowSphereDenoiserParameters(HDCamera hdCamera, Vector3 lightPosition, float lightRadius, int kernelSize)
        {
            DiffuseShadowSphereDenoiserParameters dssdParams = new DiffuseShadowSphereDenoiserParameters();

            // Set the camera parameters
            dssdParams.texWidth = hdCamera.actualWidth;
            dssdParams.texHeight = hdCamera.actualHeight;
            dssdParams.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            dssdParams.cameraFov = hdCamera.camera.fieldOfView * Mathf.PI / 180.0f;
            dssdParams.lightPosition = lightPosition;
            dssdParams.lightRadius = lightRadius;
            dssdParams.kernelSize = kernelSize;

            // Kernels
            dssdParams.bilateralHKernel = m_BilateralFilterHSingleSphereKernel;
            dssdParams.bilateralVKernel = m_BilateralFilterVSingleSphereKernel;

            // Other parameters
            dssdParams.diffuseShadowDenoiserCS = m_ShadowDenoiser;
            return dssdParams;
        }

        struct DiffuseShadowSphereDenoiserResources
        {
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;
            public RTHandle distanceBuffer;
            public RTHandle noisyBuffer;
            public RTHandle intermediateBuffer;
            public RTHandle outputBuffer;
        }

        DiffuseShadowSphereDenoiserResources PrepareDiffuseShadowSphereDenoiserResources(RTHandle distanceBuffer, RTHandle noisyBuffer, RTHandle intermediateBuffer, RTHandle outputBuffer)
        {
            DiffuseShadowSphereDenoiserResources dssdResources = new DiffuseShadowSphereDenoiserResources();

            dssdResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            dssdResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            dssdResources.distanceBuffer = distanceBuffer;
            dssdResources.noisyBuffer = noisyBuffer;
            dssdResources.intermediateBuffer = intermediateBuffer;
            dssdResources.outputBuffer = outputBuffer;

            return dssdResources;
        }

        static void ExecuteDiffuseShadowSphereDenoiser(CommandBuffer cmd, DiffuseShadowSphereDenoiserParameters dssdParams, DiffuseShadowSphereDenoiserResources dssdResources)
        {
            // Evaluate the dispatch parameters
            int shadowTileSize = 8;
            int numTilesX = (dssdParams.texWidth + (shadowTileSize - 1)) / shadowTileSize;
            int numTilesY = (dssdParams.texHeight + (shadowTileSize - 1)) / shadowTileSize;

            // Bind input uniforms
            cmd.SetComputeIntParam(dssdParams.diffuseShadowDenoiserCS, HDShaderIDs._DenoiserFilterRadius, dssdParams.kernelSize);
            cmd.SetComputeVectorParam(dssdParams.diffuseShadowDenoiserCS, HDShaderIDs._SphereLightPosition, dssdParams.lightPosition);
            cmd.SetComputeFloatParam(dssdParams.diffuseShadowDenoiserCS, HDShaderIDs._SphereLightRadius, dssdParams.lightRadius);
            cmd.SetComputeFloatParam(dssdParams.diffuseShadowDenoiserCS, HDShaderIDs._CameraFOV, dssdParams.cameraFov);

            // Bind Input Textures
            cmd.SetComputeTextureParam(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralHKernel, HDShaderIDs._DepthTexture, dssdResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralHKernel, HDShaderIDs._NormalBufferTexture, dssdResources.normalBuffer);
            cmd.SetComputeTextureParam(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralHKernel, HDShaderIDs._DenoiseInputTexture, dssdResources.noisyBuffer);
            cmd.SetComputeTextureParam(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralHKernel, HDShaderIDs._DistanceTexture, dssdResources.distanceBuffer);

            // Bind output textures
            cmd.SetComputeTextureParam(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralHKernel, HDShaderIDs._DenoiseOutputTextureRW, dssdResources.intermediateBuffer);

            // Do the Horizontal pass
            cmd.DispatchCompute(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralHKernel, numTilesX, numTilesY, dssdParams.viewCount);

            // Bind input uniforms
            cmd.SetComputeIntParam(dssdParams.diffuseShadowDenoiserCS, HDShaderIDs._DenoiserFilterRadius, dssdParams.kernelSize);
            cmd.SetComputeVectorParam(dssdParams.diffuseShadowDenoiserCS, HDShaderIDs._SphereLightPosition, dssdParams.lightPosition);
            cmd.SetComputeFloatParam(dssdParams.diffuseShadowDenoiserCS, HDShaderIDs._SphereLightRadius, dssdParams.lightRadius);
            cmd.SetComputeFloatParam(dssdParams.diffuseShadowDenoiserCS, HDShaderIDs._CameraFOV, dssdParams.cameraFov);

            cmd.SetComputeTextureParam(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralVKernel, HDShaderIDs._DenoiseInputTexture, dssdResources.intermediateBuffer);
            cmd.SetComputeTextureParam(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralVKernel, HDShaderIDs._DistanceTexture, dssdResources.distanceBuffer);
            cmd.SetComputeTextureParam(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralVKernel, HDShaderIDs._DepthTexture, dssdResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralVKernel, HDShaderIDs._NormalBufferTexture, dssdResources.normalBuffer);

            // Bind output textures
            cmd.SetComputeTextureParam(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralVKernel, HDShaderIDs._DenoiseOutputTextureRW, dssdResources.outputBuffer);

            // Do the Vertical pass
            cmd.DispatchCompute(dssdParams.diffuseShadowDenoiserCS, dssdParams.bilateralVKernel, numTilesX, numTilesY, dssdParams.viewCount);
        }

        public void DenoiseBufferSphere(CommandBuffer cmd, HDCamera hdCamera,
                            RTHandle noisyBuffer, RTHandle distanceBuffer, RTHandle outputBuffer,
                            int kernelSize, Vector3 lightPosition, float lightRadius)
        {
            // Request the intermediate buffers that we need
            RTHandle intermediateBuffer = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA3);

            DiffuseShadowSphereDenoiserParameters dssdParams = PrepareDiffuseShadowSphereDenoiserParameters(hdCamera, lightPosition, lightRadius, kernelSize);
            DiffuseShadowSphereDenoiserResources dssdResources = PrepareDiffuseShadowSphereDenoiserResources(distanceBuffer, noisyBuffer, intermediateBuffer, outputBuffer);
            ExecuteDiffuseShadowSphereDenoiser(cmd, dssdParams, dssdResources);
        }
    }
}
