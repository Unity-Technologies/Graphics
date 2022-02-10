using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderer
    {
        public override void RecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.RecordRenderGraph(renderGraph, context, cmd, ref renderingData);
            Camera camera = renderingData.cameraData.camera;

            context.SetupCameraProperties(camera);

            TextureHandle backBuffer = renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget);
            TextureHandle depth = RenderGraphSkyboxTestPass.CreateDepthTexture(renderGraph, camera);



            m_RenderOpaqueForwardPass.Render(camera, renderGraph, backBuffer, depth, ref renderingData);

            RenderGraphSkyboxTestPass.PassData testPassData = RenderGraphSkyboxTestPass.Render(camera, renderGraph, backBuffer, depth);
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

        static public TextureHandle CreateDepthTexture(RenderGraph graph, Camera camera)
        {
            bool colorRT_sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);

            //Texture description
            TextureDesc colorRTDesc = new TextureDesc(camera.pixelWidth, camera.pixelHeight);
            colorRTDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Depth, colorRT_sRGB);
            colorRTDesc.depthBufferBits = DepthBits.Depth24;
            colorRTDesc.msaaSamples = MSAASamples.None;
            colorRTDesc.enableRandomWrite = false;
            colorRTDesc.clearBuffer = true;
            colorRTDesc.clearColor = Color.black;
            colorRTDesc.name = "Depth";

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
