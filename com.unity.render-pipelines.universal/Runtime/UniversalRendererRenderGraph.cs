using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderer
    {
        protected override void RecordRenderGraphBlock(int renderPassBlock, RenderGraph renderGraph, ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            if (renderPassBlock == RenderPassBlock.BeforeRendering)
            {

            }
            else if (renderPassBlock == RenderPassBlock.MainRenderingOpaque)
            {
                m_RenderOpaqueForwardPass.Render(camera, renderGraph, frameResources.backBuffer, frameResources.depth, ref renderingData);

                // TODO: injected passes should go in between passes here? i.e.:
                //RunCustomPasses(RenderPassEvent.AfterRenderingOpaques)

                RenderGraphSkyboxTestPass.PassData testPassData = RenderGraphSkyboxTestPass.Render(camera, renderGraph, frameResources.backBuffer, frameResources.depth);
            }
            else if (renderPassBlock == RenderPassBlock.MainRenderingTransparent)
            {

            }
            else if (renderPassBlock == RenderPassBlock.AfterRendering)
            {

            }
        }
    }

    class RenderGraphSkyboxTestPass
    {
        public class PassData
        {
            public TextureHandle m_Albedo;
            public TextureHandle m_Depth;

            public Camera m_Camera;
        }

        static private TextureHandle CreateColorTexture(RenderGraph graph, Camera camera, string name)
        {
            bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

            //Texture description
            TextureDesc colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
            colorRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, colorRT_sRGB);
            colorRTDesc.depthBufferBits = 0;
            colorRTDesc.msaaSamples = MSAASamples.None;
            colorRTDesc.enableRandomWrite = false;
            colorRTDesc.clearBuffer = true;
            colorRTDesc.clearColor = Color.black;
            colorRTDesc.name = name;

            return graph.CreateTexture(colorRTDesc);
        }

        static public PassData Render(Camera camera, RenderGraph graph, TextureHandle backBuffer, TextureHandle depth)
        {
            using (var builder = graph.AddRenderPass<PassData>("Test Pass", out var passData, new ProfilingSampler("Test Pass Profiler")))
            {
                //CreateColorTexture(graph,camera,"Albedo");
                passData.m_Albedo = builder.UseColorBuffer(backBuffer, 0);
                //TextureHandle Depth = CreateDepthTexture(graph, camera);
                passData.m_Depth = builder.UseDepthBuffer(depth, DepthAccess.Read);

                //builder.WriteTexture(Albedo);

                builder.AllowPassCulling(false);

                passData.m_Camera = camera;

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    if (data.m_Camera.clearFlags == CameraClearFlags.Skybox) { context.renderContext.DrawSkybox(data.m_Camera); }
                });

                return passData;
            }
        }
    }
}
