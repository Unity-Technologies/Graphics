using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw the skybox into the given color buffer using the given depth buffer for depth testing.
    ///
    /// This pass renders the standard Unity skybox.
    /// </summary>
    public class DrawSkyboxPass : ScriptableRenderPass
    {
        /// <summary>
        /// Creates a new <c>DrawSkyboxPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        public DrawSkyboxPass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DrawSkyboxPass));

            renderPassEvent = evt;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

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

            RendererList rl;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                // Setup Legacy XR buffer states
                if (cameraData.xr.singlePassEnabled)
                {
                    rl = context.CreateSkyboxRendererList(camera,
                        cameraData.GetProjectionMatrix(0), cameraData.GetViewMatrix(0),
                        cameraData.GetProjectionMatrix(1), cameraData.GetViewMatrix(1));
                }
                else
                {
                    rl = context.CreateSkyboxRendererList(camera, cameraData.GetProjectionMatrix(0), cameraData.GetViewMatrix(0));
                }
            }
            else
#endif
            {
                rl = context.CreateSkyboxRendererList(camera);
            }

            var cmd = renderingData.commandBuffer;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
                cmd.SetSinglePassStereo(SystemInfo.supportsMultiview ? SinglePassStereoMode.Multiview : SinglePassStereoMode.Instancing);
#endif

            cmd.DrawRendererList(rl);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
                cmd.SetSinglePassStereo(SinglePassStereoMode.None);
#endif
        }

        private class PassData
        {
            internal TextureHandle color;
            internal TextureHandle depth;

            internal RenderingData renderingData;

            internal DrawSkyboxPass pass;
        }

        internal void Render(RenderGraph renderGraph, TextureHandle colorTarget, TextureHandle depthTarget, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            using (var builder = renderGraph.AddRenderPass<PassData>("Draw Skybox Pass", out var passData,
                base.profilingSampler))
            {
                passData.color = builder.UseColorBuffer(colorTarget, 0);
                passData.depth = builder.UseDepthBuffer(depthTarget, DepthAccess.Read);

                passData.renderingData = renderingData;
                passData.pass = this;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    data.pass.Execute(context.renderContext, ref data.renderingData);
                });
            }
        }
    }
}
