using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    /// <summary>
    /// End XR rendering
    ///
    /// This pass disables XR rendering. Pair this pass with the BeginXRRenderingPass.
    /// If this pass is issued without a matching BeginXRRenderingPass it will lead to
    /// undefined rendering results. 
    /// </summary>
    internal class EndXRRenderingPass : ScriptableRenderPass
    {
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            context.StopMultiEye(camera);
            context.StereoEndRender(camera);
        }
    }
}
