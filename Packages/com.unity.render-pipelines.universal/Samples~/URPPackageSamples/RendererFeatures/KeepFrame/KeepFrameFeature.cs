
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//This renderer feature will replicate a "don't clear" behaviour by injecting two passes into the pipeline:
//One pass that copies color at the end of a frame
//Another pass that draws the content of the copied texture at the beginning of a new frame
public class KeepFrameFeature : ScriptableRendererFeature
{
    //This pass is responsible for copying color to a specified destination
    class CopyFramePass : ScriptableRenderPass
    {
        private RTHandle source { get; set; }
        private RTHandle destination { get; set; }

        public void Setup(RTHandle source, RTHandle destination)
        {
            this.source = source;
            this.destination = destination;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType != CameraType.Game)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("CopyFramePass");
            Blit(cmd, source, destination);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    //This pass is responsible for drawing the old color to a full screen quad
    class DrawOldFramePass : ScriptableRenderPass
    {
        private Material m_DrawOldFrameMaterial;
        private RTHandle m_Handle;
        private string m_TextureName;

        public void Setup(Material drawOldFrameMaterial, RTHandle handle, string textureName)
        {
            m_DrawOldFrameMaterial = drawOldFrameMaterial;
            m_TextureName = textureName;

            m_Handle = handle;

        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_DrawOldFrameMaterial != null)
            {
                CommandBuffer cmd = CommandBufferPool.Get("DrawOldFramePass");
                cmd.SetGlobalTexture(m_TextureName, m_Handle);

                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                Blitter.BlitTexture(cmd, source, viewportScale, m_DrawOldFrameMaterial, 0);

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
        [Tooltip("The name of the texture used for referencing the copied frame. (Defaults to _FrameCopyTex if empty)")]
        public string textureName;
    }

    private CopyFramePass m_CopyFrame;
    private DrawOldFramePass m_DrawOldFrame;

    private RTHandle m_OldFrameHandle;

    public Settings settings = new Settings();

    //In this function the passes are created and their point of injection is set
    public override void Create()
    {
        m_CopyFrame = new CopyFramePass();
        m_CopyFrame.renderPassEvent = RenderPassEvent.AfterRenderingTransparents; //Frame color is copied late in the frame

        m_DrawOldFrame = new DrawOldFramePass();
        m_DrawOldFrame.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques; //Old frame is drawn early in the frame
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_CopyFrame);
        renderer.EnqueuePass(m_DrawOldFrame);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.msaaSamples = 1;
        descriptor.depthBufferBits = 0;
        var textureName = String.IsNullOrEmpty(settings.textureName) ? "_FrameCopyTex" : settings.textureName;
        RenderingUtils.ReAllocateIfNeeded(ref m_OldFrameHandle, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: textureName);
        
        m_DrawOldFrame.ConfigureClear(ClearFlag.None, Color.red);
        m_CopyFrame.Setup(renderer.cameraColorTargetHandle, m_OldFrameHandle);
        m_DrawOldFrame.Setup(settings.displayMaterial, m_OldFrameHandle, textureName);
    }

    protected override void Dispose(bool disposing)
    {
        m_OldFrameHandle?.Release();
    }
}
