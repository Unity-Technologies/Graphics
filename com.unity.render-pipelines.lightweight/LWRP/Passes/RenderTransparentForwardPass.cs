using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class RenderTransparentForwardPass : LightweightForwardPass
    {
        const string k_RenderTransparentsTag = "Render Transparents";

        public RenderTransparentForwardPass(LightweightForwardRenderer renderer) : base(renderer)
        {}

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_RenderTransparentsTag);
            using (new ProfilingSample(cmd, k_RenderTransparentsTag))
            {
                SetRenderTarget(cmd, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, clearFlag, CoreUtils.ConvertSRGBToActiveColorSpace(clearColor));
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Camera camera = renderingData.cameraData.camera;
                var drawSettings = CreateDrawRendererSettings(camera, SortFlags.CommonTransparent, rendererConfiguration, dynamicBatching);
                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, renderer.transparentFilterSettings);

                // Render objects that did not match any shader pass with error shader
                RenderObjectsWithError(ref context, ref cullResults, camera, renderer.transparentFilterSettings, SortFlags.None);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
