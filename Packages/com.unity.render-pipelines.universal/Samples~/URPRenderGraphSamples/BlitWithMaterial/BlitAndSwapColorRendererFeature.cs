using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//This example blits the active CameraColor to a new texture. It shows how to do a blit with material, and how to use the ResourceData to avoid another blit back to the active color target.
//This example is for API demonstrative purposes. 

public class BlitAndSwapColorRendererFeature : ScriptableRendererFeature
{

    // This pass blits the whole screen for a given material to a temp texture, and swaps the UniversalResourceData.cameraColor to this temp texture.
    // Therefor, the next pass that references the cameraColor will reference this new temp texture as the cameraColor, saving us a blit. 
    // Using the ResourceData, you can manage swapping of resources yourself and don't need a bespoke API like the SwapColorBuffer API that was specific for the cameraColor. 
    // This allows you to write more decoupled passes without the added costs of avoidable copies/blits.
    class BlitAndSwapColorPass : ScriptableRenderPass
    {
        const string m_PassName = "BlitAndSwapColorPass";

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
        Material m_BlitMaterial;

        // Function used to transfer the material from the renderer feature to the render pass.
        public void Setup(Material mat)
        {
            m_BlitMaterial = mat;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            //This should never happen since we set m_Pass.requiresIntermediateTexture = true;
            //Unless you set the render event to AfterRendering, where we only have the BackBuffer. 
            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError($"Skipping render pass. BlitAndSwapColorRendererFeature requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
                return;
            }


            // Starts the recording of the render graph pass given the name of the pass
            // and outputting the data used to pass data to the execution of the render function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(m_PassName, out var passData))
            {
                // Initialize the pass data
                InitPassData(renderGraph, frameData, ref passData);

                // Sets input.
                builder.UseTexture(passData.source);

                // Sets output.
                builder.SetRenderAttachment(passData.destination, 0);

                // Sets the render function.
                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) => ExecutePass(data, rgContext));


                //FrameData allows to get and set internal pipeline buffers. Here we update the CameraColorBuffer to the texture that we just wrote to in this pass. 
                //Because RenderGraph manages the pipeline resources and dependencies, following up passes will correctly use the right color buffer.
                //This optimization has some caveats. You have to be careful when the color buffer is persistent across frames and between different cameras, such as in camera stacking.
                //In those cases you need to make sure your texture is an RTHandle and that you properly manage the lifecycle of it.
                resourceData.cameraColor = passData.destination;
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

        private void InitPassData(RenderGraph renderGraph, ContextContainer frameData, ref PassData passData)
        {
            // Fill up the passData with the data needed by the passes

            // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
            // The active color and depth textures are the main color and depth buffers that the camera renders into
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            // The destination texture is created here, 
            // the texture is created with the same dimensions as the active color texture, but with no depth buffer, being a copy of the color texture

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, $"CameraTarget-{m_PassName}", false);

            passData.source = resourceData.activeColorTexture;
            passData.destination = destination;
            passData.material = m_BlitMaterial;
        }
    }


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
            Debug.LogWarning("BlitAndSwapColorRendererFeature material is null and will be skipped.");
            return;
        }

        m_Pass.Setup(material);
        renderer.EnqueuePass(m_Pass);

        //The pass will read the current color texture. That needs to be an intermediate texture. It's not supported to use the BackBuffer as input texture. 
        //By setting this property, URP will automatically create an intermediate texture. 
        m_Pass.requiresIntermediateTexture = true;
    }
}


