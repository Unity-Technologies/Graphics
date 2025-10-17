using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class HistoryVisualizer : ScriptableRendererFeature
{
    public enum HistoryToVisualize
    {
        RawColor,
        RawDepth
    }

    public Shader visualizeShader;
    public HistoryToVisualize historyToVisualize;
    public RenderPassEvent renderPassEvent;

    private Material m_Material;

    class CustomRenderPass : ScriptableRenderPass
    {
        private const string kHistoryShaderName = "_History";
        internal Material m_Material;
        internal HistoryToVisualize m_HistoryToVisualize;

        public CustomRenderPass( HistoryToVisualize historyVisualization)
        {
            m_HistoryToVisualize = historyVisualization;
        }

        public void Setup(Material material)
        {
            m_Material = material;
        }

        public void RequestHistory(UniversalCameraHistory historyManager)
        {
            switch (m_HistoryToVisualize)
            {
                case HistoryToVisualize.RawDepth:
                    historyManager.RequestAccess<RawDepthHistory>();
                    break;

                case HistoryToVisualize.RawColor:
                default:
                    historyManager.RequestAccess<RawColorHistory>();
                    break;
            }
        }

        RTHandle GetHistorySourceTexture(UniversalCameraHistory historyManager, int multipassId)
        {
            RTHandle historyTexture = null;
            switch (m_HistoryToVisualize)
            {
                case HistoryToVisualize.RawDepth:
                {
                    var history = historyManager.GetHistoryForRead<RawDepthHistory>();
                    // Need to get the previous texture as the current one is not yet written.
                    historyTexture = history?.GetPreviousTexture(multipassId);
                    break;
                }
                case HistoryToVisualize.RawColor:
                default:
                {
                    var history = historyManager.GetHistoryForRead<RawColorHistory>();
                    historyTexture = history?.GetPreviousTexture(multipassId);
                    break;
                }
            }

            return historyTexture;
        }
        
        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        private class PassData
        {
            internal Material material;
            internal TextureHandle historyTexture;
            internal int multipassId;
            internal Rect renderViewport;
            internal Rect cameraViewport;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null)
                return;

            var cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData == null)
                return;

            UniversalRenderer renderer = cameraData.renderer as UniversalRenderer;
            if (renderer == null)
                return;

            if (cameraData.historyManager == null)
                return;

            RequestHistory(cameraData.historyManager);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Test History visualizer.", out var passData))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                passData.material = m_Material;

                int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
            multipassId = cameraData.xr.multipassId;
#endif

                Rect r = cameraData.camera.pixelRect;
                passData.cameraViewport = r;
                r.width /= 2;
                r.height /= 2;
                passData.renderViewport = r;

                RTHandle historyTexture = GetHistorySourceTexture(cameraData.historyManager, multipassId);
                passData.historyTexture = renderGraph.ImportTexture(historyTexture);

                builder.UseTexture(passData.historyTexture);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    data.material.SetTexture(kHistoryShaderName, data.historyTexture);
                    context.cmd.SetViewport(data.renderViewport);
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1);
                    context.cmd.SetViewport(data.cameraViewport);
                });
            }
        }
    }

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(historyToVisualize);
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = renderPassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!GetMaterials())
        {
            Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, name);
            return;
        }

        m_ScriptablePass.Setup(m_Material);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }

    private bool GetMaterials()
    {
        if (m_Material == null && visualizeShader != null)
            m_Material = CoreUtils.CreateEngineMaterial(visualizeShader);
        return m_Material != null;
    }
}
