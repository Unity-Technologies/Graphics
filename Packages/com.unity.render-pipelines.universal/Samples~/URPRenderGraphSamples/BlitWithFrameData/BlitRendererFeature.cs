using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// Example of how Blit operations can be handled using frameData and multiple ScriptableRenderPasses.
// BlitStartRenderPass initializes the BlitData in the frameData and copies the camera's color attachment to a texture inside the BlitData.
// BlitRenderPass makes a blit for each material given to the RendererFeature.
// BlitEndRenderPass blits the resulting BlitData texture back to the camera's color attachment.

public class BlitRendererFeature : ScriptableRendererFeature
{
    // The class living in frameData. It will take care of managing the texture resources.
    public class BlitData : ContextItem
    {
        // Render graph texture handles.
        TextureHandle m_TextureHandleFront;
        TextureHandle m_TextureHandleBack;

        // Bool to manage which texture is the most recent.
        bool m_IsFront = true;

        // The texture which contains the color buffer from the most recent blit operation.
        public TextureHandle texture;

        // Function used to initialize BlitData. Should be called before starting to use the class for each frame.
        public void Init(RenderGraph renderGraph, TextureDesc targetDescriptor, string textureName = null)
        {
            // Checks if the texture name is valid and puts in default value if not.
            var baseTexName = String.IsNullOrEmpty(textureName) ? "_BlitTextureData" : textureName;
            
            targetDescriptor.filterMode = FilterMode.Bilinear;
            targetDescriptor.wrapMode = TextureWrapMode.Clamp;
            // We disable MSAA for the blit operations.
            targetDescriptor.msaaSamples = MSAASamples.None;
            // We disable the depth buffer, since we are only making transformations to the color buffer.
            targetDescriptor.depthBufferBits = 0;

            targetDescriptor.name = baseTexName + "Front";
            m_TextureHandleFront = renderGraph.CreateTexture(targetDescriptor);
            
            targetDescriptor.name = baseTexName + "Back";
            m_TextureHandleBack = renderGraph.CreateTexture(targetDescriptor);

            // Sets the active texture to the front buffer.
            texture = m_TextureHandleFront;
        }

        // We will need to reset the texture handle after each frame to avoid leaking invalid texture handles
        // since the texture handles only lives for one frame.
        public override void Reset()
        {
            // Resets the color buffers to avoid carrying invalid references to the next frame.
            // This could be BlitData texture handles from last frame which will now be invalid.
            m_TextureHandleFront = TextureHandle.nullHandle;
            m_TextureHandleBack = TextureHandle.nullHandle;
            texture = TextureHandle.nullHandle;
            // Reset the active texture to be the front buffer.
            m_IsFront = true;
        }

        // For this function we don't take a material as argument to show that we should remember to reset values
        // we don't use to avoid leaking values from last frame.
        public void RecordBlitColor(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Fetch UniversalResourceData from frameData to retrieve the camera's active color attachment.
            var resourceData = frameData.Get<UniversalResourceData>();
            
            // Check if BlitData's texture is valid if it isn't initialize BlitData.
            if (!texture.IsValid())
            {
                // Set up the descriptor we use for BlitData. We use the camera color's descriptor as a start.
                Init(renderGraph, resourceData.activeColorTexture.GetDescriptor(renderGraph));
            }
            
            // Copy the activeColorTexture to the current front texture
            renderGraph.AddCopyPass(resourceData.activeColorTexture, texture, "BlitCameraColorToTexturePass");
        }

        // Records a render graph render pass which blits the BlitData's current front texture back to the camera's color attachment.
        public void RecordBlitBackToColor(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Check if BlitData's texture is valid if it isn't it hasn't been initialized or an error has occured.
            if (!texture.IsValid()) return;
            
            // Fetch UniversalResourceData from frameData to retrieve the camera's active color attachment.
            var resourceData = frameData.Get<UniversalResourceData>();
            
            // Copy the current front texture back to the activeColorTexture.
            renderGraph.AddCopyPass(texture, resourceData.activeColorTexture, "BlitTextureToColorPass");
        }

        // This function blits the whole screen for a given material.
        public void RecordFullScreenPass(RenderGraph renderGraph, string passName, Material material)
        {
            // Checks if the data is previously initialized and if the material is valid.
            if (!texture.IsValid() || material == null)
            {
                Debug.LogWarning("Invalid input texture handle, will skip fullscreen pass.");
                return;
            }
            
            // Switching the active texture handles to avoid blit. If we want the most recent
            // texture we can simply look at the variable texture.
            m_IsFront = !m_IsFront;

            // Swap the active texture.
            var destination = m_IsFront ? m_TextureHandleFront : m_TextureHandleBack;

            // Setting data to be used when executing the render function.
            var blitMaterialParameters = new RenderGraphUtils.BlitMaterialParameters(texture, destination, material, 0);
            
            // Blit
            renderGraph.AddBlitPass(blitMaterialParameters, passName);
            
            // Update the texture after switching.
            texture = destination;
        }
    }

    // Initial render pass for the renderer feature which is run to initialize the data in frameData and copying
    // the camera's color attachment to a texture inside BlitData so we can do transformations using blit.
    class BlitStartRenderPass : ScriptableRenderPass
    {
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Creating the data BlitData inside frameData.
            var blitTextureData = frameData.Create<BlitData>();
            // Copies the camera's color attachment to a texture inside BlitData.
            blitTextureData.RecordBlitColor(renderGraph, frameData);
        }
    }

    // Render pass which makes a blit for each material given to the renderer feature.
    class BlitRenderPass : ScriptableRenderPass
    {
        List<Material> m_Materials;

        // Setup function used to retrieve the materials from the renderer feature.
        public void Setup(List<Material> materials)
        {
            m_Materials = materials;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Retrieves the BlitData from the current frame.
            var blitTextureData = frameData.Get<BlitData>();
            foreach(var material in m_Materials)
            {
                // Skip current cycle if the material is null since there is no need to blit if no
                // transformation happens.
                if (material == null) continue;
                // Records the material blit pass.
                blitTextureData.RecordFullScreenPass(renderGraph, $"Blit {material.name} Pass", material);
            }    
        }
    }

    // Final render pass for copying the texture back to the camera's color attachment.
    class BlitEndRenderPass : ScriptableRenderPass
    {
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Retrieves the BlitData from the current frame and blit it back again to the camera's color attachment.
            var blitTextureData = frameData.Get<BlitData>();
            blitTextureData.RecordBlitBackToColor(renderGraph, frameData);
        }
    }

    [SerializeField]
    [Tooltip("Materials used for blitting. They will be blit in the same order they have in the list starting from index 0. ")]
    List<Material> m_Materials;

    BlitStartRenderPass m_StartPass;
    BlitRenderPass m_BlitPass;
    BlitEndRenderPass m_EndPass;
    
    // Here you can create and initialize passes. This is called every time serialization happens.
    public override void Create()
    {
        m_StartPass = new BlitStartRenderPass();
        m_BlitPass = new BlitRenderPass();
        m_EndPass = new BlitEndRenderPass();

        // Configures where the render pass should be injected.
        m_StartPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        m_BlitPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        m_EndPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Early return if there is no texture to blit.
        if (m_Materials == null || m_Materials.Count == 0) return;

        // Pass the material to the blit render pass.
        m_BlitPass.Setup(m_Materials);

        // Since they have the same RenderPassEvent the order matters when enqueueing them.
        renderer.EnqueuePass(m_StartPass);
        renderer.EnqueuePass(m_BlitPass);
        renderer.EnqueuePass(m_EndPass);
    }
}


