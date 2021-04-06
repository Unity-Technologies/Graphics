using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    public class DecalDrawFowardEmissiveSystem : DecalDrawSystem
    {
        public DecalDrawFowardEmissiveSystem(DecalEntityManager entityManager) : base("DecalDrawFowardEmissiveSystem.Execute", entityManager) {}
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexEmissive;
    }

    public class DecalForwardEmissivePass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private List<ShaderTagId> m_ShaderTagIdList;
        private DecalDrawFowardEmissiveSystem m_DrawSystem;

        public DecalForwardEmissivePass(string profilerTag, DecalDrawFowardEmissiveSystem drawSystem)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_DrawSystem = drawSystem;
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalMeshForwardEmissive));
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

                m_DrawSystem?.Execute(cmd);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
