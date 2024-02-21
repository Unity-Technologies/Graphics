using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// MERGING: This pass can be merged with Draw Objects Pass and Draw Skybox pass if you set the m_PassEvent in
// the inspector to After Rendering Opagues and set the texture type to Normal.
// Your can observe this merging in the Render Graph Visualizer. If set to After Rendering Post Processing we
// can now see that the pass isn't merged with any thing.

// About MERGING: To merge two RG passes they need to be recorded within the same RecordRenderGraph function.
// The Graph will automatically determine when to break render passes as well as the load and
// store actions to apply to these render passes. To do this, the graph will analyze the use of
// textures. E.g. when a texture is used twice in a row as a active render target, the two render
// graph passes will be merged in a single render pass with two surpasses. On the other hand if a
// render target is sampled as a texture in a later pass this render target will be stored
// (and possibly resolved) and the render pass will be broken up.

// This RenderFeature shows how to used RenderGraph to output a specific texture used in URP, how a texture
// can be attached by name to a material and how two render passes can be merged if executed in the correct order.
public class OutputTextureRendererFeature : ScriptableRendererFeature
{
    // Enum used to select which texture you want to output.
    [Serializable]
    enum TextureType
    {
        OpaqueColor,
        Depth,
        Normal,
        MotionVector,
    }

    // Function to fetch the texture given the resource data and the texture type you want.
    static TextureHandle GetTextureHandleFromType(UniversalResourceData resourceData, TextureType textureType)
    {
        switch (textureType)
        {
            case TextureType.OpaqueColor:
                return resourceData.cameraOpaqueTexture;
            case TextureType.Depth:
                return resourceData.cameraDepthTexture;
            case TextureType.Normal:
                return resourceData.cameraNormalsTexture;
            case TextureType.MotionVector:
                return resourceData.motionVectorColor;
            default:
                return TextureHandle.nullHandle;
        }
    }

    // Pass which outputs a texture from rendering to inspect a texture 
    class OutputTexturePass : ScriptableRenderPass
    {
        // The texture name you wish to bind the texture handle to for a given material.
        string m_TextureName;
        // The texture type you want to retrive from URP.
        TextureType m_TextureType;
        // The material used for blitting to the color output.
        Material m_Material;

        // Function set setup the ConfigureInput() and transfer the renderer feature settings to the render pass.
        public void Setup(string textureName, TextureType textureType, Material material)
        {
            // Setup code to trigger each corrspoinding texture is ready for use one the pass is run.
            if (textureType == TextureType.OpaqueColor)
                ConfigureInput(ScriptableRenderPassInput.Color);
            else if (textureType == TextureType.Depth)
                ConfigureInput(ScriptableRenderPassInput.Depth);
            else if (textureType == TextureType.Normal)
                ConfigureInput(ScriptableRenderPassInput.Normal);
            else if (textureType == TextureType.MotionVector)
                ConfigureInput(ScriptableRenderPassInput.Motion);

            // Setup the texture name, type and material used when blitting.
            // In this example we will use a mateial using a custom name for the input texture name when blitting.
            // This texture name has to match the material texture input you are using.
            m_TextureName = String.IsNullOrEmpty(textureName) ? "_InputTexture" : textureName;
            // Texture type selects which input we would like to retrive from the camera.
            m_TextureType = textureType;
            // The material is used to blit the texture to the cameras color attachment.
            m_Material = material;
        }

        // PassData is used to pass data when recording to the execution of the pass.
        class PassData
        {
            // Texture name used for the material.
            public string textureName;
            // Texture handle to blit from.
            public TextureHandle input;
            // Material used for blitting.
            public Material material;
        }

        // Records a render graph render pass which blits the BlitData's active texture back to the camera's color attachment.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Starts the recording of the render graph pass given the name of the pass
            // and outputting the data used to pass data to the execution of the render function.
            using (var builder = renderGraph.AddRasterRenderPass("OutputTexturePass", out PassData passData))
            {
                // Fetch UniversalResourceData from frameData to retrive the URP's texture handles.
                var resourceData = frameData.Get<UniversalResourceData>();
                
                // Sets the texture name for the data which carries the data to the render function.
                passData.textureName = m_TextureName;
                // Sets the texture handle input using the helper function to fetch the correct handle from resourceData.
                passData.input = GetTextureHandleFromType(resourceData, m_TextureType);
                // Checks if the texture handle used as input is valid.
                if (!passData.input.IsValid()) {
                    Debug.Log("Input texture is not created, skipping OutputTexturePass.");
                    return;
                }
                // Sets the material for the render function data.
                passData.material = m_Material;

                // Sets input attachment to the texture handle retrived before.
                builder.UseTexture(passData.input);
                // Sets output attachment 0 to the cameras active color texture.
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                // Sets the render function which is called after the recording is done and the pass is executed.
                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));
            }
        }

        // ExecutePass is the render function set in the render graph recordings.
        // This is good practice to avoid using variables outside of the lambda it is called from.
        // It is static to avoid using member variables which could cause unintended behaviour.
        static void ExecutePass(PassData data, RasterGraphContext rgContext)
        {
            // We need to set the texture for the material using the texture handle and the texture name.
            data.material.SetTexture(data.textureName, data.input);
            // Draw procedural
            rgContext.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3);
        }
    }

    // Inputs in the inspector to change the settings for the renderer feature.
    [SerializeField]
    RenderPassEvent m_PassEvent;
    [SerializeField]
    string m_TextureName = "_InputTexture";
    [SerializeField]
    TextureType m_TextureType;
    [SerializeField]
    Material m_Material;

    OutputTexturePass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new OutputTexturePass();
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = m_PassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Setup the correct data for the render pass, and transfers the data from the renderer feature to the render pass.
        m_ScriptablePass.Setup(m_TextureName, m_TextureType, m_Material);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


