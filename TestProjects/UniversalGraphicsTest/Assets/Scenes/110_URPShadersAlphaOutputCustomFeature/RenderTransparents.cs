using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class RenderTransparents : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent myEvent = RenderPassEvent.BeforeRenderingTransparents;
        public LayerMask layerMask;
    }
    CustomRenderPass m_ScriptablePass;
    public Settings settings = new Settings();
    private CustomRenderPass pass;


    public sealed override void Create()
    {
        m_ScriptablePass = new CustomRenderPass(settings.myEvent, settings.layerMask);
    }

    public sealed override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ScriptablePass.Setup(settings.myEvent, settings.layerMask);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    class CustomRenderPass : ScriptableRenderPass
    {
        private FilteringSettings filter;
        private readonly List<ShaderTagId> shaderTagIds = new List<ShaderTagId>();
        private RenderStateBlock renderState;

        const string k_ProfilerTag = "110-URPAlphaOutputFeature";
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_ProfilerTag);

        public CustomRenderPass(RenderPassEvent renderPassEvent, int layerMask)
        {
            this.renderPassEvent = renderPassEvent;

            shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            renderState = new RenderStateBlock();
        }

        public void Setup(RenderPassEvent renderPassEvent, int layerMask)
        {
            this.renderPassEvent = renderPassEvent;
            filter = new FilteringSettings(RenderQueueRange.transparent, layerMask);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DrawingSettings glareDrawingSettings = CreateDrawingSettings(shaderTagIds, ref renderingData, SortingCriteria.CommonTransparent);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                context.DrawRenderers(renderingData.cullResults, ref glareDrawingSettings, ref filter, ref renderState);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }
}


