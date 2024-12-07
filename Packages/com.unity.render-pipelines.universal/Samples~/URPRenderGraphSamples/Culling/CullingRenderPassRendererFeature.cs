using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class CullingRenderPassRendererFeature : ScriptableRendererFeature
{
    // Layer mask used to filter objects to put in the renderer list.
    public LayerMask m_LayerMask;

    private CullingRenderPass m_CullingRenderPass;

    public override void Create()
    {
        m_CullingRenderPass = new CullingRenderPass(m_LayerMask);

        // Configures where the render pass should be injected.
        m_CullingRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_CullingRenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        m_CullingRenderPass = null;
    }
}

public class CullingRenderPass : ScriptableRenderPass
{
    private LayerMask m_LayerMask;

    // List of shader tags used to build the renderer list.
    private List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

    public CullingRenderPass(LayerMask layerMask)
    {
        m_LayerMask = layerMask;
    }

    class PassData
    {
        public RendererListHandle rendererListHandle;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var passName = "RenderList Render Pass from Culling";

        // This simple pass clears the current active color texture, then renders the scene geometry associated to the m_LayerMask layer using the culling results.
        // Add scene geometry to your own custom layers and experiment switching the layer mask in the render feature UI.
        // You can use the frame debugger to inspect the pass output.
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
        {
            // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures.
            // The active color and depth textures are the main color and depth buffers that the camera renders into.
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var cameraData = frameData.Get<UniversalCameraData>();

            // CullContextData contains the culling APIs. 
            var cullContextData = frameData.Get<CullContextData>();

            // Retrieve the culling parameters for the camera used.
            cameraData.camera.TryGetCullingParameters(false, out var cullingParameters);

            // Perform culling using the CullContextData API.
            var cullingResults = cullContextData.Cull(ref cullingParameters);

            // Fill up the passData with the data needed by the pass
            InitRendererLists(cullingResults, frameData, ref passData, renderGraph);

            // Make sure the renderer list is valid
            if (!passData.rendererListHandle.IsValid())
                  return;

            // We declare the RendererList we just created as an input dependency to this pass, via UseRendererList().
            builder.UseRendererList(passData.rendererListHandle);

            // Setup as a render target via UseTextureFragment and UseTextureFragmentDepth, which are the equivalent of using the old cmd.SetRenderTarget(color,depth).
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

            // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass.
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
        }
    }

    // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass.
    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.green, 1, 0);

        context.cmd.DrawRendererList(data.rendererListHandle);
    }

    // Sample utility method that showcases how to create a renderer list via the RenderGraph API.
    private void InitRendererLists(CullingResults cullResults, ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
    {
        // Access the relevant frame data from the Universal Render Pipeline
        var universalRenderingData = frameData.Get<UniversalRenderingData>();
        var cameraData = frameData.Get<UniversalCameraData>();
        var lightData = frameData.Get<UniversalLightData>();

        var sortFlags = cameraData.defaultOpaqueSortFlags;
        var renderQueueRange = RenderQueueRange.opaque;
        var filterSettings = new FilteringSettings(renderQueueRange, m_LayerMask);

        var forwardOnlyShaderTagIds = new ShaderTagId[]
        {
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("SRPDefaultUnlit"), // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility.
                new ShaderTagId("LightweightForward") // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility.
        };

        m_ShaderTagIdList.Clear();

        foreach (ShaderTagId sid in forwardOnlyShaderTagIds)
            m_ShaderTagIdList.Add(sid);

        var drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, universalRenderingData, cameraData, lightData, sortFlags);

        var param = new RendererListParams(cullResults, drawSettings, filterSettings);
        passData.rendererListHandle = renderGraph.CreateRendererList(param);
    }
}
