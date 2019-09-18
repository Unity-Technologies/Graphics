using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Calculate min and max depth per screen tile for tiled-based deferred shading.
    /// </summary>
    internal class TileDepthRangePass : ScriptableRenderPass
    {
        DeferredLights m_DeferredLights;

        public TileDepthRangePass(RenderPassEvent evt, DeferredLights deferredLights)
        {
            base.renderPassEvent = evt;
            m_DeferredLights = deferredLights;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int tileDepthRangeWidth = m_DeferredLights.GetTiler(0).GetTileXCount();
            int tileDepthRangeHeight = m_DeferredLights.GetTiler(0).GetTileYCount();
            RenderTextureDescriptor desc = new RenderTextureDescriptor(tileDepthRangeWidth, tileDepthRangeHeight,  UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SFloat, 0);
            cmd.GetTemporaryRT(m_DeferredLights.m_TileDepthRangeTexture.id, desc, FilterMode.Point);
            base.ConfigureTarget(m_DeferredLights.m_TileDepthRangeTexture.Identifier());
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_DeferredLights.ExecuteTileDepthRangePass(context, ref renderingData);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(m_DeferredLights.m_TileDepthRangeTexture.id);
            m_DeferredLights.m_TileDepthRangeTexture = RenderTargetHandle.CameraTarget;
        }
    }
}
