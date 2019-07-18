using System;

namespace UnityEngine.Rendering.Universal
{
    internal class ScreenSpaceShadowResolvePass : ScriptableRenderPass
    {
        Material m_ScreenSpaceShadowsMaterial;
        RenderTargetHandle m_ScreenSpaceShadowmap;
        RenderTextureDescriptor m_RenderTextureDescriptor;
        const string m_ProfilerTag = "Resolve Shadows";

        public ScreenSpaceShadowResolvePass(RenderPassEvent evt, Material screenspaceShadowsMaterial)
        {
            m_ScreenSpaceShadowsMaterial = screenspaceShadowsMaterial;
            m_ScreenSpaceShadowmap.Init("_ScreenSpaceShadowmapTexture");
            renderPassEvent = evt;
        }

        public void Setup(RenderTextureDescriptor baseDescriptor)
        {
            m_RenderTextureDescriptor = baseDescriptor;
            m_RenderTextureDescriptor.depthBufferBits = 0;
            m_RenderTextureDescriptor.msaaSamples = 1;
            m_RenderTextureDescriptor.colorFormat = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(m_ScreenSpaceShadowmap.id, m_RenderTextureDescriptor, FilterMode.Bilinear);

            RenderTargetIdentifier screenSpaceOcclusionTexture = m_ScreenSpaceShadowmap.Identifier();
            ConfigureTarget(screenSpaceOcclusionTexture);
            ConfigureClear(ClearFlag.All, Color.white);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_ScreenSpaceShadowsMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_ScreenSpaceShadowsMaterial, GetType().Name);
                return;
            }

            if (renderingData.lightData.mainLightIndex == -1)
                return;

            Camera camera = renderingData.cameraData.camera;
            bool stereo = renderingData.cameraData.isStereoEnabled;

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            if (!stereo)
            {
                // XRTODO: this code pass is not tested yet.
                Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(Matrix4x4.identity, true);
                Matrix4x4 viewMatrix = Matrix4x4.identity;
                Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
                Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(viewProjMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_ViewMatrix"), viewMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_ProjMatrix"), projMatrix);
                cmd.SetGlobalMatrix(Shader.PropertyToID("_ViewProjMatrix"), viewProjMatrix);
                if (renderingData.cameraData.xrPass.xrSdkEnabled)
                {
                    Matrix4x4 xrProjMatrix = renderingData.cameraData.xrPass.GetProjMatrix(0);
                    Matrix4x4 xrViewMatrix = renderingData.cameraData.xrPass.GetViewMatrix(0);
                    Matrix4x4 xrViewProjMatrix = xrProjMatrix * xrViewMatrix;
                    cmd.SetGlobalMatrix(Shader.PropertyToID("_InvProjMatrix"), Matrix4x4.Inverse(xrProjMatrix));
                    cmd.SetGlobalMatrix(Shader.PropertyToID("_InvViewMatrix"), Matrix4x4.Inverse(xrViewMatrix));
                    cmd.SetGlobalMatrix(Shader.PropertyToID("_InvViewProjMatrix"), Matrix4x4.Inverse(xrViewProjMatrix));

                }
                else
                {
                    Matrix4x4 xrProjMatrix = renderingData.cameraData.camera.projectionMatrix;
                    Matrix4x4 xrViewMatrix = renderingData.cameraData.camera.worldToCameraMatrix;
                    Matrix4x4 xrViewProjMatrix = xrProjMatrix * xrViewMatrix;
                    cmd.SetGlobalMatrix(Shader.PropertyToID("_InvProjMatrix"), Matrix4x4.Inverse(xrProjMatrix));
                    cmd.SetGlobalMatrix(Shader.PropertyToID("_InvViewMatrix"), Matrix4x4.Inverse(xrViewMatrix));
                    cmd.SetGlobalMatrix(Shader.PropertyToID("_InvViewProjMatrix"), Matrix4x4.Inverse(xrViewProjMatrix));

                }

                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_ScreenSpaceShadowsMaterial);
            }
            else
            {
                // Avoid setting and restoring camera view and projection matrices when in stereo.
                RenderTargetIdentifier screenSpaceOcclusionTexture = m_ScreenSpaceShadowmap.Identifier();
                Blit(cmd, screenSpaceOcclusionTexture, screenSpaceOcclusionTexture, m_ScreenSpaceShadowsMaterial);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(m_ScreenSpaceShadowmap.id);
        }
    }
}
