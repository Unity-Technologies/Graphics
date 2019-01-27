using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    /// <summary>
    /// Copy the given depth buffer into the given destination depth buffer.
    /// 
    /// You can use this pass to copy a depth buffer to a destination,
    /// so you can use it later in rendering. If the source texture has MSAA
    /// enabled, the pass uses a custom MSAA resolve. If the source texture
    /// does not have MSAA enabled, the pass uses a Blit or a Copy Texture
    /// operation, depending on what the current platform supports.
    /// </summary>
    internal class CopyDepthPass : ScriptableRenderPass
    {
        private RenderTargetHandle source { get; set; }
        private RenderTargetHandle destination { get; set; }
        Material m_CopyDepthMaterial;

        const string k_DepthCopyTag = "Copy Depth";

        public CopyDepthPass(Material copyDepthMaterial)
        {
            m_CopyDepthMaterial = copyDepthMaterial;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Targt</param>
        public void Setup(RenderTargetHandle source, RenderTargetHandle destination)
        {
            this.source = source;
            this.destination = destination;
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
            
            CommandBuffer cmd = CommandBufferPool.Get(k_DepthCopyTag);
            RenderTargetIdentifier depthSurface = source.Identifier();
            RenderTargetIdentifier copyDepthSurface = destination.Identifier();

            RenderTextureDescriptor descriptor = ScriptableRenderer.CreateRenderTextureDescriptor(ref renderingData.cameraData);
            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 32; //TODO: fix this ;
            descriptor.msaaSamples = 1;
            descriptor.bindMS = false;
            cmd.GetTemporaryRT(destination.id, descriptor, FilterMode.Point);

            cmd.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());

            if (renderingData.cameraData.msaaSamples > 1)
            {
                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                if (renderingData.cameraData.msaaSamples == 4)
                {
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                }
                else
                {
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                }
                cmd.Blit(depthSurface, copyDepthSurface, m_CopyDepthMaterial);
            }
            else
            {
                cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                ScriptableRenderer.CopyTexture(cmd, depthSurface, copyDepthSurface, m_CopyDepthMaterial);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            
            if (destination != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(destination.id);
                destination = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
