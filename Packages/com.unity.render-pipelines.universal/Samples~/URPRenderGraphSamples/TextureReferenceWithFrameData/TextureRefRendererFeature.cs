using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule.Util;

// In this example we will create a texture reference ContextItem in frameData to hold a reference
// used by future passes. This is useful to avoid additional blit operations copying back and forth
// to the camera's color attachment. Instead of copying it back after the blit operation we can instead
// update the reference to the blit destination and use that for future passes.
// The frameData is the preferred way to share resources between passes. Previously, it was common to use global textures for this.
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
            // We should always reset texture handles since they are only valid for the current frame.
            texture = TextureHandle.nullHandle;
        }
    }

    // This pass updates the reference when making a blit operation using a material and the camera's color attachment.
    class UpdateRefPass : ScriptableRenderPass
    {
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
                
                var texRefExist = frameData.Contains<TexRefData>();
                var texRef = frameData.GetOrCreate<TexRefData>();

                // First time running this pass. Fetch ref from active color buffer.
                if (!texRefExist)
                {
                    var resourceData = frameData.Get<UniversalResourceData>();
                    // For this first occurrence we would like:
                    texRef.texture = resourceData.activeColorTexture;
                }
                
                // Create the destination texture for the pass.
                var descriptor = texRef.texture.GetDescriptor(renderGraph);
                // We disable MSAA for the blit operations.
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.name = $"BlitMaterialRefTex_{mat.name}";
                descriptor.clearBuffer = false;

                // Create a new temporary texture to keep the blit result.
                var destination = renderGraph.CreateTexture(descriptor);
                
                // Fill in the pass data used by the render function.
                var blitPassParams = new RenderGraphUtils.BlitMaterialParameters(texRef.texture, destination, mat, 0);
                renderGraph.AddBlitPass(blitPassParams, $"UpdateRefPass_{mat.name}");
                
                // Update the texRef.
                texRef.texture = destination;
            }
        }
    }

    // After updating the reference we will need to use the result copying it back to camera's
    // color attachment.
    class CopyBackRefPass : ScriptableRenderPass
    {
        // This function blits the reference back to the camera's color attachment.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Early exit if TexRefData doesn't exist within frameData since there is nothing to copy back.
            if (!frameData.Contains<TexRefData>()) return;

            // Fetch UniversalResourceData to retrieve the camera's active color texture.
            var resourceData = frameData.Get<UniversalResourceData>();
            // Fetch TexRefData to retrieve the texture reference.
            var texRef = frameData.Get<TexRefData>();
            
            renderGraph.AddBlitPass(texRef.texture, resourceData.activeColorTexture, Vector2.one, Vector2.zero, passName: "Blit Back Pass");
        }
    }

    [Tooltip("The material used when making the blit operation.")]
    public Material[] displayMaterials = new Material[1];

    UpdateRefPass m_UpdateRefPass;
    CopyBackRefPass m_CopyBackRefPass;

    // Here you can create passes and do the initialization of them. This is called every time serialization happens.
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
        // Early exit if there are no materials.
        if (displayMaterials == null)
        {
            Debug.LogWarning("TexterRefRendererFeature materials is null and will be skipped.");
            return;
        }

        // Since they have the same RenderPassEvent the order matters when enqueueing them.
        m_UpdateRefPass.Setup(displayMaterials);
        renderer.EnqueuePass(m_UpdateRefPass);
        renderer.EnqueuePass(m_CopyBackRefPass);
    }
}


