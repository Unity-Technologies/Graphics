using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This pass creates an RTHandle and blits the camera color to it.
// The RTHandle is then set as a global texture, which is available to shaders in the scene.
public class BlitToRTHandlePass : ScriptableRenderPass
{
    class PassData
    {
        public TextureHandle source;
        public Material material;
    }

    private static Vector4 m_ScaleBias = new Vector4(1f, 1f, 0f, 0f);
    private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("BlitToRTHandle_CopyColor");
    private RTHandle m_InputHandle;
    private RTHandle m_OutputHandle;
    private const string k_OutputName = "_CopyColorTexture";
    private int m_OutputId = Shader.PropertyToID(k_OutputName);
    private Material m_Material;

    public BlitToRTHandlePass(RenderPassEvent evt, Material mat)
    {
        renderPassEvent = evt;
        m_Material = mat;
    }

#pragma warning disable 618, 672 // Type or member is obsolete, Member overrides obsolete member

    // Unity calls the Configure method in the Compatibility mode (non-RenderGraph path)
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // Configure the custom RTHandle
        var desc = cameraTextureDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        RenderingUtils.ReAllocateIfNeeded(ref m_OutputHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_OutputName );

        // Set the RTHandle as the output target in the Compatibility mode
        ConfigureTarget(m_OutputHandle);
    }

    // Unity calls the Execute method in the Compatibility mode
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // Set camera color as the input
        m_InputHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            // Blit the input RTHandle to the output one
            Blitter.BlitCameraTexture(cmd, m_InputHandle, m_OutputHandle, m_Material, 0);

            // Make the output texture available for the shaders in the scene
            cmd.SetGlobalTexture(m_OutputId, m_OutputHandle.nameID);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

#pragma warning restore 618, 672

    // Unity calls the RecordRenderGraph method to add and configure one or more render passes in the render graph system.
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        // Create the custom RTHandle
        var desc = cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        RenderingUtils.ReAllocateHandleIfNeeded(ref m_OutputHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_OutputName );

        // Set camera color as a texture resource for this render graph instance
        TextureHandle source = resourceData.activeColorTexture;

        // Set RTHandle as a texture resource for this render graph instance
        TextureHandle destination = renderGraph.ImportTexture(m_OutputHandle);

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("BlitToRTHandle_CopyColor", out var passData))
        {
            if (!source.IsValid() || !destination.IsValid())
                return;

            passData.source = source;
            passData.material = m_Material;
            builder.UseTexture(source, AccessFlags.Read); // Set the camera color as the input
            builder.SetRenderAttachment(destination, 0, AccessFlags.Write); // Set the target texture
            builder.SetGlobalTextureAfterPass(destination, m_OutputId); // Make the output texture available for the shaders in the scene

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                // Blit the input texture to the destination target specified in the SetRenderAttachment method
                Blitter.BlitTexture(context.cmd, data.source, m_ScaleBias, data.material, 0);
            });
        }

        // In this example the pass executes after rendering transparent objects, and the transparent objects are reading the destination texture.
        // The following code sets the TextureHandle as the camera color target to avoid visual artefacts.
        resourceData.cameraColor = destination;
    }

    public void Dispose()
    {
        m_InputHandle?.Release();
        m_OutputHandle?.Release();
    }
}
