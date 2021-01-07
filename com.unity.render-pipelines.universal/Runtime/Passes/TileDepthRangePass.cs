using System;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Calculate min and max depth per screen tile for tiled-based deferred shading.
    /// </summary>
    internal class TileDepthRangePass : ScriptableRenderPass
    {
        DeferredLights m_DeferredLights;
        int m_PassIndex = 0;

        public TileDepthRangePass(RenderPassEvent evt, DeferredLights deferredLights, int passIndex)
        {
            base.profilingSampler = new ProfilingSampler(nameof(TileDepthRangePass));
            base.renderPassEvent = evt;
            m_DeferredLights = deferredLights;
            m_PassIndex = passIndex;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int outputTexId;
            RTHandle outputTex;
            RenderTextureDescriptor desc;

            if (m_PassIndex == 0 && m_DeferredLights.HasTileDepthRangeExtraPass())
            {
                int alignment = 1 << DeferredConfig.kTileDepthInfoIntermediateLevel;
                int depthInfoWidth = (m_DeferredLights.RenderWidth + alignment - 1) >> DeferredConfig.kTileDepthInfoIntermediateLevel;
                int depthInfoHeight = (m_DeferredLights.RenderHeight + alignment - 1) >> DeferredConfig.kTileDepthInfoIntermediateLevel;

                outputTex = m_DeferredLights.DepthInfoTexture;
                outputTexId = URPShaderIDs._DepthInfoTexture;
                desc = new RenderTextureDescriptor(depthInfoWidth, depthInfoHeight, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_UInt, 0);
            }
            else
            {
                int tileDepthRangeWidth = m_DeferredLights.GetTiler(0).TileXCount;
                int tileDepthRangeHeight = m_DeferredLights.GetTiler(0).TileYCount;

                outputTex = m_DeferredLights.TileDepthInfoTexture;
                outputTexId = URPShaderIDs._TileDepthInfoTexture;
                desc = new RenderTextureDescriptor(tileDepthRangeWidth, tileDepthRangeHeight, UnityEngine.Experimental.Rendering.GraphicsFormat.R32_UInt, 0);
            }
            cmd.GetTemporaryRT(outputTexId, desc, FilterMode.Point);
            base.ConfigureTarget(outputTex);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_PassIndex == 0)
                m_DeferredLights.ExecuteTileDepthInfoPass(context, ref renderingData);
            else
                m_DeferredLights.ExecuteDownsampleBitmaskPass(context, ref renderingData);
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(URPShaderIDs._TileDepthInfoTexture);
            m_DeferredLights.TileDepthInfoTexture = RTHandles.Alloc(RenderTargetHandle.CameraTarget.Identifier());
        }
    }
}
