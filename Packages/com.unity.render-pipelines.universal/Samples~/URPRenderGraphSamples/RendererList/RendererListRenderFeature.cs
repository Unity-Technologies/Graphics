using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.Universal;

// This example clears the current active color texture, then renders the scene geometry associated to the m_LayerMask layer.
// Add scene geometry to your own custom layers and experiment switching the layer mask in the render feature UI.
// You can use the frame debugger to inspect the pass output.
public class RendererListRenderFeature : ScriptableRendererFeature
{
    class RendererListPass : ScriptableRenderPass
    {
        // Layer mask used to filter objects to put in the renderer list
        private LayerMask m_LayerMask;
        
        // List of shader tags used to build the renderer list
        private List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        public RendererListPass(LayerMask layerMask)
        {
            m_LayerMask = layerMask;
        }
        
        // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
        private class PassData
        {
            public RendererListHandle rendererListHandle;
        }

        // Sample utility method that showcases how to create a renderer list via the RenderGraph API
        private void InitRendererLists(ContextContainer frameData, ref PassData passData, RenderGraph renderGraph)
        {
            // Access the relevant frame data from the Universal Render Pipeline
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            
            var sortFlags = cameraData.defaultOpaqueSortFlags;
            RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
            FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, m_LayerMask);
            
            ShaderTagId[] forwardOnlyShaderTagIds = new ShaderTagId[]
            {
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("SRPDefaultUnlit"), // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
                new ShaderTagId("LightweightForward") // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
            };
            
            m_ShaderTagIdList.Clear();
            
            foreach (ShaderTagId sid in forwardOnlyShaderTagIds)
                m_ShaderTagIdList.Add(sid);
            
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, universalRenderingData, cameraData, lightData, sortFlags);

            var param = new RendererListParams(universalRenderingData.cullResults, drawSettings, filterSettings);
            passData.rendererListHandle = renderGraph.CreateRendererList(param);
        }

        // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.green, 1,0);
            
            context.cmd.DrawRendererList(data.rendererListHandle);
        }
        
        // This is where the renderGraph handle can be accessed.
        // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            string passName = "RenderList Render Pass";
            
            // This simple pass clears the current active color texture, then renders the scene geometry associated to the m_LayerMask layer.
            // Add scene geometry to your own custom layers and experiment switching the layer mask in the render feature UI.
            // You can use the frame debugger to inspect the pass output

            // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
                // The active color and depth textures are the main color and depth buffers that the camera renders into
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                
                // Fill up the passData with the data needed by the pass
                InitRendererLists(frameData, ref passData, renderGraph);
                
                // Make sure the renderer list is valid
                //if (!passData.rendererListHandle.IsValid())
                  //  return;
                
                // We declare the RendererList we just created as an input dependency to this pass, via UseRendererList()
                builder.UseRendererList(passData.rendererListHandle);
                
                // Setup as a render target via UseTextureFragment and UseTextureFragmentDepth, which are the equivalent of using the old cmd.SetRenderTarget(color,depth)
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                
                builder.SetRenderFunc<PassData>(ExecutePass);
            }
        }
    }

    RendererListPass m_ScriptablePass;

    public LayerMask m_LayerMask;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new RendererListPass(m_LayerMask);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


