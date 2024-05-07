using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Example of how Blit operatrions can be handled using frameData using multiple ScriptaleRenderPasses.
public class BlitRendererFeature : ScriptableRendererFeature
{
    // The class living in frameData. It will take care of managing the texture resources.
    public class BlitData : ContextItem, IDisposable
    {
        // Textures used for the blit operations.
        RTHandle m_TextureFront;
        RTHandle m_TextureBack;
        // Render graph texture handles.
        TextureHandle m_TextureHandleFront;
        TextureHandle m_TextureHandleBack;

        // Scale bias is used to control how the blit operation is done. The x and y parameter controls the scale
        // and z and w controls the offset.
        static Vector4 scaleBias = new Vector4(1f, 1f, 0f, 0f);

        // Bool to manage which texture is the most resent.
        bool m_IsFront = true;

        // The texture which contains the color buffer from the most resent blit operation.
        public TextureHandle texture;

        // Function used to initialize BlitDatat. Should be called before starting to use the class for each frame.
        public void Init(RenderGraph renderGraph, RenderTextureDescriptor targetDescriptor, string textureName = null)
        {
            // Checks if the texture name is valid and puts in default value if not.
            var texName = String.IsNullOrEmpty(textureName) ? "_BlitTextureData" : textureName;
            // Reallocate if the RTHandles are being initialized for the first time or if the targetDescriptor has changed since last frame.
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_TextureFront, targetDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: texName + "Front");
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_TextureBack, targetDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: texName + "Back");
            // Create the texture handles inside render graph by importing the RTHandles in render graph.
            m_TextureHandleFront = renderGraph.ImportTexture(m_TextureFront);
            m_TextureHandleBack = renderGraph.ImportTexture(m_TextureBack);
            // Sets the active texture to the front buffer
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
            // Reset the acrive texture to be the front buffer.
            m_IsFront = true;
        }

        // The data we use to transfer data to the render function.
        class PassData
        {
            // When makeing a blit operation we will need a source, a destination and a material.
            // The source and destination is used to know where to copy from and to.
            public TextureHandle source;
            public TextureHandle destination;
            // The material is used to transform the color buffer while copying.
            public Material material;
        }

        // For this function we don't take a material as argument to show that we should remember to reset values
        // we don't use to avoid leaking values from last frame.
        public void RecordBlitColor(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Check if BlitData's texture is valid if it isn't initialize BlitData.
            if (!texture.IsValid())
            {
                // Setup the descriptor we use for BlitData. We should use the camera target's descriptor as a start.
                var cameraData = frameData.Get<UniversalCameraData>();
                var descriptor = cameraData.cameraTargetDescriptor;
                // We disable MSAA for the blit operations.
                descriptor.msaaSamples = 1;
                // We disable the depth buffer, since we are only makeing transformations to the color buffer.
                descriptor.depthBufferBits = 0;
                Init(renderGraph, descriptor);
            }

            // Starts the recording of the render graph pass given the name of the pass
            // and outputting the data used to pass data to the execution of the render function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("BlitColorPass", out var passData))
            {
                // Fetch UniversalResourceData from frameData to retrive the camera's active color attachment.
                var resourceData = frameData.Get<UniversalResourceData>();

                // Remember to reset material since it contains the value from last frame.
                // If we don't do this we would get the material last commited to the BlitPassData using RenderGraph
                // since we reuse the object allocation.
                passData.material = null;
                passData.source = resourceData.activeColorTexture;
                passData.destination = texture;

                // Sets input attachment to the cameras color buffer.
                builder.UseTexture(passData.source);
                // Sets output attachment 0 to BlitData's active texture.
                builder.SetRenderAttachment(passData.destination, 0);

                // Sets the render function.
                builder.SetRenderFunc((PassData passData, RasterGraphContext rgContext) => ExecutePass(passData, rgContext));
            }
        }

        // Records a render graph render pass which blits the BlitData's active texture back to the camera's color attachment.
        public void RecordBlitBackToColor(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Check if BlitData's texture is valid if it isn't it hasn't been initialized or an error has occured.
            if (!texture.IsValid()) return;

            // Starts the recording of the render graph pass given the name of the pass
            // and outputting the data used to pass data to the execution of the render function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>($"BlitBackToColorPass", out var passData))
            {
                // Fetch UniversalResourceData from frameData to retrive the camera's active color attachment.
                var resourceData = frameData.Get<UniversalResourceData>();

                // Remember to reset material. Otherwise you would use the last material used in RecordFullScreenPass.
                passData.material = null;
                passData.source = texture;
                passData.destination = resourceData.activeColorTexture;

                // Sets input attachment to BitData's active texture.
                builder.UseTexture(passData.source);
                // Sets output attachment 0 to the cameras color buffer.
                builder.SetRenderAttachment(passData.destination, 0);

                // Sets the render function.
                builder.SetRenderFunc((PassData passData, RasterGraphContext rgContext) => ExecutePass(passData, rgContext));
            }
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

            // Starts the recording of the render graph pass given the name of the pass
            // and outputting the data used to pass data to the execution of the render function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                // Switching the active texture handles to avoid blit. If we want the most recent
                // texture we can simply look at the variable texture
                m_IsFront = !m_IsFront;

                // Setting data to be used when executing the render function.
                passData.material = material;
                passData.source = texture;

                // Swap the active texture.
                if (m_IsFront)
                    passData.destination = m_TextureHandleFront;
                else
                    passData.destination = m_TextureHandleBack;

                // Sets input attachment to BlitData's old active texture.
                builder.UseTexture(passData.source);
                // Sets output attachment 0 to BitData's new active texture.
                builder.SetRenderAttachment(passData.destination, 0);

                // Update the texture after switching.
                texture = passData.destination;

                // Sets the render function.
                builder.SetRenderFunc((PassData passData, RasterGraphContext rgContext) => ExecutePass(passData, rgContext));
            }
        }

        // ExecutePass is the render function for each of the blit render graph recordings.
        // This is good practice to avoid using variables outside of the lambda it is called from.
        // It is static to avoid using member variables which could cause unintended behaviour.
        static void ExecutePass(PassData data, RasterGraphContext rgContext)
        {
            // We can use blit with or without a material both using the static scaleBias to avoid reallocations.
            if (data.material == null)
                Blitter.BlitTexture(rgContext.cmd, data.source, scaleBias, 0, false);
            else
                Blitter.BlitTexture(rgContext.cmd, data.source, scaleBias, data.material, 0);
        }

        // We need to release the textures once the renderer is released which will dispose every item inside
        // frameData (also data types previously created in earlier frames).
        public void Dispose()
        {
            m_TextureFront?.Release();
            m_TextureBack?.Release();
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

        // Setup function used to retrive the materials from the renderer feature.
        public void Setup(List<Material> materials)
        {
            m_Materials = materials;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Retrives the BlitData from the current frame.
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

    // Final render pass to copying the texture back to the camera's color attachment.
    class BlitEndRenderPass : ScriptableRenderPass
    {
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Retrives the BlitData from the current frame and blit it back again to the camera's color attachment.
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

    // Here you can create passes and do the initialization of them. This is called everytime serialization happens.
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


