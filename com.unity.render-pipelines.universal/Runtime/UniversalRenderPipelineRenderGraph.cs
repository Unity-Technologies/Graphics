using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline
    {
        static void RecordRenderGraph(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            var renderer = renderingData.cameraData.renderer;

            renderer.RecordRenderGraph(m_RenderGraph, context, cmd, ref renderingData);
        }

        static void RecordAndExecuteRenderGraph(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            RenderGraphParameters rgParams = new RenderGraphParameters()
            {
                executionName = camera.name,
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount,
            };

            using (m_RenderGraph.RecordAndExecute(rgParams))
            {
                RecordRenderGraph(context, cmd, ref renderingData);
            }
        }
    }
}
