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
        Material m_materialDummyTint;

        int m_Pass;
        public void init(Material material, Material materialTint, Material materialDummyTint, int pass)
        {
            m_Material = material;
            m_materialTint = materialTint;
            m_materialDummyTint = materialDummyTint;

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

            if (textureDesc.msaaSamples != MSAASamples.None && RenderGraphUtils.CanAddCopyPassMSAA(textureDesc))
            {
                textureDesc.name = "Temp FBFetchMSAA";
                TextureHandle fbFetchDestinationMSAA = renderGraph.CreateTexture(textureDesc);

                // Copy to the temp texture
                renderGraph.AddCopyPass(resourceData.activeColorTexture, fbFetchDestinationMSAA, 0, 0, mip, mip, "Copy MSAA 1");

                // Tint the temp texture, this works through alpha blending on the blit material and thus ensures we're not just sampling
                // an auto-resolved surface or something but that the copied pixels are good for "framebuffer operations"
                BlitMaterialParameters param = new BlitMaterialParameters(TextureHandle.nullHandle, fbFetchDestinationMSAA, m_materialTint, 0);
                renderGraph.AddBlitPass(param, passName: "Tint Temp Texture Blit");

                // Copy the whole thing back to the main color texture
                renderGraph.AddCopyPass(fbFetchDestinationMSAA, resourceData.activeColorTexture, 0, 0, mip, mip, "Copy MSAA 2");

                // This blit does nothing, it tints the color buffer by 1,1,1 it just ensures the blit pass below renders correctly
                // I "assume" it ensures vulkan thinks the color buffer has changed and it will correctly pick up the values in the inverse blit pass below.
                // Without this dummy pass it actually picks up the untinted values (i.e. the results of the copy pass above are not visible)
                // My suspicion is that this is because the copy pass goes through the renderpass api and the blit goes through setRT
                // we've seen similar issues before in other code where mixing renderpasses/setRT confused things.
                // When looking at this in renderdoc without this dummy pass you will see:
                // Pass "DrawOpaqueObjects": Resolves to 2d color attachment #x
                // Pass "Copy MSAA 2": Resolves to 2d color attachment #y
                // Pass "Blit inverse using Material": Reads from color attachment #x < so it incorrectly picks an old version X istead of Y (note: this is the resolve target specifically the MSAA samples target seems to be correct?)
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
                {
                    BlitMaterialParameters paramDummy = new BlitMaterialParameters(TextureHandle.nullHandle, resourceData.activeColorTexture, m_materialDummyTint, 0);
                    renderGraph.AddBlitPass(paramDummy, passName: "Dummy Blit");
                }
            }
            else
            {
                // If we can't do MSAA copies that part of the test doesn't apply. But we just tint the buffer here so the ref images are still the same for all cases
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
    private Material materialDummy = null;

    public int pass = 0;
    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();
        materialDummy = new Material(materialTint);
        materialDummy.SetColor("_Color", Color.white);
        materialDummy.SetColor("_EyeOneColor", Color.white);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || materialTint == null) return;
        m_ScriptablePass.init(material, materialTint, materialDummy, pass);
        m_ScriptablePass.requiresIntermediateTexture = true;
        renderer.EnqueuePass(m_ScriptablePass);
    }
}
