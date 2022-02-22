using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderer
    {
        protected override void RecordRenderGraphBlock(RenderGraphRenderPassBlock renderPassBlock, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            switch (renderPassBlock)
            {
                case RenderGraphRenderPassBlock.BeforeRendering:
                    OnBeforeRendering(context, ref renderingData);

                    break;
                case RenderGraphRenderPassBlock.MainRendering:
                    OnMainRendering(context, ref renderingData);

                    break;
                case RenderGraphRenderPassBlock.AfterRendering:
                    OnAfterRendering(context, ref renderingData);

                    break;
            }
        }

        private void OnBeforeRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {

        }

        private void OnMainRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RenderGraphTestPass.Render(renderingData.renderGraph);
        }

        private void OnAfterRendering(ScriptableRenderContext context, ref RenderingData renderingData)
        {

        }

    }


    class RenderGraphTestPass
    {
        public class PassData
        {
            public TextureHandle m_Albedo;
        }

        static public PassData Render(RenderGraph graph)
        {
            using (var builder = graph.AddRenderPass<PassData>("Test Pass", out var passData, new ProfilingSampler("Test Pass Profiler")))
            {
                TextureHandle backbuffer = graph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget);
                passData.m_Albedo = builder.UseColorBuffer(backbuffer, 0);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(RTClearFlags.All, Color.red, 0, 0);
                });

                return passData;
            }
        }
    }

}
