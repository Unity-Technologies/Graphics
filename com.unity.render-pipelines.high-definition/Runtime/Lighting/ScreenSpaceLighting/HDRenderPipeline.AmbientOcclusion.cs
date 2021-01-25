using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        private bool m_AOHistoryReady = false;
        private bool m_AORunningFullRes = false;

        internal float EvaluateSpecularOcclusionFlag(HDCamera hdCamera)
        {
            AmbientOcclusion ssoSettings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            bool enableRTAO = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && ssoSettings.rayTracing.value;
            if (enableRTAO)
                return EvaluateRTSpecularOcclusionFlag(hdCamera, ssoSettings);
            else
                return 1.0f;
        }

        internal bool IsAmbientOcclusionActive(HDCamera camera, AmbientOcclusion settings) => camera.frameSettings.IsEnabled(FrameSettingsField.SSAO) && settings.intensity.value > 0f;

        struct RenderAOParameters
        {
            public ComputeShader    gtaoCS;
            public int              gtaoKernel;
            public ComputeShader    spatialDenoiseAOCS;
            public int              denoiseKernelSpatial;
            public ComputeShader    temporalDenoiseAOCS;
            public int              denoiseKernelTemporal;
            public ComputeShader    copyHistoryAOCS;
            public int              denoiseKernelCopyHistory;
            public ComputeShader    upsampleAndBlurAOCS;
            public int              upsampleAndBlurKernel;
            public int              upsampleAOKernel;

            public Vector2          runningRes;
            public int              viewCount;
            public bool             historyReady;
            public int              outputWidth;
            public int              outputHeight;
            public bool             fullResolution;
            public bool             runAsync;
            public bool             temporalAccumulation;
            public bool             bilateralUpsample;

            public ShaderVariablesAmbientOcclusion cb;
        }

        RenderAOParameters PrepareRenderAOParameters(HDCamera camera, Vector2 historySize, in HDUtils.PackedMipChainInfo depthMipInfo)
        {
            var parameters = new RenderAOParameters();

            ref var cb = ref parameters.cb;

            // Grab current settings
            var settings = camera.volumeStack.GetComponent<AmbientOcclusion>();
            parameters.fullResolution = settings.fullResolution;

            if (parameters.fullResolution)
            {
                parameters.runningRes = new Vector2(camera.actualWidth, camera.actualHeight);
                cb._AOBufferSize = new Vector4(camera.actualWidth, camera.actualHeight, 1.0f / camera.actualWidth, 1.0f / camera.actualHeight);
            }
            else
            {
                parameters.runningRes = new Vector2(camera.actualWidth, camera.actualHeight) * 0.5f;
                cb._AOBufferSize = new Vector4(camera.actualWidth * 0.5f, camera.actualHeight * 0.5f, 2.0f / camera.actualWidth, 2.0f / camera.actualHeight);
            }

            float invHalfTanFOV = -camera.mainViewConstants.projMatrix[1, 1];
            float aspectRatio = parameters.runningRes.y / parameters.runningRes.x;
            uint frameCount = camera.GetCameraFrameCount();

            cb._AOParams0 = new Vector4(
                parameters.fullResolution ? 0.0f : 1.0f,
                parameters.runningRes.y * invHalfTanFOV * 0.25f,
                settings.radius.value,
                settings.stepCount
            );

            cb._AOParams1 = new Vector4(
                settings.intensity.value,
                1.0f / (settings.radius.value * settings.radius.value),
                (frameCount / 6) % 4,
                (frameCount % 6)
            );


            // We start from screen space position, so we bake in this factor the 1 / resolution as well.
            cb._AODepthToViewParams = new Vector4(
                2.0f / (invHalfTanFOV * aspectRatio * parameters.runningRes.x),
                2.0f / (invHalfTanFOV * parameters.runningRes.y),
                1.0f / (invHalfTanFOV * aspectRatio),
                1.0f / invHalfTanFOV
            );

            float scaleFactor = (parameters.runningRes.x * parameters.runningRes.y) / (540.0f * 960.0f);
            float radInPixels = Mathf.Max(16, settings.maximumRadiusInPixels * Mathf.Sqrt(scaleFactor));

            cb._AOParams2 = new Vector4(
                historySize.x,
                historySize.y,
                1.0f / (settings.stepCount + 1.0f),
                radInPixels
            );

            float stepSize = m_AORunningFullRes ? 1 : 0.5f;

            float blurTolerance = 1.0f - settings.blurSharpness.value;
            float maxBlurTolerance = 0.25f;
            float minBlurTolerance = -2.5f;
            blurTolerance = minBlurTolerance + (blurTolerance * (maxBlurTolerance - minBlurTolerance));

            float bTolerance = 1f - Mathf.Pow(10f, blurTolerance) * stepSize;
            bTolerance *= bTolerance;
            const float upsampleTolerance = -7.0f; // TODO: Expose?
            float uTolerance = Mathf.Pow(10f, upsampleTolerance);
            float noiseFilterWeight = 1f / (Mathf.Pow(10f, 0.0f) + uTolerance);

            cb._AOParams3 = new Vector4(
                bTolerance,
                uTolerance,
                noiseFilterWeight,
                stepSize
            );

            float upperNudgeFactor = 1.0f - settings.ghostingReduction.value;
            const float maxUpperNudgeLimit = 5.0f;
            const float minUpperNudgeLimit = 0.25f;
            upperNudgeFactor = minUpperNudgeLimit + (upperNudgeFactor * (maxUpperNudgeLimit - minUpperNudgeLimit));
            cb._AOParams4 = new Vector4(
                settings.directionCount,
                upperNudgeFactor,
                minUpperNudgeLimit,
                settings.spatialBilateralAggressiveness.value * 15.0f
            );

            cb._FirstTwoDepthMipOffsets = new Vector4(depthMipInfo.mipLevelOffsets[1].x, depthMipInfo.mipLevelOffsets[1].y, depthMipInfo.mipLevelOffsets[2].x, depthMipInfo.mipLevelOffsets[2].y);

            parameters.bilateralUpsample = settings.bilateralUpsample;
            parameters.gtaoCS = defaultResources.shaders.GTAOCS;
            parameters.gtaoCS.shaderKeywords = null;
            parameters.temporalAccumulation = settings.temporalAccumulation.value && camera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors);

            if (parameters.temporalAccumulation)
            {
                parameters.gtaoCS.EnableKeyword("TEMPORAL");
            }
            if (parameters.fullResolution)
            {
                parameters.gtaoCS.EnableKeyword("FULL_RES");
            }
            else
            {
                parameters.gtaoCS.EnableKeyword("HALF_RES");
            }

            parameters.gtaoKernel = parameters.gtaoCS.FindKernel("GTAOMain");

            parameters.upsampleAndBlurAOCS = defaultResources.shaders.GTAOBlurAndUpsample;

            parameters.spatialDenoiseAOCS = defaultResources.shaders.GTAOSpatialDenoiseCS;
            parameters.spatialDenoiseAOCS.shaderKeywords = null;
            if (parameters.temporalAccumulation)
                parameters.spatialDenoiseAOCS.EnableKeyword("TO_TEMPORAL");
            parameters.denoiseKernelSpatial = parameters.spatialDenoiseAOCS.FindKernel("SpatialDenoise");

            parameters.temporalDenoiseAOCS = defaultResources.shaders.GTAOTemporalDenoiseCS;
            parameters.temporalDenoiseAOCS.shaderKeywords = null;
            if (parameters.fullResolution)
                parameters.temporalDenoiseAOCS.EnableKeyword("FULL_RES");
            else
                parameters.temporalDenoiseAOCS.EnableKeyword("HALF_RES");

            parameters.denoiseKernelTemporal = parameters.temporalDenoiseAOCS.FindKernel("TemporalDenoise");

            parameters.copyHistoryAOCS = defaultResources.shaders.GTAOCopyHistoryCS;
            parameters.denoiseKernelCopyHistory = parameters.copyHistoryAOCS.FindKernel("GTAODenoise_CopyHistory");

            parameters.upsampleAndBlurKernel = parameters.upsampleAndBlurAOCS.FindKernel("BlurUpsample");
            parameters.upsampleAOKernel = parameters.upsampleAndBlurAOCS.FindKernel(settings.bilateralUpsample ? "BilateralUpsampling" : "BoxUpsampling");

            parameters.outputWidth = camera.actualWidth;
            parameters.outputHeight = camera.actualHeight;

            parameters.viewCount = camera.viewCount;
            parameters.historyReady = m_AOHistoryReady;
            m_AOHistoryReady = true; // assumes that if this is called, then render is done as well.

            parameters.runAsync = camera.frameSettings.SSAORunsAsync();

            return parameters;
        }

        TextureHandle CreateAmbientOcclusionTexture(RenderGraph renderGraph)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { enableRandomWrite = true, colorFormat = GraphicsFormat.R8_UNorm, name = "Ambient Occlusion" });
        }

        TextureHandle Render(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectors, in HDUtils.PackedMipChainInfo depthMipInfo, ShaderVariablesRaytracing shaderVariablesRaytracing, TextureHandle rayCountTexture)
        {
            var settings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();

            TextureHandle result;
            // AO has side effects (as it uses an imported history buffer)
            // So we can't rely on automatic pass stripping. This is why we have to be explicit here.
            if (IsAmbientOcclusionActive(hdCamera, settings))
            {
                using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.AmbientOcclusion)))
                {
                    float scaleFactor = m_AORunningFullRes ? 1.0f : 0.5f;
                    if (settings.fullResolution != m_AORunningFullRes)
                    {
                        m_AORunningFullRes = settings.fullResolution;
                        scaleFactor = m_AORunningFullRes ? 1.0f : 0.5f;
                    }

                    hdCamera.AllocateAmbientOcclusionHistoryBuffer(scaleFactor);

                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value)
                        return RenderRTAO(renderGraph, hdCamera, depthPyramid, normalBuffer, motionVectors, rayCountTexture, shaderVariablesRaytracing);
                    else
                    {
                        var historyRT = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion);
                        var currentHistory = renderGraph.ImportTexture(historyRT);
                        var outputHistory = renderGraph.ImportTexture(hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion));

                        Vector2 historySize = new Vector2(historyRT.referenceSize.x * historyRT.scaleFactor.x,
                            historyRT.referenceSize.y * historyRT.scaleFactor.y);
                        var rtScaleForHistory = hdCamera.historyRTHandleProperties.rtHandleScale;

                        var aoParameters = PrepareRenderAOParameters(hdCamera, historySize * rtScaleForHistory, depthMipInfo);

                        var packedData = RenderAO(renderGraph, aoParameters, depthPyramid, normalBuffer);
                        result = DenoiseAO(renderGraph, aoParameters, depthPyramid, motionVectors, packedData, currentHistory, outputHistory);
                    }
                }
            }
            else
            {
                result = renderGraph.defaultResources.blackTextureXR;
            }
            return result;
        }

        class RenderAOPassData
        {
            public RenderAOParameters parameters;
            public TextureHandle packedData;
            public TextureHandle depthPyramid;
            public TextureHandle normalBuffer;
        }

        TextureHandle RenderAO(RenderGraph renderGraph, in RenderAOParameters parameters, TextureHandle depthPyramid, TextureHandle normalBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<RenderAOPassData>("GTAO Horizon search and integration", out var passData, ProfilingSampler.Get(HDProfileId.HorizonSSAO)))
            {
                builder.EnableAsyncCompute(parameters.runAsync);

                float scaleFactor = parameters.fullResolution ? 1.0f : 0.5f;

                passData.parameters = parameters;
                passData.packedData = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one * scaleFactor, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "AO Packed data" }));
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);

                builder.SetRenderFunc(
                    (RenderAOPassData data, RenderGraphContext ctx) =>
                    {
                        RenderAO(data.parameters, data.packedData, data.depthPyramid, data.normalBuffer, ctx.cmd);
                    });

                return passData.packedData;
            }
        }

        class DenoiseAOPassData
        {
            public RenderAOParameters parameters;
            public TextureHandle packedData;
            public TextureHandle packedDataBlurred;
            public TextureHandle currentHistory;
            public TextureHandle outputHistory;
            public TextureHandle denoiseOutput;
            public TextureHandle motionVectors;
        }

        TextureHandle DenoiseAO(RenderGraph renderGraph,
            in RenderAOParameters parameters,
            TextureHandle depthTexture,
            TextureHandle motionVectors,
            TextureHandle aoPackedData,
            TextureHandle currentHistory,
            TextureHandle outputHistory)
        {
            TextureHandle denoiseOutput;

            using (var builder = renderGraph.AddRenderPass<DenoiseAOPassData>("Denoise GTAO", out var passData))
            {
                builder.EnableAsyncCompute(parameters.runAsync);

                float scaleFactor = parameters.fullResolution ? 1.0f : 0.5f;

                passData.parameters = parameters;
                passData.packedData = builder.ReadTexture(aoPackedData);
                if (parameters.temporalAccumulation)
                {
                    passData.motionVectors = builder.ReadTexture(motionVectors);
                    passData.currentHistory = builder.ReadTexture(currentHistory); // can also be written on first frame, but since it's an imported resource, it doesn't matter in term of lifetime.
                    passData.outputHistory = builder.WriteTexture(outputHistory);
                }

                passData.packedDataBlurred = builder.CreateTransientTexture(
                    new TextureDesc(Vector2.one * scaleFactor, true, true) { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "AO Packed blurred data" });

                if (parameters.fullResolution)
                    passData.denoiseOutput = builder.WriteTexture(CreateAmbientOcclusionTexture(renderGraph));
                else
                    passData.denoiseOutput = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one * 0.5f, true, true) { enableRandomWrite = true, colorFormat = GraphicsFormat.R32_SFloat, name = "Final Half Res AO Packed" }));

                denoiseOutput = passData.denoiseOutput;

                builder.SetRenderFunc(
                    (DenoiseAOPassData data, RenderGraphContext ctx) =>
                    {
                        DenoiseAO(data.parameters,
                            data.packedData,
                            data.packedDataBlurred,
                            data.currentHistory,
                            data.outputHistory,
                            data.motionVectors,
                            data.denoiseOutput,
                            ctx.cmd);
                    });

                if (parameters.fullResolution)
                    return passData.denoiseOutput;
            }

            return UpsampleAO(renderGraph, parameters, denoiseOutput, depthTexture);
        }

        class UpsampleAOPassData
        {
            public RenderAOParameters parameters;
            public TextureHandle depthTexture;
            public TextureHandle input;
            public TextureHandle output;
        }

        TextureHandle UpsampleAO(RenderGraph renderGraph, in RenderAOParameters parameters, TextureHandle input, TextureHandle depthTexture)
        {
            using (var builder = renderGraph.AddRenderPass<UpsampleAOPassData>("Upsample GTAO", out var passData, ProfilingSampler.Get(HDProfileId.UpSampleSSAO)))
            {
                builder.EnableAsyncCompute(parameters.runAsync);

                passData.parameters = parameters;
                passData.input = builder.ReadTexture(input);
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.output = builder.WriteTexture(CreateAmbientOcclusionTexture(renderGraph));

                builder.SetRenderFunc(
                    (UpsampleAOPassData data, RenderGraphContext ctx) =>
                    {
                        UpsampleAO(data.parameters, data.depthTexture, data.input, data.output, ctx.cmd);
                    });

                return passData.output;
            }
        }

        static void RenderAO(in RenderAOParameters      parameters,
            RTHandle                packedDataTexture,
            RTHandle                depthTexture,
            RTHandle                normalBuffer,
            CommandBuffer           cmd)
        {
            ConstantBuffer.Push(cmd, parameters.cb, parameters.gtaoCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);
            cmd.SetComputeTextureParam(parameters.gtaoCS, parameters.gtaoKernel, HDShaderIDs._AOPackedData, packedDataTexture);
            cmd.SetComputeTextureParam(parameters.gtaoCS, parameters.gtaoKernel, HDShaderIDs._NormalBufferTexture, normalBuffer);
            cmd.SetComputeTextureParam(parameters.gtaoCS, parameters.gtaoKernel, HDShaderIDs._CameraDepthTexture, depthTexture);

            const int groupSizeX = 8;
            const int groupSizeY = 8;
            int threadGroupX = ((int)parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
            int threadGroupY = ((int)parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;

            cmd.DispatchCompute(parameters.gtaoCS, parameters.gtaoKernel, threadGroupX, threadGroupY, parameters.viewCount);
        }

        static void DenoiseAO(in RenderAOParameters   parameters,
            RTHandle                packedDataTex,
            RTHandle                packedDataBlurredTex,
            RTHandle                packedHistoryTex,
            RTHandle                packedHistoryOutputTex,
            RTHandle                motionVectors,
            RTHandle                aoOutputTex,
            CommandBuffer           cmd)
        {
            const int groupSizeX = 8;
            const int groupSizeY = 8;
            int threadGroupX = ((int)parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
            int threadGroupY = ((int)parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;

            if (parameters.temporalAccumulation || parameters.fullResolution)
            {
                var blurCS = parameters.spatialDenoiseAOCS;
                ConstantBuffer.Set<ShaderVariablesAmbientOcclusion>(cmd, blurCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);

                // Spatial
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelSpatial, HDShaderIDs._AOPackedData, packedDataTex);
                if (parameters.temporalAccumulation)
                {
                    cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelSpatial, HDShaderIDs._AOPackedBlurred, packedDataBlurredTex);
                }
                else
                {
                    cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelSpatial, HDShaderIDs._OcclusionTexture, aoOutputTex);
                }

                cmd.DispatchCompute(blurCS, parameters.denoiseKernelSpatial, threadGroupX, threadGroupY, parameters.viewCount);
            }

            if (parameters.temporalAccumulation)
            {
                if (!parameters.historyReady)
                {
                    cmd.SetComputeTextureParam(parameters.copyHistoryAOCS, parameters.denoiseKernelCopyHistory, HDShaderIDs._InputTexture, packedDataTex);
                    cmd.SetComputeTextureParam(parameters.copyHistoryAOCS, parameters.denoiseKernelCopyHistory, HDShaderIDs._OutputTexture, packedHistoryTex);
                    cmd.DispatchCompute(parameters.copyHistoryAOCS, parameters.denoiseKernelCopyHistory, threadGroupX, threadGroupY, parameters.viewCount);
                }

                var blurCS = parameters.temporalDenoiseAOCS;
                ConstantBuffer.Set<ShaderVariablesAmbientOcclusion>(cmd, blurCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);

                // Temporal
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOPackedData, packedDataTex);
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOPackedBlurred, packedDataBlurredTex);
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOPackedHistory, packedHistoryTex);
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._AOOutputHistory, packedHistoryOutputTex);
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._CameraMotionVectorsTexture, motionVectors);
                cmd.SetComputeTextureParam(blurCS, parameters.denoiseKernelTemporal, HDShaderIDs._OcclusionTexture, aoOutputTex);
                cmd.DispatchCompute(blurCS, parameters.denoiseKernelTemporal, threadGroupX, threadGroupY, parameters.viewCount);
            }
        }

        static void UpsampleAO(in RenderAOParameters   parameters,
            RTHandle                depthTexture,
            RTHandle                input,
            RTHandle                output,
            CommandBuffer           cmd)
        {
            bool blurAndUpsample = !parameters.temporalAccumulation;

            ConstantBuffer.Set<ShaderVariablesAmbientOcclusion>(cmd, parameters.upsampleAndBlurAOCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);

            if (blurAndUpsample)
            {
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAndBlurKernel, HDShaderIDs._AOPackedData, input);
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAndBlurKernel, HDShaderIDs._OcclusionTexture, output);
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAndBlurKernel, HDShaderIDs._CameraDepthTexture, depthTexture);

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = ((int)(parameters.runningRes.x) + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = ((int)(parameters.runningRes.y) + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(parameters.upsampleAndBlurAOCS, parameters.upsampleAndBlurKernel, threadGroupX, threadGroupY, parameters.viewCount);
            }
            else
            {
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAOKernel, HDShaderIDs._AOPackedData, input);
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAOKernel, HDShaderIDs._OcclusionTexture, output);
                cmd.SetComputeTextureParam(parameters.upsampleAndBlurAOCS, parameters.upsampleAOKernel, HDShaderIDs._CameraDepthTexture, depthTexture);

                const int groupSizeX = 8;
                const int groupSizeY = 8;
                int threadGroupX = ((int)parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
                int threadGroupY = ((int)parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(parameters.upsampleAndBlurAOCS, parameters.upsampleAOKernel, threadGroupX, threadGroupY, parameters.viewCount);
            }
        }

        void UpdateShaderVariableGlobalAmbientOcclusion(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            var settings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();
            bool aoEnabled = false;
            if (IsAmbientOcclusionActive(hdCamera, settings))
            {
                aoEnabled = true;
                // If raytraced AO is enabled but raytracing state is wrong then we disable it.
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value && !HDRenderPipeline.currentPipeline.GetRayTracingState())
                {
                    aoEnabled = false;
                }
            }

            if (aoEnabled)
                cb._AmbientOcclusionParam = new Vector4(0f, 0f, 0f, settings.directLightingStrength.value);
            else
                cb._AmbientOcclusionParam = Vector4.zero;
        }
    }
}
