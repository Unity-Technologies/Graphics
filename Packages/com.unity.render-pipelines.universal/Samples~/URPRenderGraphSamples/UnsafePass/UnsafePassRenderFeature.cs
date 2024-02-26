using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// This example copies the active color texture to a new texture, it then downsamples the source texture twice. This example is for API demonstrative purposes,
// so the new textures are not used anywhere else in the frame, you can use the frame debugger to verify their contents.
// The key concept of this example, is the UnsafePass usage: these type of passes are unsafe and allow using command like SetRenderTarget() which are
// not compatible with RasterRenderPasses. Using UnsafePasses means that the RenderGraph won't try to optimize the pass by merging it inside a NativeRenderPass.
// In some cases using UnsafePasses makes sense, if for example we know that a set of adjacent passes are not mergeable, so this can optimize the RenderGraph
// compiling times, on top of simplifying the multiple passes setup.
public class UnsafePassRenderFeature : ScriptableRendererFeature
{
    class UnsafePass : ScriptableRenderPass
    {
        // This class stores the data needed by the pass, passed as parameter to the delegate function that executes the pass
        private class PassData
        {
            internal TextureHandle src;
            internal TextureHandle dest;
            internal TextureHandle destHalf;
            internal TextureHandle destQuarter;
        }

        // This static method is used to execute the pass and passed as the RenderFunc delegate to the RenderGraph render pass
        static void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            // Set manually the RenderTarget for each blit. Each SetRenderTarget call would require a separate RasterCommandPass if we wanted
            // to setup RenderGraph for merging passes when possible.
            // In this case we know that these 3 subpasses are not compatible for merging, because RenderTargets have different dimensions, 
            // so we simplify our code to use an unsafe pass, also saving RenderGraph processing time.
            
            // copy the current scene color

            CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            
            context.cmd.SetRenderTarget(data.dest);
            Blitter.BlitTexture(unsafeCmd, data.src, new Vector4(1, 1, 0, 0), 0, false);
            
            // downscale x2
            
            context.cmd.SetRenderTarget(data.destHalf);
            Blitter.BlitTexture(unsafeCmd, data.dest, new Vector4(1, 1, 0, 0), 0, false);
            
            context.cmd.SetRenderTarget(data.destQuarter);
            Blitter.BlitTexture(unsafeCmd, data.destHalf, new Vector4(1, 1, 0, 0), 0, false);
            
            // upscale x2
            
            context.cmd.SetRenderTarget(data.destHalf);
            Blitter.BlitTexture(unsafeCmd, data.destQuarter, new Vector4(1, 1, 0, 0), 0, false);
            
            context.cmd.SetRenderTarget(data.dest);
            Blitter.BlitTexture(unsafeCmd, data.destHalf, new Vector4(1, 1, 0, 0), 0, false);
        }

        // This is where the renderGraph handle can be accessed.
        // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            string passName = "Unsafe Pass";

            // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
            using (var builder = renderGraph.AddUnsafePass<PassData>(passName, out var passData))
            {
                // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
                // The active color and depth textures are the main color and depth buffers that the camera renders into
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // Fill up the passData with the data needed by the pass

                // Get the active color texture through the frame data, and set it as the source texture for the blit
                passData.src = resourceData.activeColorTexture;

                // The destination textures are created here, 
                // the texture is created with the same dimensions as the active color texture, but with no depth buffer, being a copy of the color texture
                // we also disable MSAA as we don't need multisampled textures for this sample
                // the other two textures halve the resolution of the previous one

                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;

                TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "UnsafeTexture", false);
                desc.width /= 2;
                desc.height /= 2;
                TextureHandle destinationHalf = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "UnsafeTexture2", false);
                desc.width /= 2;
                desc.height /= 2;
                TextureHandle destinationQuarter = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "UnsafeTexture3", false);
                passData.dest = destination;
                passData.destHalf = destinationHalf;
                passData.destQuarter = destinationQuarter;

                // We declare the src texture as an input dependency to this pass, via UseTexture()
                builder.UseTexture(passData.src);
                
                // UnsafePasses don't setup the outputs using UseTextureFragment/UseTextureFragmentDepth, you should specify your writes with UseTexture instead
                builder.UseTexture(passData.dest, AccessFlags.Write);
                builder.UseTexture(passData.destHalf, AccessFlags.Write);
                builder.UseTexture(passData.destQuarter, AccessFlags.Write);

                // We disable culling for this pass for the demonstrative purpose of this sample, as normally this pass would be culled,
                // since the destination texture is not used anywhere else
                builder.AllowPassCulling(false);

                // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }
    }

    UnsafePass m_UnsafePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_UnsafePass = new UnsafePass();

        // Configures where the render pass should be injected.
        m_UnsafePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_UnsafePass);
    }
}


