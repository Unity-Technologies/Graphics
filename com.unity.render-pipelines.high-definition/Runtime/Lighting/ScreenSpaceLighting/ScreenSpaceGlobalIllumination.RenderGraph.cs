using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        class TraceSSGIPassData
        {
            public SSGITraceParameters parameters;
            public TextureHandle depthTexture;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorsBuffer;
            public TextureHandle colorPyramid;
            public TextureHandle historyDepth;
            public TextureHandle hitPointBuffer;
            public TextureHandle outputBuffer0;
            public TextureHandle outputBuffer1;
        }

        struct TraceOutput
        {
            public TextureHandle outputBuffer0;
            public TextureHandle outputBuffer1;
        }

        TraceOutput TraceSSGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination giSettings, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<TraceSSGIPassData>("Trace SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGITrace)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = PrepareSSGITraceParameters(hdCamera, giSettings);
                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ObjectMotionVectors))
                {
                    passData.motionVectorsBuffer = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);
                }
                else
                {
                    passData.motionVectorsBuffer = builder.ReadTexture(motionVectorsBuffer);
                }

                var colorPyramid = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                passData.colorPyramid = colorPyramid != null ? builder.ReadTexture(renderGraph.ImportTexture(colorPyramid)) : renderGraph.defaultResources.blackTextureXR;
                var historyDepth = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
                passData.historyDepth = historyDepth != null ? builder.ReadTexture(renderGraph.ImportTexture(historyDepth)) : renderGraph.defaultResources.blackTextureXR;
                passData.hitPointBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Hit Point"});
                passData.outputBuffer0 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Signal0"}));
                passData.outputBuffer1 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Signal1" }));

                builder.SetRenderFunc(
                (TraceSSGIPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    SSGITraceResources resources = new SSGITraceResources();
                    resources.depthTexture = data.depthTexture;
                    resources.normalBuffer = data.normalBuffer;
                    resources.motionVectorsBuffer = data.motionVectorsBuffer;
                    resources.colorPyramid = data.colorPyramid;
                    resources.historyDepth = data.historyDepth;
                    resources.hitPointBuffer = data.hitPointBuffer;
                    resources.outputBuffer0 = data.outputBuffer0;
                    resources.outputBuffer1 = data.outputBuffer1;
                    ExecuteSSGITrace(ctx.cmd, data.parameters, resources);
                });
                TraceOutput traceOutput = new TraceOutput();
                traceOutput.outputBuffer0 = passData.outputBuffer0;
                traceOutput.outputBuffer1 = passData.outputBuffer1;
                return traceOutput;
            }
        }

        class UpscaleSSGIPassData
        {
            public SSGIUpscaleParameters parameters;
            public TextureHandle depthTexture;
            public TextureHandle inputBuffer;
            public TextureHandle outputBuffer;
        }

        TextureHandle UpscaleSSGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination giSettings, HDUtils.PackedMipChainInfo info, TextureHandle depthPyramid, TextureHandle inputBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<UpscaleSSGIPassData>("Upscale SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGIUpscale)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = PrepareSSGIUpscaleParameters(hdCamera, giSettings, info);
                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.inputBuffer = builder.ReadTexture(inputBuffer);
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Final" }));

                builder.SetRenderFunc(
                (UpscaleSSGIPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    SSGIUpscaleResources resources = new SSGIUpscaleResources();
                    resources.depthTexture = data.depthTexture;
                    resources.inputBuffer = data.inputBuffer;
                    resources.outputBuffer = data.outputBuffer;
                    ExecuteSSGIUpscale(ctx.cmd, data.parameters, resources);
                });
                return passData.outputBuffer;
            }
        }

        class ConvertSSGIPassData
        {
            public SSGIConvertParameters parameters;
            public TextureHandle depthTexture;
            public TextureHandle stencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle inoutputBuffer0;
            public TextureHandle inoutputBuffer1;
        }

        TextureHandle ConvertSSGI(RenderGraph renderGraph, HDCamera hdCamera, bool halfResolution, TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle inoutputBuffer0, TextureHandle inoutputBuffer1)
        {
            using (var builder = renderGraph.AddRenderPass<ConvertSSGIPassData>("Upscale SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGIUpscale)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = PrepareSSGIConvertParameters(hdCamera, halfResolution);
                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.stencilBuffer = builder.ReadTexture(stencilBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.inoutputBuffer0 = builder.WriteTexture(builder.ReadTexture(inoutputBuffer0));
                passData.inoutputBuffer1 = builder.WriteTexture(builder.ReadTexture(inoutputBuffer1));

                builder.SetRenderFunc(
                (ConvertSSGIPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    SSGIConvertResources resources = new SSGIConvertResources();
                    resources.depthTexture = data.depthTexture;
                    resources.stencilBuffer = data.stencilBuffer;
                    resources.normalBuffer = data.normalBuffer;
                    resources.inoutBuffer0 = data.inoutputBuffer0;
                    resources.inputBufer1 = data.inoutputBuffer1;
                    ExecuteSSGIConversion(ctx.cmd, data.parameters, resources);
                });
                return passData.inoutputBuffer0;
            }
        }

        TextureHandle RenderSSGI(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthPyramid, TextureHandle stencilBuffer, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, ShaderVariablesRaytracing shaderVariablesRayTracingCB, HDUtils.PackedMipChainInfo info)
        {
            // Grab the global illumination volume component
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.SSGIPass)))
            {
                // Trace the signal
                TraceOutput traceOutput = TraceSSGI(renderGraph, hdCamera, giSettings, depthPyramid, normalBuffer, motionVectorsBuffer);

                // Evaluate the history validity
                float historyValidity = EvaluateIndirectDiffuseHistoryValidityCombined(hdCamera, giSettings.fullResolutionSS, false);
                SSGIDenoiser ssgiDenoiser = GetSSGIDenoiser();
                SSGIDenoiser.SSGIDenoiserOutput denoiserOutput = ssgiDenoiser.Denoise(renderGraph, hdCamera, depthPyramid, normalBuffer, motionVectorsBuffer, traceOutput.outputBuffer0, traceOutput.outputBuffer1, m_DepthBufferMipChainInfo, !giSettings.fullResolutionSS, historyValidity: historyValidity);
                // Propagate the history
                PropagateIndirectDiffuseHistoryValidityCombined(hdCamera, giSettings.fullResolutionSS, false);

                // Convert back the result to RGB space
                TextureHandle colorBuffer = ConvertSSGI(renderGraph, hdCamera, !giSettings.fullResolutionSS, depthPyramid, stencilBuffer, normalBuffer, denoiserOutput.outputBuffer0, denoiserOutput.outputBuffer1);

                // Upscale it if required
                // If this was a half resolution effect, we still have to upscale it
                if (!giSettings.fullResolutionSS)
                    colorBuffer = UpscaleSSGI(renderGraph, hdCamera, giSettings, info, depthPyramid, colorBuffer);
                return colorBuffer;
            }
        }
    }
}
