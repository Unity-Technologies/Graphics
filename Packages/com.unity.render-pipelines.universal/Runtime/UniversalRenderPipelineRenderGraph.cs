using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline
    {
        static void RecordAndExecuteRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ScriptableRenderer renderer, CommandBuffer cmd, Camera camera)
        {
            RenderGraphParameters rgParams = new RenderGraphParameters
            {
                executionId = camera.GetEntityId(),
                generateDebugData = camera.cameraType != CameraType.Preview && !camera.isProcessingRenderRequest,
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount,
            };

            try
            {
                renderGraph.BeginRecording(rgParams);
                renderer.RecordRenderGraph(renderGraph, context);
            }
            catch (Exception e)
            {
                if (renderGraph.ResetGraphAndLogException(e))
                    throw;
                return;
            }
            renderGraph.EndRecordingAndExecute();
        }
    }
}
