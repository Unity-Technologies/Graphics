using System;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    /// <summary>
    /// End XR rendering
    ///
    /// This pass disables XR rendering. Pair this pass with the BeginXRRenderingPass.
    /// If this pass is issued without a matching BeginXRRenderingPass it will lead to
    /// undefined rendering results. 
    /// </summary>
    public class EndXRRenderingPass : ScriptableRenderPass
    {
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            Camera camera = renderingData.cameraData.camera;
            context.StopMultiEye(camera);
            context.StereoEndRender(camera);
        }
    }
}
