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
        RTHandle m_IndirectDiffuseBuffer2 = null;
        RTHandle m_IndirectDiffuseBuffer3 = null;
        RTHandle m_IndirectDiffuseHitPointBuffer = null;

        // The set of kernels that we shall be using
        int m_TraceGlobalIlluminationKernel;
        int m_TraceGlobalIlluminationHalfKernel;
        int m_ReprojectGlobalIlluminationKernel;
        int m_ReprojectGlobalIlluminationHalfKernel;
        int m_BilateralUpSampleColorKernel;
        int m_ConvertYCoCgToRGBKernel;
        int m_ConvertYCoCgToRGBHalfKernel;

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
                m_BilateralUpSampleColorKernel = bilateralUpsampleCS.FindKernel("BilateralUpSampleColor");
                m_ConvertYCoCgToRGBKernel = ssGICS.FindKernel("ConvertYCoCgToRGB");
                m_ConvertYCoCgToRGBHalfKernel = ssGICS.FindKernel("ConvertYCoCgToRGBHalf");
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
            public RTHandle outputBuffer0;
            public RTHandle outputBuffer1;
        }

        SSGITraceResources PrepareSSGITraceResources(HDCamera hdCamera, RTHandle outputBuffer0, RTHandle outputBuffer1, RTHandle hitPointBuffer)
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
            ssgiTraceResources.outputBuffer0 = outputBuffer0;
            ssgiTraceResources.outputBuffer1 = outputBuffer1;

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
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.projectKernel, HDShaderIDs._IndirectDiffuseTexture0RW, resources.outputBuffer0);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.projectKernel, HDShaderIDs._IndirectDiffuseTexture1RW, resources.outputBuffer1);

            // Do the reprojection
            cmd.DispatchCompute(parameters.ssGICS, parameters.projectKernel, numTilesXHR, numTilesYHR, parameters.viewCount);
        }

        struct SSGIConvertParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Compute Shader
            public ComputeShader ssGICS;
            public int convertKernel;
            public ComputeBuffer offsetBuffer;
        }

        SSGIConvertParameters PrepareSSGIConvertParameters(HDCamera hdCamera, bool halfResolution)
        {
            SSGIConvertParameters parameters = new SSGIConvertParameters();

            // Set the camera parameters
            if (!halfResolution)
            {
                parameters.texWidth = hdCamera.actualWidth;
                parameters.texHeight = hdCamera.actualHeight;
            }
            else
            {
                parameters.texWidth = hdCamera.actualWidth / 2;
                parameters.texHeight = hdCamera.actualHeight / 2;
            }
            parameters.viewCount = hdCamera.viewCount;

            // Grab the right kernel
            parameters.ssGICS = m_Asset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS;
            parameters.convertKernel = halfResolution? m_ConvertYCoCgToRGBHalfKernel : m_ConvertYCoCgToRGBKernel;

            var info = m_SharedRTManager.GetDepthBufferMipChainInfo();
            parameters.offsetBuffer = info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);

            return parameters;
        }

        struct SSGIConvertResources
        {
            public RTHandle depthTexture;
            public RTHandle stencilBuffer;
            public RTHandle normalBuffer;
            public RTHandle inoutBuffer0;
            public RTHandle inputBufer1;
        }

        SSGIConvertResources PrepareSSGIConvertResources(HDCamera hdCamera, RTHandle inoutBuffer0, RTHandle outputBuffer1)
        {
            SSGIConvertResources resources = new SSGIConvertResources();

            // Input buffers
            resources.depthTexture = m_SharedRTManager.GetDepthTexture();
            resources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            resources.stencilBuffer = m_SharedRTManager.GetStencilBuffer();
            // Output buffers
            resources.inoutBuffer0 = inoutBuffer0;
            resources.inputBufer1 = outputBuffer1;

            return resources;
        }

        static void ExecuteSSGIConversion(CommandBuffer cmd, SSGIConvertParameters parameters, SSGIConvertResources resources)
        {
            // Re-evaluate the dispatch parameters (we are evaluating the upsample in full resolution)
            int ssgiTileSize = 8;
            int numTilesXHR = (parameters.texWidth + (ssgiTileSize - 1)) / ssgiTileSize;
            int numTilesYHR = (parameters.texHeight + (ssgiTileSize - 1)) / ssgiTileSize;

            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.convertKernel, HDShaderIDs._DepthTexture, resources.depthTexture);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.convertKernel, HDShaderIDs._NormalBufferTexture, resources.normalBuffer);
            cmd.SetComputeBufferParam(parameters.ssGICS, parameters.convertKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, parameters.offsetBuffer);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.convertKernel, HDShaderIDs._IndirectDiffuseTexture0RW, resources.inoutBuffer0);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.convertKernel, HDShaderIDs._IndirectDiffuseTexture1, resources.inputBufer1);
            cmd.SetComputeTextureParam(parameters.ssGICS, parameters.convertKernel, HDShaderIDs._StencilTexture, resources.stencilBuffer, 0, RenderTextureSubElement.Stencil);
            cmd.SetComputeIntParams(parameters.ssGICS, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);
            cmd.DispatchCompute(parameters.ssGICS, parameters.convertKernel, numTilesXHR, numTilesYHR, parameters.viewCount);
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

        SSGIUpscaleParameters PrepareSSGIUpscaleParameters(HDCamera hdCamera, GlobalIllumination settings, HDUtils.PackedMipChainInfo info)
        {
            SSGIUpscaleParameters parameters = new SSGIUpscaleParameters();

            // Set the camera parameters
            parameters.texWidth = hdCamera.actualWidth;
            parameters.texHeight = hdCamera.actualHeight;
            parameters.viewCount = hdCamera.viewCount;
            parameters.halfScreenSize.Set(parameters.texWidth / 2, parameters.texHeight / 2, 1.0f / (parameters.texWidth * 0.5f), 1.0f / (parameters.texHeight * 0.5f));

            // Set the generation parameters
            parameters.firstMipOffset.Set(HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].x), HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].y));

            // Grab the right kernel
            parameters.bilateralUpsampleCS = m_Asset.renderPipelineResources.shaders.bilateralUpsampleCS;
            parameters.upscaleKernel = m_BilateralUpSampleColorKernel;

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

        private float EvaluateIndirectDiffuseHistoryValidityCombined(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Evaluate the history validity
            float effectHistoryValidity = hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination0, fullResolution, rayTraced)
                                          && hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination1, fullResolution, rayTraced) ? 1.0f : 0.0f;
            return EvaluateHistoryValidity(hdCamera) * effectHistoryValidity;
        }

        private float EvaluateIndirectDiffuseHistoryValidity0(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Evaluate the history validity
            float effectHistoryValidity = hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination0, fullResolution, rayTraced) ? 1.0f : 0.0f;
            return EvaluateHistoryValidity(hdCamera) * effectHistoryValidity;
        }

        private float EvaluateIndirectDiffuseHistoryValidity1(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Evaluate the history validity
            float effectHistoryValidity = hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination1, fullResolution, rayTraced) ? 1.0f : 0.0f;
            return EvaluateHistoryValidity(hdCamera) * effectHistoryValidity;
        }

        private void PropagateIndirectDiffuseHistoryValidityCombined(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination0, fullResolution, rayTraced);
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination1, fullResolution, rayTraced);
        }

        private void PropagateIndirectDiffuseHistoryValidity0(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination0, fullResolution, rayTraced);
        }

        private void PropagateIndirectDiffuseHistoryValidity1(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination1, fullResolution, rayTraced);
        }

        void RenderSSGI(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // Grab the global illumination volume component
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            // Based on if we are doing it in half resolution or full, we need to define initial and final buffer to avoid a useless blit
            RTHandle buffer00, buffer01, buffer10, buffer11;
            if (giSettings.fullResolutionSS)
            {
                buffer00 = m_IndirectDiffuseBuffer0;
                buffer01 = m_IndirectDiffuseBuffer1;
                buffer10 = m_IndirectDiffuseBuffer2;
                buffer11 = m_IndirectDiffuseBuffer3;
            }
            else
            {
                buffer00 = m_IndirectDiffuseBuffer2;
                buffer01 = m_IndirectDiffuseBuffer3;
                buffer10 = m_IndirectDiffuseBuffer0;
                buffer11 = m_IndirectDiffuseBuffer1;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SSGIPass)))
            {
                // Trace the signal
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SSGITrace)))
                {
                    SSGITraceParameters parameters = PrepareSSGITraceParameters(hdCamera, giSettings);
                    SSGITraceResources resources = PrepareSSGITraceResources(hdCamera, buffer00, buffer01, m_IndirectDiffuseHitPointBuffer);
                    ExecuteSSGITrace(cmd, parameters, resources);
                }

                // Denoise it
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SSGIDenoise)))
                {
                    // Evaluate the history validity
                    float historyValidity = EvaluateIndirectDiffuseHistoryValidityCombined(hdCamera, giSettings.fullResolutionSS, false);

                    SSGIDenoiser ssgiDenoiser = GetSSGIDenoiser();
                    ssgiDenoiser.Denoise(cmd, hdCamera, buffer00, buffer01, buffer10, buffer11, halfResolution: !giSettings.fullResolutionSS, historyValidity: historyValidity);

                    // Propagate the history
                    PropagateIndirectDiffuseHistoryValidityCombined(hdCamera, giSettings.fullResolutionSS, false);
                }

                // Convert it
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SSGIConvert)))
                {
                    SSGIConvertParameters parameters = PrepareSSGIConvertParameters(hdCamera, !giSettings.fullResolutionSS);
                    SSGIConvertResources resources = PrepareSSGIConvertResources(hdCamera, buffer00, buffer01);
                    ExecuteSSGIConversion(cmd, parameters, resources);
                }

                // Upscale it if required
                // If this was a half resolution effect, we still have to upscale it
                if (!giSettings.fullResolutionSS)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SSGIUpscale)))
                    {
                        ComputeShader bilateralUpsampleCS = m_Asset.renderPipelineResources.shaders.bilateralUpsampleCS;

                        SSGIUpscaleParameters parameters = PrepareSSGIUpscaleParameters(hdCamera, giSettings, m_SharedRTManager.GetDepthBufferMipChainInfo());
                        SSGIUpscaleResources resources = PrepareSSGIUpscaleResources(hdCamera, buffer00, buffer10);
                        ExecuteSSGIUpscale(cmd, parameters, resources);
                    }
                }

                (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, m_IndirectDiffuseBuffer0, FullScreenDebugMode.ScreenSpaceGlobalIllumination);
            }
        }
    }
}
