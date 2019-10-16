using System;

namespace UnityEngine.Rendering.Universal.Internal
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
    public class CopyDepthPass : ScriptableRenderPass
    {
        private RenderTargetHandle source { get; set; }
        private RenderTargetHandle destination { get; set; }
        Material m_CopyDepthMaterial;
        const string m_ProfilerTag = "Copy Depth";

        public CopyDepthPass(RenderPassEvent evt, Material copyDepthMaterial)
        {
            m_CopyDepthMaterial = copyDepthMaterial;
            renderPassEvent = evt;
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

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var descriptor = cameraTextureDescriptor;
            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 32; //TODO: do we really need this. double check;
            descriptor.msaaSamples = 1;
            cmd.GetTemporaryRT(destination.id, descriptor, FilterMode.Point);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_CopyDepthMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_CopyDepthMaterial, GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            RenderTargetIdentifier depthSurface = source.Identifier();
            RenderTargetIdentifier copyDepthSurface = destination.Identifier();

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            int cameraSamples = descriptor.msaaSamples;

            // TODO: we don't need a command buffer here. We can set these via Material.Set* API
            cmd.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());

            if (renderingData.cameraData.isPureURPCamera)
            {
                switch(cameraSamples)
                {
                    case 1:
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        break;
                    case 2:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        break;
                    case 4:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        break;
                    default:
                        // XRTODO: Add err msg. This case shouldn't really happend. Could be undefined behavior
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        break;
                }
                Matrix4x4 projMatrix;
                Matrix4x4 viewMatrix;
                projMatrix = GL.GetGPUProjectionMatrix(Matrix4x4.identity, true);
                viewMatrix = Matrix4x4.identity;
                Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
                Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_ViewMatrix"), viewMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_InvViewMatrix"), Matrix4x4.Inverse(viewMatrix));
                cmd.SetGlobalMatrix(Shader.PropertyToID("_ProjMatrix"), projMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_InvProjMatrix"), Matrix4x4.Inverse(projMatrix));
                cmd.SetGlobalMatrix(Shader.PropertyToID("_ViewProjMatrix"), viewProjMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_InvViewProjMatrix"), Matrix4x4.Inverse(viewProjMatrix));

                // XRTODO: Can't use coreutil to set rendertarget becuase it doesn't configure scriptable renderer's internal states
                // Must use ScriptableRenderer.SetRenderTarget. Better solution is to override Configure to specify rt upfront.
                // Or redesign renderpass to only specify rt descriptor instead of taking in specific gpu resources.
                //CoreUtils.SetRenderTarget(cmd, copyDepthSurface);
                ScriptableRenderer.SetRenderTarget(cmd, copyDepthSurface, BuiltinRenderTextureType.CameraTarget, clearFlag, clearColor);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyDepthMaterial);
            }
            else
            {
                if (cameraSamples > 1)
                {
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                    if (cameraSamples == 4)
                    {
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    }
                    else
                    {
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    }

                    Blit(cmd, depthSurface, copyDepthSurface, m_CopyDepthMaterial);
                }
                else
                {
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    CopyTexture(cmd, depthSurface, copyDepthSurface, m_CopyDepthMaterial);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void CopyTexture(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, Material material)
        {
            // TODO: In order to issue a copyTexture we need to also check if source and dest have same size
            //if (SystemInfo.copyTextureSupport != CopyTextureSupport.None)
            //    cmd.CopyTexture(source, dest);
            //else
            Blit(cmd, source, dest, material);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(destination.id);
            destination = RenderTargetHandle.CameraTarget;
        }
    }
}
