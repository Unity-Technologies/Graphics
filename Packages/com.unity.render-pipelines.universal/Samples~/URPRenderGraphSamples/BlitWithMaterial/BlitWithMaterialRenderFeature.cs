using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

// This example copies the active color texture to a new texture using a custom material. This example is for API demonstrative purposes,
// so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.
public class BlitWithMaterialRenderFeature : ScriptableRendererFeature
{
    class BlitWithMaterialPass : ScriptableRenderPass
    {
        private Material m_BlitMaterial;
        
        public BlitWithMaterialPass(Material blitMaterial)
        {
            m_BlitMaterial = blitMaterial;
        }
        
        // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
        private class PassData
        {
            internal TextureHandle src;
            internal TextureHandle dst;
            internal Material blitMaterial;
        }

        // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.blitMaterial, 0);
        }

        private void InitPassData(RenderGraph renderGraph, ContextContainer frameData, ref PassData passData)
        {
            // Fill up the passData with the data needed by the passes
            
            // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
            // The active color and depth textures are the main color and depth buffers that the camera renders into
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            
            // The destination texture is created here, 
            // the texture is created with the same dimensions as the active color texture, but with no depth buffer, being a copy of the color texture
            // we also disable MSAA as we don't need multisampled textures for this sample
                
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
                
            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "BlitMaterialTexture", false);
            
            passData.src = resourceData.activeColorTexture;
            passData.dst = destination;
            passData.blitMaterial = m_BlitMaterial;
        }
        
        // This is where the renderGraph handle can be accessed.
        // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            string passName = "Blit With Material";
            
            // This simple pass copies the active color texture to a new texture using a custom material. This sample is for API demonstrative purposes,
            // so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.

            // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                // Initialize the pass data
                InitPassData(renderGraph, frameData, ref passData);

                // We declare the src texture as an input dependency to this pass, via UseTexture()
                builder.UseTexture(passData.src);

                // Setup as a render target via UseTextureFragment, which is the equivalent of using the old cmd.SetRenderTarget
                builder.SetRenderAttachment(passData.dst, 0);
                
                // We disable culling for this pass for the demonstrative purpose of this sample, as normally this pass would be culled,
                // since the destination texture is not used anywhere else
                builder.AllowPassCulling(false);

                // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }

    BlitWithMaterialPass m_BlitWithMaterialPass;
    
    public Material m_BlitColorMaterial;

    /// <inheritdoc/>
    public override void Create()
    {
        m_BlitWithMaterialPass = new BlitWithMaterialPass(m_BlitColorMaterial);

        // Configures where the render pass should be injected.
        m_BlitWithMaterialPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_BlitWithMaterialPass);
    }
}


