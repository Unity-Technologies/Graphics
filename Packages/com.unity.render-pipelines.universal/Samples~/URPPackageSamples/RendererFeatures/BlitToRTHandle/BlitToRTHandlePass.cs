using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// This pass creates an RTHandle and blits the camera color to it.
// The RTHandle is then set as a global texture, which is available to shaders in the scene.
public class BlitToRTHandlePass : ScriptableRenderPass
{
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
    
    public void SetInput(RTHandle src)
    {
       // The Renderer Feature uses this variable to set the input RTHandle.
        m_InputHandle = src;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        // Configure the custom RTHandle
        var desc = cameraTextureDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;
        RenderingUtils.ReAllocateIfNeeded(ref m_OutputHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_OutputName );
        
        // Set the RTHandle as the output target
        ConfigureTarget(m_OutputHandle);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
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
    
    public void Dispose()
    {
        m_InputHandle?.Release();
        m_OutputHandle?.Release();
    }
}