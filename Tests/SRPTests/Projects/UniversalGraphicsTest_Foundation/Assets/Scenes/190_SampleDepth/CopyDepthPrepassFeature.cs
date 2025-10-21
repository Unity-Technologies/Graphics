using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
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
    Shader m_CopyDepthPS;
    ThreeCopyDepths copyDepthPasses;
    const int k_NumOfMaterials = 3;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!Init())
        {
            Debug.LogWarningFormat("{0}.AddRenderPasses(): Missing materials. {1} render pass will not be added.", GetType().Name, name);
            return;
        }

        renderer.EnqueuePass(copyDepthPasses);
    }

    public override void Create()
    {
        Init();
    }

    private bool Init()
    {
        m_CopyDepthPS = GraphicsSettings.GetRenderPipelineSettings<ForceDepthPrepassFeatureResources>()?.CopyDepthPS;
        if (m_CopyDepthPS == null)
            return false;

        if (copyDepthPasses == null)
        {
            copyDepthPasses = new ThreeCopyDepths(ref m_CopyDepthPS);
            copyDepthPasses.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (copyDepthPasses != null)
        {
            copyDepthPasses.Dispose();
            copyDepthPasses = null;
        }
    }
}

internal class ThreeCopyDepths : ScriptableRenderPass
{
    private CopyDepthPass m_CopyDepthPass1;
    private CopyDepthPass m_CopyDepthPass2;
    private CopyDepthPass m_CopyDepthPass3;
    private RTHandle m_Depth1;
    private RTHandle m_Depth2;

    public ThreeCopyDepths(ref Shader copyDepthShader)
    {
        m_CopyDepthPass1 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, copyDepthShader, copyToDepth: true, customPassName: "First Copy");
        m_CopyDepthPass2 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, copyDepthShader, copyToDepth: true, copyResolvedDepth: true, customPassName: "Second Copy");
        m_CopyDepthPass3 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, copyDepthShader, copyToDepth: true, copyResolvedDepth: true, customPassName: "Third Copy");
    }

    internal void EnqueuePasses(ScriptableRenderer renderer)
    {
        renderer.EnqueuePass(m_CopyDepthPass1);
        renderer.EnqueuePass(m_CopyDepthPass2);
        renderer.EnqueuePass(m_CopyDepthPass3);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        var depthDesc = cameraData.cameraTargetDescriptor;
        depthDesc.graphicsFormat = GraphicsFormat.None;
        depthDesc.depthStencilFormat =  cameraData.cameraTargetDescriptor.depthStencilFormat;
        depthDesc.msaaSamples = 1;

        TextureHandle activeDepth = resourceData.activeDepthTexture;
        TextureHandle copiedDepth1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "CopiedDepth1", false);
        TextureHandle copiedDepth2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "CopiedDepth2", false);

        m_CopyDepthPass1.Render(renderGraph, copiedDepth1, activeDepth, resourceData, cameraData, false);
        m_CopyDepthPass2.Render(renderGraph, copiedDepth2, copiedDepth1, resourceData, cameraData, false);
        m_CopyDepthPass3.Render(renderGraph, activeDepth, copiedDepth2, resourceData, cameraData, false);
    }

    public void Dispose()
    {
        m_Depth1?.Release();
        m_Depth2?.Release();

        m_CopyDepthPass1?.Dispose();
        m_CopyDepthPass2?.Dispose();
        m_CopyDepthPass3?.Dispose();

        m_CopyDepthPass1 = null;
        m_CopyDepthPass2 = null;
        m_CopyDepthPass3 = null;
    }
}
