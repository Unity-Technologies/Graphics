using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class SceneViewDepthCopyPass : ScriptableRenderPass
    {
        private RenderTargetHandle source { get; set; }


        public SceneViewDepthCopyPass(LightweightForwardRenderer renderer) : base(renderer)
        {}

        public void Setup(RenderTargetHandle source)
        {
            this.source = source;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            // Restore Render target for additional editor rendering.
            // Note: Scene view camera always perform depth prepass
            CommandBuffer cmd = CommandBufferPool.Get("Copy Depth to Camera");
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
            cmd.EnableShaderKeyword(LightweightKeywordStrings.DepthNoMsaa);
            cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa2);
            cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa4);
            cmd.Blit(source.Identifier(), BuiltinRenderTextureType.CameraTarget, renderer.GetMaterial(MaterialHandles.DepthCopy));
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}