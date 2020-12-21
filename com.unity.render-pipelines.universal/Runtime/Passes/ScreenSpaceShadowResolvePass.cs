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
        RenderTextureDescriptor m_RenderTextureDescriptor;

        public ScreenSpaceShadowResolvePass(RenderPassEvent evt, Material screenspaceShadowsMaterial)
        {
            base.profilingSampler = new ProfilingSampler(nameof(ScreenSpaceShadowResolvePass));

            m_ScreenSpaceShadowsMaterial = screenspaceShadowsMaterial;
            renderPassEvent = evt;
        }

        public void Setup(RenderTextureDescriptor baseDescriptor)
        {
            m_RenderTextureDescriptor = baseDescriptor;
            m_RenderTextureDescriptor.depthBufferBits = 0;
            m_RenderTextureDescriptor.msaaSamples = 1;
            m_RenderTextureDescriptor.graphicsFormat = RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R8_UNorm, FormatUsage.Linear | FormatUsage.Render)
                ? GraphicsFormat.R8_UNorm
                : GraphicsFormat.B8G8R8A8_UNorm;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            m_ScreenSpaceShadowmap = RTHandles.Alloc(
                m_RenderTextureDescriptor.width, m_RenderTextureDescriptor.height, 1,
                (DepthBits)m_RenderTextureDescriptor.depthBufferBits,
                m_RenderTextureDescriptor.graphicsFormat,
                FilterMode.Bilinear,
                dimension: m_RenderTextureDescriptor.dimension,
                enableRandomWrite: m_RenderTextureDescriptor.enableRandomWrite,
                useMipMap: m_RenderTextureDescriptor.useMipMap,
                autoGenerateMips: m_RenderTextureDescriptor.autoGenerateMips,
                msaaSamples: (MSAASamples)m_RenderTextureDescriptor.msaaSamples,
                useDynamicScale: m_RenderTextureDescriptor.useDynamicScale,
                memoryless: m_RenderTextureDescriptor.memoryless,
                name: "_ScreenSpaceShadowmapTexture");
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

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            m_ScreenSpaceShadowmap.Release();
        }
    }
}
