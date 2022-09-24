using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

/// <summary>
/// This feature tests the Copy Depth of CopyToDepth flag for y-flip issues
/// It will copy the output of the depth after opaque using the CopyToDepth flag
/// Then before the depth sampling, it will restore the depth without the flag
/// There are three total copies to avoid double y-flipping which would cause a false pass
/// </summary>
internal class ForceDepthPrepassFeature : ScriptableRendererFeature
{
    private ThreeCopyDepths copyDepthPasses;
    [SerializeField]
    [Reload("Shaders/Utils/CopyDepth.shader")]
    private Shader m_CopyDepthPS;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var asset = UniversalRenderPipeline.asset;

        if (asset.enableRenderGraph)
            renderer.EnqueuePass(copyDepthPasses);
        else
            copyDepthPasses.EnqueuePasses(renderer);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        copyDepthPasses.Setup(renderer, renderingData.cameraData.cameraTargetDescriptor);
    }

    public override void Create()
    {
        copyDepthPasses = new ThreeCopyDepths(m_CopyDepthPS);
        copyDepthPasses.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }
}

internal class ThreeCopyDepths : ScriptableRenderPass
{
    private Material m_CopyDepthMaterial1;
    private Material m_CopyDepthMaterial2;
    private Material m_CopyDepthMaterial3;
    private CopyDepthPass m_CopyDepthPass1;
    private CopyDepthPass m_CopyDepthPass2;
    private CopyDepthPass m_CopyDepthPass3;
    private RTHandle m_Depth1;
    private RTHandle m_Depth2;

    public ThreeCopyDepths(Shader shader)
    {
        m_CopyDepthMaterial1 = CoreUtils.CreateEngineMaterial(shader);
        m_CopyDepthMaterial2 = CoreUtils.CreateEngineMaterial(shader);
        m_CopyDepthMaterial3 = CoreUtils.CreateEngineMaterial(shader);
        m_CopyDepthPass1 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, m_CopyDepthMaterial1, copyToDepth: true);
        m_CopyDepthPass2 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, m_CopyDepthMaterial2, copyToDepth: true, copyResolvedDepth: true);
        m_CopyDepthPass3 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, m_CopyDepthMaterial3, copyToDepth: true, copyResolvedDepth: true);
    }

    public void Setup(ScriptableRenderer renderer, RenderTextureDescriptor cameraTextureDescriptor)
    {
        var depthDesc = cameraTextureDescriptor;
        depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
        depthDesc.depthStencilFormat = cameraTextureDescriptor.depthStencilFormat;
        depthDesc.msaaSamples = 1;
        RenderingUtils.ReAllocateIfNeeded(ref m_Depth1, depthDesc, name: "CopiedDepth1");
        RenderingUtils.ReAllocateIfNeeded(ref m_Depth2, depthDesc, name: "CopiedDepth2");
        m_CopyDepthPass1.Setup(
            renderer.cameraDepthTargetHandle,
            m_Depth1
        );
        m_CopyDepthPass2.Setup(
            m_Depth1,
            m_Depth2
        );
        m_CopyDepthPass3.Setup(
            m_Depth2,
            renderer.cameraDepthTargetHandle
        );
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
    }

    internal void EnqueuePasses(ScriptableRenderer renderer)
    {
        renderer.EnqueuePass(m_CopyDepthPass1);
        renderer.EnqueuePass(m_CopyDepthPass2);
        renderer.EnqueuePass(m_CopyDepthPass3);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ref RenderingData renderingData)
    {
        var depthDesc = renderingData.cameraData.cameraTargetDescriptor;
        depthDesc.graphicsFormat = GraphicsFormat.None;
        depthDesc.depthStencilFormat =  renderingData.cameraData.cameraTargetDescriptor.depthStencilFormat;
        depthDesc.msaaSamples = 1;

        var copiedDepth1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "CopiedDepth1", false);
        var copiedDepth2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "CopiedDepth2", false);

        m_CopyDepthPass1.Render(renderGraph, ref copiedDepth1, UniversalRenderer.m_ActiveRenderGraphDepth, ref renderingData, "First Copy");
        m_CopyDepthPass1.Render(renderGraph, ref copiedDepth2, copiedDepth1, ref renderingData, "Second Copy");
        m_CopyDepthPass1.Render(renderGraph, ref UniversalRenderer.m_ActiveRenderGraphDepth, copiedDepth2,  ref renderingData, "Third Copy");
    }
}
