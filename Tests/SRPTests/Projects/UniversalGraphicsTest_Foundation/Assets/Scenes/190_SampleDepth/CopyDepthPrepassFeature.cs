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
    [SerializeField]
    [Reload("Shaders/Utils/CopyDepth.shader")]
    private Shader m_CopyDepthPS;
    private Material[] m_CopyDepthMaterials;
    private ThreeCopyDepths copyDepthPasses;
    private const int k_NumOfMaterials = 3;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!Init())
        {
            Debug.LogWarningFormat("{0}.AddRenderPasses(): Missing materials. {1} render pass will not be added.", GetType().Name, name);
            return;
        }

        if (UniversalRenderPipeline.asset.enableRenderGraph)
            renderer.EnqueuePass(copyDepthPasses);
        else
            copyDepthPasses.EnqueuePasses(renderer);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        copyDepthPasses.SetupForNonRGPath(renderer, renderingData.cameraData.cameraTargetDescriptor);
    }

    public override void Create()
    {
        Init();
    }

    private bool Init()
    {
        if (!GetMaterials())
            return false;

        if (copyDepthPasses == null)
        {
            copyDepthPasses = new ThreeCopyDepths(ref m_CopyDepthMaterials);
            copyDepthPasses.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        return true;
    }

    protected override void Dispose(bool disposing)
    {
        DestroyMaterials();
        copyDepthPasses.Dispose();
        copyDepthPasses = null;
    }

    private void DestroyMaterials()
    {
        for (int i = 0; i < m_CopyDepthMaterials.Length; i++)
            CoreUtils.Destroy(m_CopyDepthMaterials[i]);

        m_CopyDepthMaterials = null;
    }

    private bool GetMaterials()
    {
        if (m_CopyDepthPS == null)
            return false;

        if (m_CopyDepthMaterials != null && m_CopyDepthMaterials.Length != k_NumOfMaterials)
            DestroyMaterials();

        if (m_CopyDepthMaterials == null)
            m_CopyDepthMaterials = new Material[k_NumOfMaterials];

        bool allMaterialsAreReady = true;
        for (int i = 0; i < k_NumOfMaterials; i++)
        {
            if (m_CopyDepthMaterials[i] == null)
                m_CopyDepthMaterials[i] = CoreUtils.CreateEngineMaterial(m_CopyDepthPS);

            allMaterialsAreReady &= m_CopyDepthMaterials[i] != null;
        }

        return allMaterialsAreReady;
    }
}

internal class ThreeCopyDepths : ScriptableRenderPass
{
    private CopyDepthPass m_CopyDepthPass1;
    private CopyDepthPass m_CopyDepthPass2;
    private CopyDepthPass m_CopyDepthPass3;
    private RTHandle m_Depth1;
    private RTHandle m_Depth2;

    public ThreeCopyDepths(ref Material[] copyDepthMaterials)
    {
        m_CopyDepthPass1 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, copyDepthMaterials[0], copyToDepth: true);
        m_CopyDepthPass2 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, copyDepthMaterials[1], copyToDepth: true, copyResolvedDepth: true);
        m_CopyDepthPass3 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, copyDepthMaterials[2], copyToDepth: true, copyResolvedDepth: true);
    }

    public void SetupForNonRGPath(ScriptableRenderer renderer, RenderTextureDescriptor cameraTextureDescriptor)
    {
        var depthDesc = cameraTextureDescriptor;
        depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
        depthDesc.depthStencilFormat = cameraTextureDescriptor.depthStencilFormat;
        depthDesc.msaaSamples = 1;
        RenderingUtils.ReAllocateIfNeeded(ref m_Depth1, depthDesc, name: "CopiedDepth1");
        RenderingUtils.ReAllocateIfNeeded(ref m_Depth2, depthDesc, name: "CopiedDepth2");

        m_CopyDepthPass1.Setup(renderer.cameraDepthTargetHandle, m_Depth1);
        m_CopyDepthPass2.Setup(m_Depth1, m_Depth2);
        m_CopyDepthPass3.Setup(m_Depth2, renderer.cameraDepthTargetHandle);
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

    public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
    {
        UniversalRenderer renderer = (UniversalRenderer) renderingData.cameraData.renderer;
        var depthDesc = renderingData.cameraData.cameraTargetDescriptor;
        depthDesc.graphicsFormat = GraphicsFormat.None;
        depthDesc.depthStencilFormat =  renderingData.cameraData.cameraTargetDescriptor.depthStencilFormat;
        depthDesc.msaaSamples = 1;

        TextureHandle activeDepth = renderer.activeDepthTexture;
        TextureHandle copiedDepth1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "CopiedDepth1", false);
        TextureHandle copiedDepth2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "CopiedDepth2", false);

        m_CopyDepthPass1.Render(renderGraph, copiedDepth1, activeDepth, ref renderingData, "First Copy");
        m_CopyDepthPass1.Render(renderGraph, copiedDepth2, copiedDepth1, ref renderingData, "Second Copy");
        m_CopyDepthPass1.Render(renderGraph, activeDepth, copiedDepth2,  ref renderingData, "Third Copy");
    }

    public void Dispose()
    {
        m_Depth1?.Release();
        m_Depth2?.Release();
        m_CopyDepthPass1 = null;
        m_CopyDepthPass2 = null;
        m_CopyDepthPass3 = null;
    }
}
