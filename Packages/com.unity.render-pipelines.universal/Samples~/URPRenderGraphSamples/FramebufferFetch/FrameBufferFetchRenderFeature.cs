using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

// This example copies the target of the previous pass to a new texture using a custom material and framebuffer fetch. This example is for API demonstrative purposes,
// so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.

// Framebuffer fetch: this is an advanced TBDR GPU optimization that allows subpasses to read the output of previous subpasses directly from the framebuffer,
// greatly reducing the bandwidth usage.
public class FrameBufferFetchRenderFeature : ScriptableRendererFeature
{
    class FrameBufferFetchPass : ScriptableRenderPass
    {
        private Material m_BlitMaterial;
        private Material m_FBFetchMaterial;
        
        public FrameBufferFetchPass(Material blitMaterial, Material fbFetchMaterial)
        {
            m_BlitMaterial = blitMaterial;
            m_FBFetchMaterial = fbFetchMaterial;
        }
        
        // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
        private class PassData
        {
            internal TextureHandle src;
            internal Material material;
        }

        // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        static void ExecuteBlitPass(PassData data, RasterGraphContext context)
        {
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, 0);
        }
        
        // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        static void ExecuteFBFetchPass(PassData data, RasterGraphContext context)
        {
            context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 1, MeshTopology.Triangles, 3, 1, null);
            
            // other ways to draw a fullscreen triangle/quad:
            //CoreUtils.DrawFullScreen(context.cmd, data.material, null, 1);
            //Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 1);
        }

        private void BlitPass(RenderGraph renderGraph, ContextContainer frameData, TextureHandle destination)
        {
            string passName = "InitialBlitPass";
            
            // This simple pass copies the active color texture to a new texture using a custom material. This sample is for API demonstrative purposes,
            // so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.

            // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
                // The active color and depth textures are the main color and depth buffers that the camera renders into
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                
                // Get the active color texture through the frame data, and set it as the source texture for the blit
                passData.src = resourceData.activeColorTexture;
                passData.material = m_BlitMaterial;
                
                // We declare the src texture as an input dependency to this pass, via UseTexture()
                builder.UseTexture(passData.src);

                // Setup as a render target via UseTextureFragment, which is the equivalent of using the old cmd.SetRenderTarget
                builder.SetRenderAttachment(destination, 0);
                
                // We disable culling for this pass for the demonstrative purpose of this sample, as normally this pass would be culled,
                // since the destination texture is not used anywhere else
                builder.AllowPassCulling(false);

                // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecuteBlitPass(data, context));
            }
        }
        
        private void FBFetchPass(RenderGraph renderGraph, ContextContainer frameData, TextureHandle source, TextureHandle destination)
        {
            string passName = "FrameBufferFetchPass";
            
            // This simple pass copies the target of the previous pass to a new texture using a custom material and framebuffer fetch. This sample is for API demonstrative purposes,
            // so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.

            // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                // Fill the pass data
                passData.material = m_FBFetchMaterial;
                
                // We declare the src texture as an input dependency to this pass, via UseTexture()
                //builder.UseTexture(passData.blitDest);
                builder.SetInputAttachment(source, 0, AccessFlags.Read);

                // Setup as a render target via UseTextureFragment, which is the equivalent of using the old cmd.SetRenderTarget
                builder.SetRenderAttachment(destination, 0);
                
                // We disable culling for this pass for the demonstrative purpose of this sample, as normally this pass would be culled,
                // since the destination texture is not used anywhere else
                builder.AllowPassCulling(false);

                // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecuteFBFetchPass(data, context));
            }
        }
        
        // This is where the renderGraph handle can be accessed.
        // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // This pass showcases how to implement framebuffer fetch: this is an advanced TBDR GPU optimization
            // that allows subpasses to read the output of previous subpasses directly from the framebuffer, reducing greatly the bandwidth usage.
            // The first pass BlitPass simply copies the Camera Color in a temporary render target, the second pass FBFetchPass copies the temporary render target
            // to another render target using framebuffer fetch.
            // As a result, the passes are merged (you can verify in the RenderGraph Visualizer) and the bandwidth usage is reduced, since we can discard the temporary render target.

            // The destination textures are created here, 
            // the texture is created with the same dimensions as the active color texture, but with no depth buffer, being a copy of the color texture
            // we also disable MSAA as we don't need multisampled textures for this sample.
                
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
                
            TextureHandle blitDestination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "BlitDestTexture", false);
            TextureHandle fbFetchDestination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "FBFetchDestTextureTexture", false);
            
            BlitPass(renderGraph, frameData, blitDestination);
            
            FBFetchPass(renderGraph, frameData, blitDestination, fbFetchDestination);
        }
    }

    FrameBufferFetchPass m_FbFetchPass;
    
    public Material m_BlitColorMaterial;
    public Material m_FBFetchMaterial;

    /// <inheritdoc/>
    public override void Create()
    {
        m_FbFetchPass = new FrameBufferFetchPass(m_BlitColorMaterial, m_FBFetchMaterial);

        // Configures where the render pass should be injected.
        m_FbFetchPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_FbFetchPass);
    }
}


