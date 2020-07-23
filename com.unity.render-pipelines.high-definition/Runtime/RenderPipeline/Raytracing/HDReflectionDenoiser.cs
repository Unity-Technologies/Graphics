namespace UnityEngine.Rendering.HighDefinition
{
    internal struct ReflectionDenoiserParameters
    {
        // Camera Properties
        public int texWidth;
        public int texHeight;
        public int viewCount;

        // De-noising parameters
        public int maxKernelSize;
        public float historyValidity;

        // Kernels
        public int temporalAccumulationKernel;
        public int copyHistoryKernel;
        public int bilateralFilterHKernel;
        public int bilateralFilterVKernel;

        // Other parameters
        public Texture2D reflectionFilterMapping;
        public ComputeShader reflectionDenoiserCS;
    }

    internal struct ReflectionDenoiserResources
    {
        // Input buffer
        public RTHandle depthStencilBuffer;
        public RTHandle normalBuffer;

        // Intermediate textures
        public RTHandle intermediateBuffer0;
        public RTHandle intermediateBuffer1;

        // Output buffers
        public RTHandle historySignal;
        public RTHandle noisyToOutputSignal;
    }

    class HDReflectionDenoiser
    {
        ComputeShader m_ReflectionDenoiserCS;
        Texture2D m_ReflectionFilterMapping;
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;
        int s_TemporalAccumulationKernel;
        int s_CopyHistoryKernel;
        int s_BilateralFilterHKernel;
        int s_BilateralFilterVKernel;

        public HDReflectionDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {
            m_ReflectionDenoiserCS = rpRTResources.reflectionDenoiserCS;
            m_ReflectionFilterMapping = rpRTResources.reflectionFilterMapping;
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;

            // Fetch all the kernels we shall be using
            s_TemporalAccumulationKernel = m_ReflectionDenoiserCS.FindKernel("TemporalAccumulation");
            s_CopyHistoryKernel = m_ReflectionDenoiserCS.FindKernel("CopyHistory");
            s_BilateralFilterHKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterH");
            s_BilateralFilterVKernel = m_ReflectionDenoiserCS.FindKernel("BilateralFilterV");
        }

        public void Release()
        {
        }


        internal ReflectionDenoiserParameters PrepareReflectionDenoiserParameters(HDCamera hdCamera, float historyValidity, int maxKernelSize)
        {
            ReflectionDenoiserParameters reflDenoiserParams = new ReflectionDenoiserParameters();
            // Camera parameters
            reflDenoiserParams.texWidth = hdCamera.actualWidth;
            reflDenoiserParams.texHeight = hdCamera.actualHeight;
            reflDenoiserParams.viewCount = hdCamera.viewCount;

            // De-noising parameters
            reflDenoiserParams.historyValidity = historyValidity;
            reflDenoiserParams.maxKernelSize = maxKernelSize;

            // Kernels
            reflDenoiserParams.temporalAccumulationKernel = s_TemporalAccumulationKernel;
            reflDenoiserParams.copyHistoryKernel = s_CopyHistoryKernel;
            reflDenoiserParams.bilateralFilterHKernel = s_BilateralFilterHKernel;
            reflDenoiserParams.bilateralFilterVKernel = s_BilateralFilterVKernel;

            // Other parameters
            reflDenoiserParams.reflectionFilterMapping = m_ReflectionFilterMapping;
            reflDenoiserParams.reflectionDenoiserCS = m_ReflectionDenoiserCS;

            return reflDenoiserParams;
        }

        internal ReflectionDenoiserResources PrepareReflectionDenoiserResources(HDCamera hdCamera,
                                                                                RTHandle noisyToOutputSignal, RTHandle historySignal,
                                                                                RTHandle intermediateBuffer0, RTHandle intermediateBuffer1)
        {
            ReflectionDenoiserResources reflDenoiserResources = new ReflectionDenoiserResources();
            reflDenoiserResources.historySignal = historySignal;
            reflDenoiserResources.noisyToOutputSignal = noisyToOutputSignal;
            reflDenoiserResources.intermediateBuffer0 = intermediateBuffer0;
            reflDenoiserResources.intermediateBuffer1 = intermediateBuffer1;
            reflDenoiserResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            reflDenoiserResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            return reflDenoiserResources;
        }

        public static void DenoiseBuffer(CommandBuffer cmd, ReflectionDenoiserParameters reflDenoiserParameters, ReflectionDenoiserResources reflDenoiserResources)
        {
            // Evaluate the dispatch parameters
            int tileSize = 8;
            int numTilesX = (reflDenoiserParameters.texWidth + (tileSize - 1)) / tileSize;
            int numTilesY = (reflDenoiserParameters.texHeight + (tileSize - 1)) / tileSize;

            // Apply a vectorized temporal filtering pass and store it back in the denoisebuffer0 with the analytic value in the third channel
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.temporalAccumulationKernel, HDShaderIDs._DenoiseInputTexture, reflDenoiserResources.noisyToOutputSignal);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.temporalAccumulationKernel, HDShaderIDs._HistoryBuffer, reflDenoiserResources.historySignal);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.temporalAccumulationKernel, HDShaderIDs._DepthTexture, reflDenoiserResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.temporalAccumulationKernel, HDShaderIDs._NormalBufferTexture, reflDenoiserResources.normalBuffer);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.temporalAccumulationKernel, HDShaderIDs._DenoiseOutputTextureRW, reflDenoiserResources.intermediateBuffer0);
            cmd.SetComputeFloatParam(reflDenoiserParameters.reflectionDenoiserCS, HDShaderIDs._HistoryValidity, reflDenoiserParameters.historyValidity);
            cmd.DispatchCompute(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.temporalAccumulationKernel, numTilesX, numTilesY, reflDenoiserParameters.viewCount);

            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.copyHistoryKernel, HDShaderIDs._DenoiseInputTexture, reflDenoiserResources.intermediateBuffer0);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.copyHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, reflDenoiserResources.historySignal);
            cmd.DispatchCompute(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.copyHistoryKernel, numTilesX, numTilesY, reflDenoiserParameters.viewCount);

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(reflDenoiserParameters.reflectionDenoiserCS, HDShaderIDs._DenoiserFilterRadius, reflDenoiserParameters.maxKernelSize);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterHKernel, HDShaderIDs._DenoiseInputTexture, reflDenoiserResources.intermediateBuffer0);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterHKernel, HDShaderIDs._DepthTexture, reflDenoiserResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterHKernel, HDShaderIDs._NormalBufferTexture, reflDenoiserResources.normalBuffer);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterHKernel, HDShaderIDs._DenoiseOutputTextureRW, reflDenoiserResources.intermediateBuffer1);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterHKernel, HDShaderIDs._ReflectionFilterMapping, reflDenoiserParameters.reflectionFilterMapping);
            cmd.DispatchCompute(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterHKernel, numTilesX, numTilesY, reflDenoiserParameters.viewCount);

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(reflDenoiserParameters.reflectionDenoiserCS, HDShaderIDs._DenoiserFilterRadius, reflDenoiserParameters.maxKernelSize);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterVKernel, HDShaderIDs._DenoiseInputTexture, reflDenoiserResources.intermediateBuffer1);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterVKernel, HDShaderIDs._DepthTexture, reflDenoiserResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterVKernel, HDShaderIDs._NormalBufferTexture, reflDenoiserResources.normalBuffer);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterVKernel, HDShaderIDs._DenoiseOutputTextureRW, reflDenoiserResources.noisyToOutputSignal);
            cmd.SetComputeTextureParam(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterVKernel, HDShaderIDs._ReflectionFilterMapping, reflDenoiserParameters.reflectionFilterMapping);
            cmd.DispatchCompute(reflDenoiserParameters.reflectionDenoiserCS, reflDenoiserParameters.bilateralFilterVKernel, numTilesX, numTilesY, reflDenoiserParameters.viewCount);
        }
    }
}
