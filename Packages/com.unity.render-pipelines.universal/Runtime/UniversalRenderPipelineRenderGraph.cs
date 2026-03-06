using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline
    {
        static void RecordAndExecuteRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ScriptableRenderer renderer, CommandBuffer cmd, Camera camera)
        {
            var universalRenderer = renderer as UniversalRenderer;
            var renderTextureUVOriginStrategy = (universalRenderer != null && universalRenderer.useTileOnlyMode) ?
                RenderTextureUVOriginStrategy.PropagateAttachmentOrientation : RenderTextureUVOriginStrategy.BottomLeft;

            RenderGraphParameters rgParams = new RenderGraphParameters
            {
                executionId = camera.GetEntityId(),
                generateDebugData = camera.cameraType != CameraType.Preview && !camera.isProcessingRenderRequest,
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount,
                renderTextureUVOriginStrategy = renderTextureUVOriginStrategy,
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
