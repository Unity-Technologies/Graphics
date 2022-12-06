using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline
    {
        static void RecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var renderer = renderingData.cameraData.renderer;

            renderer.RecordRenderGraph(renderGraph, context, ref renderingData);
        }

        static void RecordAndExecuteRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = renderingData.commandBuffer;
            Camera camera = renderingData.cameraData.camera;
            RenderGraphParameters rgParams = new RenderGraphParameters()
            {
                // TODO Rendergraph - we are reusing the sampler name, as camera.name does an alloc. we could probably cache this as the current string we get is a bit too informative
                executionName = Profiling.TryGetOrAddCameraSampler(camera).name,
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount,
            };

            using (renderGraph.RecordAndExecute(rgParams))
            {
                RecordRenderGraph(renderGraph, context, ref renderingData);
            }
        }
    }
}
