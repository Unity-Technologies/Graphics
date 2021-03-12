using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public class DecalForwardEmissivePass : ScriptableRenderPass
    {
        public static readonly RenderQueueRange k_RenderQueue_AllOpaque = new RenderQueueRange { lowerBound = (int)RenderQueue.Geometry, upperBound = (int)RenderQueue.GeometryLast };

        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private List<ShaderTagId> m_ShaderTagIdList;
        public DecalDrawFowardEmissiveSystem m_DrawSystem;

        public DecalForwardEmissivePass(string profilerTag, DecalDrawFowardEmissiveSystem decalDrawFowardEmissiveSystem)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_DrawSystem = decalDrawFowardEmissiveSystem;
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            m_FilteringSettings = new FilteringSettings(k_RenderQueue_AllOpaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId(DecalUtilities.GetDecalPassName(DecalUtilities.MaterialDecalPass.DecalMeshForwardEmissive)));
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
