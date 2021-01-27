using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class AmbientOcclusionSystem
    {
        TextureHandle CreateAmbientOcclusionTexture(RenderGraph renderGraph)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { enableRandomWrite = true, colorFormat = GraphicsFormat.R8_UNorm, name = "Ambient Occlusion" });
        }

        public TextureHandle Render(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectors, in HDUtils.PackedMipChainInfo depthMipInfo, ShaderVariablesRaytracing shaderVariablesRaytracing, TextureHandle rayCountTexture)
        {
            var settings = hdCamera.volumeStack.GetComponent<AmbientOcclusion>();

            TextureHandle result;
            // AO has side effects (as it uses an imported history buffer)
            // So we can't rely on automatic pass stripping. This is why we have to be explicit here.
            if (IsActive(hdCamera, settings))
            {
                using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.AmbientOcclusion)))
                {
                    float scaleFactor = m_RunningFullRes ? 1.0f : 0.5f;
                    if (settings.fullResolution != m_RunningFullRes)
                    {
                        m_RunningFullRes = settings.fullResolution;
                        scaleFactor = m_RunningFullRes ? 1.0f : 0.5f;
                    }

                    hdCamera.AllocateAmbientOcclusionHistoryBuffer(scaleFactor);

                    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value)
                        return m_RaytracingAmbientOcclusion.RenderRTAO(renderGraph, hdCamera, depthPyramid, normalBuffer, motionVectors, rayCountTexture, shaderVariablesRaytracing);
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
            public RenderAOParameters   parameters;
            public TextureHandle        packedData;
            public TextureHandle        depthPyramid;
            public TextureHandle        normalBuffer;
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
            public RenderAOParameters   parameters;
            public TextureHandle        packedData;
            public TextureHandle        packedDataBlurred;
            public TextureHandle        currentHistory;
            public TextureHandle        outputHistory;
            public TextureHandle        denoiseOutput;
            public TextureHandle        motionVectors;
        }

        TextureHandle DenoiseAO(    RenderGraph             renderGraph,
                                    in RenderAOParameters   parameters,
                                    TextureHandle           depthTexture,
                                    TextureHandle           motionVectors,
                                    TextureHandle           aoPackedData,
                                    TextureHandle           currentHistory,
                                    TextureHandle           outputHistory)
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
                    DenoiseAO(  data.parameters,
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
            public RenderAOParameters   parameters;
            public TextureHandle        depthTexture;
            public TextureHandle        input;
            public TextureHandle        output;
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
    }
}
