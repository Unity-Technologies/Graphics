using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KeepFrameFeature : ScriptableRendererFeature
{
    class CopyFramePass : ScriptableRenderPass
    {
        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle destination { get; set; }

        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination)
        {
            this.source = source;
            this.destination = destination;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType != CameraType.Game)
                return;
            
            CommandBuffer cmd = CommandBufferPool.Get("CopyFramePass");
            RenderTargetIdentifier opaqueColorRT = destination.Identifier();
            Blit(cmd, source, opaqueColorRT);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(destination.id);
            
            if (destination != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(destination.id);
                destination = RenderTargetHandle.CameraTarget;
            }
        }
    }

    class DrawOldFramePass : ScriptableRenderPass
    {
        private Material m_DrawOldFrameMaterial;
        private RenderTargetHandle handle;

        public void Setup(Material drawOldFrameMaterial, RenderTargetHandle handle)
        {
            m_DrawOldFrameMaterial = drawOldFrameMaterial;
            this.handle = handle;
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
        {
            RenderTextureDescriptor descriptor = cameraTextureDescripor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            cmd.GetTemporaryRT(handle.id, descriptor,FilterMode.Bilinear);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_DrawOldFrameMaterial != null)
            {
                CommandBuffer cmd = CommandBufferPool.Get("DrawOldFramePass");
                cmd.SetGlobalTexture("_FrameCopyTex", handle.id);
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_DrawOldFrameMaterial, 0, 0);
                cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
    
    [Serializable]
    public class Settings
    {
        [Tooltip("The material that is used when the old frame is redrawn at the start of the new frame (before opaques).")]
        public Material displayMaterial;
        [Tooltip("What the to call the texture that is referenced in the display material")]
        public string textureName;
    }

    private CopyFramePass m_CopyFrame;
    private DrawOldFramePass m_DrawOldFame;

    private RenderTargetHandle m_OldFrameHandle;
    
    public Settings settings = new Settings();

    public override void Create()
    {
        m_CopyFrame = new CopyFramePass();
        m_CopyFrame.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        m_DrawOldFame = new DrawOldFramePass();
        m_DrawOldFame.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_OldFrameHandle.Init("_OldFrameRenderTarget");
        m_DrawOldFame.ConfigureClear(ClearFlag.None, Color.red);

        m_CopyFrame.Setup(renderer.cameraColorTarget, m_OldFrameHandle);
        renderer.EnqueuePass(m_CopyFrame);
        
        m_DrawOldFame.Setup(settings.displayMaterial, m_OldFrameHandle);
        renderer.EnqueuePass(m_DrawOldFame);
    }
}


