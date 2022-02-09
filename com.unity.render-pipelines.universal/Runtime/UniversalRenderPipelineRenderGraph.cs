using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline
    {
        static void RecordRenderGraph(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            var renderer = renderingData.cameraData.renderer;

            RenderGraphTestPass.PassData testPassData = RenderGraphTestPass.Render(camera, m_RenderGraph);

            renderer.RecordRenderGraph(context, cmd, ref renderingData);
        }

        static void RecordAndExecuteRenderGraph(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;

            RenderGraphParameters rgParams = new RenderGraphParameters()
            {
                executionName = camera.name,
                commandBuffer = cmd,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount,
            };

            using (m_RenderGraph.RecordAndExecute(rgParams))
            {
                RecordRenderGraph(context, cmd, ref renderingData);
            }
        }
    }



    class RenderGraphTestPass
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

        static private TextureHandle CreateDepthTexture(RenderGraph graph, Camera camera)
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

        static public PassData Render(Camera camera, RenderGraph graph)
        {
            using (var builder = graph.AddRenderPass<PassData>("Test Pass", out var passData, new ProfilingSampler("Test Pass Profiler")))
            {
                TextureHandle Albedo = graph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget); //CreateColorTexture(graph,camera,"Albedo");
                passData.m_Albedo = builder.UseColorBuffer(Albedo, 0);
                TextureHandle Depth = CreateDepthTexture(graph, camera);
                passData.m_Depth = builder.UseDepthBuffer(Depth, DepthAccess.Write);

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
