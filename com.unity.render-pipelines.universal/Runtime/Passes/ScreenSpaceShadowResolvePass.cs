using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Resolves shadows in a screen space texture.
    /// </summary>
    public class ScreenSpaceShadowResolvePass : ScriptableRenderPass
    {
        Material m_ScreenSpaceShadowsMaterial;
        RTHandle m_ScreenSpaceShadowmap;

        public ScreenSpaceShadowResolvePass(RenderPassEvent evt, Material screenspaceShadowsMaterial)
        {
            base.profilingSampler = new ProfilingSampler(nameof(ScreenSpaceShadowResolvePass));

            m_ScreenSpaceShadowsMaterial = screenspaceShadowsMaterial;
            renderPassEvent = evt;
        }

        public void Dispose()
        {
            m_ScreenSpaceShadowmap.Release();
        }

        public void Setup(RenderTextureDescriptor baseDescriptor)
        {
            var desc = baseDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.graphicsFormat = RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R8_UNorm, FormatUsage.Linear | FormatUsage.Render)
                ? GraphicsFormat.R8_UNorm
                : GraphicsFormat.B8G8R8A8_UNorm;

            RenderingUtils.ReAllocateIfNeeded(ref m_ScreenSpaceShadowmap, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ScreenSpaceShadowmapTexture");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(m_ScreenSpaceShadowmap);
            ConfigureClear(ClearFlag.All, Color.white);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_ScreenSpaceShadowsMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_ScreenSpaceShadowsMaterial, GetType().Name);
                return;
            }

            if (renderingData.lightData.mainLightIndex == -1)
                return;

            Camera camera = renderingData.cameraData.camera;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.ResolveShadows)))
            {
                if (!renderingData.cameraData.xr.enabled)
                {
                    cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_ScreenSpaceShadowsMaterial);
                    cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                }
                else
                {
                    // Avoid setting and restoring camera view and projection matrices when in stereo.
                    Blit(cmd, m_ScreenSpaceShadowmap, m_ScreenSpaceShadowmap, m_ScreenSpaceShadowsMaterial);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
