using UnityEngine;
using UnityEngine.Experimental.Rendering;
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

    private Material m_CopyDepthMaterial1;
    private Material m_CopyDepthMaterial2;
    private Material m_CopyDepthMaterial3;
    private CopyDepthPass m_CopyDepthPass1;
    private CopyDepthPass m_CopyDepthPass2;
    private CopyDepthPass m_CopyDepthPass3;
    private RTHandle m_Depth1;
    private RTHandle m_Depth2;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_CopyDepthPass1);
        renderer.EnqueuePass(m_CopyDepthPass2);
        renderer.EnqueuePass(m_CopyDepthPass3);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        var depthDesc = renderingData.cameraData.cameraTargetDescriptor;
        depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
        depthDesc.depthStencilFormat = renderingData.cameraData.cameraTargetDescriptor.depthStencilFormat;
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

    public override void Create()
    {
        m_CopyDepthMaterial1 = CoreUtils.CreateEngineMaterial(m_CopyDepthPS);
        m_CopyDepthMaterial2 = CoreUtils.CreateEngineMaterial(m_CopyDepthPS);
        m_CopyDepthMaterial3 = CoreUtils.CreateEngineMaterial(m_CopyDepthPS);
        m_CopyDepthPass1 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, m_CopyDepthMaterial1, copyToDepth: true);
        m_CopyDepthPass2 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, m_CopyDepthMaterial2, copyToDepth: true, copyResolvedDepth: true);
        m_CopyDepthPass3 = new CopyDepthPass(RenderPassEvent.AfterRenderingOpaques, m_CopyDepthMaterial3, copyToDepth: true, copyResolvedDepth: true);
    }

    protected override void Dispose(bool disposing)
    {
        m_Depth1?.Release();
        m_Depth2?.Release();
        CoreUtils.Destroy(m_CopyDepthMaterial1);
        CoreUtils.Destroy(m_CopyDepthMaterial2);
        CoreUtils.Destroy(m_CopyDepthMaterial3);
    }
}
