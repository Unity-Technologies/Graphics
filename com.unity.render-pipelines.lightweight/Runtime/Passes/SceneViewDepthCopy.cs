using System;
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
        
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            // Restore Render target for additional editor rendering.
            // Note: Scene view camera always perform depth prepass
            CommandBuffer cmd = CommandBufferPool.Get(k_CopyDepthToCameraTag);
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
            cmd.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());
            cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
            cmd.Blit(source.Identifier(), BuiltinRenderTextureType.CameraTarget, renderer.GetMaterial(MaterialHandle.CopyDepth));
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
