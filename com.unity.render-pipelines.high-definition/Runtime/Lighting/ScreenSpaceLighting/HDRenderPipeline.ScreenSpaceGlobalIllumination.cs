using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // The set of kernels that we shall be using
        int m_TraceGlobalIlluminationKernel;
        int m_TraceGlobalIlluminationHalfKernel;
        int m_ReprojectGlobalIlluminationKernel;
        int m_ReprojectGlobalIlluminationHalfKernel;
        int m_BilateralUpSampleColorKernel;
        int m_ConvertSSGIKernel;
        int m_ConvertSSGIHalfKernel;

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
                m_ConvertSSGIKernel = ssGICS.FindKernel("ConvertSSGI");
                m_ConvertSSGIHalfKernel = ssGICS.FindKernel("ConvertSSGIHalf");
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

        class TraceSSGIPassData
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
            public int frameIndex;
            public Vector4 colorPyramidUvScaleAndLimitPrevFrame;

            // Compute Shader
            public ComputeShader ssGICS;
            public int traceKernel;
            public int projectKernel;

            // Other parameters
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public ComputeBuffer offsetBuffer;

            // Prepass buffers
            public TextureHandle depthTexture;
            public TextureHandle stencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorsBuffer;

            // History buffers
            public TextureHandle colorPyramid;
            public TextureHandle historyDepth;

            // Intermediate buffers
            public TextureHandle hitPointBuffer;

            // Input signal buffers
            public TextureHandle outputBuffer0;
            public TextureHandle outputBuffer1;
        }

        struct TraceOutput
        {
            public TextureHandle outputBuffer0;
            public TextureHandle outputBuffer1;
        }

        TraceOutput TraceSSGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination giSettings, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle stencilBuffer, TextureHandle motionVectorsBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<TraceSSGIPassData>("Trace SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGITrace)))
            {
                builder.EnableAsyncCompute(false);

                //if (true)
                {
                    passData.texWidth = hdCamera.actualWidth;
                    passData.texHeight = hdCamera.actualHeight;
                    passData.halfScreenSize.Set(passData.texWidth * 0.5f, passData.texHeight * 0.5f, 2.0f / passData.texWidth, 2.0f / passData.texHeight);
                }
                /*
                else
                {
                    passData.texWidth = hdCamera.actualWidth / 2;
                    passData.texHeight = hdCamera.actualHeight / 2;
                    passData.halfScreenSize.Set(passData.texWidth, passData.texHeight, 1.0f / passData.texWidth, 1.0f / passData.texHeight);
                }
                */
                passData.viewCount = hdCamera.viewCount;

                // Set the generation parameters
                passData.nearClipPlane = hdCamera.camera.nearClipPlane;
                passData.farClipPlane = hdCamera.camera.farClipPlane;
                passData.fullResolutionSS = true;
                passData.thickness = giSettings.depthBufferThickness.value;
                passData.raySteps = giSettings.raySteps;
                passData.frameIndex = RayTracingFrameIndex(hdCamera, 16);
                passData.colorPyramidUvScaleAndLimitPrevFrame = HDUtils.ComputeViewportScaleAndLimit(hdCamera.historyRTHandleProperties.previousViewportSize, hdCamera.historyRTHandleProperties.previousRenderTargetSize);

                // Grab the right kernel
                passData.ssGICS = asset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS;
                passData.traceKernel = true ? m_TraceGlobalIlluminationKernel : m_TraceGlobalIlluminationHalfKernel;
                passData.projectKernel = true ? m_ReprojectGlobalIlluminationKernel : m_ReprojectGlobalIlluminationHalfKernel;

                BlueNoise blueNoise = GetBlueNoiseManager();
                passData.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
                passData.offsetBuffer = m_DepthBufferMipChainInfo.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);

                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);

                if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ObjectMotionVectors))
                {
                    passData.motionVectorsBuffer = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);
                }
                else
                {
                    passData.motionVectorsBuffer = builder.ReadTexture(motionVectorsBuffer);
                }

                // History buffers
                var colorPyramid = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                passData.colorPyramid = colorPyramid != null ? builder.ReadTexture(renderGraph.ImportTexture(colorPyramid)) : renderGraph.defaultResources.blackTextureXR;
                var historyDepth = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
                passData.historyDepth = historyDepth != null ? builder.ReadTexture(renderGraph.ImportTexture(historyDepth)) : renderGraph.defaultResources.blackTextureXR;

                // Temporary textures
                passData.hitPointBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "SSGI Hit Point"});

                // Output textures
                passData.outputBuffer0 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "SSGI Color"}));
                passData.outputBuffer1 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "SSGI Additional Data" }));

                builder.SetRenderFunc(
                    (TraceSSGIPassData data, RenderGraphContext ctx) =>
                    {
                        int ssgiTileSize = 8;
                        int numTilesXHR = (data.texWidth + (ssgiTileSize - 1)) / ssgiTileSize;
                        int numTilesYHR = (data.texHeight + (ssgiTileSize - 1)) / ssgiTileSize;

                        // Inject all the input scalars
                        float n = data.nearClipPlane;
                        float f = data.farClipPlane;
                        float thicknessScale = 1.0f / (1.0f + data.thickness);
                        float thicknessBias = -n / (f - n) * (data.thickness * thicknessScale);
                        ctx.cmd.SetComputeFloatParam(data.ssGICS, HDShaderIDs._IndirectDiffuseThicknessScale, thicknessScale);
                        ctx.cmd.SetComputeFloatParam(data.ssGICS, HDShaderIDs._IndirectDiffuseThicknessBias, thicknessBias);
                        ctx.cmd.SetComputeIntParam(data.ssGICS, HDShaderIDs._IndirectDiffuseSteps, data.raySteps);
                        ctx.cmd.SetComputeIntParam(data.ssGICS, HDShaderIDs._IndirectDiffuseFrameIndex, data.frameIndex);
                        // Inject half screen size if required
                        if (!data.fullResolutionSS)
                            ctx.cmd.SetComputeVectorParam(data.ssGICS, HDShaderIDs._HalfScreenSize, data.halfScreenSize);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.ditheredTextureSet);

                        // Inject all the input textures/buffers
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.traceKernel, HDShaderIDs._DepthTexture, data.depthTexture);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.traceKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.traceKernel, HDShaderIDs._IndirectDiffuseHitPointTextureRW, data.hitPointBuffer);
                        ctx.cmd.SetComputeBufferParam(data.ssGICS, data.traceKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, data.offsetBuffer);

                        // Do the ray marching
                        ctx.cmd.DispatchCompute(data.ssGICS, data.traceKernel, numTilesXHR, numTilesYHR, data.viewCount);

                        // Update global constant buffer.
                        // This should probably be a shader specific uniform instead of reusing the global constant buffer one since it's the only one updated here.
                        ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Inject all the input scalars
                        ctx.cmd.SetComputeVectorParam(data.ssGICS, HDShaderIDs._ColorPyramidUvScaleAndLimitPrevFrame, data.colorPyramidUvScaleAndLimitPrevFrame);
                        ctx.cmd.SetComputeIntParam(data.ssGICS, HDShaderIDs._ObjectMotionStencilBit, (int)StencilUsage.ObjectMotionVector);

                        // Bind all the input buffers
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._DepthTexture, data.depthTexture);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._StencilTexture, data.stencilBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorsBuffer);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._IndirectDiffuseHitPointTexture, data.hitPointBuffer);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._ColorPyramidTexture, data.colorPyramid);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._HistoryDepthTexture, data.historyDepth);
                        ctx.cmd.SetComputeBufferParam(data.ssGICS, data.projectKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, data.offsetBuffer);

                        // Bind the output texture
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._IndirectDiffuseTexture0RW, data.outputBuffer0);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._IndirectDiffuseTexture1RW, data.outputBuffer1);

                        // Do the reprojection
                        ctx.cmd.DispatchCompute(data.ssGICS, data.projectKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });

                TraceOutput traceOutput = new TraceOutput();
                traceOutput.outputBuffer0 = passData.outputBuffer0;
                traceOutput.outputBuffer1 = passData.outputBuffer1;
                return traceOutput;
            }
        }

        class UpscaleSSGIPassData
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

            public TextureHandle depthTexture;
            public TextureHandle inputBuffer;
            public TextureHandle outputBuffer;
        }

        TextureHandle UpscaleSSGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination giSettings, HDUtils.PackedMipChainInfo info, TextureHandle depthPyramid, TextureHandle inputBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<UpscaleSSGIPassData>("Upscale SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGIUpscale)))
            {
                builder.EnableAsyncCompute(false);

                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;
                passData.halfScreenSize.Set(passData.texWidth / 2, passData.texHeight / 2, 1.0f / (passData.texWidth * 0.5f), 1.0f / (passData.texHeight * 0.5f));

                // Set the generation parameters
                passData.firstMipOffset.Set(HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].x), HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].y));

                // Grab the right kernel
                passData.bilateralUpsampleCS = m_Asset.renderPipelineResources.shaders.bilateralUpsampleCS;
                passData.upscaleKernel = m_BilateralUpSampleColorKernel;

                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.inputBuffer = builder.ReadTexture(inputBuffer);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Final" }));

                builder.SetRenderFunc(
                    (UpscaleSSGIPassData data, RenderGraphContext ctx) =>
                    {
                        // Re-evaluate the dispatch parameters (we are evaluating the upsample in full resolution)
                        int ssgiTileSize = 8;
                        int numTilesXHR = (data.texWidth + (ssgiTileSize - 1)) / ssgiTileSize;
                        int numTilesYHR = (data.texHeight + (ssgiTileSize - 1)) / ssgiTileSize;

                        // Inject the input scalars
                        ctx.cmd.SetComputeVectorParam(data.bilateralUpsampleCS, HDShaderIDs._HalfScreenSize, data.halfScreenSize);
                        ctx.cmd.SetComputeVectorParam(data.bilateralUpsampleCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, data.firstMipOffset);

                        // Inject all the input buffers
                        ctx.cmd.SetComputeTextureParam(data.bilateralUpsampleCS, data.upscaleKernel, HDShaderIDs._DepthTexture, data.depthTexture);
                        ctx.cmd.SetComputeTextureParam(data.bilateralUpsampleCS, data.upscaleKernel, HDShaderIDs._LowResolutionTexture, data.inputBuffer);

                        // Inject the output textures
                        ctx.cmd.SetComputeTextureParam(data.bilateralUpsampleCS, data.upscaleKernel, HDShaderIDs._OutputUpscaledTexture, data.outputBuffer);

                        // Upscale the buffer to full resolution
                        ctx.cmd.DispatchCompute(data.bilateralUpsampleCS, data.upscaleKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });
                return passData.outputBuffer;
            }
        }

        class ConvertSSGIPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Compute Shader
            public ComputeShader ssGICS;
            public int convertKernel;
            public ComputeBuffer offsetBuffer;

            // Prepass buffers
            public TextureHandle depthTexture;
            public TextureHandle stencilBuffer;
            public TextureHandle normalBuffer;

            // Input buffers
            public TextureHandle inputBuffer0;
            public TextureHandle inputBuffer1;

            // Output buffer
            public TextureHandle outputBuffer;
        }

        TextureHandle ConvertSSGI(RenderGraph renderGraph, HDCamera hdCamera, bool halfResolution,
            TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer,
            TextureHandle inputBuffer0, TextureHandle inputBuffer1)
        {
            using (var builder = renderGraph.AddRenderPass<ConvertSSGIPassData>("Convert SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGIConvert)))
            {
                builder.EnableAsyncCompute(false);

                // Set the camera parameters
                if (!halfResolution)
                {
                    passData.texWidth = hdCamera.actualWidth;
                    passData.texHeight = hdCamera.actualHeight;
                }
                else
                {
                    passData.texWidth = hdCamera.actualWidth / 2;
                    passData.texHeight = hdCamera.actualHeight / 2;
                }
                passData.viewCount = hdCamera.viewCount;

                // Grab the right kernel
                passData.ssGICS = m_Asset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS;
                passData.convertKernel = halfResolution ? m_ConvertSSGIHalfKernel : m_ConvertSSGIKernel;

                passData.offsetBuffer = m_DepthBufferMipChainInfo.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);

                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.inputBuffer0 = builder.ReadWriteTexture(inputBuffer0);
                passData.inputBuffer1 = builder.ReadWriteTexture(inputBuffer1);
                // Output buffer
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Converted" }));

                builder.SetRenderFunc(
                    (ConvertSSGIPassData data, RenderGraphContext ctx) =>
                    {
                        // Re-evaluate the dispatch parameters (we are evaluating the upsample in full resolution)
                        int ssgiTileSize = 8;
                        int numTilesXHR = (data.texWidth + (ssgiTileSize - 1)) / ssgiTileSize;
                        int numTilesYHR = (data.texHeight + (ssgiTileSize - 1)) / ssgiTileSize;

                        // Scalars
                        ctx.cmd.SetComputeIntParams(data.ssGICS, HDShaderIDs._SsrStencilBit, (int)StencilUsage.TraceReflectionRay);

                        // Prepass data
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.convertKernel, HDShaderIDs._DepthTexture, data.depthTexture);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.convertKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.convertKernel, HDShaderIDs._StencilTexture, data.stencilBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeBufferParam(data.ssGICS, data.convertKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, data.offsetBuffer);

                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.convertKernel, HDShaderIDs._IndirectDiffuseTexture0, data.inputBuffer0);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.convertKernel, HDShaderIDs._IndirectDiffuseTexture1, data.inputBuffer1);

                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.convertKernel, HDShaderIDs._IndirectDiffuseTextureRW, data.outputBuffer);

                        ctx.cmd.DispatchCompute(data.ssGICS, data.convertKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });
                return passData.outputBuffer;
            }
        }

        TextureHandle RenderSSGI(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer,
            ShaderVariablesRaytracing shaderVariablesRayTracingCB, HDUtils.PackedMipChainInfo info)
        {
            // Grab the global illumination volume component
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.SSGIPass)))
            {
                // Trace the signal
                TraceOutput traceOutput = TraceSSGI(renderGraph, hdCamera, giSettings, depthPyramid, normalBuffer, stencilBuffer, motionVectorsBuffer);

                // Evaluate the history validity
                float historyValidity = EvaluateIndirectDiffuseHistoryValidityCombined(hdCamera, true, false);
                SSGIDenoiser ssgiDenoiser = GetSSGIDenoiser();
                SSGIDenoiser.SSGIDenoiserOutput denoiserOutput = ssgiDenoiser.Denoise(renderGraph, hdCamera, depthPyramid, normalBuffer, motionVectorsBuffer, traceOutput.outputBuffer0, traceOutput.outputBuffer1, m_DepthBufferMipChainInfo, false, historyValidity: historyValidity);
                // Propagate the history
                PropagateIndirectDiffuseHistoryValidityCombined(hdCamera, true, false);

                // Convert back the result to RGB space
                TextureHandle colorBuffer = ConvertSSGI(renderGraph, hdCamera, false, depthPyramid, stencilBuffer, normalBuffer, denoiserOutput.outputBuffer0, denoiserOutput.outputBuffer1);
                /*
                // Upscale it if required
                // If this was a half resolution effect, we still have to upscale it
                if (!giSettings.fullResolutionSS)
                    colorBuffer = UpscaleSSGI(renderGraph, hdCamera, giSettings, info, depthPyramid, colorBuffer);
                */
                return colorBuffer;
            }
        }

        TextureHandle RenderScreenSpaceIndirectDiffuse(HDCamera hdCamera, in PrepassOutput prepassOutput, TextureHandle rayCountTexture, TextureHandle historyValidationTexture)
        {
            TextureHandle result;
            switch (GetIndirectDiffuseMode(hdCamera))
            {
                case IndirectDiffuseMode.ScreenSpace:
                    result = RenderSSGI(m_RenderGraph, hdCamera, prepassOutput.depthPyramidTexture, prepassOutput.stencilBuffer, prepassOutput.normalBuffer, prepassOutput.resolvedMotionVectorsBuffer, m_ShaderVariablesRayTracingCB, GetDepthBufferMipChainInfo());
                    break;

                case IndirectDiffuseMode.Raytrace:
                    result = RenderRayTracedIndirectDiffuse(m_RenderGraph, hdCamera,
                        prepassOutput.depthBuffer, prepassOutput.stencilBuffer, prepassOutput.normalBuffer, prepassOutput.resolvedMotionVectorsBuffer, historyValidationTexture, m_SkyManager.GetSkyReflection(hdCamera), rayCountTexture,
                        m_ShaderVariablesRayTracingCB);
                    break;
                default:
                    result =  m_RenderGraph.defaultResources.blackTextureXR;
                    break;
            }
            PushFullScreenDebugTexture(m_RenderGraph, result, FullScreenDebugMode.ScreenSpaceGlobalIllumination);
            return result;
        }
    }
}
