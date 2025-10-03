using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Vector2 = UnityEngine.Vector2;

// This RendererFeature demonstrates how to integrate a Compute Shader with RenderGraph.
// In this example, the output of the Compute Shader is used to modify the CameraColor texture.
// Additionally, once the CameraColor texture is updated, it is used as input for another Compute Shader pass.

// This sample is based on this video https://www.youtube.com/watch?v=v_WkGKn601M by Git-Amend, who is part of
// the Unity Insider Program (https://unity.com/unity-insiders). In the original sample the output image of the
// compute shader is applied to a RenderTexture instead of to the CameraColor texture.

public class ComputeShaderScreenInOutRenderFeature : ScriptableRendererFeature 
{
    class HeatmapPass : ScriptableRenderPass 
    {
        // Compute Shader programs.
        ComputeShader m_HeatmapComputeShader;
        ComputeShader m_HeatmapBrightnessComputeShader;
        
        // Kernel of each computeShader shader.
        int m_KernelHeatMapComputeShader;
        int m_KernelHeatmapBrightnessComputeShader;

        // Heatmap computeShader shader (uses a computeShader shader to simulate a group of enemies moving around).
        BufferHandle m_EnemyBuffer;
        Vector2[] m_EnemyPositions;
        const int k_EnemyCount = 64;

        // Texture Handles intended for later use by the render graph.
        TextureHandle m_HeatmapTextureHandle;
        TextureHandle m_HeatmapBrightnessTextureHandle;

        public void Setup(ComputeShader heatmapCS, ComputeShader heatmapBrightnessCS)
        {
            // Both computeShader shaders are defined here.
            // The first computeShader shader generates an output that is stored in the CameraColor.
            // The second computeShader shader then takes this CameraColor as its input, processes it further,
            // and produces the final result.
            m_HeatmapComputeShader = heatmapCS;
            m_HeatmapBrightnessComputeShader = heatmapBrightnessCS;
            m_KernelHeatMapComputeShader = heatmapCS.FindKernel("CSMain");
            m_KernelHeatmapBrightnessComputeShader = heatmapBrightnessCS.FindKernel("CSMain");
            
            // The enemy positions are initialized
            m_EnemyPositions = new Vector2[k_EnemyCount];
        }

        // Compute Pass Data.
        // This will be used for both Compute Shaders.
        class ComputePassData 
        {
            public ComputeShader computeShader;
            public int kernel;
            public int enemyCount;
            public Vector2[] positions;//This allows us to use the position inside the pass.
            public BufferHandle enemyHandle;
            public TextureHandle input;
            public TextureHandle output;
            public int width;
            public int height;
        }
        
        void UpdateEnemyPositions(int width, int height)
        {
            for (int i = 0; i < k_EnemyCount; i++) {
                float t = Time.time * 0.5f + i * 0.1f;
                float x = Mathf.PerlinNoise(t, i * 1.31f) * width;
                float y = Mathf.PerlinNoise(i * 0.91f, t) * height;
                m_EnemyPositions[i] = new Vector2(x, y);
            }
        }
        
        // This is the core of the RenderGraph system, where the computeShader passes are executed every frame.
        // The purpose of the computeShader pass can be summarized in three steps:
        
        // 1- Update enemy positions using Perlin noise, then upload them to a GPU buffer.
        // 2- Set up two computeShader passes in the render graph: the first generates a heatmap texture
        //      based on enemy positions, while the second further processes the resulting texture
        //      adding a bit of brightness with another computeShader shader.
        // 3- Assign the resulting texture from one computeShader pass to the next, and finally to the camera's color buffer for rendering.
        public override void RecordRenderGraph(RenderGraph graph, ContextContainer context) 
        {
            // Retrieving the Universal Resource Data, which contains all texture resources,
            // such as the active color texture, depth texture, and more.
            var resourceData = context.Get<UniversalResourceData>();
            
            // Getting the dimensions from the camera Color.
            var width = resourceData.cameraColor.GetDescriptor(graph).width;
            var height = resourceData.cameraColor.GetDescriptor(graph).height;
            
            // Update the enemy positions.
            UpdateEnemyPositions(width, height);

            // Creating a texture descriptor based on the activeColorTexture's descriptor values.
            // This texture descriptor will be used by both texture handlers:
            // m_HeatmapTextureHandle and m_HeatmapBrightnessTextureHandle.
            var heatmapDesc = resourceData.activeColorTexture.GetDescriptor(graph);
            
            // Defining some attributes of the descriptor
            heatmapDesc.name = "HeatmapHandle";
            heatmapDesc.enableRandomWrite = true;   // Use this to write to the texture efficiently
                                                    // with a compute shader, enabling random tile
                                                    // access instead of sequential tile writing.
            heatmapDesc.msaaSamples = MSAASamples.None;
            
            // Creating the texture for the m_HeatmapTextureHandle texture handle
            // based on the camera color descriptor.
            m_HeatmapTextureHandle = graph.CreateTexture(heatmapDesc);
            
            // Reusing the previously created heatmapDesc, but this time changing only the name.
            heatmapDesc.name = "BrightnessHeatmapHandle";
            
            // Creating the texture for the m_HeatmapBrightnessTextureHandle texture handle based on the camera color descriptor.
            m_HeatmapBrightnessTextureHandle = graph.CreateTexture(heatmapDesc);
            
            // Creating the buffer
            var bufferDesc = new BufferDesc()
            {
                name = "EnemyBuffer",
                stride = sizeof(float) * 2,
                count = k_EnemyCount,
                target = GraphicsBuffer.Target.Structured
            };
            
            // Now adding it to the RenderGraph.
            m_EnemyBuffer = graph.CreateBuffer(bufferDesc);

            // This is the definition of the computeShader render pass,
            // where the data to be processed by the computeShader shader pass is assigned.
            using (var builder = graph.AddComputePass<ComputePassData>("ComputeHeatmapPass", out var passData))
            {
                // Assign data to the computeShader shader data
                passData.computeShader = m_HeatmapComputeShader;
                passData.kernel = m_KernelHeatMapComputeShader;
                passData.output = m_HeatmapTextureHandle;
                passData.enemyHandle = m_EnemyBuffer;
                passData.enemyCount = k_EnemyCount;
                passData.positions = m_EnemyPositions;
                passData.width = width;
                passData.height = height;

                // Declare resource usage within this pass using the builder.
                builder.UseTexture(passData.output, AccessFlags.Write);
                builder.UseBuffer(passData.enemyHandle, AccessFlags.Read);

                // Set the function to execute the computeShader pass (using static to improve the performance).
                builder.SetRenderFunc(static(ComputePassData data, ComputeGraphContext ctx) =>
                {
                    // The SetBufferData use a command buffer to send the enemy position data
                    // from the passData.enemyHandle to the passData.positions.
                    ctx.cmd.SetBufferData(data.enemyHandle, data.positions); // Use data.enemyPositions
                                                                             // to ensure it remains scoped to the render function.
                    ctx.cmd.SetComputeIntParam(data.computeShader, "k_EnemyCount", data.enemyCount);
                    ctx.cmd.SetComputeBufferParam(data.computeShader, data.kernel, "m_EnemyPositions", data.enemyHandle);
                    ctx.cmd.SetComputeTextureParam(data.computeShader, data.kernel, "heatmapTexture", data.output);
                    ctx.cmd.DispatchCompute(data.computeShader, data.kernel, Mathf.CeilToInt(data.width / 8f), Mathf.CeilToInt(data.height / 8f), 1);
                });
            }
            
            // Here if you set resourceData.cameraColor = m_HeatmapTextureHandle and comment out the second pass, you
            // will get the result of the compute pass directly instead of reusing it in a second pass.

            // This is the second computeShader render pass.
            // In this pass, the input is the current `m_HeatmapTextureHandle`,
            // and the output, after being processed by the brightness computeShader shader,
            // will be stored in `m_HeatmapBrightnessTextureHandle`.
            using (var builder = graph.AddComputePass<ComputePassData>("ComputeCameraColorFromHeatmapPass", out var passData))
            {
                // Assign data to the computeShader shader data.
                passData.computeShader = m_HeatmapBrightnessComputeShader;
                passData.kernel = m_KernelHeatmapBrightnessComputeShader;
                passData.input = m_HeatmapTextureHandle;
                passData.output = m_HeatmapBrightnessTextureHandle;
                passData.width = width;
                passData.height = height;

                // Declare resource usage within this pass using the builder.
                builder.UseTexture(passData.input, AccessFlags.Read);
                builder.UseTexture(passData.output, AccessFlags.Write);

                // Set the function to execute the computeShader pass.
                builder.SetRenderFunc(static(ComputePassData data, ComputeGraphContext ctx) =>
                {
                    ctx.cmd.SetComputeTextureParam(data.computeShader, data.kernel, "heatmapTexture", data.input);
                    ctx.cmd.SetComputeTextureParam(data.computeShader, data.kernel, "result", data.output);
                    ctx.cmd.DispatchCompute(data.computeShader, data.kernel, Mathf.CeilToInt(data.width / 8f), Mathf.CeilToInt(data.height / 8f), 1);
                });
            }
            
            // The resulted texture of the last computeShader pass is assigned to the current Camera Color.
            resourceData.cameraColor = m_HeatmapBrightnessTextureHandle;
        }
    }
    
    // The inspector fields of the Renderer Feature.
    [SerializeField] ComputeShader HeatmapComputeShader;
    [SerializeField] ComputeShader HeatmapBrightnessComputeShader;

    // The HeatmapPass instance.
    HeatmapPass heatmapPass;

    public override void Create() 
    {
        heatmapPass = new HeatmapPass
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) 
    {
        if (HeatmapComputeShader == null || HeatmapBrightnessComputeShader == null)
        {
            Debug.Log("Set both shaders for the ComputeShaderRendererFeature.");
            return;
        }
        
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.Log(
                "The ComputeShaderRendererFeature cannot be added because this system doesn't support compute shaders.");
        }

        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            heatmapPass.Setup(HeatmapComputeShader, HeatmapBrightnessComputeShader);
            renderer.EnqueuePass(heatmapPass);
        }
    }
}
