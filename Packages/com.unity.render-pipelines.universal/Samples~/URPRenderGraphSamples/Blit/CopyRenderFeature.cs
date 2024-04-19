using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// This example copies the active color texture to a new texture. This example is for API demonstrative purposes,
// so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.
public class CopyRenderFeature : ScriptableRendererFeature
{
    class CopyRenderPass : ScriptableRenderPass
    {
        string m_PassName = "Copy To or From Temp Texture";

        public CopyRenderPass()
        {
            //The pass will read the current color texture. That needs to be an intermediate texture. It's not supported to use the BackBuffer as input texture. 
            //By setting this property, URP will automatically create an intermediate texture. 
            //It's good practice to set it here and not from the RenderFeature. This way, the pass is selfcontaining and you can use it to directly enqueue the pass from a monobehaviour without a RenderFeature.
            requiresIntermediateTexture = true;
        }

        // This is where the renderGraph handle can be accessed.
        // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
            // The active color and depth textures are the main color and depth buffers that the camera renders into
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            // The destination texture is created here, 
            // the texture is created with the same dimensions as the active color texture
            var source = resourceData.activeColorTexture;

            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = $"CameraColor-{m_PassName}";
            destinationDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destinationDesc);  
           
            if (RenderGraphUtils.CanAddCopyPassMSAA())
            {
                // This simple pass copies the active color texture to a new texture. 
                renderGraph.AddCopyPass(resourceData.activeColorTexture, destination, passName: m_PassName);

                //Need to copy back otherwise the pass gets culled since the result of the previous copy is not read. This is just for demonstration purposes.
                renderGraph.AddCopyPass(destination, resourceData.activeColorTexture, passName: m_PassName);
            }
            else
            {
                Debug.Log("Can't add the copy pass due to MSAA");
            }
        }
    }

    CopyRenderPass m_CopyRenderPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_CopyRenderPass = new CopyRenderPass();

        // Configures where the render pass should be injected.
        m_CopyRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_CopyRenderPass);
    }
}


