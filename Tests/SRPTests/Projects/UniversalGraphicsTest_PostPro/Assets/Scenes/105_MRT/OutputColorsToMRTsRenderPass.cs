using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Copy input color buffer's channel to several output color buffers
// Used to test MRT support
internal class OutputColorsToMRTsRenderPass : ScriptableRenderPass
{
    private RTHandle source { get; set; }
    private RTHandle[] destination { get; set; }
    public RenderTextureDescriptor destDescriptor;

    string profilerTag = "Split Color";
    Material colorToMrtMaterial;

    public OutputColorsToMRTsRenderPass(Material colorToMrtMaterial)
    {
        renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

        destination = new RTHandle[2];

        destDescriptor = new RenderTextureDescriptor(1920, 1080, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, 0);

        this.colorToMrtMaterial = colorToMrtMaterial;
    }

    // Configure the pass with the source and destination to execute on.
    public void Setup(ref RenderingData renderingData, RTHandle source, RTHandle[] destination)
    {
        this.source = source;
        this.destination[0] = destination[0];
        this.destination[1] = destination[1];
        destDescriptor.width = renderingData.cameraData.cameraTargetDescriptor.width;
        destDescriptor.height = renderingData.cameraData.cameraTargetDescriptor.height;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        cmd.SetGlobalTexture(destination[0].name, destination[0].nameID);
        cmd.SetGlobalTexture(destination[1].name, destination[1].nameID);

        ConfigureTarget(destination);
        //ConfigureClear(m_HasDepthPrepass ? ClearFlag.None : ClearFlag.Depth, Color.black);

        ConfigureClear(ClearFlag.Color, Color.yellow);
    }

    Matrix4x4 scaleMatrix = Matrix4x4.Scale(new Vector3(0.85f, 0.85f, 0.85f));

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

        cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        //  cmd.SetGlobalTexture(shaderPropertyId_srcTex, source);
        cmd.DrawMesh(RenderingUtils.fullscreenMesh, scaleMatrix, colorToMrtMaterial);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
