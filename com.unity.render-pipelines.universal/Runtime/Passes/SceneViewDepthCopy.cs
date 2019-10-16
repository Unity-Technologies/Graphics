namespace UnityEngine.Rendering.Universal
{
    internal class SceneViewDepthCopyPass : ScriptableRenderPass
    {
        private RenderTargetHandle source { get; set; }

        Material m_CopyDepthMaterial;
        const string m_ProfilerTag = "Copy Depth for Scene View";

        public SceneViewDepthCopyPass(RenderPassEvent evt, Material copyDepthMaterial)
        {
            m_CopyDepthMaterial = copyDepthMaterial;
            renderPassEvent = evt;
        }

        public void Setup(RenderTargetHandle source)
        {
            this.source = source;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_CopyDepthMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_CopyDepthMaterial, GetType().Name);
                return;
            }

            // Restore Render target for additional editor rendering.
            // Note: Scene view camera always perform depth prepass
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            // XRTODO: logic here is flawed.
            // Anything but BuiltinRenderTextureType.CameraTarget would not work.
            // ScriptableRenderer has its own states about current active rt. It accidentially works here becuase the current rt happens to be built-in camera rt.
            // However, this assumption does not always hold
            // Do logic using ScriptableRenderer.SetRenderTarget or override Configure to specify rt upfront.
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);

            cmd.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());
            cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);

            if (renderingData.cameraData.isPureURPCamera)
            {
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

                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyDepthMaterial);
                // XRTODO: fullscreen triangle/quad if support vertexID
                //var propertyBlock = new MaterialPropertyBlock();
                //cmd.DrawProcedural(Matrix4x4.identity, m_CopyDepthMaterial, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
            }
            else
            {
                cmd.Blit(source.Identifier(), BuiltinRenderTextureType.CameraTarget, m_CopyDepthMaterial);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
