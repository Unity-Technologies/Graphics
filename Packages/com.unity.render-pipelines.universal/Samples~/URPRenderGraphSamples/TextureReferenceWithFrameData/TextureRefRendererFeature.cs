using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule.Util;

// In this example we will create a texture reference ContextItem in frameData to hold a reference
// used by furture passes. This is usefull to avoid additional blit operations copying back and forth
// to the cameras color attachment. Instead of copying it back after the blit operation we can instead
// update the reference to the blit destination and use that for future passes.
// The is the prefered way to share resources between passes. Previously, it was common to use global textures for this.
// However, it's better to avoid using global textures where you can.
public class TextureRefRendererFeature : ScriptableRendererFeature
{
    // The ContextItem used to store the texture reference at.
    public class TexRefData : ContextItem
    {
        // The texture reference variable.
        public TextureHandle texture = TextureHandle.nullHandle;

        // Reset function required by ContextItem. It should reset all variables not carried
        // over to next frame.
        public override void Reset()
        {
            // We should always reset texture handles since they are only vaild for the current frame.
            texture = TextureHandle.nullHandle;
        }
    }

    // This pass updates the reference when making a blit operation using a material and the camera's color attachment.
    class UpdateRefPass : ScriptableRenderPass
    {
        // The data we want to transfer to the render function after recording.
        class PassData
        {
            // For the blit operation we will need the source and destination of the color attachments.
            public TextureHandle source;
            public TextureHandle destination;
            // We will also need a material to transform the color attachment when making a blit operation.
            public Material material;
        }

        // Scale bias is used to blit from source to distination given a 2d scale in the x and y parameters
        // and an offset in the z and w parameters.
        static Vector4 scaleBias = new Vector4(1f, 1f, 0f, 0f);

        // Material used in the blit operation.
        Material[] m_DisplayMaterials;

        // Function used to transfer the material from the renderer feature to the render pass.
        public void Setup(Material[] materials)
        {
            m_DisplayMaterials = materials;
        }

        // This function blits the whole screen for a given material.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            foreach (var mat in m_DisplayMaterials)
            {
                // Skip material if it is null.
                if (mat == null)
                {
                    Debug.LogWarning($"Skipping render pass for unassigned material.");
                    continue;
                }


                // Starts the recording of the render graph pass given the name of the pass
                // and outputting the data used to pass data to the execution of the render function.
                using (var builder = renderGraph.AddRasterRenderPass<PassData>($"UpdateRefPass_{mat.name}", out var passData))
                {
                    var texRefExist = frameData.Contains<TexRefData>();
                    var texRef = frameData.GetOrCreate<TexRefData>();

                    // First time running this pass. Fetch ref from active color buffer.
                    if (!texRefExist)
                    {
                        var resourceData = frameData.Get<UniversalResourceData>();
                        // For this first occurence we would like 
                        texRef.texture = resourceData.activeColorTexture;
                    }

                    // Fill in the pass data using by the render function.

                    // Use the old reference from TexRefData.
                    passData.source = texRef.texture;

                    var descriptor = passData.source.GetDescriptor(renderGraph);
                    // We disable MSAA for the blit operations.
                    descriptor.msaaSamples = MSAASamples.None;
                    descriptor.name = $"BlitMaterialRefTex_{mat.name}";
                    descriptor.clearBuffer = false;

                    // Create a new temporary texture to keep the blit result.
                    passData.destination = renderGraph.CreateTexture(descriptor);

                    // Material used in the blit operation.
                    passData.material = mat;

                    // Update the texture reference to the blit destination.
                    texRef.texture = passData.destination;

                    // Sets input attachment.
                    builder.UseTexture(passData.source);
                    // Sets color attachment 0.
                    builder.SetRenderAttachment(passData.destination, 0);

                    // Sets the render function.
                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
                }
            }
        }

        // ExecutePass is the render function for each of the blit render graph recordings.
        // This is good practice to avoid using variables outside of the lambda it is called from.
        // It is static to avoid using member variables which could cause unintended behaviour.
        static void ExecutePass(PassData data, RasterGraphContext rgContext)
        {
            Blitter.BlitTexture(rgContext.cmd, data.source, scaleBias, data.material, 0);
        }
    }

    // After updating the reference we will need to use the result copying it back to camera's
    // color attachment.
    class CopyBackRefPass : ScriptableRenderPass
    {
        // This function blits the reference back to the camera's color attachment.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Early exit if TexRefData doesn't exist within frameData since where is nothing to copy back.
            if (!frameData.Contains<TexRefData>()) return;

            // Fetch UniversalResourceData to retrive the camera's active color texture.
            var resourceData = frameData.Get<UniversalResourceData>();
            // Fetch TexRefData to retrive the texture reference.
            var texRef = frameData.Get<TexRefData>();
            
            renderGraph.AddBlitPass(texRef.texture, resourceData.activeColorTexture, Vector2.one, Vector2.zero, passName: "Blit Back Pass");
        }
    }

    [Tooltip("The material used when making the blit operation.")]
    public Material[] displayMaterials = new Material[1];

    UpdateRefPass m_UpdateRefPass;
    CopyBackRefPass m_CopyBackRefPass;

    // Here you can create passes and do the initialization of them. This is called everytime serialization happens.
    public override void Create()
    {
        m_UpdateRefPass = new UpdateRefPass();
        m_CopyBackRefPass = new CopyBackRefPass();

        // Configures where the render pass should be injected.
        m_UpdateRefPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        m_CopyBackRefPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Since they have the same RenderPassEvent the order matters when enqueueing them.

        // Early exit if there are no materials.
        if (displayMaterials == null)
        {
            Debug.LogWarning("TexterRefRendererFeature materials is null and will be skipped.");
            return;
        }

        m_UpdateRefPass.Setup(displayMaterials);
        renderer.EnqueuePass(m_UpdateRefPass);
        renderer.EnqueuePass(m_CopyBackRefPass);
    }
}


