using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class SceneViewDepthCopyPass : ScriptableRenderPass
    {
        const string k_CopyDepthToCameraTag = "Copy Depth to Camera";

        private RenderTargetHandle source { get; set; }

        Material m_CopyDepthMaterial;

        public SceneViewDepthCopyPass(Material copyDepthMaterial)
        {
            m_CopyDepthMaterial = copyDepthMaterial;
        }

        public void Setup(RenderTargetHandle source)
        {
            this.source = source;
        }
        
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_CopyDepthMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_CopyDepthMaterial, GetType().Name);
                return;
            }

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
            cmd.Blit(source.Identifier(), BuiltinRenderTextureType.CameraTarget, m_CopyDepthMaterial);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
