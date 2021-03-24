using System.Collections.Generic;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class DecalDrawGBufferSystem : DecalDrawSystem
    {
        public DecalDrawGBufferSystem(DecalEntityManager entityManager) : base("DecalDrawGBufferSystem.Execute", entityManager) {}
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexGBuffer;
    }

    public class DecalGBufferRenderPass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private List<ShaderTagId> m_ShaderTagIdList;
        private DecalDrawGBufferSystem m_DrawSystem;
        private DecalScreenSpaceSettings m_Settings;
        DeferredLights m_DeferredLights;

        public DecalGBufferRenderPass(string profilerTag, DecalScreenSpaceSettings settings, DecalDrawGBufferSystem decalDrawFowardEmissiveSystem)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;
            //ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_DrawSystem = decalDrawFowardEmissiveSystem;
            m_Settings = settings;
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId("DecalGBufferMesh"));
        }

        internal void Setup(DeferredLights deferredLights)
        {
            m_DeferredLights = deferredLights;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(m_DeferredLights.GbufferAttachmentIdentifiers, m_DeferredLights.DepthAttachmentIdentifier);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                float width = renderingData.cameraData.pixelWidth;
                float height = renderingData.cameraData.pixelHeight;
                cmd.SetGlobalVector("_ScreenSize", new Vector4(width, height, 1f / width, 1f / height));

                // Split here allows clear to be executed before DrawRenderers
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                m_DrawSystem?.Execute(cmd);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
