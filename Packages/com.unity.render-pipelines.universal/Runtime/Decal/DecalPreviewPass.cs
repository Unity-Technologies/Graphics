using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalPreviewPass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private List<ShaderTagId> m_ShaderTagIdList;
        private ProfilingSampler m_ProfilingSampler;

        private PassData m_PassData;

        public DecalPreviewPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_ProfilingSampler = new ProfilingSampler("Decal Preview Render");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalScreenSpaceMesh));

            m_PassData = new PassData();
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            UniversalRenderingData universalRenderingData = renderingData.frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = renderingData.frameData.Get<UniversalLightData>();


            SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, universalRenderingData, cameraData, lightData, sortingCriteria);
            var param = new RendererListParams(universalRenderingData.cullResults, drawingSettings, m_FilteringSettings);
            var rendererList = context.CreateRendererList(ref param);
            using (new ProfilingScope(universalRenderingData.commandBuffer, m_ProfilingSampler))
            {
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(universalRenderingData.commandBuffer), m_PassData, rendererList);
            }
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList)
        {
            {
                cmd.DrawRendererList(rendererList);
            }
        }

        private class PassData
        {
            internal RendererListHandle rendererList;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Decal Preview Pass", out var passData, m_ProfilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;

                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, cameraData, lightData, sortingCriteria);
                var param = new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    ExecutePass(rgContext.cmd, data, data.rendererList);
                });
            }
        }
    }
}
