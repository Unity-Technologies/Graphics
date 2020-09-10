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
            public TextureHandle outputBuffer;
        }

        TextureHandle TraceSSGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination giSettings, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<TraceSSGIPassData>("Trace SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGITrace)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = PrepareSSGITraceParameters(hdCamera, giSettings);
                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorsBuffer = builder.ReadTexture(motionVectorsBuffer);
                var colorPyramid = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                passData.colorPyramid = colorPyramid != null ? builder.ReadTexture(renderGraph.ImportTexture(colorPyramid)) : renderGraph.defaultResources.blackTextureXR;
                var historyDepth = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
                passData.historyDepth = historyDepth != null ? builder.ReadTexture(renderGraph.ImportTexture(historyDepth)) : renderGraph.defaultResources.blackTextureXR;
                passData.hitPointBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Hit Point"});
                passData.outputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Signal"}));

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
                    resources.outputBuffer = data.outputBuffer;
                    ExecuteSSGITrace(ctx.cmd, data.parameters, resources);
                });
                return passData.outputBuffer;
            }
        }

        class UpscaleSSGIPassData
        {
            public SSGIUpscaleParameters parameters;
            public TextureHandle depthTexture;
            public TextureHandle inputBuffer;
            public TextureHandle outputBuffer;
        }

        TextureHandle UpscaleSSGI(RenderGraph renderGraph, HDCamera hdCamera, GlobalIllumination giSettings, TextureHandle depthPyramid, TextureHandle inputBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<UpscaleSSGIPassData>("Upscale SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGIUpscale)))
            {
                builder.EnableAsyncCompute(false);

                passData.parameters = PrepareSSGIUpscaleParameters(hdCamera, giSettings); ;
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

        TextureHandle RenderSSGI(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, ShaderVariablesRaytracing shaderVariablesRayTracingCB)
        {
            // Grab the global illumination volume component
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.SSGIPass)))
            {
                // Trace the signal
                TextureHandle colorBuffer = TraceSSGI(renderGraph, hdCamera, giSettings, depthPyramid, normalBuffer, motionVectorsBuffer);

                // Denoise the signal
                float historyValidity = EvaluateHistoryValidity(hdCamera);
                SSGIDenoiser ssgiDenoiser = GetSSGIDenoiser();
                ssgiDenoiser.Denoise(renderGraph, hdCamera, depthPyramid, normalBuffer, motionVectorsBuffer, colorBuffer, !giSettings.fullResolutionSS, historyValidity: historyValidity);

                // Upscale it if required
                // If this was a half resolution effect, we still have to upscale it
                if (!giSettings.fullResolutionSS)
                    colorBuffer = UpscaleSSGI(renderGraph, hdCamera, giSettings, depthPyramid, colorBuffer);
                return colorBuffer;
            }
        }
    }
}
