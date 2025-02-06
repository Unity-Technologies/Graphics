using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Experimental.Rendering;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;
using System;

public class CoreCopyBlitTestFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        Material m_Material;
        Material m_materialTint;

        int m_Pass;
        public void init(Material material, Material materialTint, int pass)
        {
            m_Material = material;
            m_materialTint = materialTint;
            m_Pass = pass;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            var textureDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
            textureDesc.autoGenerateMips = true;
            textureDesc.depthBufferBits = 0;

            var mip = 0;

            if (textureDesc.msaaSamples != MSAASamples.None && RenderGraphUtils.CanAddCopyPassMSAA())
            {
                textureDesc.name = "Temp FBFetchMSAA";
                TextureHandle fbFetchDestinationMSAA = renderGraph.CreateTexture(textureDesc);

                // Copy to the temp texture
                renderGraph.AddCopyPass(resourceData.activeColorTexture, fbFetchDestinationMSAA, 0, 0, mip, mip, "Copy MSAA 1");

                // Tint the temp texture, this works through alpha blending on the blit material and thus ensures we're not just sampling
                // an auto-resolved surface or something but that the copied pixels are good for "framebuffer operations"
                BlitMaterialParameters param = new BlitMaterialParameters(TextureHandle.nullHandle, fbFetchDestinationMSAA, m_materialTint, 0);
                renderGraph.AddBlitPass(param);

                // Copy the whole thing back to the main color texture
                renderGraph.AddCopyPass(fbFetchDestinationMSAA, resourceData.activeColorTexture, 0, 0, mip, mip, "Copy MSAA 2");
            }
            else
            {
                BlitMaterialParameters param = new BlitMaterialParameters(TextureHandle.nullHandle, resourceData.activeColorTexture, m_materialTint, 0);
                renderGraph.AddBlitPass(param);
            }

            // Test Non MSAA copy pass. We force MSAA off in the desc.
            textureDesc.msaaSamples = MSAASamples.None;
            textureDesc.name = "Temp Blit";
            TextureHandle blitDestination = renderGraph.CreateTexture(textureDesc);
            textureDesc.name = "Temp FBFetch";
            TextureHandle fbFetchDestination = renderGraph.CreateTexture(textureDesc);

            // Blit from activeColorTexture (possibly multisampled) to a temp texture, guaranteed single sampled
            // This also tests the source texture setup of blit. Material is also a shadergraph material to ensure it works.
            int sourceTexturePropertyID = Shader.PropertyToID("_BlitTexture");
            RenderGraphUtils.BlitMaterialParameters para = new(resourceData.activeColorTexture, blitDestination, m_Material, m_Pass, null, 0, mip,
                sourceMip: mip,
                sourceTexturePropertyID: sourceTexturePropertyID,
                geometry: RenderGraphUtils.FullScreenGeometryType.ProceduralTriangle);
            para.numSlices = -1;
            renderGraph.AddBlitPass(para, passName: "Blit inverse using Material");

            // Copy to another temp texture, this tests the non-msaa copy pass
            renderGraph.AddCopyPass(blitDestination, fbFetchDestination, 0, 0, mip, mip, passName: "Copy non MSAA");

            // Blit back to the main view this is just so the image comparison picks up the results
            renderGraph.AddBlitPass(fbFetchDestination, resourceData.activeColorTexture, new Vector2(1, 1), new Vector2(0, 0), passName: "Blit back");

        }
    }

    CustomRenderPass m_ScriptablePass;
    public Material material = null;
    public Material materialTint = null;
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
        if (material == null || materialTint == null) return;
        m_ScriptablePass.init(material, materialTint, pass);
        m_ScriptablePass.requiresIntermediateTexture = true;
        renderer.EnqueuePass(m_ScriptablePass);
    }
}
