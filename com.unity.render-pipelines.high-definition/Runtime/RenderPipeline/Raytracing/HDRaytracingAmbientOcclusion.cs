using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class HDRaytracingAmbientOcclusion
    {
        // External structures
        HDRenderPipelineRayTracingResources m_PipelineRayTracingResources = null;
        HDRenderPipeline m_RenderPipeline = null;

        // The target denoising kernel
        int m_RTAOApplyIntensityKernel;

        // String values
        const string m_RayGenShaderName = "RayGenAmbientOcclusion";
        const string m_MissShaderName = "MissShaderAmbientOcclusion";
        const string m_ClosestHitShaderName = "ClosestHitMain";

        public void Init(HDRenderPipeline renderPipeline)
        {
            // Keep track of the pipeline asset
            m_PipelineRayTracingResources = renderPipeline.asset.renderPipelineRayTracingResources;

            // keep track of the render pipeline
            m_RenderPipeline = renderPipeline;

            // Grab the kernels we need
            m_RTAOApplyIntensityKernel = m_PipelineRayTracingResources.aoRaytracingCS.FindKernel("RTAOApplyIntensity");
        }

        public float EvaluateRTSpecularOcclusionFlag(HDCamera hdCamera, AmbientOcclusion ssoSettings)
        {
            float remappedRayLength = (Mathf.Clamp(ssoSettings.rayLength, 1.25f, 1.5f) - 1.25f) / 0.25f;
            return Mathf.Lerp(0.0f, 1.0f, 1.0f - remappedRayLength);
        }

        static RTHandle AmbientOcclusionHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_AmbientOcclusionHistoryBuffer{1}", viewName, frameIndex));
        }

        // The set of parameters that are used to trace the ambient occlusion
        public struct AmbientOcclusionTraceParameters
        {
            // Camera data
            public int actualWidth;
            public int actualHeight;
            public int viewCount;

            // Evaluation parameters
            public float rayLength;
            public int sampleCount;
            public bool denoise;

            // Other parameters
            public RayTracingShader aoShaderRT;
            public ShaderVariablesRaytracing raytracingCB;
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public RayTracingAccelerationStructure rayTracingAccelerationStructure;
        }

        public struct AmbientOcclusionTraceResources
        {
            // Input Buffer
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;

            // Debug textures
            public RTHandle rayCountTexture;

            // Output Buffer
            public RTHandle outputTexture;
            public RTHandle velocityBuffer;
        }

        AmbientOcclusionTraceParameters PrepareAmbientOcclusionTraceParameters(HDCamera hdCamera, ShaderVariablesRaytracing raytracingCB)
        {
            AmbientOcclusionTraceParameters rtAOParameters = new AmbientOcclusionTraceParameters();
            var aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();

            // Camera data
            rtAOParameters.actualWidth = hdCamera.actualWidth;
            rtAOParameters.actualHeight = hdCamera.actualHeight;
            rtAOParameters.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            rtAOParameters.rayLength = aoSettings.rayLength;
            rtAOParameters.sampleCount = aoSettings.sampleCount;
            rtAOParameters.denoise = aoSettings.denoise;

            // Other parameters
            rtAOParameters.raytracingCB = raytracingCB;
            rtAOParameters.aoShaderRT = m_PipelineRayTracingResources.aoRaytracingRT;
            rtAOParameters.rayTracingAccelerationStructure = m_RenderPipeline.RequestAccelerationStructure();
            BlueNoise blueNoise = m_RenderPipeline.GetBlueNoiseManager();
            rtAOParameters.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();

            return rtAOParameters;
        }

        // The set of parameters that are used to trace the ambient occlusion
        public struct AmbientOcclusionComposeParameters
        {
            // Generic attributes
            public float intensity;

            // Camera data
            public int actualWidth;
            public int actualHeight;
            public int viewCount;

            // Kernels
            public int intensityKernel;

            // Shaders
            public ComputeShader aoShaderCS;
        }

        AmbientOcclusionComposeParameters PrepareAmbientOcclusionComposeParameters(HDCamera hdCamera, ShaderVariablesRaytracing raytracingCB)
        {
            AmbientOcclusionComposeParameters aoComposeParameters = new AmbientOcclusionComposeParameters();
            var aoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            aoComposeParameters.intensity = aoSettings.intensity.value;
            aoComposeParameters.actualWidth = hdCamera.actualWidth;
            aoComposeParameters.actualHeight = hdCamera.actualHeight;
            aoComposeParameters.viewCount = hdCamera.viewCount;
            aoComposeParameters.aoShaderCS = m_PipelineRayTracingResources.aoRaytracingCS;
            aoComposeParameters.intensityKernel = m_RTAOApplyIntensityKernel;
            return aoComposeParameters;
        }

        static public void TraceAO(CommandBuffer cmd, AmbientOcclusionTraceParameters aoTraceParameters, AmbientOcclusionTraceResources aoTraceResources)
        {
            // Define the shader pass to use for the reflection pass
            cmd.SetRayTracingShaderPass(aoTraceParameters.aoShaderRT, "VisibilityDXR");

            // Set the acceleration structure for the pass
            cmd.SetRayTracingAccelerationStructure(aoTraceParameters.aoShaderRT, HDShaderIDs._RaytracingAccelerationStructureName, aoTraceParameters.rayTracingAccelerationStructure);

            // Inject the ray generation data (be careful of the global constant buffer limitation)
            aoTraceParameters.raytracingCB._RaytracingRayMaxLength = aoTraceParameters.rayLength;
            aoTraceParameters.raytracingCB._RaytracingNumSamples = aoTraceParameters.sampleCount;
            ConstantBuffer.PushGlobal(cmd, aoTraceParameters.raytracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Set the data for the ray generation
            cmd.SetRayTracingTextureParam(aoTraceParameters.aoShaderRT, HDShaderIDs._DepthTexture, aoTraceResources.depthStencilBuffer);
            cmd.SetRayTracingTextureParam(aoTraceParameters.aoShaderRT, HDShaderIDs._NormalBufferTexture, aoTraceResources.normalBuffer);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, aoTraceParameters.ditheredTextureSet);

            // Set the output textures
            cmd.SetRayTracingTextureParam(aoTraceParameters.aoShaderRT, HDShaderIDs._RayCountTexture, aoTraceResources.rayCountTexture);
            cmd.SetRayTracingTextureParam(aoTraceParameters.aoShaderRT, HDShaderIDs._AmbientOcclusionTextureRW, aoTraceResources.outputTexture);
            cmd.SetRayTracingTextureParam(aoTraceParameters.aoShaderRT, HDShaderIDs._VelocityBuffer, aoTraceResources.velocityBuffer);

            // Run the computation
            cmd.DispatchRays(aoTraceParameters.aoShaderRT, m_RayGenShaderName, (uint)aoTraceParameters.actualWidth, (uint)aoTraceParameters.actualHeight, (uint)aoTraceParameters.viewCount);
        }

        static RTHandle RequestAmbientOcclusionHistoryTexture(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedAmbientOcclusion,
                AmbientOcclusionHistoryBufferAllocatorFunction, 1);
        }

        static public void ComposeAO(CommandBuffer cmd, AmbientOcclusionComposeParameters aoComposeParameters, RTHandle outputTexture)
        {
            cmd.SetComputeFloatParam(aoComposeParameters.aoShaderCS, HDShaderIDs._RaytracingAOIntensity, aoComposeParameters.intensity);
            cmd.SetComputeTextureParam(aoComposeParameters.aoShaderCS, aoComposeParameters.intensityKernel, HDShaderIDs._AmbientOcclusionTextureRW, outputTexture);
            int texWidth = aoComposeParameters.actualWidth;
            int texHeight = aoComposeParameters.actualHeight;
            int areaTileSize = 8;
            int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;
            cmd.DispatchCompute(aoComposeParameters.aoShaderCS, aoComposeParameters.intensityKernel, numTilesXHR, numTilesYHR, aoComposeParameters.viewCount);

            // Bind the textures and the params
            cmd.SetGlobalTexture(HDShaderIDs._AmbientOcclusionTexture, outputTexture);
        }
    }
}
