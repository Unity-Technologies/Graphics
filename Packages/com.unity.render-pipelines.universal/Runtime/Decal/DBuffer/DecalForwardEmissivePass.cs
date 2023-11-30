using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalDrawFowardEmissiveSystem : DecalDrawSystem
    {
        public DecalDrawFowardEmissiveSystem(DecalEntityManager entityManager) : base("DecalDrawFowardEmissiveSystem.Execute", entityManager) { }
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexEmissive;
    }

    internal class DecalForwardEmissivePass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private List<ShaderTagId> m_ShaderTagIdList;
        private DecalDrawFowardEmissiveSystem m_DrawSystem;
        private PassData m_PassData;

        public DecalForwardEmissivePass(DecalDrawFowardEmissiveSystem drawSystem)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_DrawSystem = drawSystem;
            m_ProfilingSampler = new ProfilingSampler("Decal Forward Emissive Render");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalMeshForwardEmissive));
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalProjectorForwardEmissive));

            m_PassData = new PassData();
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            InitPassData(ref m_PassData);

            UniversalRenderingData universalRenderingData = renderingData.frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = renderingData.frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = renderingData.frameData.Get<UniversalLightData>();

            var param = InitRendererListParams(universalRenderingData, cameraData, lightData);

            var rendererList = context.CreateRendererList(ref param);
            using (new ProfilingScope(universalRenderingData.commandBuffer, m_ProfilingSampler))
            {
                ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(universalRenderingData.commandBuffer), m_PassData, rendererList);
            }
        }

        private class PassData
        {
            internal DecalDrawFowardEmissiveSystem drawSystem;

            internal RendererListHandle rendererList;
        }

        private void InitPassData(ref PassData passData)
        {
            passData.drawSystem = m_DrawSystem;
        }

        private RendererListParams InitRendererListParams(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            SortingCriteria sortingCriteria = cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, cameraData, lightData, sortingCriteria);
            return new RendererListParams(renderingData.cullResults, drawingSettings, m_FilteringSettings);
        }

        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList)
        {
            passData.drawSystem.Execute(cmd);
            cmd.DrawRendererList(rendererList);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Decal Forward Emissive Pass", out var passData, m_ProfilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                InitPassData(ref passData);
                var param = InitRendererListParams(renderingData, cameraData, lightData);
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);

                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    ExecutePass(rgContext.cmd, data, data.rendererList);
                });
            }
        }
    }
}
