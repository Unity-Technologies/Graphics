using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class CoreCopyBlitTestFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        Material m_Material;
        int m_Pass;
        public void init(Material material, int pass)
        {
            m_Material = material;
            m_Pass = pass;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.autoGenerateMips = true;
            desc.depthBufferBits = 0;
            var mip = 0;

            if (RenderGraphUtils.CanAddCopyPassMSAA())
            {
                TextureHandle fbFetchDestinationMSAA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "Temp FBFetchMSAA", false);
                renderGraph.AddCopyPass(resourceData.activeColorTexture, fbFetchDestinationMSAA, 0, 0, mip, mip, "Copy MSAA 1");
                renderGraph.AddCopyPass(fbFetchDestinationMSAA, resourceData.activeColorTexture, 0, 0, mip, mip, "Copy MSAA 2");
            }

            desc.msaaSamples = 1;
            TextureHandle blitDestination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "Temp Blit", false);
            TextureHandle fbFetchDestination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "Temp FBFetch", false);

            int sourceTexturePropertyID = Shader.PropertyToID("_BlitTexture");
            RenderGraphUtils.BlitMaterialParameters para = new(resourceData.activeColorTexture, blitDestination, m_Material, m_Pass, null, 0, mip,
                sourceMip: mip,
                sourceTexturePropertyID: sourceTexturePropertyID,
                geometry: RenderGraphUtils.FullScreenGeometryType.ProceduralTriangle);
            renderGraph.AddBlitPass(para, passName: "Blit inverse using Material");
            renderGraph.AddCopyPass(blitDestination, fbFetchDestination, 0, 0, mip, mip, passName: "Copy non MSAA");
            renderGraph.AddBlitPass(fbFetchDestination, resourceData.activeColorTexture, new Vector2(1, 1), new Vector2(0, 0), sourceMip: mip, numSlices: 1, numMips: 1, passName: "Blit back");
        }
    }

    CustomRenderPass m_ScriptablePass;
    public Material material = null;
    public int pass = 0;
    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null) return;
        m_ScriptablePass.init(material, pass);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}
