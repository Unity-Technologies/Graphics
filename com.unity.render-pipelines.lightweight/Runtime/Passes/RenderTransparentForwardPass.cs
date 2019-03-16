using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Render all transparent forward objects into the given color and depth target
    ///
    /// You can use this pass to render objects that have a material and/or shader
    /// with the pass names LightweightForward or SRPDefaultUnlit. The pass only renders
    /// objects in the rendering queue range of Transparent objects.
    /// </summary>
    internal class RenderTransparentForwardPass : ScriptableRenderPass
    {
        FilteringSettings m_FilteringSettings;
        const string m_ProfilerTag = "Render Transparents";
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        public RenderTransparentForwardPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            renderPassEvent = evt;

            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingSample(cmd, m_ProfilerTag))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Camera camera = renderingData.cameraData.camera;
                var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);

                // Render objects that did not match any shader pass with error shader
                RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, m_FilteringSettings, SortingCriteria.None);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
