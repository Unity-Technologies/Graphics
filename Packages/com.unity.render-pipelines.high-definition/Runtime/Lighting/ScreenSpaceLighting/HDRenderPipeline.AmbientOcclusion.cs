using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        bool m_AOHistoryReady = false;

        float EvaluateSpecularOcclusionFlag(HDCamera hdCamera)
        {
            var ssoSettings = hdCamera.volumeStack.GetComponent<ScreenSpaceAmbientOcclusion>();
            bool enableRTAO = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && ssoSettings.rayTracing.value;
            if (enableRTAO)
                return EvaluateRTSpecularOcclusionFlag(hdCamera, ssoSettings);
            else
                return 1.0f;
        }

        bool IsAmbientOcclusionActive(HDCamera camera, ScreenSpaceAmbientOcclusion settings) => camera.frameSettings.IsEnabled(FrameSettingsField.SSAO) && settings.intensity.value > 0f;

        struct RenderAOParameters
        {
            public Vector2 runningRes;
            public int viewCount;
            public bool fullResolution;
            public bool runAsync;
            public bool temporalAccumulation;
            public bool bilateralUpsample;

            public ShaderVariablesAmbientOcclusion cb;
        }

        RenderAOParameters PrepareRenderAOParameters(HDCamera camera, Vector2 historySize, in HDUtils.PackedMipChainInfo depthMipInfo)
        {
            var parameters = new RenderAOParameters();

            ref var cb = ref parameters.cb;

            // Grab current settings
            var settings = camera.volumeStack.GetComponent<ScreenSpaceAmbientOcclusion>();
            parameters.fullResolution = settings.fullResolution;

            if (parameters.fullResolution)
            {
                parameters.runningRes = new Vector2(camera.actualWidth, camera.actualHeight);
                cb._AOBufferSize = new Vector4(camera.actualWidth, camera.actualHeight, 1.0f / camera.actualWidth, 1.0f / camera.actualHeight);
            }
            else
            {
                // Ceil is needed because we upsample the AO too, round would loose a pixel is the resolution is odd
                parameters.runningRes = new Vector2(Mathf.CeilToInt(camera.actualWidth * 0.5f), Mathf.CeilToInt(camera.actualHeight * 0.5f));
                cb._AOBufferSize = new Vector4(parameters.runningRes.x, parameters.runningRes.y, 1.0f / parameters.runningRes.x, 1.0f / parameters.runningRes.y);
            }

            parameters.temporalAccumulation = settings.temporalAccumulation.value && camera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors);

            parameters.viewCount = camera.viewCount;
            parameters.runAsync = camera.frameSettings.SSAORunsAsync();
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

            float stepSize = settings.fullResolution ? 1 : 0.5f;

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
            parameters.temporalAccumulation = settings.temporalAccumulation.value && camera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors);

            parameters.viewCount = camera.viewCount;
            parameters.runAsync = camera.frameSettings.SSAORunsAsync();

            return parameters;
        }

        TextureHandle CreateAmbientOcclusionTexture(RenderGraph renderGraph, bool fullResolution)
        {
            if (fullResolution)
                return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { enableRandomWrite = true, format = GraphicsFormat.R8_UNorm, name = "Ambient Occlusion" });
            else
                return renderGraph.CreateTexture(new TextureDesc(Vector2.one * 0.5f, true, true) { enableRandomWrite = true, format = GraphicsFormat.R32_SFloat, name = "Final Half Res AO Packed" });
        }

        TextureHandle RenderAmbientOcclusion(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectors, TextureHandle historyValidityBuffer,
            in HDUtils.PackedMipChainInfo depthMipInfo, ShaderVariablesRaytracing shaderVariablesRaytracing, TextureHandle rayCountTexture)
        {
            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceAmbientOcclusion>();

            TextureHandle result;
            if (IsAmbientOcclusionActive(hdCamera, settings))
            {
                using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.AmbientOcclusion)))
                {
                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value && GetRayTracingState())
                        result = RenderRTAO(renderGraph, hdCamera, depthBuffer, normalBuffer, motionVectors, historyValidityBuffer, rayCountTexture, shaderVariablesRaytracing);
                    else
                    {
                        m_AOHistoryReady = !hdCamera.AllocateAmbientOcclusionHistoryBuffer(settings.fullResolution ? 1.0f : 0.5f);

                        var historyRT = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion);
                        var currentHistory = renderGraph.ImportTexture(historyRT);
                        var outputHistory = renderGraph.ImportTexture(hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion));

                        Vector2 historySize = historyRT.GetScaledSize();
                        var rtScaleForHistory = hdCamera.historyRTHandleProperties.rtHandleScale;

                        var aoParameters = PrepareRenderAOParameters(hdCamera, historySize * rtScaleForHistory, depthMipInfo);

                        result = RenderAO(renderGraph, aoParameters, depthPyramid, normalBuffer);
                        if (aoParameters.temporalAccumulation || aoParameters.fullResolution)
                            result = SpatialDenoiseAO(renderGraph, aoParameters, result);
                        if (aoParameters.temporalAccumulation)
                            result = TemporalDenoiseAO(renderGraph, aoParameters, depthPyramid, motionVectors, result, currentHistory, outputHistory);
                        if (!aoParameters.fullResolution)
                            result = UpsampleAO(renderGraph, aoParameters, result, depthPyramid);
                    }
                }
            }
            else
            {
                result = renderGraph.defaultResources.blackTextureXR;
            }

            PushFullScreenDebugTexture(m_RenderGraph, result, FullScreenDebugMode.ScreenSpaceAmbientOcclusion);

            return result;
        }

        class RenderAOPassData
        {
            public RenderAOParameters parameters;

            public ComputeShader gtaoCS;
            public int gtaoKernel;

            public TextureHandle packedData;
            public TextureHandle depthPyramid;
            public TextureHandle normalBuffer;
        }

        TextureHandle RenderAO(RenderGraph renderGraph, in RenderAOParameters parameters, TextureHandle depthPyramid, TextureHandle normalBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<RenderAOPassData>("GTAO Horizon search and integration", out var passData, ProfilingSampler.Get(HDProfileId.HorizonSSAO)))
            {
                builder.EnableAsyncCompute(parameters.runAsync);

                passData.parameters = parameters;
                passData.gtaoCS = runtimeShaders.GTAOCS;
                passData.gtaoCS.shaderKeywords = null;

                if (parameters.temporalAccumulation)
                    passData.gtaoCS.EnableKeyword("TEMPORAL");
                if (parameters.fullResolution)
                    passData.gtaoCS.EnableKeyword("FULL_RES");
                else
                    passData.gtaoCS.EnableKeyword("HALF_RES");

                passData.gtaoKernel = passData.gtaoCS.FindKernel("GTAOMain");

                float scaleFactor = parameters.fullResolution ? 1.0f : 0.5f;

                passData.packedData = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one * scaleFactor, true, true)
                { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "AO Packed data" }));
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);

                builder.SetRenderFunc(
                    (RenderAOPassData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.Push(ctx.cmd, data.parameters.cb, data.gtaoCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);
                        ctx.cmd.SetComputeTextureParam(data.gtaoCS, data.gtaoKernel, HDShaderIDs._AOPackedData, data.packedData);
                        ctx.cmd.SetComputeTextureParam(data.gtaoCS, data.gtaoKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.gtaoCS, data.gtaoKernel, HDShaderIDs._CameraDepthTexture, data.depthPyramid);

                        const int groupSizeX = 8;
                        const int groupSizeY = 8;
                        int threadGroupX = ((int)data.parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
                        int threadGroupY = ((int)data.parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;

                        ctx.cmd.DispatchCompute(data.gtaoCS, data.gtaoKernel, threadGroupX, threadGroupY, data.parameters.viewCount);
                    });

                return passData.packedData;
            }
        }

        class SpatialDenoiseAOPassData
        {
            public RenderAOParameters parameters;
            public ComputeShader spatialDenoiseAOCS;
            public int denoiseKernelSpatial;

            public TextureHandle packedData;
            public TextureHandle denoiseOutput;
        }

        TextureHandle SpatialDenoiseAO(RenderGraph renderGraph, in RenderAOParameters parameters, TextureHandle aoPackedData)
        {
            using (var builder = renderGraph.AddRenderPass<SpatialDenoiseAOPassData>("Spatial Denoise GTAO", out var passData))
            {
                builder.EnableAsyncCompute(parameters.runAsync);

                float scaleFactor = parameters.fullResolution ? 1.0f : 0.5f;

                passData.parameters = parameters;

                passData.spatialDenoiseAOCS = runtimeShaders.GTAOSpatialDenoiseCS;
                passData.spatialDenoiseAOCS.shaderKeywords = null;
                if (parameters.temporalAccumulation)
                    passData.spatialDenoiseAOCS.EnableKeyword("TO_TEMPORAL");
                passData.denoiseKernelSpatial = passData.spatialDenoiseAOCS.FindKernel("SpatialDenoise");

                passData.packedData = builder.ReadTexture(aoPackedData);
                if (parameters.temporalAccumulation)
                    passData.denoiseOutput = builder.WriteTexture(renderGraph.CreateTexture(
                        new TextureDesc(Vector2.one * (parameters.fullResolution ? 1.0f : 0.5f), true, true) { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "AO Packed blurred data" }));
                else
                    passData.denoiseOutput = builder.WriteTexture(CreateAmbientOcclusionTexture(renderGraph, parameters.fullResolution));

                builder.SetRenderFunc(
                    (SpatialDenoiseAOPassData data, RenderGraphContext ctx) =>
                    {
                        const int groupSizeX = 8;
                        const int groupSizeY = 8;
                        int threadGroupX = ((int)data.parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
                        int threadGroupY = ((int)data.parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;

                        var blurCS = data.spatialDenoiseAOCS;
                        ConstantBuffer.Set<ShaderVariablesAmbientOcclusion>(ctx.cmd, blurCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);

                        // Spatial
                        ctx.cmd.SetComputeTextureParam(blurCS, data.denoiseKernelSpatial, HDShaderIDs._AOPackedData, data.packedData);
                        ctx.cmd.SetComputeTextureParam(blurCS, data.denoiseKernelSpatial, HDShaderIDs._OcclusionTexture, data.denoiseOutput);
                        ctx.cmd.DispatchCompute(blurCS, data.denoiseKernelSpatial, threadGroupX, threadGroupY, data.parameters.viewCount);
                    });

                return passData.denoiseOutput;
            }
        }

        class TemporalDenoiseAOPassData
        {
            public RenderAOParameters parameters;

            public ComputeShader temporalDenoiseAOCS;
            public int denoiseKernelTemporal;
            public ComputeShader copyHistoryAOCS;
            public int denoiseKernelCopyHistory;
            public bool historyReady;

            public TextureHandle packedDataBlurred;
            public TextureHandle currentHistory;
            public TextureHandle outputHistory;
            public TextureHandle denoiseOutput;
            public TextureHandle motionVectors;
        }

        TextureHandle TemporalDenoiseAO(RenderGraph renderGraph,
            in RenderAOParameters parameters,
            TextureHandle depthTexture,
            TextureHandle motionVectors,
            TextureHandle aoPackedDataBlurred,
            TextureHandle currentHistory,
            TextureHandle outputHistory)
        {
            using (var builder = renderGraph.AddRenderPass<TemporalDenoiseAOPassData>("Temporal Denoise GTAO", out var passData))
            {
                builder.EnableAsyncCompute(parameters.runAsync);

                float scaleFactor = parameters.fullResolution ? 1.0f : 0.5f;

                passData.parameters = parameters;
                passData.temporalDenoiseAOCS = runtimeShaders.GTAOTemporalDenoiseCS;
                passData.temporalDenoiseAOCS.shaderKeywords = null;
                if (parameters.fullResolution)
                    passData.temporalDenoiseAOCS.EnableKeyword("FULL_RES");
                else
                    passData.temporalDenoiseAOCS.EnableKeyword("HALF_RES");
                passData.denoiseKernelTemporal = passData.temporalDenoiseAOCS.FindKernel("TemporalDenoise");
                passData.copyHistoryAOCS = runtimeShaders.GTAOCopyHistoryCS;
                passData.denoiseKernelCopyHistory = passData.copyHistoryAOCS.FindKernel("GTAODenoise_CopyHistory");
                passData.historyReady = m_AOHistoryReady;

                passData.motionVectors = builder.ReadTexture(motionVectors);
                passData.currentHistory = builder.ReadTexture(currentHistory); // can also be written on first frame, but since it's an imported resource, it doesn't matter in term of lifetime.
                passData.outputHistory = builder.WriteTexture(outputHistory);
                passData.packedDataBlurred = builder.ReadTexture(aoPackedDataBlurred);
                passData.denoiseOutput = builder.WriteTexture(CreateAmbientOcclusionTexture(renderGraph, parameters.fullResolution));

                builder.SetRenderFunc(
                    (TemporalDenoiseAOPassData data, RenderGraphContext ctx) =>
                    {
                        const int groupSizeX = 8;
                        const int groupSizeY = 8;
                        int threadGroupX = ((int)data.parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
                        int threadGroupY = ((int)data.parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;

                        if (!data.historyReady)
                        {
                            ctx.cmd.SetComputeTextureParam(data.copyHistoryAOCS, data.denoiseKernelCopyHistory, HDShaderIDs._InputTexture, data.packedDataBlurred);
                            ctx.cmd.SetComputeTextureParam(data.copyHistoryAOCS, data.denoiseKernelCopyHistory, HDShaderIDs._OutputTexture, data.currentHistory);
                            ctx.cmd.DispatchCompute(data.copyHistoryAOCS, data.denoiseKernelCopyHistory, threadGroupX, threadGroupY, data.parameters.viewCount);
                        }

                        var blurCS = data.temporalDenoiseAOCS;
                        ConstantBuffer.Set<ShaderVariablesAmbientOcclusion>(ctx.cmd, blurCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);

                        // Temporal
                        ctx.cmd.SetComputeTextureParam(blurCS, data.denoiseKernelTemporal, HDShaderIDs._AOPackedBlurred, data.packedDataBlurred);
                        ctx.cmd.SetComputeTextureParam(blurCS, data.denoiseKernelTemporal, HDShaderIDs._AOPackedHistory, data.currentHistory);
                        ctx.cmd.SetComputeTextureParam(blurCS, data.denoiseKernelTemporal, HDShaderIDs._AOOutputHistory, data.outputHistory);
                        ctx.cmd.SetComputeTextureParam(blurCS, data.denoiseKernelTemporal, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectors);
                        ctx.cmd.SetComputeTextureParam(blurCS, data.denoiseKernelTemporal, HDShaderIDs._OcclusionTexture, data.denoiseOutput);
                        ctx.cmd.DispatchCompute(blurCS, data.denoiseKernelTemporal, threadGroupX, threadGroupY, data.parameters.viewCount);
                    });

                return passData.denoiseOutput;
            }
        }

        class UpsampleAOPassData
        {
            public RenderAOParameters parameters;

            public ComputeShader upsampleAndBlurAOCS;
            public int upsampleAOKernel;

            public TextureHandle depthTexture;
            public TextureHandle input;
            public TextureHandle output;
        }

        TextureHandle UpsampleAO(RenderGraph renderGraph, in RenderAOParameters parameters, TextureHandle input, TextureHandle depthTexture)
        {
            if (parameters.fullResolution)
                return input;

            using (var builder = renderGraph.AddRenderPass<UpsampleAOPassData>("Upsample GTAO", out var passData, ProfilingSampler.Get(HDProfileId.UpSampleSSAO)))
            {
                builder.EnableAsyncCompute(parameters.runAsync);

                passData.parameters = parameters;
                passData.upsampleAndBlurAOCS = runtimeShaders.GTAOBlurAndUpsample;
                if (parameters.temporalAccumulation)
                    passData.upsampleAOKernel = passData.upsampleAndBlurAOCS.FindKernel(parameters.bilateralUpsample ? "BilateralUpsampling" : "BoxUpsampling");
                else
                    passData.upsampleAOKernel = passData.upsampleAndBlurAOCS.FindKernel("BlurUpsample");
                passData.input = builder.ReadTexture(input);
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.output = builder.WriteTexture(CreateAmbientOcclusionTexture(renderGraph, true));

                builder.SetRenderFunc(
                    (UpsampleAOPassData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.Set<ShaderVariablesAmbientOcclusion>(ctx.cmd, data.upsampleAndBlurAOCS, HDShaderIDs._ShaderVariablesAmbientOcclusion);

                        ctx.cmd.SetComputeTextureParam(data.upsampleAndBlurAOCS, data.upsampleAOKernel, HDShaderIDs._AOPackedData, data.input);
                        ctx.cmd.SetComputeTextureParam(data.upsampleAndBlurAOCS, data.upsampleAOKernel, HDShaderIDs._OcclusionTexture, data.output);
                        ctx.cmd.SetComputeTextureParam(data.upsampleAndBlurAOCS, data.upsampleAOKernel, HDShaderIDs._CameraDepthTexture, data.depthTexture);

                        const int groupSizeX = 8;
                        const int groupSizeY = 8;
                        int threadGroupX = ((int)data.parameters.runningRes.x + (groupSizeX - 1)) / groupSizeX;
                        int threadGroupY = ((int)data.parameters.runningRes.y + (groupSizeY - 1)) / groupSizeY;
                        ctx.cmd.DispatchCompute(data.upsampleAndBlurAOCS, data.upsampleAOKernel, threadGroupX, threadGroupY, data.parameters.viewCount);
                    });

                return passData.output;
            }
        }

        void UpdateShaderVariableGlobalAmbientOcclusion(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            var settings = hdCamera.volumeStack.GetComponent<ScreenSpaceAmbientOcclusion>();
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
