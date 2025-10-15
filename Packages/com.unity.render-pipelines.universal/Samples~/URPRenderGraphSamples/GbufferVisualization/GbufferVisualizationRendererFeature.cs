using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

// This example uses the gBuffer components in a RenderPass when they are not global.
// The RenderPass will by default show the contents of the Specular Metallic Texture (_GBuffer2) on geometry in the scene,
// but you can change the sampled gBuffer component by modifying the example shader.
// Make sure to (1) set the rendering path to Deferred and (2) add a 3D object in your scene to see a result.
public class GbufferVisualizationRendererFeature : ScriptableRendererFeature
{
    class GBufferVisualizationRenderPass : ScriptableRenderPass
    {
        Material m_Material;
        string m_PassName = "Visualize GBuffer Components (and make gBuffer global)";
        
        private static readonly int GbufferLightingIndex = 3;
        
        // Other gBuffer components indices
        // private static readonly int GBufferNormalSmoothnessIndex = 2;
        // private static readonly int GbufferDepthIndex = 4;
        // private static readonly int GBufferRenderingLayersIndex = 5;

        // Components marked as optional are only present when the pipeline requests it.
        // If for example there is no rendering layers texture, _GBuffer5 will contain the ShadowMask texture.
        private static readonly int[] s_GBufferShaderPropertyIDs = new int[]
        {
            // Contains Albedo texture.
            Shader.PropertyToID("_GBuffer0"),

            // Contains Specular Metallic texture.
            Shader.PropertyToID("_GBuffer1"),

            // Contains Normals and Smoothness, referenced as _CameraNormalsTexture in other shaders.
            Shader.PropertyToID("_GBuffer2"),

            // Contains Lighting texture.
            Shader.PropertyToID("_GBuffer3"),

            // Contains Depth texture, referenced as _CameraDepthTexture in other shaders (optional).
            Shader.PropertyToID("_GBuffer4"),

            // Contains Rendering Layers texture, referenced as _CameraRenderingLayersTexture in other shaders (optional).
            Shader.PropertyToID("_GBuffer5"),

            // Contains ShadowMask texture (optional).
            Shader.PropertyToID("_GBuffer6")
        };

        private class PassData
        {
            // In this example, we want to use the gBuffer components in our pass.
            public TextureHandle[] gBuffer;
            public Material material;
        }

        public void Setup(Material material)
        {
            m_Material = material;
        }

        // This method will draw the contents of the gBuffer component requested in the shader.
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Here, we read all the gBuffer components as an example even though the shader only needs one.
            // We still need to set it explicitly since it is not accessible globally (so the
            // shader won't have access to it by default).
            for (int i = 0; i < data.gBuffer.Length; i++)
            {
                data.material.SetTexture(s_GBufferShaderPropertyIDs[i], data.gBuffer[i]);
            }

            // Draw the gBuffer component requested by the shader over the geometry.
            context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1);
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            // The gBuffer components are only used in deferred mode
            if (m_Material == null || universalRenderingData.renderingMode != RenderingMode.Deferred)
                return;

            // Get the gBuffer texture handles stored in the resourceData
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle[] gBuffer = resourceData.gBuffer;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(m_PassName, out var passData))
            {
                passData.material = m_Material;

                // For this pass, we want to write to the activeColorTexture, which is the gBuffer Lighting component (_GBuffer3) in the deferred path.
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);

                // We are reading the gBuffer components in our pass, so we need to call UseTexture on them.
                // When they are global, they can be all read with builder.UseAllGlobalTexture(true), but
                // in this pass they are not global.
                for (int i = 0; i < resourceData.gBuffer.Length; i++)
                {
                    if (i == GbufferLightingIndex)
                    {
                        // We already specify we are writing to it above (SetRenderAttachment).
                        continue;
                    }

                    builder.UseTexture(resourceData.gBuffer[i]);
                }

                // We need to set the gBuffer in the pass' data, otherwise the pass won't have access to it when it is executed.
                passData.gBuffer = gBuffer;

                // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }

    GBufferVisualizationRenderPass m_GBufferRenderPass;
    public Material m_Material;

    /// <inheritdoc/>
    public override void Create()
    {
        m_GBufferRenderPass = new GBufferVisualizationRenderPass
        {
            // This pass must be injected after rendering the deferred lights or later.
            renderPassEvent = RenderPassEvent.AfterRenderingDeferredLights
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // The gBuffers are only used in the Deferred rendering path.
        if (m_Material != null)
        {
            m_GBufferRenderPass.Setup(m_Material);
            renderer.EnqueuePass(m_GBufferRenderPass);
        }
    }
}
