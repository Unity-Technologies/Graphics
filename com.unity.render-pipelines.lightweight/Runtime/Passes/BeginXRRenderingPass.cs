using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    /// <summary>
    /// Start rendering that supports XR.
    ///
    /// This pass enables XR rendering. You must also configure
    /// the XR rendering in the global XR Graphics settings.
    ///
    /// Pair this pass with the EndXRRenderingPass.  If this
    /// pass is issued without a matching EndXRRenderingPass
    /// it will lead to undefined rendering results. 
    /// </summary>
    internal class BeginXRRenderingPass : ScriptableRenderPass
    {
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            context.StartMultiEye(camera);
        }
    }
}
