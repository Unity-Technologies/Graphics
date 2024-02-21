using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// In this example it is shown how to use Multiple Render Targets (MRT) in RenderGraph using URP. This is useful when more than 4 channels of data (a single RGBA texture) needs to be written by a pass. 
public class MrtRendererFeature : ScriptableRendererFeature
{
    // This pass is using MRT and will output to 3 different Render Targets.
    class MrtPass : ScriptableRenderPass
    {
        // The data we want to transfer to the render function after recording.
        class PassData
        {
            // Texture handle for the color input.
            public TextureHandle color;
            // Input texture name for the material.
            public string texName;
            // Material used for the MRT Pass.
            public Material material;
        }

        // Input texture name for the material.
        string m_texName;
        // Material used for the MRT Pass.
        Material m_Material;
        // RTHandle outputs for the MRT destinations.
        RTHandle[] m_RTs = new RTHandle[3];
        RenderTargetInfo[] m_RTInfos = new RenderTargetInfo[3];

        // Function used to transfer the material from the renderer feature to the render pass.
        public void Setup(string texName, Material material, RenderTexture[] renderTextures)
        {
            m_Material = material;
            m_texName = String.IsNullOrEmpty(texName) ? "_ColorTexture" : texName;


            //Create RTHandles from the RenderTextures if they have changed.
            for (int i = 0; i < 3; i++)
            {
                if (m_RTs[i] == null || m_RTs[i].rt != renderTextures[i])
                {
                    m_RTs[i]?.Release();
                    m_RTs[i] = RTHandles.Alloc(renderTextures[i], $"ChannelTexture[{i}]");
                    m_RTInfos[i] = new RenderTargetInfo()
                    {
                        format = renderTextures[i].graphicsFormat,
                        height = renderTextures[i].height,
                        width = renderTextures[i].width,
                        bindMS = renderTextures[i].bindTextureMS,
                        msaaSamples = 1,
                        volumeDepth = renderTextures[i].volumeDepth,
                    };
                }
            }
        }

        // This function blits the whole screen for a given material.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var handles = new TextureHandle[3];
            // Imports the texture handles them in RenderGraph.
            for (int i = 0; i < 3; i++)
            {
                handles[i] = renderGraph.ImportTexture(m_RTs[i], m_RTInfos[i]);
            }
            // Starts the recording of the render graph pass given the name of the pass
            // and outputting the data used to pass data to the execution of the render function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("MRT Pass", out var passData))
            {
                // Fetch the universal resource data to exstract the camera's color attachment.
                var resourceData = frameData.Get<UniversalResourceData>();

                // Fill in the pass data using by the render function.
                // Use the camera's color attachment as input.
                passData.color = resourceData.activeColorTexture;
                // Input Texture name for the material.
                passData.texName = m_texName;
                // Material used in the pass.
                passData.material = m_Material;


                // Sets input attachment.
                builder.UseTexture(passData.color);
                // Sets color attachments.
                for (int i = 0; i < 3; i++)
                {
                    builder.SetRenderAttachment(handles[i], i);
                }

                // Sets the render function.
                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
            }
        }

        // ExecutePass is the render function for each of the blit render graph recordings.
        // This is good practice to avoid using variables outside of the lambda it is called from.
        // It is static to avoid using member variables which could cause unintended behaviour.
        static void ExecutePass(PassData data, RasterGraphContext rgContext)
        {
            // Sets the input color texture to the name used in the MRTPass
            data.material.SetTexture(data.texName, data.color);
            // Draw the fullscreen triangle with the MRT shader.
            rgContext.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3);
        }
    }

    [Tooltip("The material used when making the MRT pass.")]
    public Material mrtMaterial;
    [Tooltip("Name to apply the camera's color attachment to for the given material.")]
    public string textureName = "_ColorTexture";
    [Tooltip("Render Textures to output the result to. Is has to have the size of 3.")]
    public RenderTexture[] renderTextures = new RenderTexture[3];

    MrtPass m_MrtPass;

    // Here you can create passes and do the initialization of them. This is called everytime serialization happens.
    public override void Create()
    {
        m_MrtPass = new MrtPass();

        // Configures where the render pass should be injected.
        m_MrtPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Since they have the same RenderPassEvent the order matters when enqueueing them.

        // Early exit if there are no materials.
        if (mrtMaterial == null || renderTextures.Length != 3)
        {
            Debug.LogWarning("Skipping MRTPass because the material is null or render textures doesn't have a size of 3.");
            return;
        }

        foreach (var rt in renderTextures)
        {
            if (rt == null)
            {
                Debug.LogWarning("Skipping MRTPass because one of the render textures is null.");
                return;
            }
        }

        // Call the pass Setup function to transfer the RendererFeature settings to the RenderPass.
        m_MrtPass.Setup(textureName, mrtMaterial, renderTextures);
        renderer.EnqueuePass(m_MrtPass);
    }
}


