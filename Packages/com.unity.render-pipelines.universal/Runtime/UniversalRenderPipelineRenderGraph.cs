using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline
    {
        static void RecordAndExecuteRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ScriptableRenderer renderer, CommandBuffer cmd, Camera camera, RenderTextureUVOriginStrategy uvOriginStrategy)
        {
            RenderGraphParameters rgParams = new RenderGraphParameters
            {
                executionId = camera.GetEntityId(),
                generateDebugData = camera.cameraType != CameraType.Preview && !camera.isProcessingRenderRequest,
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount,
                renderTextureUVOriginStrategy = uvOriginStrategy,
            };

            try
            {
                renderGraph.BeginRecording(rgParams);
                renderer.RecordRenderGraph(renderGraph, context);
                renderGraph.EndRecordingAndExecute();
            }
            catch (Exception e)
            {
                if (renderGraph.ResetGraphAndLogException(e))
                    throw;
            }
        }
    }
}
