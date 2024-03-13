#if ENABLE_VR && ENABLE_XR_MODULE

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw the XR occlusion mesh into the current depth buffer when XR is enabled.
    /// </summary>
    public class XROcclusionMeshPass : ScriptableRenderPass
    {
        /// <summary>
        /// Used to indicate if the active target of the pass is the back buffer
        /// </summary>
        public bool m_IsActiveTargetBackBuffer; // TODO: Remove this when we remove non-RG path

        public XROcclusionMeshPass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(XROcclusionMeshPass));
            renderPassEvent = evt;
            m_IsActiveTargetBackBuffer = false;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.xr.enabled)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();

            if (m_IsActiveTargetBackBuffer)
                cmd.SetViewport(renderingData.cameraData.xr.GetViewport());

            renderingData.cameraData.xr.RenderOcclusionMesh(cmd);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}

#endif
