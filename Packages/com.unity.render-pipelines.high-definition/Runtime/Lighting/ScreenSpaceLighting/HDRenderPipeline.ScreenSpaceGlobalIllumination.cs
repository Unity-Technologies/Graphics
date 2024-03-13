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
        int m_BilateralUpSampleColorKernelHalf;
        int m_BilateralUpSampleColorKernel;

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
                m_BilateralUpSampleColorKernelHalf = bilateralUpsampleCS.FindKernel("BilateralUpSampleColorHalf");
                m_BilateralUpSampleColorKernel = bilateralUpsampleCS.FindKernel("BilateralUpSampleColor");
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
                    bool allowSsgi = hdCamera.colorPyramidHistoryIsValid && !hdCamera.isFirstFrame;

                    // We can use the ray tracing version of the effect if:
                    // - It is enabled in the frame settings
                    // - It is enabled in the volume
                    // - The RTAS has been build validated
                    // - The RTLightCluster has been validated
                    bool raytracing = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing)
                        && settings.tracing.value != RayCastingMode.RayMarching
                        && GetRayTracingState();
                    if (raytracing)
                        mode = settings.tracing.value == RayCastingMode.RayTracing ? IndirectDiffuseMode.RayTraced : IndirectDiffuseMode.Mixed;
                    else
                        mode = allowSsgi ? IndirectDiffuseMode.ScreenSpace : IndirectDiffuseMode.Off;
                }
            }
            return mode;
        }

        int CombineIndirectDiffuseHistoryStateToMask(bool fullResolution, bool rayTraced)
        {
            // Combine the flags to define the current mask
            int flagMask = 0;
            flagMask |= (fullResolution ? (int)HDCamera.HistoryEffectFlags.FullResolution : 0);
            flagMask |= (rayTraced ? (int)HDCamera.HistoryEffectFlags.RayTraced : 0);
            return flagMask;
        }

        private float EvaluateIndirectDiffuseHistoryValidityCombined(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            int flagMask = CombineIndirectDiffuseHistoryStateToMask(fullResolution, rayTraced);
            // Evaluate the history validity
            float effectHistoryValidity = hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination0, flagMask) && hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination1, flagMask) ? 1.0f : 0.0f;
            return EvaluateHistoryValidity(hdCamera) * effectHistoryValidity;
        }

        private float EvaluateIndirectDiffuseHistoryValidity0(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Combine the flags to define the current mask
            int flagMask = CombineIndirectDiffuseHistoryStateToMask(fullResolution, rayTraced);
            // Evaluate the history validity
            float effectHistoryValidity = hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination0, flagMask) ? 1.0f : 0.0f;
            return EvaluateHistoryValidity(hdCamera) * effectHistoryValidity;
        }

        private float EvaluateIndirectDiffuseHistoryValidity1(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Combine the flags to define the current mask
            int flagMask = CombineIndirectDiffuseHistoryStateToMask(fullResolution, rayTraced);
            // Evaluate the history validity
            float effectHistoryValidity = hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination1, flagMask) ? 1.0f : 0.0f;
            return EvaluateHistoryValidity(hdCamera) * effectHistoryValidity;
        }

        private void PropagateIndirectDiffuseHistoryValidityCombined(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Combine the flags to define the current mask
            int flagMask = CombineIndirectDiffuseHistoryStateToMask(fullResolution, rayTraced);
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination0, flagMask);
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination1, flagMask);
        }

        private void PropagateIndirectDiffuseHistoryValidity0(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Combine the flags to define the current mask
            int flagMask = CombineIndirectDiffuseHistoryStateToMask(fullResolution, rayTraced);
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination0, flagMask);
        }

        private void PropagateIndirectDiffuseHistoryValidity1(HDCamera hdCamera, bool fullResolution, bool rayTraced)
        {
            // Combine the flags to define the current mask
            int flagMask = CombineIndirectDiffuseHistoryStateToMask(fullResolution, rayTraced);
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.GlobalIllumination1, flagMask);
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
            public int rayMiss;
            public float lowResPercentage;

            // Compute Shader
            public ComputeShader ssGICS;
            public int traceKernel;
            public int projectKernel;

            // Other parameters
            public BlueNoise.DitheredTextureSet ditheredTextureSet;
            public ShaderVariablesRaytracing shaderVariablesRayTracingCB;
            public ComputeBuffer offsetBuffer;

            // Prepass buffers
            public ComputeBufferHandle lightList;
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
            public TextureHandle outputBuffer;
        }

        internal Vector2 GetSSGILowResPercentage(HDCamera hdCamera)
        {
            return new Vector2(hdCamera.lowResScaleForScreenSpaceLighting, hdCamera.historyLowResScaleForScreenSpaceLighting);
        }

        TextureHandle TraceSSGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination giSettings, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle stencilBuffer, TextureHandle motionVectorsBuffer, ComputeBufferHandle lightList)
        {
            using (var builder = renderGraph.AddRenderPass<TraceSSGIPassData>("Trace SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGITrace)))
            {
                builder.EnableAsyncCompute(false);

                if (giSettings.fullResolutionSS.value)
                {
                    passData.lowResPercentage = 1.0f;
                    passData.texWidth = hdCamera.actualWidth;
                    passData.texHeight = hdCamera.actualHeight;
                }
                else
                {
                    float ssgiLowResMultiplier = GetSSGILowResPercentage(hdCamera).x;
                    passData.lowResPercentage = ssgiLowResMultiplier;
                    passData.texWidth = (int)Mathf.Floor(hdCamera.actualWidth * ssgiLowResMultiplier);
                    passData.texHeight = (int)Mathf.Floor(hdCamera.actualHeight * ssgiLowResMultiplier);
                }
                passData.viewCount = hdCamera.viewCount;

                // Set the generation parameters
                passData.nearClipPlane = hdCamera.camera.nearClipPlane;
                passData.farClipPlane = hdCamera.camera.farClipPlane;
                passData.fullResolutionSS = true;
                passData.thickness = giSettings.depthBufferThickness.value;
                passData.raySteps = giSettings.maxRaySteps;
                passData.frameIndex = RayTracingFrameIndex(hdCamera, 16);
                passData.rayMiss = (int)giSettings.rayMiss.value;

                // Grab the right kernel
                passData.ssGICS = asset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS;
                passData.traceKernel = giSettings.fullResolutionSS.value ? m_TraceGlobalIlluminationKernel : m_TraceGlobalIlluminationHalfKernel;
                passData.projectKernel = giSettings.fullResolutionSS.value ? m_ReprojectGlobalIlluminationKernel : m_ReprojectGlobalIlluminationHalfKernel;

                BlueNoise blueNoise = GetBlueNoiseManager();
                passData.ditheredTextureSet = blueNoise.DitheredTextureSet8SPP();
                passData.shaderVariablesRayTracingCB = m_ShaderVariablesRayTracingCB;
                passData.offsetBuffer = hdCamera.depthBufferMipChainInfo.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);

                passData.lightList = builder.ReadComputeBuffer(lightList);
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
                { colorFormat = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "SSGI Hit Point" });

                // Output textures
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "SSGI Color" }));

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
                        ctx.cmd.SetComputeFloatParam(data.ssGICS, HDShaderIDs._RayMarchingThicknessScale, thicknessScale);
                        ctx.cmd.SetComputeFloatParam(data.ssGICS, HDShaderIDs._RayMarchingThicknessBias, thicknessBias);
                        ctx.cmd.SetComputeIntParam(data.ssGICS, HDShaderIDs._RayMarchingSteps, data.raySteps);
                        ctx.cmd.SetComputeIntParam(data.ssGICS, HDShaderIDs._RayMarchingReflectSky, 1);
                        ctx.cmd.SetComputeIntParam(data.ssGICS, HDShaderIDs._IndirectDiffuseFrameIndex, data.frameIndex);

                        // Inject the ray-tracing sampling data
                        BlueNoise.BindDitheredTextureSet(ctx.cmd, data.ditheredTextureSet);

                        // Inject all the input textures/buffers
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.traceKernel, HDShaderIDs._DepthTexture, data.depthTexture);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.traceKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.traceKernel, HDShaderIDs._IndirectDiffuseHitPointTextureRW, data.hitPointBuffer);
                        ctx.cmd.SetComputeBufferParam(data.ssGICS, data.traceKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, data.offsetBuffer);
                        ctx.cmd.SetComputeBufferParam(data.ssGICS, data.traceKernel, HDShaderIDs.g_vLightListTile, data.lightList);
                        ctx.cmd.SetComputeFloatParam(data.ssGICS, HDShaderIDs._RayMarchingLowResPercentageInv, 1.0f / data.lowResPercentage);

                        // Do the ray marching
                        ctx.cmd.DispatchCompute(data.ssGICS, data.traceKernel, numTilesXHR, numTilesYHR, data.viewCount);

                        // Update global constant buffer.
                        // This should probably be a shader specific uniform instead of reusing the global constant buffer one since it's the only one updated here.
                        ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                        // Inject all the input scalars
                        ctx.cmd.SetComputeIntParam(data.ssGICS, HDShaderIDs._ObjectMotionStencilBit, (int)StencilUsage.ObjectMotionVector);
                        ctx.cmd.SetComputeIntParam(data.ssGICS, HDShaderIDs._RayMarchingFallbackHierarchy, data.rayMiss);
                        ctx.cmd.SetComputeFloatParam(data.ssGICS, HDShaderIDs._RayMarchingLowResPercentageInv, 1.0f / data.lowResPercentage);

                        // Bind all the input buffers
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._DepthTexture, data.depthTexture);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._StencilTexture, data.stencilBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorsBuffer);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._IndirectDiffuseHitPointTexture, data.hitPointBuffer);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._ColorPyramidTexture, data.colorPyramid);
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._HistoryDepthTexture, data.historyDepth);
                        ctx.cmd.SetComputeBufferParam(data.ssGICS, data.projectKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, data.offsetBuffer);
                        ctx.cmd.SetComputeBufferParam(data.ssGICS, data.projectKernel, HDShaderIDs.g_vLightListTile, data.lightList);

                        // Bind the output texture
                        ctx.cmd.SetComputeTextureParam(data.ssGICS, data.projectKernel, HDShaderIDs._IndirectDiffuseTextureRW, data.outputBuffer);

                        // Do the re-projection
                        ctx.cmd.DispatchCompute(data.ssGICS, data.projectKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });

                return passData.outputBuffer;
            }
        }

        class UpscaleSSGIPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;
            public Vector4 halfScreenSize;
            public float lowResPercentage;
            public ShaderVariablesBilateralUpsample shaderVariablesBilateralUpsampleCB;

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

                float lowResPercentage = GetSSGILowResPercentage(hdCamera).x;
                passData.shaderVariablesBilateralUpsampleCB._HalfScreenSize = new Vector4(passData.texWidth / lowResPercentage, passData.texHeight / lowResPercentage, 1.0f / (passData.texWidth * lowResPercentage), 1.0f / (passData.texHeight * lowResPercentage));
                passData.lowResPercentage = lowResPercentage;
                unsafe
                {
                    for (int i = 0; i < 16; ++i)
                        passData.shaderVariablesBilateralUpsampleCB._DistanceBasedWeights[i] = BilateralUpsample.distanceBasedWeights_2x2[i];

                    for (int i = 0; i < 32; ++i)
                        passData.shaderVariablesBilateralUpsampleCB._TapOffsets[i] = BilateralUpsample.tapOffsets_2x2[i];
                }

                // Setup lowres bounds.
                int lowResWidth = (int)Mathf.Floor(passData.texWidth * passData.lowResPercentage);
                int lowResHeight = (int)Mathf.Floor(passData.texHeight * passData.lowResPercentage);
                passData.halfScreenSize.Set(lowResWidth, lowResHeight, 1.0f / lowResWidth, 1.0f / lowResHeight);

                // Grab the right kernel
                passData.bilateralUpsampleCS = m_Asset.renderPipelineResources.shaders.bilateralUpsampleCS;
                passData.upscaleKernel = passData.lowResPercentage == 0.5f ? m_BilateralUpSampleColorKernelHalf : m_BilateralUpSampleColorKernel;

                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.inputBuffer = builder.ReadTexture(inputBuffer);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "SSGI Final" }));

                builder.SetRenderFunc(
                    (UpscaleSSGIPassData data, RenderGraphContext ctx) =>
                    {
                        // Re-evaluate the dispatch parameters (we are evaluating the upsample in full resolution)
                        int ssgiTileSize = 8;
                        int numTilesXHR = (data.texWidth + (ssgiTileSize - 1)) / ssgiTileSize;
                        int numTilesYHR = (data.texHeight + (ssgiTileSize - 1)) / ssgiTileSize;

                        ConstantBuffer.PushGlobal(ctx.cmd, data.shaderVariablesBilateralUpsampleCB, HDShaderIDs._ShaderVariablesBilateralUpsample);

                        ctx.cmd.SetComputeFloatParam(data.bilateralUpsampleCS, HDShaderIDs._RayMarchingLowResPercentage, data.lowResPercentage);

                        // Inject all the input buffers
                        ctx.cmd.SetComputeTextureParam(data.bilateralUpsampleCS, data.upscaleKernel, HDShaderIDs._DepthTexture, data.depthTexture);
                        ctx.cmd.SetComputeTextureParam(data.bilateralUpsampleCS, data.upscaleKernel, HDShaderIDs._LowResolutionTexture, data.inputBuffer);
                        ctx.cmd.SetComputeVectorParam(data.bilateralUpsampleCS, HDShaderIDs._HalfScreenSize, data.halfScreenSize);

                        // Inject the output textures
                        ctx.cmd.SetComputeTextureParam(data.bilateralUpsampleCS, data.upscaleKernel, HDShaderIDs._OutputUpscaledTexture, data.outputBuffer);

                        // Upscale the buffer to full resolution
                        ctx.cmd.DispatchCompute(data.bilateralUpsampleCS, data.upscaleKernel, numTilesXHR, numTilesYHR, data.viewCount);
                    });
                return passData.outputBuffer;
            }
        }

        TextureHandle DenoiseSSGI(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle rtGIBuffer, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationTexture, bool fullResolution)
        {
            var giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
            if (giSettings.denoiseSS)
            {
                // Evaluate the history's validity
                float historyValidity0 = EvaluateIndirectDiffuseHistoryValidity0(hdCamera, fullResolution, false);
                Vector2 resolutionPercentages = fullResolution ? Vector2.one : GetSSGILowResPercentage(hdCamera);

                HDTemporalFilter temporalFilter = GetTemporalFilter();
                HDDiffuseDenoiser diffuseDenoiser = GetDiffuseDenoiser();

                // Run the temporal denoiser
                TextureHandle historyBufferHF = renderGraph.ImportTexture(RequestIndirectDiffuseHistoryTextureHF(hdCamera));
                HDTemporalFilter.TemporalFilterParameters filterParams;
                filterParams.singleChannel = false;
                filterParams.historyValidity = historyValidity0;
                filterParams.occluderMotionRejection = false;
                filterParams.receiverMotionRejection = false;
                filterParams.exposureControl = true;
                filterParams.resolutionMultiplier = resolutionPercentages.x;
                filterParams.historyResolutionMultiplier = resolutionPercentages.y;
                TextureHandle denoisedRTGI = temporalFilter.Denoise(renderGraph, hdCamera, filterParams, rtGIBuffer, renderGraph.defaultResources.blackTextureXR, historyBufferHF, depthPyramid, normalBuffer, motionVectorBuffer, historyValidationTexture);

                // Apply the diffuse denoiser
                HDDiffuseDenoiser.DiffuseDenoiserParameters ddParams;
                ddParams.singleChannel = false;
                ddParams.kernelSize = giSettings.denoiserRadiusSS;
                ddParams.halfResolutionFilter = giSettings.halfResolutionDenoiserSS;
                ddParams.jitterFilter = giSettings.secondDenoiserPassSS;
                ddParams.resolutionMultiplier = resolutionPercentages.x;
                rtGIBuffer = diffuseDenoiser.Denoise(renderGraph, hdCamera, ddParams, denoisedRTGI, depthPyramid, normalBuffer, rtGIBuffer);

                // If the second pass is requested, do it otherwise blit
                if (giSettings.secondDenoiserPassSS)
                {
                    float historyValidity1 = EvaluateIndirectDiffuseHistoryValidity1(hdCamera, fullResolution, false);

                    // Run the temporal filter
                    TextureHandle historyBufferLF = renderGraph.ImportTexture(RequestIndirectDiffuseHistoryTextureLF(hdCamera));
                    filterParams.singleChannel = false;
                    filterParams.historyValidity = historyValidity1;
                    filterParams.occluderMotionRejection = false;
                    filterParams.receiverMotionRejection = false;
                    filterParams.exposureControl = true;
                    filterParams.resolutionMultiplier = resolutionPercentages.x;
                    filterParams.historyResolutionMultiplier = resolutionPercentages.y;
                    denoisedRTGI = temporalFilter.Denoise(renderGraph, hdCamera, filterParams, rtGIBuffer, renderGraph.defaultResources.blackTextureXR, historyBufferLF, depthPyramid, normalBuffer, motionVectorBuffer, historyValidationTexture);

                    // Apply the diffuse filter
                    ddParams.singleChannel = false;
                    ddParams.kernelSize = giSettings.denoiserRadiusSS * 0.5f;
                    ddParams.halfResolutionFilter = giSettings.halfResolutionDenoiserSS;
                    ddParams.jitterFilter = false;
                    ddParams.resolutionMultiplier = resolutionPercentages.x;
                    rtGIBuffer = diffuseDenoiser.Denoise(renderGraph, hdCamera, ddParams, denoisedRTGI, depthPyramid, normalBuffer, rtGIBuffer);

                    // Propagate the history validity for the second buffer
                    PropagateIndirectDiffuseHistoryValidity1(hdCamera, fullResolution, false);
                }

                // Propagate the history validity for the first buffer
                PropagateIndirectDiffuseHistoryValidity0(hdCamera, fullResolution, false);

                return rtGIBuffer;
            }
            else
                return rtGIBuffer;
        }

        TextureHandle RenderSSGI(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle depthStencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle historyValidationTexture,
            ShaderVariablesRaytracing shaderVariablesRayTracingCB, HDUtils.PackedMipChainInfo info, ComputeBufferHandle lightList)
        {
            // Grab the global illumination volume component
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.SSGIPass)))
            {
                // Trace the signal
                TextureHandle colorBuffer = TraceSSGI(renderGraph, hdCamera, giSettings, depthPyramid, normalBuffer, depthStencilBuffer, motionVectorsBuffer, lightList);

                // Denoise the result
                TextureHandle denoisedSSGI = DenoiseSSGI(renderGraph, hdCamera, colorBuffer, depthStencilBuffer, normalBuffer, motionVectorsBuffer, historyValidationTexture, giSettings.fullResolutionSS.value);

                // Upscale it if required
                // If this was a half resolution effect, we still have to upscale it
                if (!giSettings.fullResolutionSS.value)
                    colorBuffer = UpscaleSSGI(renderGraph, hdCamera, giSettings, info, depthPyramid, denoisedSSGI);

                return colorBuffer;
            }
        }

        TextureHandle RenderScreenSpaceIndirectDiffuse(HDCamera hdCamera, in PrepassOutput prepassOutput, TextureHandle rayCountTexture, TextureHandle historyValidationTexture, ComputeBufferHandle lightList)
        {
            TextureHandle result;
            switch (GetIndirectDiffuseMode(hdCamera))
            {
                case IndirectDiffuseMode.ScreenSpace:
                    result = RenderSSGI(m_RenderGraph, hdCamera, prepassOutput.depthPyramidTexture, prepassOutput.depthBuffer, prepassOutput.normalBuffer, prepassOutput.resolvedMotionVectorsBuffer, historyValidationTexture, m_ShaderVariablesRayTracingCB, hdCamera.depthBufferMipChainInfo, lightList);
                    break;

                case IndirectDiffuseMode.RayTraced:
                case IndirectDiffuseMode.Mixed:
                    result = RenderRayTracedIndirectDiffuse(m_RenderGraph, hdCamera,
                        prepassOutput, historyValidationTexture, m_SkyManager.GetSkyReflection(hdCamera), rayCountTexture,
                        m_ShaderVariablesRayTracingCB);
                    break;
                default:
                    result = m_RenderGraph.defaultResources.blackTextureXR;
                    break;
            }
            PushFullScreenDebugTexture(m_RenderGraph, result, FullScreenDebugMode.ScreenSpaceGlobalIllumination);
            return result;
        }
    }
}
