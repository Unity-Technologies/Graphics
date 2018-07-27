using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class SceneViewDepthCopyPass : ScriptableRenderPass
    {
        private RenderTargetHandle source { get; set; }
        private Material depthCopyMaterial { get; set; }

        public SceneViewDepthCopyPass(Material depthCopyMaterial)
        {
            this.depthCopyMaterial = depthCopyMaterial;
        }

        public void Setup(RenderTargetHandle source)
        {
            this.source = source;
        }

        public override void Execute(ref ScriptableRenderContext context,
            ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            // Restore Render target for additional editor rendering.
            // Note: Scene view camera always perform depth prepass
            CommandBuffer cmd = CommandBufferPool.Get("Copy Depth to Camera");
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
            cmd.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());
            cmd.EnableShaderKeyword(LightweightKeywordStrings.DepthNoMsaa);
            cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa2);
            cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa4);
            cmd.Blit(source.Identifier(), BuiltinRenderTextureType.CameraTarget, depthCopyMaterial);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}