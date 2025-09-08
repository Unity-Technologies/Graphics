using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline
    {
        static void RecordAndExecuteRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ScriptableRenderer renderer, CommandBuffer cmd, Camera camera, UniversalRenderPipelineAsset asset)
        {
            RenderGraphParameters rgParams = new RenderGraphParameters
            {
                executionId = camera.GetEntityId(),
                generateDebugData = camera.cameraType != CameraType.Preview && !camera.isProcessingRenderRequest,
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount,
#if ENABLE_RENDERTEXTURE_UV_ORIGIN_STRATEGY
                renderTextureUVOriginStrategy = asset.renderTextureUVOriginStrategy,
#else
                renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.BottomLeft,
#endif
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
