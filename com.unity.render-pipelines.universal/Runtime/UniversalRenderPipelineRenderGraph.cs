using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline
    {
        static void RecordRenderGraph(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            var renderer = renderingData.cameraData.renderer;
            RenderGraph renderGraph = renderingData.renderGraph;

            //renderer.RecordRenderGraph(m_RenderGraph, context, cmd, ref renderingData);
        }

        static void RecordAndExecuteRenderGraph(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = renderingData.commandBuffer;
            Camera camera = renderingData.cameraData.camera;
            RenderGraph renderGraph = renderingData.renderGraph;

            RenderGraphParameters rgParams = new RenderGraphParameters()
            {
                executionName = camera.name,
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount,
            };

            using (renderGraph.RecordAndExecute(rgParams))
            {
                RecordRenderGraph(context, cmd, ref renderingData);
            }
        }
    }
}
