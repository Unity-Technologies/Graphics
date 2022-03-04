using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline
    {
        static void RecordRenderGraph(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var renderer = renderingData.cameraData.renderer;
            RenderGraph renderGraph = renderingData.renderGraph;

            renderer.RecordRenderGraph(context, ref renderingData);
        }

        static void RecordAndExecuteRenderGraph(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = renderingData.commandBuffer;
            Camera camera = renderingData.cameraData.camera;
            RenderGraph renderGraph = renderingData.renderGraph;

            RenderGraphParameters rgParams = new RenderGraphParameters()
            {
                executionName = "URP RenderGraph",
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount,
            };

            using (renderGraph.RecordAndExecute(rgParams))
            {
                RecordRenderGraph(context, ref renderingData);
            }
        }
    }
}
