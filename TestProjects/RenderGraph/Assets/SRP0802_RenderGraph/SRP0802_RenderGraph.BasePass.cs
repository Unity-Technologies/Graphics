using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE BASE PASS --------------------------------------------------------------------------------------------
// This pass renders objects into 2 RenderTargets:
// Albedo - grey texture and the skybox
// Emission - animated color
public partial class SRP0802_RenderGraph
{
    ShaderTagId m_PassName1 = new ShaderTagId("SRP0802_Pass1"); //The shader pass tag just for SRP0802

    public class SRP0802_BasePassData
    {
        public RendererListHandle m_renderList_opaque;
        public RendererListHandle m_renderList_transparent;
        public TextureHandle m_Albedo;
        public TextureHandle m_Emission;
        public TextureHandle m_Depth;
    }

    private TextureHandle CreateColorTexture(RenderGraph graph, Camera camera, string name)
    {
        bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

        //Texture description
        TextureDesc colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
        colorRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default,colorRT_sRGB);
        colorRTDesc.depthBufferBits = 0;
        colorRTDesc.msaaSamples = MSAASamples.None;
        colorRTDesc.enableRandomWrite = false;
        colorRTDesc.clearBuffer = true;
        colorRTDesc.clearColor = Color.black;
        colorRTDesc.name = name;

        return graph.CreateTexture(colorRTDesc);
    }

    private TextureHandle CreateDepthTexture(RenderGraph graph, Camera camera)
    {
        bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

        //Texture description
        TextureDesc colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
        colorRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Depth,colorRT_sRGB);
        colorRTDesc.depthBufferBits = DepthBits.Depth24;
        colorRTDesc.msaaSamples = MSAASamples.None;
        colorRTDesc.enableRandomWrite = false;
        colorRTDesc.clearBuffer = true;
        colorRTDesc.clearColor = Color.black;
        colorRTDesc.name = "Depth";

        return graph.CreateTexture(colorRTDesc);
    }

    public SRP0802_BasePassData Render_SRP0802_BasePass(Camera camera, RenderGraph graph, CullingResults cull)
    {
        using (var builder = graph.AddRasterRenderPass<SRP0802_BasePassData>( "Base Pass", out var passData, new ProfilingSampler("Base Pass Profiler" ) ) )
        {
            //Textures - Multi-RenderTarget
            passData.m_Albedo = CreateColorTexture(graph,camera,"Albedo");
            builder.UseTextureFragment(passData.m_Albedo, 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
            passData.m_Emission = CreateColorTexture(graph,camera,"Emission");
            builder.UseTextureFragment(passData.m_Emission, 1, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
            passData.m_Depth = CreateDepthTexture(graph,camera);
            builder.UseTextureFragmentDepth(passData.m_Depth, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);

            //Renderers
            UnityEngine.Rendering.RendererUtils.RendererListDesc rendererDesc_base_Opaque = new UnityEngine.Rendering.RendererUtils.RendererListDesc(m_PassName1,cull,camera);
            rendererDesc_base_Opaque.sortingCriteria = SortingCriteria.CommonOpaque;
            rendererDesc_base_Opaque.renderQueueRange = RenderQueueRange.opaque;
            passData.m_renderList_opaque = graph.CreateRendererList(rendererDesc_base_Opaque);
            builder.UseRendererList(passData.m_renderList_opaque);

            UnityEngine.Rendering.RendererUtils.RendererListDesc rendererDesc_base_Transparent = new UnityEngine.Rendering.RendererUtils.RendererListDesc(m_PassName1,cull,camera);
            rendererDesc_base_Transparent.sortingCriteria = SortingCriteria.CommonTransparent;
            rendererDesc_base_Transparent.renderQueueRange = RenderQueueRange.transparent;
            passData.m_renderList_transparent = graph.CreateRendererList(rendererDesc_base_Transparent);
            builder.UseRendererList(passData.m_renderList_transparent);
            
            //Builder
            builder.SetRenderFunc((SRP0802_BasePassData data, RasterGraphContext context) => 
            {
                //Skybox - this will draw to the first target, i.e. Albedo
                //if(camera.clearFlags == CameraClearFlags.Skybox)  {  context.renderContext.DrawSkybox(camera);  }

                context.cmd.DrawRendererList(data.m_renderList_opaque);
                context.cmd.DrawRendererList(data.m_renderList_transparent);
            });

            return passData;
        }
    }
}
