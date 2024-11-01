using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

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
        
        public FrameBufferFetchPass( Material fbFetchMaterial)
        {
            m_FBFetchMaterial = fbFetchMaterial;

            //The pass will read the current color texture. That needs to be an intermediate texture. It's not supported to use the BackBuffer as input texture. 
            //By setting this property, URP will automatically create an intermediate texture. This has a performance cost so don't set this if you don't need it.
            //It's good practice to set it here and not from the RenderFeature. This way, the pass is selfcontaining and you can use it to directly enqueue the pass from a monobehaviour without a RenderFeature.
            requiresIntermediateTexture = true;
        }
        
        // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
        private class PassData
        {
            internal TextureHandle src;
            internal Material material;
            internal bool useMSAA;
        }
        
        // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        static void ExecuteFBFetchPass(PassData data, RasterGraphContext context)
        {
            context.cmd.DrawProcedural(Matrix4x4.identity, data.material, data.useMSAA? 1 : 0, MeshTopology.Triangles, 3, 1, null);
        }
        
        private void FBFetchPass(RenderGraph renderGraph, ContextContainer frameData, TextureHandle source, TextureHandle destination, bool useMSAA)
        {
            string passName = "FrameBufferFetchPass";
            
            // This simple pass copies the target of the previous pass to a new texture using a custom material and framebuffer fetch. This sample is for API demonstrative purposes,
            // so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.

            // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                // Fill the pass data
                passData.material = m_FBFetchMaterial;
                passData.useMSAA = useMSAA;

                // We declare the src as input attachment. This is required for Frame buffer fetch. 
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


            // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
            // The active color and depth textures are the main color and depth buffers that the camera renders into
            var resourceData = frameData.Get<UniversalResourceData>();

            // The destination texture is created here, 
            // the texture is created with the same dimensions as the active color texture
            var source = resourceData.activeColorTexture;

            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = "FBFetchDestTexture";
            destinationDesc.clearBuffer = false;

            if (destinationDesc.msaaSamples == MSAASamples.None || RenderGraphUtils.CanAddCopyPassMSAA())
            {
                TextureHandle fbFetchDestination = renderGraph.CreateTexture(destinationDesc);

                FBFetchPass(renderGraph, frameData, source, fbFetchDestination, destinationDesc.msaaSamples != MSAASamples.None);

                //Copy back the FBF output to the camera color to easily see the result in the game view
                //This copy pass also uses FBF under the hood. All the passes should be merged this way and the destination attachment should be memoryless (no load/store of memory).
                renderGraph.AddCopyPass(fbFetchDestination, source, passName: "Copy Back FF Destination (also using FBF)");
            }
            else
            {
                Debug.Log("Can't add the FBF pass and the copy pass due to MSAA");
            }
        }
    }

    FrameBufferFetchPass m_FbFetchPass;
    
    public Material m_FBFetchMaterial;

    /// <inheritdoc/>
    public override void Create()
    {
        m_FbFetchPass = new FrameBufferFetchPass(m_FBFetchMaterial);

        // Configures where the render pass should be injected.
        m_FbFetchPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Early exit if there are no materials.
        if (m_FBFetchMaterial == null)
        {
            Debug.LogWarning( this.name + " material is null and will be skipped.");
            return;
        }

        renderer.EnqueuePass(m_FbFetchPass);
    }
}


