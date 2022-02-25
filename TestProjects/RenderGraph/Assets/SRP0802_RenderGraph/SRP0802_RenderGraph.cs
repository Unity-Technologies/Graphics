using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

// PIPELINE MAIN --------------------------------------------------------------------------------------------
public partial class SRP0802_RenderGraph : RenderPipeline
{
    private RenderGraph graph = new RenderGraph("SRP0802_RenderGraphPass");

    public SRP0802_RenderGraph()
    {
        graph.RegisterDebug();
    }

    class Empty
    {
    };

    static TextureDesc CreateTestDesc(TextureDesc baseDesc, string name, GraphicsFormat format)
    {
        TextureDesc res = baseDesc;
        res.name = name;
        res.colorFormat = format;
        return res;
    }

    public bool enableTestGraph;


    static void CreateTestGraph(ScriptableRenderContext context)
    {
        RenderGraph graph = new RenderGraph("TestGraph");

        //Texture description
        TextureDesc baseDesc = new TextureDesc(1920, 1080);
        baseDesc.colorFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, false);
        baseDesc.depthBufferBits = 0;
        baseDesc.msaaSamples = MSAASamples.None;
        baseDesc.enableRandomWrite = false;
        baseDesc.clearBuffer = true;
        baseDesc.clearColor = Color.black;
        baseDesc.name = "Base";

        //Execute graph 
        CommandBuffer cmdRG = CommandBufferPool.Get("TestGraphExecCmd");
        RenderGraphParameters rgParams = new RenderGraphParameters()
        {
            executionName = "TestGraphExec",
            commandBuffer = cmdRG,
            scriptableRenderContext = context,
            currentFrameIndex = Time.frameCount
        };

        graph.nativePassCompile = true;
        using (graph.RecordAndExecute(rgParams))
        {
            var shadowMapAtlas = graph.CreateTexture(CreateTestDesc(baseDesc, "Shadow Map Atlas", GraphicsFormat.D32_SFloat_S8_UInt));
            using (var builder = graph.AddRasterRenderPass<Empty>("Shadow Maps", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTextureFragmentDepth(shadowMapAtlas, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
            }

            var depthStencil = graph.CreateTexture(CreateTestDesc(baseDesc, "DepthStencil", GraphicsFormat.D32_SFloat_S8_UInt));
            var normalBuffer = graph.CreateTexture(CreateTestDesc(baseDesc, "NormalBuffer", GraphicsFormat.R8G8B8A8_UNorm));

            using (var builder = graph.AddRasterRenderPass<Empty>("Depth Prepass", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTextureFragment(normalBuffer, 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTextureFragmentDepth(depthStencil, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
            }

            var motionVectorsBuffer = graph.CreateTexture(CreateTestDesc(baseDesc, "NormalBuffer", GraphicsFormat.R8G8B8A8_UNorm));
            using (var builder = graph.AddRasterRenderPass<Empty>("Motion Vectors", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTextureFragmentDepth(depthStencil, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTextureFragment(motionVectorsBuffer, 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
            }

            var depthBufferCopy = graph.CreateTexture(CreateTestDesc(baseDesc, "Depth Buffer Mip Chain", GraphicsFormat.R8G8B8A8_UNorm));
            using (var builder = graph.AddRasterRenderPass<Empty>("Copy Depth Buffer", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTexture(depthStencil, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTexture(depthBufferCopy, IBaseRenderGraphBuilder.AccessFlags.Write);
            }

            var aoPackedData = graph.CreateTexture(CreateTestDesc(baseDesc, "GTAO Data", GraphicsFormat.R8G8B8A8_UNorm));
            using (var builder = graph.AddRasterRenderPass<Empty>("GTAO Calcualtions", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTexture(depthBufferCopy, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTexture(normalBuffer, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTexture(aoPackedData, IBaseRenderGraphBuilder.AccessFlags.Write);
            }

            var DBuffer0 = graph.CreateTexture(CreateTestDesc(baseDesc, "Dbuffer 0", GraphicsFormat.R8G8B8A8_UNorm));
            var DBuffer1 = graph.CreateTexture(CreateTestDesc(baseDesc, "Dbuffer 1", GraphicsFormat.R8G8B8A8_UNorm));
            var DBuffer2 = graph.CreateTexture(CreateTestDesc(baseDesc, "Dbuffer 2", GraphicsFormat.R8G8B8A8_UNorm));
            using (var builder = graph.AddRasterRenderPass<Empty>("DBuffer Laydown", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTextureFragmentDepth(depthStencil, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTextureFragment(DBuffer0, 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTextureFragment(DBuffer1, 1, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTextureFragment(DBuffer2, 2, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
            }

            var diffuseLighting = graph.CreateTexture(CreateTestDesc(baseDesc, "Diffuse Lighting", GraphicsFormat.R8G8B8A8_UNorm));
            var cameraColor = graph.CreateTexture(CreateTestDesc(baseDesc, "Camera Color", GraphicsFormat.R8G8B8A8_UNorm));
            var vtFeedback = graph.CreateTexture(CreateTestDesc(baseDesc, "Vt Feedback", GraphicsFormat.R8G8B8A8_UNorm));

            using (var builder = graph.AddRasterRenderPass<Empty>("Forward Opaque", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTextureFragmentDepth(depthStencil, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTextureFragment(cameraColor, 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTextureFragment(diffuseLighting, 1, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTextureFragment(vtFeedback, 2, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTexture(DBuffer0, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTexture(DBuffer1, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTexture(DBuffer2, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTexture(aoPackedData, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTexture(shadowMapAtlas, IBaseRenderGraphBuilder.AccessFlags.Read);
            }

            using (var builder = graph.AddRasterRenderPass<Empty>("Sub Surface Scattering", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTextureFragmentDepth(depthStencil, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTextureFragment(cameraColor, 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTexture(diffuseLighting, IBaseRenderGraphBuilder.AccessFlags.Read);
            }

            using (var builder = graph.AddRasterRenderPass<Empty>("Render SKy", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTextureFragmentDepth(depthStencil, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTextureFragment(cameraColor, 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
            }

            using (var builder = graph.AddRasterRenderPass<Empty>("VT Feedback Downsample", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTexture(vtFeedback, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.AllowPassCulling(false);
            }

            var cameraColorMip = graph.CreateTexture(CreateTestDesc(baseDesc, "Camera Color", GraphicsFormat.R8G8B8A8_UNorm));
            using (var builder = graph.AddRasterRenderPass<Empty>("Color Mipchain", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTexture(cameraColor, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTexture(cameraColorMip, IBaseRenderGraphBuilder.AccessFlags.Write);
            }

            using (var builder = graph.AddRasterRenderPass<Empty>("Forward Transparent", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTextureFragmentDepth(depthStencil, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTextureFragment(cameraColor, 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTexture(shadowMapAtlas, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTexture(cameraColorMip, IBaseRenderGraphBuilder.AccessFlags.Read);
            }

            using (var builder = graph.AddRasterRenderPass<Empty>("Final Output", out var passData, new ProfilingSampler("Base Pass Profiler")))
            {
                builder.UseTexture(cameraColor, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.AllowPassCulling(false);
            }
        }
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context,cameras);

        if (enableTestGraph)
        {
            CreateTestGraph(context);
            enableTestGraph = false;
        }

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context,camera);

            //Culling
            ScriptableCullingParameters cullingParams;
            if (!camera.TryGetCullingParameters(out cullingParams)) continue;
            CullingResults cull = context.Cull(ref cullingParams);

            //Camera setup some builtin variables e.g. camera projection matrices etc
            context.SetupCameraProperties(camera);

            //Execute graph 
            CommandBuffer cmdRG = CommandBufferPool.Get("ExecuteRenderGraph");
            RenderGraphParameters rgParams = new RenderGraphParameters()
            {
                executionName = "SRP0802_RenderGraph_Execute",
                commandBuffer = cmdRG,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount
            };

            using (graph.RecordAndExecute(rgParams))
            {
                SRP0802_BasePassData basePassData = Render_SRP0802_BasePass(camera,graph,cull); //BasePass
                Render_SRP0802_AddPass(graph,basePassData.m_Albedo,basePassData.m_Emission); //AddPass
            }

            context.ExecuteCommandBuffer(cmdRG);
            CommandBufferPool.Release(cmdRG);
            
            //Submit camera rendering
            context.Submit();
            EndCameraRendering(context,camera);
        }
        
        graph.EndFrame();
        EndFrameRendering(context,cameras);
    }

    protected override void Dispose(bool disposing)
    {
        graph.Cleanup();
        graph = null;
    }
}
