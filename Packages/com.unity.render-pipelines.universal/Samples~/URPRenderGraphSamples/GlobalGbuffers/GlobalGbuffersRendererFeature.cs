using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

// This example feature sets the gBuffer components as globals (it renders nothing itself). By adding this
// feature to the scriptable renderer, other passes after it can access the gBuffers as globals.
// Make sure to set the rendering path to Deferred for it to work.

// Setting the gBuffers as globals may lead to reduced performance and memory use. Ideally, it's better to manage the
// textures yourself and do builder.UseTexture only for the textures you actually need.
public class GlobalGbuffersRendererFeature : ScriptableRendererFeature
{
    class GlobalGBuffersRenderPass : ScriptableRenderPass
    {
        Material m_Material;
        string m_PassName = "Make gBuffer Components Global";

        private static readonly int GBufferNormalSmoothnessIndex = 2;
        private static readonly int GbufferLightingIndex = 3;
        private static readonly int GBufferRenderingLayersIndex = 5;

        // The pipeline already sets the gBuffer depth component to be global in a few places, so uncomment this code as needed
        // private static readonly int GbufferDepthIndex = 4;

        // Components marked as optional are only present when the pipeline requests it.
        // If for example there is no rendering layers texture, _GBuffer5 will contain the ShadowMask texture
        private static readonly int[] s_GBufferShaderPropertyIDs = new int[]
        {
            // Contains Albedo Texture
            Shader.PropertyToID("_GBuffer0"),

            // Contains Specular Metallic Texture
            Shader.PropertyToID("_GBuffer1"),

            // Contains Normals and Smoothness, referenced as _CameraNormalsTexture in other shaders
            Shader.PropertyToID("_GBuffer2"),

            // Contains Lighting texture
            Shader.PropertyToID("_GBuffer3"),

            // Contains Depth texture, referenced as _CameraDepthTexture in other shaders (optional)
            Shader.PropertyToID("_GBuffer4"),

            // Contains Rendering Layers Texture, referenced as _CameraRenderingLayersTexture in other shaders (optional)
            Shader.PropertyToID("_GBuffer5"),

            // Contains ShadowMask texture (optional)
            Shader.PropertyToID("_GBuffer6")
        };

        private class PassData
        {
        }

        // This sets the gBuffer components as global after the current pass. After the pass, the gBuffers components made global
        // will be made accessible using 'builder.UseAllGlobalTextures(true)' instead of 'builder.UseTexture(gBuffer[i])
        // Shaders that use global texture will be able to fetch them without the need to call 'material.SetTexture()'
        // like we do in the ExecutePass function of this pass.
        private void SetGlobalGBufferTextures(IRasterRenderGraphBuilder builder, TextureHandle[] gBuffer)
        {
            // This loop will make the gBuffers accessible by all shaders using _GBufferX texture shader IDs
            for (int i = 0; i < gBuffer.Length; i++)
            {
                if (i != GbufferLightingIndex && gBuffer[i].IsValid())
                    builder.SetGlobalTextureAfterPass(gBuffer[i], s_GBufferShaderPropertyIDs[i]);
            }

            // Some global textures are accessed using specific shader IDs that are internal to URP. To use the gBuffer in these places, we
            // need to set the ID to point to the corresponding gBuffer component.
            if (gBuffer[GBufferNormalSmoothnessIndex].IsValid())
            {
                // After this pass, shaders that use the _CameraNormalsTexture will get the gBuffer's NormalsSmoothnessTexture component
                builder.SetGlobalTextureAfterPass(gBuffer[GBufferNormalSmoothnessIndex],
                    Shader.PropertyToID("_CameraNormalsTexture"));
            }
            
            // The pipeline already sets the gBuffer depth component to be global in a few places, so uncomment this code as needed
            // if (GbufferDepthIndex < gBuffer.Length && gBuffer[GbufferDepthIndex].IsValid())
            // {
            //     // After this pass, shaders that use the _CameraDepthTexture will get the gBuffer's Depth component (note that it is also set global by the copy depth pass)
            //     builder.SetGlobalTextureAfterPass(gBuffer[GbufferDepthIndex],
            //         Shader.PropertyToID("_CameraDepthTexture"));
            // }

            if (GBufferRenderingLayersIndex < gBuffer.Length && gBuffer[GBufferRenderingLayersIndex].IsValid())
            {
                // After this pass, shaders that use the _CameraRenderingLayersTexture will get the gBuffer's RenderingLayersTexture component
                builder.SetGlobalTextureAfterPass(gBuffer[GBufferRenderingLayersIndex],
                    Shader.PropertyToID("_CameraRenderingLayersTexture"));
            }
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            // The gBuffer components are only used in deferred mode
            if (universalRenderingData.renderingMode != RenderingMode.Deferred)
                return;
            
            // Get the gBuffer texture handles are stored in the resourceData
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle[] gBuffer = resourceData.gBuffer;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(m_PassName, out var passData))
            {
                builder.AllowPassCulling(false);
                // Set the gBuffers to be global after the pass
                SetGlobalGBufferTextures(builder, gBuffer);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => { /* nothing to be rendered */ });
            }
        }
    }

    GlobalGBuffersRenderPass m_GlobalGbuffersRenderPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_GlobalGbuffersRenderPass = new GlobalGBuffersRenderPass
        {
            //  This pass must be injected after rendering the deferred lights or later.
            renderPassEvent = RenderPassEvent.AfterRenderingDeferredLights
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_GlobalGbuffersRenderPass);
    }
}
