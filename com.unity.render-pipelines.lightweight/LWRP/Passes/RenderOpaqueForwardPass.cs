using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class RenderOpaqueForwardPass : LightweightForwardPass
    {
        const string k_RenderOpaquesTag = "Render Opaques";
        public FilterRenderersSettings opaqueFilterSettings { get; private set; }

        public RenderOpaqueForwardPass()
        {
            opaqueFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque,
            };
        }

        public override void Execute(LightweightForwardRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_RenderOpaquesTag);
            using (new ProfilingSample(cmd, k_RenderOpaquesTag))
            {
                SetRenderTarget(cmd, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, clearFlag, CoreUtils.ConvertSRGBToActiveColorSpace(clearColor));

                // TODO: We need a proper way to handle multiple camera/ camera stack. Issue is: multiple cameras can share a same RT
                // (e.g, split screen games). However devs have to be dilligent with it and know when to clear/preserve color.
                // For now we make it consistent by resolving viewport with a RT until we can have a proper camera management system
                //if (colorAttachmentHandle == -1 && !cameraData.isDefaultViewport)
                //    cmd.SetViewport(camera.pixelRect);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Camera camera = renderingData.cameraData.camera;
                var drawSettings = CreateDrawRendererSettings(camera, SortFlags.CommonOpaque, rendererConfiguration, dynamicBatching);
                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, opaqueFilterSettings);

                // Render objects that did not match any shader pass with error shader
                RenderObjectsWithError(renderer, ref context, ref cullResults, camera, opaqueFilterSettings, SortFlags.None);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
