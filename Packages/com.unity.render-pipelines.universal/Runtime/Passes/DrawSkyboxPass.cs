using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw the skybox into the given color buffer using the given depth buffer for depth testing.
    ///
    /// This pass renders the standard Unity skybox.
    /// </summary>
    public class DrawSkyboxPass : ScriptableRenderPass
    {
        private PassData m_PassData;
        private RendererList m_SkyRendererList;

        /// <summary>
        /// Creates a new <c>DrawSkyboxPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        public DrawSkyboxPass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DrawSkyboxPass));

            renderPassEvent = evt;
            m_PassData = new PassData();
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var activeDebugHandler = GetActiveDebugHandler(ref renderingData);
            if (activeDebugHandler != null)
            {
                // TODO: The skybox needs to work the same as the other shaders, but until it does we'll not render it
                // when certain debug modes are active (e.g. wireframe/overdraw modes)
                if (activeDebugHandler.IsScreenClearNeeded)
                {
                    return;
                }
            }

            InitSkyboxRendererList(context, ref renderingData);
            InitPassData(ref renderingData, ref m_PassData);
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData.skyRendererList, ref renderingData);
        }

        private static void ExecutePass(RasterCommandBuffer cmd, RendererList rendererList, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
                cmd.SetSinglePassStereo(SystemInfo.supportsMultiview ? SinglePassStereoMode.Multiview : SinglePassStereoMode.Instancing);
#endif
            cmd.DrawRendererList(rendererList);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
                cmd.SetSinglePassStereo(SinglePassStereoMode.None);
#endif
        }

        private class PassData
        {
            internal RenderingData renderingData;
            internal RendererList skyRendererList;
        }

        private void InitPassData(ref RenderingData renderingData, ref PassData passData)
        {
            passData.renderingData = renderingData;
            passData.skyRendererList = m_SkyRendererList;
        }

        private void InitSkyboxRendererList(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                // Setup Legacy XR buffer states
                if (cameraData.xr.singlePassEnabled)
                {
                    m_SkyRendererList = context.CreateSkyboxRendererList(cameraData.camera,
                        cameraData.GetProjectionMatrix(0), cameraData.GetViewMatrix(0),
                        cameraData.GetProjectionMatrix(1), cameraData.GetViewMatrix(1));
                }
                else
                {
                    m_SkyRendererList = context.CreateSkyboxRendererList(cameraData.camera, cameraData.GetProjectionMatrix(0), cameraData.GetViewMatrix(0));
                }
            }
            else
#endif
            {
                m_SkyRendererList = context.CreateSkyboxRendererList(cameraData.camera);
            }
        }

        internal void Render(RenderGraph renderGraph, ScriptableRenderContext context, TextureHandle colorTarget, TextureHandle depthTarget, ref RenderingData renderingData)
        {
            var activeDebugHandler = GetActiveDebugHandler(ref renderingData);
            if (activeDebugHandler != null)
            {
                // TODO: The skybox needs to work the same as the other shaders, but until it does we'll not render it
                // when certain debug modes are active (e.g. wireframe/overdraw modes)
                if (activeDebugHandler.IsScreenClearNeeded)
                {
                    return;
                }
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Skybox Pass", out var passData,
                base.profilingSampler))
            {
                InitSkyboxRendererList(context, ref renderingData);
                InitPassData(ref renderingData, ref passData);
                builder.UseTextureFragment(colorTarget, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragmentDepth(depthTarget, IBaseRenderGraphBuilder.AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data.skyRendererList, ref data.renderingData);
                });
            }
        }
    }
}
