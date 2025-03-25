using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Create a Scriptable Renderer Feature that replicates Don't Clear behavior by injecting two render passes into the pipeline.
// The first pass copies the camera color target at the end of a frame. The second pass draws the contents of the copied texture at the beginning of a new frame.
// For more information about creating Scriptable Renderer Features, refer to https://docs.unity3d.com/Manual/urp/customizing-urp.html.
public class KeepFrameFeature : ScriptableRendererFeature
{
    // Create the custom render pass that copies the camera color to a destination texture.
    class CopyFramePass : ScriptableRenderPass
    {
        // Declare the destination texture.
        RTHandle m_Destination;

        // Declare the resources the render pass uses.
        public void Setup(RTHandle destination)
        {
            m_Destination = destination;
        }

#pragma warning disable 618, 672 // Type or member is obsolete, Member overrides obsolete member

        // Override the Execute method to implement the rendering logic.
        // This method is used only in the Compatibility Mode path.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Skip rendering if the camera isn't a game camera.
            if (renderingData.cameraData.camera.cameraType != CameraType.Game)
                return;

            // Set the source texture as the camera color target.
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Get a command buffer.
            CommandBuffer cmd = CommandBufferPool.Get("CopyFramePass");

            // Blit the camera color target to the destination texture.
            Blit(cmd, source, m_Destination);

            // Execute the command buffer.
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

#pragma warning restore 618, 672

        // Override the RecordRenderGraph method to implement the rendering logic.
        // This method is used only in the render graph system path.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Get the resources the pass uses.
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Skip rendering if the camera isn't a game camera.
            if (cameraData.camera.cameraType != CameraType.Game)
                return;
            
            // Set the source texture as the camera color target.
            TextureHandle source = resourceData.activeColorTexture;
            
            // Import the texture that persists across frames, so the render graph system can use it.
            TextureHandle destination = renderGraph.ImportTexture(m_Destination);
            
            if (!source.IsValid() || !destination.IsValid())
                return;
            
            // Blit the content of the copied texture to the camera color target with a material.
            RenderGraphUtils.BlitMaterialParameters para = new(source, destination, Blitter.GetBlitMaterial(TextureDimension.Tex2D), 0);
            renderGraph.AddBlitPass(para, "Copy Frame Pass");
        }
    }

    // Create the custom render pass that draws the contents of the copied texture at the beginning of a new frame.
    class DrawOldFramePass : ScriptableRenderPass
    {
        // Declare the resources the render pass uses.
        class PassData
        {
            public TextureHandle source;
            public Material material;
            public string name;
        }

        Material m_DrawOldFrameMaterial;
        RTHandle m_Handle;
        string m_TextureName;

        // Set up the resources the render pass uses.
        public void Setup(Material drawOldFrameMaterial, RTHandle handle, string textureName)
        {
            m_DrawOldFrameMaterial = drawOldFrameMaterial;
            m_TextureName = textureName;
            m_Handle = handle;
        }

        // Blit the copied texture to the camera color target.
        // This method uses common draw commands that both the render graph system and Compatibility Mode paths can use.
        static void ExecutePass(RasterCommandBuffer cmd, RTHandle source, Material material)
        {
            if (material == null)
                return;

            // Get the viewport scale.
            Vector2 viewportScale = source.useScaling ? new Vector2(source.rtHandleProperties.rtHandleScale.x, source.rtHandleProperties.rtHandleScale.y) : Vector2.one;

            // Blit the copied texture to the camera color target.
            Blitter.BlitTexture(cmd, source, viewportScale, material, 0);
        }

#pragma warning disable 618, 672 // Type or member is obsolete, Member overrides obsolete member

        // Override the Execute method to implement the rendering logic.
        // This method is used only in the Compatibility Mode path.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Get a command buffer.
            CommandBuffer cmd = CommandBufferPool.Get(nameof(DrawOldFramePass));
            cmd.SetGlobalTexture(m_TextureName, m_Handle);

            // Set the source texture as the camera color target.
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Blit the camera color target to the destination texture.
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(cmd), source, m_DrawOldFrameMaterial);

            // Execute the command buffer.
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

#pragma warning restore 618, 672

        // Override the RecordRenderGraph method to implement the rendering logic.
        // This method is used only in the render graph system path.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Get the resources the pass uses.
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Import the texture that persists across frames, so the render graph system can use it.
            TextureHandle oldFrameTextureHandle = renderGraph.ImportTexture(m_Handle); 

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Draw Old Frame Pass", out var passData))
            {
                // Set the destination texture as the camera color target.
                TextureHandle destination = resourceData.activeColorTexture;

                if (!oldFrameTextureHandle.IsValid() || !destination.IsValid())
                    return;

                // Set the resources the pass uses.
                passData.material = m_DrawOldFrameMaterial;
                passData.source = oldFrameTextureHandle;
                passData.name = m_TextureName;

                // Set the render graph system to read the copied texture, and write to the camera color target.
                builder.UseTexture(oldFrameTextureHandle, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                // Allow global state modifications. Use this only where necessary as it introduces a synchronization point in the frame, which might have an impact on performance.
                builder.AllowGlobalStateModification(true);

                // Set the render method.
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(data.name, data.source);
                    ExecutePass(context.cmd, data.source, data.material);
                });
            }
        }
    }

    [Serializable]
    public class Settings
    {
        [Tooltip("Sets the material to use to draw the previous frame.")]
        public Material displayMaterial;
        [Tooltip("Sets the texture to copy each frame into. The default it _FrameCopyTex.")]
        public string textureName;
    }

    CopyFramePass m_CopyFrame;
    DrawOldFramePass m_DrawOldFrame;

    RTHandle m_OldFrameHandle;

    public Settings settings = new Settings();

    // In this function the passes are created and their point of injection is set
    public override void Create()
    {
        m_CopyFrame = new CopyFramePass();
        m_CopyFrame.renderPassEvent = RenderPassEvent.AfterRenderingTransparents; // Frame color is copied late in the frame

        m_DrawOldFrame = new DrawOldFramePass();
        m_DrawOldFrame.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques; // Old frame is drawn early in the frame
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.msaaSamples = 1;
        descriptor.depthBufferBits = 0;
        descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;
        var textureName = String.IsNullOrEmpty(settings.textureName) ? "_FrameCopyTex" : settings.textureName;
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_OldFrameHandle, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: textureName);

        m_CopyFrame.Setup(m_OldFrameHandle);
        m_DrawOldFrame.Setup(settings.displayMaterial, m_OldFrameHandle, textureName);

        renderer.EnqueuePass(m_CopyFrame);
        renderer.EnqueuePass(m_DrawOldFrame);
    }

    protected override void Dispose(bool disposing)
    {
        m_OldFrameHandle?.Release();
    }
}
