using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class AmbientOcclusionSystem
    {
        RenderGraphMutableResource CreateAmbientOcclusionTexture(RenderGraph renderGraph)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { enableRandomWrite = true, colorFormat = GraphicsFormat.R8_UNorm, name = "Ambient Occlusion" }, HDShaderIDs._AmbientOcclusionTexture);
        }

        public RenderGraphResource Render(RenderGraph renderGraph, HDCamera hdCamera, RenderGraphResource depthPyramid, RenderGraphResource motionVectors, int frameCount)
        {
            var settings = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();

            RenderGraphResource result;
            // AO has side effects (as it uses an imported history buffer)
            // So we can't rely on automatic pass stripping. This is why we have to be explicit here.
            if (IsActive(hdCamera, settings))
            {
                {
                    EnsureRTSize(settings, hdCamera);

                    var aoParameters = PrepareRenderAOParameters(hdCamera, renderGraph.rtHandleProperties, frameCount);

                    var currentHistory = renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion));
                    var outputHistory = renderGraph.ImportTexture(hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion));

                    var packedData = RenderAO(renderGraph, aoParameters, depthPyramid);
                    result = DenoiseAO(renderGraph, aoParameters, motionVectors, packedData, currentHistory, outputHistory);
                }
            }
            else
            {
                result = renderGraph.ImportTexture(TextureXR.GetBlackTexture(), HDShaderIDs._AmbientOcclusionTexture);
            }
            return result;
        }

        class RenderAOPassData
        {
            public RenderAOParameters           parameters;
            public RenderGraphMutableResource   packedData;
            public RenderGraphResource          depthPyramid;
        }

        RenderGraphResource RenderAO(RenderGraph renderGraph, in RenderAOParameters parameters, RenderGraphResource depthPyramid)
        {
            using (var builder = renderGraph.AddRenderPass<RenderAOPassData>("GTAO Horizon search and integration", out var passData, CustomSamplerId.RenderSSAO.GetSampler()))
            {
                builder.EnableAsyncCompute(parameters.runAsync);

                float scaleFactor = parameters.fullResolution ? 1.0f : 0.5f;

                passData.parameters = parameters;
                passData.packedData = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one * scaleFactor, true, true)
                    { colorFormat = GraphicsFormat.R32_UInt, enableRandomWrite = true, name = "AO Packed data" }));
                passData.depthPyramid = builder.ReadTexture(depthPyramid);

                builder.SetRenderFunc(
                (RenderAOPassData data, RenderGraphContext ctx) =>
                {
                    RenderAO(data.parameters, ctx.resources.GetTexture(data.packedData), m_Resources, ctx.cmd);
                });

                return passData.packedData;
            }
        }

        class DenoiseAOPassData
        {
            public RenderAOParameters           parameters;
            public RenderGraphResource          packedData;
            public RenderGraphMutableResource   packedDataBlurred;
            public RenderGraphResource          currentHistory;
            public RenderGraphMutableResource   outputHistory;
            public RenderGraphMutableResource   denoiseOutput;
            public RenderGraphResource          motionVectors;
        }

        RenderGraphResource DenoiseAO(  RenderGraph                 renderGraph,
                                        in RenderAOParameters       parameters,
                                        RenderGraphResource         motionVectors,
                                        RenderGraphResource         aoPackedData,
                                        RenderGraphMutableResource  currentHistory,
                                        RenderGraphMutableResource  outputHistory)
        {
            RenderGraphResource denoiseOutput;

            using (var builder = renderGraph.AddRenderPass<DenoiseAOPassData>("Denoise GTAO", out var passData))
            {
                builder.EnableAsyncCompute(parameters.runAsync);

                float scaleFactor = parameters.fullResolution ? 1.0f : 0.5f;

                passData.parameters = parameters;
                passData.packedData = builder.ReadTexture(aoPackedData);
                passData.motionVectors = builder.ReadTexture(motionVectors);
                passData.packedDataBlurred = builder.WriteTexture(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one * scaleFactor, true, true) { colorFormat = GraphicsFormat.R32_UInt, enableRandomWrite = true, name = "AO Packed blurred data" } ));
                passData.currentHistory = builder.ReadTexture(currentHistory); // can also be written on first frame, but since it's an imported resource, it doesn't matter in term of lifetime.
                passData.outputHistory = builder.WriteTexture(outputHistory);

                var format = parameters.fullResolution ? GraphicsFormat.R8_UNorm : GraphicsFormat.R32_UInt;
                if (parameters.fullResolution)
                    passData.denoiseOutput = builder.WriteTexture(CreateAmbientOcclusionTexture(renderGraph));
                else
                    passData.denoiseOutput = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one * 0.5f, true, true) { enableRandomWrite = true, colorFormat = GraphicsFormat.R32_UInt, name = "Final Half Res AO Packed" }));

                denoiseOutput = passData.denoiseOutput;

                builder.SetRenderFunc(
                (DenoiseAOPassData data, RenderGraphContext ctx) =>
                {
                    var res = ctx.resources;
                    DenoiseAO(  data.parameters,
                                res.GetTexture(data.packedData),
                                res.GetTexture(data.packedDataBlurred),
                                res.GetTexture(data.currentHistory),
                                res.GetTexture(data.outputHistory),
                                res.GetTexture(data.denoiseOutput),
                                ctx.cmd);
                });

                if (parameters.fullResolution)
                    return passData.denoiseOutput;
            }

            return UpsampleAO(renderGraph, parameters, denoiseOutput);
        }

        class UpsampleAOPassData
        {
            public RenderAOParameters           parameters;
            public RenderGraphResource          input;
            public RenderGraphMutableResource   output;
        }

        RenderGraphResource UpsampleAO(RenderGraph renderGraph, in RenderAOParameters parameters, RenderGraphResource input)
        {
            using (var builder = renderGraph.AddRenderPass<UpsampleAOPassData>("Upsample GTAO", out var passData, CustomSamplerId.ResolveSSAO.GetSampler()))
            {
                builder.EnableAsyncCompute(parameters.runAsync);

                passData.parameters = parameters;
                passData.input = builder.ReadTexture(input);
                passData.output = builder.WriteTexture(CreateAmbientOcclusionTexture(renderGraph));

                builder.SetRenderFunc(
                (UpsampleAOPassData data, RenderGraphContext ctx) =>
                {
                    UpsampleAO(data.parameters, ctx.resources.GetTexture(data.input), ctx.resources.GetTexture(data.output), ctx.cmd);
                });

                return passData.output;
            }
        }
    }
}
