using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalDrawScreenSpaceSystem : DecalDrawSystem
    {
        public DecalDrawScreenSpaceSystem(DecalEntityManager entityManager) : base("DecalDrawScreenSpaceSystem.Execute", entityManager) {}
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexScreenSpace;
    }

    internal class ScreenSpaceDecalRenderPass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private List<ShaderTagId> m_ShaderTagIdList;
        private DecalDrawScreenSpaceSystem m_DrawSystem;
        private DecalScreenSpaceSettings m_Settings;

        public ScreenSpaceDecalRenderPass(DecalScreenSpaceSettings settings, DecalDrawScreenSpaceSystem decalDrawFowardEmissiveSystem)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques + 1;
            ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_DrawSystem = decalDrawFowardEmissiveSystem;
            m_Settings = settings;
            m_ProfilingSampler = new ProfilingSampler("Decal Screen Space Render");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();

            if (m_Settings.supportAdditionalLights)
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalScreenSpaceProjector));
            else
                m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DecalScreenSpaceMesh));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;// renderingData.cameraData.defaultOpaqueSortFlags;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_LOW", m_Settings.blend == DecalNormalBlend.NormalLow);
                CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_MEDIUM", m_Settings.blend == DecalNormalBlend.NormalMedium);
                CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_HIGH", m_Settings.blend == DecalNormalBlend.NormalHigh);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                m_DrawSystem?.Execute(cmd);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new System.ArgumentNullException("cmd");
            }

            CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_LOW", false);
            CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_MEDIUM", false);
            CoreUtils.SetKeyword(cmd, "DECALS_NORMAL_BLEND_HIGH", false);
        }
    }
}
