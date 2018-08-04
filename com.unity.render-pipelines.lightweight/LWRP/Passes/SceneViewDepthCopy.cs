using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class SceneViewDepthCopyPass : ScriptableRenderPass
    {
        const string k_CopyDepthToCameraTag = "Copy Depth to Camera";

        private RenderTargetHandle source { get; set; }

        public void Setup(RenderTargetHandle source)
        {
            this.source = source;
        }

        public override void Execute(LightweightForwardRenderer renderer, ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            // Restore Render target for additional editor rendering.
            // Note: Scene view camera always perform depth prepass
            CommandBuffer cmd = CommandBufferPool.Get(k_CopyDepthToCameraTag);
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
            cmd.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());
            cmd.EnableShaderKeyword(LightweightKeywordStrings.DepthNoMsaa);
            cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa2);
            cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa4);
            cmd.Blit(source.Identifier(), BuiltinRenderTextureType.CameraTarget, renderer.GetMaterial(MaterialHandles.DepthCopy));
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
