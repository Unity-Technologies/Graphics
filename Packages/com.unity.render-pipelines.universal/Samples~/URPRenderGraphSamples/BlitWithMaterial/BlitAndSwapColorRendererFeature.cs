using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule.Util;

//This example blits the active CameraColor to a new texture. It shows how to do a blit with material, and how to use the ResourceData to avoid another blit back to the active color target.
//This example is for API demonstrative purposes. 


// This pass blits the whole screen for a given material to a temp texture, and swaps the UniversalResourceData.cameraColor to this temp texture.
// Therefor, the next pass that references the cameraColor will reference this new temp texture as the cameraColor, saving us a blit. 
// Using the ResourceData, you can manage swapping of resources yourself and don't need a bespoke API like the SwapColorBuffer API that was specific for the cameraColor. 
// This allows you to write more decoupled passes without the added costs of avoidable copies/blits.
public class BlitAndSwapColorPass : ScriptableRenderPass
{
    const string m_PassName = "BlitAndSwapColorPass";

    // Material used in the blit operation.
    Material m_BlitMaterial;

    // Function used to transfer the material from the renderer feature to the render pass.
    public void Setup(Material mat)
    {
        m_BlitMaterial = mat;

        //The pass will read the current color texture. That needs to be an intermediate texture. It's not supported to use the BackBuffer as input texture. 
        //By setting this property, URP will automatically create an intermediate texture. This has a performance cost so don't set this if you don't need it.
        //It's good practice to set it here and not from the RenderFeature. This way, the pass is selfcontaining and you can use it to directly enqueue the pass from a monobehaviour without a RenderFeature.
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
        // The active color and depth textures are the main color and depth buffers that the camera renders into
        var resourceData = frameData.Get<UniversalResourceData>();

        //This should never happen since we set m_Pass.requiresIntermediateTexture = true;
        //Unless you set the render event to AfterRendering, where we only have the BackBuffer. 
        if (resourceData.isActiveTargetBackBuffer)
        {
            Debug.LogError($"Skipping render pass. BlitAndSwapColorRendererFeature requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
            return;
        }

        // The destination texture is created here, 
        // the texture is created with the same dimensions as the active color texture
        var source = resourceData.activeColorTexture;

        var destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = $"CameraColor-{m_PassName}";
        destinationDesc.clearBuffer = false;

        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, m_BlitMaterial, 0);
        renderGraph.AddBlitPass(para, passName: m_PassName);

        //FrameData allows to get and set internal pipeline buffers. Here we update the CameraColorBuffer to the texture that we just wrote to in this pass. 
        //Because RenderGraph manages the pipeline resources and dependencies, following up passes will correctly use the right color buffer.
        //This optimization has some caveats. You have to be careful when the color buffer is persistent across frames and between different cameras, such as in camera stacking.
        //In those cases you need to make sure your texture is an RTHandle and that you properly manage the lifecycle of it.
        resourceData.cameraColor = destination;
    }
}

public class BlitAndSwapColorRendererFeature : ScriptableRendererFeature
{    
    [Tooltip("The material used when making the blit operation.")]
    public Material material;

    [Tooltip("The event where to inject the pass.")]
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    BlitAndSwapColorPass m_Pass;

    // Here you can create passes and do the initialization of them. This is called everytime serialization happens.
    public override void Create()
    {
        m_Pass = new BlitAndSwapColorPass();

        // Configures where the render pass should be injected.
        m_Pass.renderPassEvent = renderPassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Early exit if there are no materials.
        if (material == null)
        {
            Debug.LogWarning(this.name + " material is null and will be skipped.");
            return;
        }

        m_Pass.Setup(material);
        renderer.EnqueuePass(m_Pass);        
    }
}


