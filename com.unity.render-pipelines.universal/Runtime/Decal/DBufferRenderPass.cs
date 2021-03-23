using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public class DecalDrawIntoDBufferSystem : DecalDrawSystem
    {
        public DecalDrawIntoDBufferSystem(DecalEntityManager entityManager) : base("DecalDrawIntoDBufferSystem.Execute", entityManager) {}
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexDBuffer;
    }

    public class DBufferRenderPass : ScriptableRenderPass
    {
        private static string[] s_DBufferNames = { "_DBufferTexture0", "_DBufferTexture1", "_DBufferTexture2", "_DBufferTexture3" };
        private static string s_DBufferDepthName = "DBufferDepth";
        private static GraphicsFormat[] s_DBufferFormats = { GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.R8G8_UNorm };

        private DecalDrawIntoDBufferSystem m_DecalDrawIntoDBufferSystem;
        private DBufferSettings m_Settings;
        private Material m_DBufferClear;

        private FilteringSettings m_FilteringSettings;
        private List<ShaderTagId> m_ShaderTagIdList;
        private int m_DBufferCount;
        private ProfilingSampler m_ClearSampler;
        private ProfilingSampler m_ProfilingSampler;

        private ProfilingSampler m_RenderIntoDBufferSmpler; // TODO: Remove

        public DBufferRenderPass(string profilerTag, Material dBufferClear, DBufferSettings settings, DecalDrawIntoDBufferSystem decalDrawIntoDBufferSystem)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 1;
            ConfigureInput(ScriptableRenderPassInput.Depth); // Require depth

            m_DecalDrawIntoDBufferSystem = decalDrawIntoDBufferSystem;
            m_Settings = settings;
            m_DBufferClear = dBufferClear;
            m_RenderIntoDBufferSmpler = new ProfilingSampler("V1.DecalSystem.RenderIntoDBuffer"); // TODO: Remove
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            m_ClearSampler = new ProfilingSampler("DBuffer Setup");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId(DecalUtilities.GetDecalPassName(DecalUtilities.MaterialDecalPass.DBufferMesh)));
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            int dBufferCount = 0;
            // base
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = s_DBufferFormats[dBufferCount];
                desc.depthBufferBits = 0;

                cmd.GetTemporaryRT(Shader.PropertyToID(s_DBufferNames[dBufferCount]), desc);
                dBufferCount++;
            }

            if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormal || m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMask)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = s_DBufferFormats[dBufferCount];
                desc.depthBufferBits = 0;

                cmd.GetTemporaryRT(Shader.PropertyToID(s_DBufferNames[dBufferCount]), desc);
                dBufferCount++;
            }

            if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMask)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = s_DBufferFormats[dBufferCount];
                desc.depthBufferBits = 0;

                cmd.GetTemporaryRT(Shader.PropertyToID(s_DBufferNames[dBufferCount]), desc);
                dBufferCount++;
            }

            // depth
            {
                var depthDesc = renderingData.cameraData.cameraTargetDescriptor;
                depthDesc.graphicsFormat = GraphicsFormat.DepthAuto;
                depthDesc.depthBufferBits = 24;

                cmd.GetTemporaryRT(Shader.PropertyToID(s_DBufferDepthName), depthDesc);
            }

            m_DBufferCount = dBufferCount;

            // TODO: Remove
            Color clearColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
            Color clearColorNormal = new Color(0.5f, 0.5f, 0.5f, 1.0f); // for normals 0.5 is neutral
            Color clearColorAOSBlend = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            var colorAttachments = new RenderTargetIdentifier[dBufferCount];
            for (int dbufferIndex = 0; dbufferIndex < dBufferCount; ++dbufferIndex)
                colorAttachments[dbufferIndex] = new RenderTargetIdentifier(s_DBufferNames[dbufferIndex]);

            //ConfigureTarget(colorAttachments, new RenderTargetIdentifier("_CameraDepthTexture"));
            ConfigureTarget(colorAttachments, new RenderTargetIdentifier(s_DBufferDepthName));

            // for alpha compositing, color is cleared to 0, alpha to 1
            // https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
            //ConfigureClear(ClearFlag.Color, new Color(0f, 0f, 0f, 1));
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

                CoreUtils.SetKeyword(cmd, "DECALS_1RT", m_Settings.surfaceData == DecalSurfaceData.Albedo);
                CoreUtils.SetKeyword(cmd, "DECALS_2RT", m_Settings.surfaceData == DecalSurfaceData.AlbedoNormal);
                CoreUtils.SetKeyword(cmd, "DECALS_3RT", m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMask);

                // TODO: This should be replace with mrt clear once we support it
                // Clear render targets
                var clearSampleName = "Clear";
                cmd.BeginSample(clearSampleName);
                cmd.DrawProcedural(Matrix4x4.identity, m_DBufferClear, 0, MeshTopology.Quads, 4, 1, null);
                cmd.EndSample(clearSampleName);

                // Split here allows clear to be executed before DrawRenderers
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                float width = renderingData.cameraData.pixelWidth;
                float height = renderingData.cameraData.pixelHeight;
                cmd.SetGlobalVector("_ScreenSize", new Vector4(width, height, 1f / width, 1f / height));

                // TODO: Remove
                if (m_DecalDrawIntoDBufferSystem == null)
                {
                    using (new ProfilingScope(cmd, m_RenderIntoDBufferSmpler))
                    {
                        DecalSystem.instance.RenderIntoDBuffer(cmd);
                    }
                }

                m_DecalDrawIntoDBufferSystem?.Execute(cmd);

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

            CoreUtils.SetKeyword(cmd, "DECALS_1RT", false);
            CoreUtils.SetKeyword(cmd, "DECALS_2RT", false);
            CoreUtils.SetKeyword(cmd, "DECALS_3RT", false);

            for (int dbufferIndex = 0; dbufferIndex < m_DBufferCount; ++dbufferIndex)
            {
                cmd.ReleaseTemporaryRT(Shader.PropertyToID(s_DBufferNames[dbufferIndex]));
            }

            cmd.ReleaseTemporaryRT(Shader.PropertyToID(s_DBufferDepthName));
        }
    }
}
