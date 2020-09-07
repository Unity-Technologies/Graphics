using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    // Define if we use SSGI, RTGI or none
    enum IndirectDiffuseMode
    {
        Off,
        ScreenSpace,
        Raytrace
    }

    public partial class HDRenderPipeline
    {
        // Buffers used for the evaluation
        RTHandle m_IndirectDiffuseBuffer0 = null;
        RTHandle m_IndirectDiffuseBuffer1 = null;
        RTHandle m_IndirectDiffuseHitPointBuffer = null;

        // The set of kernels that we shall be using
        int m_TraceGlobalIlluminationKernel;
        int m_TraceGlobalIlluminationHalfKernel;
        int m_ReprojectGlobalIlluminationKernel;
        int m_ReprojectGlobalIlluminationHalfKernel;
        int m_BilateralUpSampleColorTMKernel;

        void InitScreenSpaceGlobalIllumination()
        {
            if (m_Asset.currentPlatformRenderPipelineSettings.supportSSGI)
            {
                // Grab the sets of shaders that we'll be using
                ComputeShader ssGICS = m_Asset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS;
                ComputeShader bilateralUpsampleCS = m_Asset.renderPipelineResources.shaders.bilateralUpsampleCS;

                // Grab the set of kernels that we shall be using
                m_TraceGlobalIlluminationKernel = ssGICS.FindKernel("TraceGlobalIllumination");
                m_TraceGlobalIlluminationHalfKernel = ssGICS.FindKernel("TraceGlobalIlluminationHalf");
                m_ReprojectGlobalIlluminationKernel = ssGICS.FindKernel("ReprojectGlobalIllumination");
                m_ReprojectGlobalIlluminationHalfKernel = ssGICS.FindKernel("ReprojectGlobalIlluminationHalf");
                m_BilateralUpSampleColorTMKernel = bilateralUpsampleCS.FindKernel("BilateralUpSampleColorTM");
            }
        }

        // This is shared between SSGI and RTGI
        IndirectDiffuseMode GetIndirectDiffuseMode(HDCamera hdCamera)
        {
            IndirectDiffuseMode mode = IndirectDiffuseMode.Off;

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSGI))
            {
                var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
                if (settings.enable.value)
                {
                    // RTGI is only valid if raytracing is enabled
                    bool raytracing = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value;
                    mode = raytracing ? IndirectDiffuseMode.Raytrace : IndirectDiffuseMode.ScreenSpace;
                }
            }
            return mode;
        }

        // Bind the indirect diffuse texture for the lightloop to read from it
        void BindIndirectDiffuseTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._IndirectDiffuseTexture, m_IndirectDiffuseBuffer0);
        }

        // If there is no SSGI, bind a black 1x1 texture
        static void BindBlackIndirectDiffuseTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._IndirectDiffuseTexture, TextureXR.GetBlackTexture());
        }

        struct SSGITraceParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;
            public Vector4 halfScreenSize;

            // Generation parameters
            public float nearClipPlane;
            public float farClipPlane;
            public bool fullResolutionSS;
            public float thickness;
            public int raySteps;
            public float maximalRadius;
            public float clampValueSS;
            public Vector4 colorPyramidUvScaleAndLimitPrevFrame;

            // Compute Shader
            public ComputeShader ssGICS;
            public int traceKernel;
            public int projectKernel;

            // Other parameters
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public ComputeBuffer offsetBuffer;
        }

        SSGITraceParameters PrepareSSGITraceParameters(HDCamera hdCamera, GlobalIllumination settings)
        {
            SSGITraceParameters parameters = new SSGITraceParameters();

            // Set the camera parameters
            if (settings.fullResolutionSS)
            {
                parameters.texWidth = hdCamera.actualWidth;
                parameters.texHeight = hdCamera.actualHeight;
                parameters.halfScreenSize.Set(parameters.texWidth * 0.5f, parameters.texHeight * 0.5f, 2.0f / parameters.texWidth, 2.0f / parameters.texHeight);
            }
            else
            {
                parameters.texWidth = hdCamera.actualWidth / 2;
                parameters.texHeight = hdCamera.actualHeight / 2;
                parameters.halfScreenSize.Set(parameters.texWidth, parameters.texHeight, 1.0f / parameters.texWidth, 1.0f / parameters.texHeight);
            }
            parameters.viewCount = hdCamera.viewCount;

            // Set the generation parameters
            parameters.nearClipPlane = hdCamera.camera.nearClipPlane;
            parameters.farClipPlane = hdCamera.camera.farClipPlane;
            parameters.fullResolutionSS = settings.fullResolutionSS;
            parameters.thickness = settings.depthBufferThickness.value;
            parameters.raySteps = settings.raySteps;
            parameters.maximalRadius = settings.maximalRadius;
            parameters.clampValueSS = settings.clampValueSS;
            parameters.colorPyramidUvScaleAndLimitPrevFrame = HDUtils.ComputeViewportScaleAndLimit(hdCamera.historyRTHandleProperties.previousViewportSize, hdCamera.historyRTHandleProperties.previousRenderTargetSize);

            // Grab the right kernel
            parameters.ssGICS = m_Asset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS;
            parameters.traceKernel = settings.fullResolutionSS ? m_TraceGlobalIlluminationKernel : m_TraceGlobalIlluminationHalfKernel;
            parameters.projectKernel = settings.fullResolutionSS ? m_ReprojectGlobalIlluminationKernel : m_ReprojectGlobalIlluminationHalfKernel;

            BlueNoise blueNoise = GetBlueNoiseManager();
            parameters.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
            parameters.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
            var info = m_SharedRTManager.GetDepthBufferMipChainInfo();
            parameters.offsetBuffer = info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);

            return parameters;
        }

        struct SSGITraceResources
        {
            // Input buffers
            public RTHandle depthTexture;
            public RTHandle normalBuffer;
            public RTHandle motionVectorsBuffer;
            public RTHandle colorPyramid;
            public RTHandle historyDepth;

            // Intermediate buffers
            public RTHandle hitPointBuffer;

            // Output buffers
            public RTHandle outputBuffer;
        }

        SSGITraceResources PrepareSSGITraceResources(HDCamera hdCamera, RTHandle outputBuffer, RTHandle hitPointBuffer)
        {
            SSGITraceResources ssgiTraceResources = new SSGITraceResources();

            // Input buffers
            ssgiTraceResources.depthTexture = m_SharedRTManager.GetDepthTexture();
            ssgiTraceResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            ssgiTraceResources.motionVectorsBuffer = m_SharedRTManager.GetMotionVectorsBuffer();
            var previousColorPyramid = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
            ssgiTraceResources.colorPyramid = previousColorPyramid != null ? previousColorPyramid : TextureXR.GetBlackTexture();
            var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            ssgiTraceResources.historyDepth = historyDepthBuffer != null ? historyDepthBuffer : TextureXR.GetBlackTexture();

            // Output buffers
            ssgiTraceResources.hitPointBuffer = hitPointBuffer;

            // Output buffers
            ssgiTraceResources.outputBuffer = outputBuffer;

            return ssgiTraceResources;
        }

        static void ExecuteSSGITrace(CommandBuffer cmd, SSGITraceParameters parameters, SSGITraceResources resources)
        {
            int ssgiTileSize = 8;
            int numTilesXHR = (parameters.texWidth + (ssgiTileSize - 1)) / ssgiTileSize;
            int numTilesYHR = (parameters.texHeight + (ssgiTileSize - 1)) / ssgiTileSize;

            // Inject all the input scalars
            float n = parameters.nearClipPlane;
            float f = parameters.farClipPlane;
            float thicknessScale = 1.0f / (1.0f + parameters.thickness);
            float thicknessBias = -n / (f - n) * (parameters.thickness * thicknessScale);
            cmd.SetComputeFloatParam(parameters.ssGICS, HDShaderIDs._IndirectDiffuseThicknessScale, thicknessScale);
            cmd.SetComputeFloatParam(parameters.ssGICS, HDShaderIDs._IndirectDiffuseThicknessBias, thicknessBias);
            cmd.SetComputeIntParam(parameters.ssGICS, HDShaderIDs._IndirectDiffuseSteps, parameters.raySteps);
            cmd.SetComputeFloatParam(parameters.ssGICS, HDShaderIDs._IndirectDiffuseMaximalRadius, parameters.maximalRadius);
            // Inject half screen size if required
            if (!parameters.fullResolutionSS)
                cmd.SetComputeVectorParam(parameters.ssGICS, HDShaderIDs._HalfScreenSize, parameters.halfScreenSize);

            // Inject the ray-tracing sampling data
            BlueNoise.BindDitheredTextureSet(cmd, parameters.ditheredTextureSet);

            // Inject all the input textures/buffers
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.traceKernel, HDShaderIDs._DepthTexture, resources.depthTexture);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.traceKernel, HDShaderIDs._NormalBufferTexture, resources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.traceKernel, HDShaderIDs._IndirectDiffuseHitPointTextureRW, resources.hitPointBuffer);
            cmd.SetComputeBufferParam(parameters.ssGICS, parameters.traceKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, parameters.offsetBuffer);

            // Do the ray marching
            cmd.DispatchCompute(parameters.ssGICS, parameters.traceKernel, numTilesXHR, numTilesYHR, parameters.viewCount);

            // Update global constant buffer.
            // This should probably be a shader specific uniform instead of reusing the global constant buffer one since it's the only one updated here.
            parameters.shaderVariablesRayTracingCB._RaytracingIntensityClamp = parameters.clampValueSS;
            ConstantBuffer.PushGlobal(cmd, parameters.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

            // Inject all the input scalars
            cmd.SetComputeVectorParam(parameters.ssGICS, HDShaderIDs._ColorPyramidUvScaleAndLimitPrevFrame, parameters.colorPyramidUvScaleAndLimitPrevFrame);

            // Bind all the input buffers
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.projectKernel, HDShaderIDs._DepthTexture, resources.depthTexture);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.projectKernel, HDShaderIDs._NormalBufferTexture, resources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.projectKernel, HDShaderIDs._CameraMotionVectorsTexture, resources.motionVectorsBuffer);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.projectKernel, HDShaderIDs._IndirectDiffuseHitPointTexture, resources.hitPointBuffer);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.projectKernel, HDShaderIDs._ColorPyramidTexture, resources.colorPyramid);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.projectKernel, HDShaderIDs._HistoryDepthTexture, resources.historyDepth);
            cmd.SetComputeBufferParam(parameters.ssGICS, parameters.projectKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, parameters.offsetBuffer);

            // Bind the output texture
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.projectKernel, HDShaderIDs._IndirectDiffuseTextureRW, resources.outputBuffer);

            // Do the reprojection
            cmd.DispatchCompute(parameters.ssGICS, parameters.projectKernel, numTilesXHR, numTilesYHR, parameters.viewCount);
        }

        struct SSGIUpscaleParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;
            public Vector4 halfScreenSize;

            // Generation parameters
            public Vector2 firstMipOffset;

            // Compute Shader
            public ComputeShader bilateralUpsampleCS;
            public int upscaleKernel;
        }

        SSGIUpscaleParameters PrepareSSGIUpscaleParameters(HDCamera hdCamera, GlobalIllumination settings)
        {
            SSGIUpscaleParameters parameters = new SSGIUpscaleParameters();

            // Set the camera parameters
            parameters.texWidth = hdCamera.actualWidth;
            parameters.texHeight = hdCamera.actualHeight;
            parameters.viewCount = hdCamera.viewCount;
            parameters.halfScreenSize.Set(parameters.texWidth / 2, parameters.texHeight / 2, 1.0f / (parameters.texWidth * 0.5f), 1.0f / (parameters.texHeight * 0.5f));

            // Set the generation parameters
            var info = m_SharedRTManager.GetDepthBufferMipChainInfo();
            parameters.firstMipOffset.Set(HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].x), HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].y));

            // Grab the right kernel
            parameters.bilateralUpsampleCS = m_Asset.renderPipelineResources.shaders.bilateralUpsampleCS;
            parameters.upscaleKernel = m_BilateralUpSampleColorTMKernel;

            return parameters;
        }

        struct SSGIUpscaleResources
        {
            // Input buffers
            public RTHandle depthTexture;
            public RTHandle inputBuffer;

            // Output buffers
            public RTHandle outputBuffer;
        }

        SSGIUpscaleResources PrepareSSGIUpscaleResources(HDCamera hdCamera, RTHandle inputBuffer, RTHandle outputBuffer)
        {
            SSGIUpscaleResources ssgiUpscaleResources = new SSGIUpscaleResources();

            // Input buffers
            ssgiUpscaleResources.depthTexture = m_SharedRTManager.GetDepthTexture();
            ssgiUpscaleResources.inputBuffer = inputBuffer;

            // Output buffers
            ssgiUpscaleResources.outputBuffer = outputBuffer;

            return ssgiUpscaleResources;
        }

        static void ExecuteSSGIUpscale(CommandBuffer cmd, SSGIUpscaleParameters parameters, SSGIUpscaleResources resources)
        {
            // Re-evaluate the dispatch parameters (we are evaluating the upsample in full resolution)
            int ssgiTileSize = 8;
            int numTilesXHR = (parameters.texWidth + (ssgiTileSize - 1)) / ssgiTileSize;
            int numTilesYHR = (parameters.texHeight + (ssgiTileSize - 1)) / ssgiTileSize;

            // Inject the input scalars
            cmd.SetComputeVectorParam(parameters.bilateralUpsampleCS, HDShaderIDs._HalfScreenSize, parameters.halfScreenSize);
            cmd.SetComputeVectorParam(parameters.bilateralUpsampleCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, parameters.firstMipOffset);

            // Inject all the input buffers
            cmd.SetComputeTextureParam(parameters.bilateralUpsampleCS, parameters.upscaleKernel, HDShaderIDs._DepthTexture, resources.depthTexture);
            cmd.SetComputeTextureParam(parameters.bilateralUpsampleCS, parameters.upscaleKernel, HDShaderIDs._LowResolutionTexture, resources.inputBuffer);

            // Inject the output textures
            cmd.SetComputeTextureParam(parameters.bilateralUpsampleCS, parameters.upscaleKernel, HDShaderIDs._OutputUpscaledTexture, resources.outputBuffer);

            // Upscale the buffer to full resolution
            cmd.DispatchCompute(parameters.bilateralUpsampleCS, parameters.upscaleKernel, numTilesXHR, numTilesYHR, parameters.viewCount);
        }

        void RenderSSGI(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // Grab the global illumination volume component
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            // Based on if we are doing it in half resolution or full, we need to define initial and final buffer to avoid a useless blit
            RTHandle buffer0, buffer1;
            if (giSettings.fullResolutionSS)
            {
                buffer0 = m_IndirectDiffuseBuffer0;
                buffer1 = m_IndirectDiffuseBuffer1;
            }
            else
            {
                buffer0 = m_IndirectDiffuseBuffer1;
                buffer1 = m_IndirectDiffuseBuffer0;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SSGIPass)))
            {
                // Trace the signal
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SSGITrace)))
                {
                    SSGITraceParameters parameters = PrepareSSGITraceParameters(hdCamera, giSettings);
                    SSGITraceResources resources = PrepareSSGITraceResources(hdCamera, buffer0, m_IndirectDiffuseHitPointBuffer);
                    ExecuteSSGITrace(cmd, parameters, resources);
                }

                // Denoise it
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SSGIDenoise)))
                {
                    float historyValidity = EvaluateHistoryValidity(hdCamera);

                    SSGIDenoiser ssgiDenoiser = GetSSGIDenoiser();
                    ssgiDenoiser.Denoise(cmd, hdCamera, buffer0, buffer1, halfResolution: !giSettings.fullResolutionSS, historyValidity: historyValidity);
                }

                // Upscale it if required
                // If this was a half resolution effect, we still have to upscale it
                if (!giSettings.fullResolutionSS)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SSGIUpscale)))
                    {
                        ComputeShader bilateralUpsampleCS = m_Asset.renderPipelineResources.shaders.bilateralUpsampleCS;

                        SSGIUpscaleParameters parameters = PrepareSSGIUpscaleParameters(hdCamera, giSettings);
                        SSGIUpscaleResources resources = PrepareSSGIUpscaleResources(hdCamera, buffer0, buffer1);
                        ExecuteSSGIUpscale(cmd, parameters, resources);
                    }
                }

                (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, m_IndirectDiffuseBuffer0, FullScreenDebugMode.ScreenSpaceGlobalIllumination);
            }
        }
    }
}
