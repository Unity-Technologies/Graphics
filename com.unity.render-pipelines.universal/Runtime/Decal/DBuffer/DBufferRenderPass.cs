using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class DecalDrawDBufferSystem : DecalDrawSystem
    {
        public DecalDrawDBufferSystem(DecalEntityManager entityManager) : base("DecalDrawIntoDBufferSystem.Execute", entityManager) { }
        protected override int GetPassIndex(DecalCachedChunk decalCachedChunk) => decalCachedChunk.passIndexDBuffer;
    }

    internal class DBufferRenderPass : ScriptableRenderPass
    {
        private static string[] s_DBufferNames = { "_DBufferTexture0", "_DBufferTexture1", "_DBufferTexture2", "_DBufferTexture3" };
        private static string s_DBufferDepthName = "DBufferDepth";

        private DecalDrawDBufferSystem m_DrawSystem;
        private DBufferSettings m_Settings;
        private Material m_DBufferClear;

        private FilteringSettings m_FilteringSettings;
        private List<ShaderTagId> m_ShaderTagIdList;
        private int m_DBufferCount;
        private ProfilingSampler m_ProfilingSampler;

        private RTHandle m_DBufferDepth;

        internal DeferredLights deferredLights { get; set; }
        private bool isDeferred => deferredLights != null;
        internal RTHandle[] dBufferColorHandles { get; private set; }

        internal RTHandle dBufferDepth => m_DBufferDepth;

        public DBufferRenderPass(Material dBufferClear, DBufferSettings settings, DecalDrawDBufferSystem drawSystem)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPrePasses + 1;
            ConfigureInput(ScriptableRenderPassInput.Normal); // Require depth

            m_DrawSystem = drawSystem;
            m_Settings = settings;
            m_DBufferClear = dBufferClear;
            m_ProfilingSampler = new ProfilingSampler("DBuffer Render");
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);

            m_ShaderTagIdList = new List<ShaderTagId>();
            m_ShaderTagIdList.Add(new ShaderTagId(DecalShaderPassNames.DBufferMesh));

            int dBufferCount = (int)settings.surfaceData + 1;
            dBufferColorHandles = new RTHandle[dBufferCount];
            m_DBufferCount = dBufferCount;
        }

        public void Dispose()
        {
            m_DBufferDepth?.Release();
            foreach (var handle in dBufferColorHandles)
                handle?.Release();
        }

        public void Setup(in CameraData cameraData)
        {
            // base
            {
                var desc = cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;

                RenderingUtils.ReAllocateIfNeeded(ref dBufferColorHandles[0], desc, name: s_DBufferNames[0]);
            }

            if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormal || m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
            {
                var desc = cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;

                RenderingUtils.ReAllocateIfNeeded(ref dBufferColorHandles[1], desc, name: s_DBufferNames[1]);
            }

            if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
            {
                var desc = cameraData.cameraTargetDescriptor;
                desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;

                RenderingUtils.ReAllocateIfNeeded(ref dBufferColorHandles[2], desc, name: s_DBufferNames[2]);
            }

            // depth
            RTHandle depthHandle;
            if (isDeferred)
            {
                depthHandle = cameraData.renderer.cameraDepthTargetHandle;
            }
            else
            {
                var depthDesc = cameraData.cameraTargetDescriptor;
                depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
                depthDesc.depthStencilFormat = cameraData.cameraTargetDescriptor.depthStencilFormat;
                depthDesc.msaaSamples = 1;

                RenderingUtils.ReAllocateIfNeeded(ref m_DBufferDepth, depthDesc, name: s_DBufferDepthName);
                depthHandle = m_DBufferDepth;
            }

            ConfigureTarget(dBufferColorHandles, depthHandle);
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

                cmd.SetGlobalTexture(dBufferColorHandles[0].name, dBufferColorHandles[0].nameID);
                if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormal || m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
                    cmd.SetGlobalTexture(dBufferColorHandles[1].name, dBufferColorHandles[1].nameID);
                if (m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS)
                    cmd.SetGlobalTexture(dBufferColorHandles[2].name, dBufferColorHandles[2].nameID);

                if (isDeferred)
                {
                    cmd.SetGlobalTexture("_CameraNormalsTexture", deferredLights.GbufferAttachmentIdentifiers[deferredLights.GBufferNormalSmoothnessIndex]);
                }
                else
                {
                    cmd.SetGlobalTexture(m_DBufferDepth.name, m_DBufferDepth.nameID);
                }

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT1, m_Settings.surfaceData == DecalSurfaceData.Albedo);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT2, m_Settings.surfaceData == DecalSurfaceData.AlbedoNormal);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT3, m_Settings.surfaceData == DecalSurfaceData.AlbedoNormalMAOS);

                // TODO: This should be replace with mrt clear once we support it
                // Clear render targets
                ClearDBuffers(cmd, renderingData.cameraData);

                // Split here allows clear to be executed before DrawRenderers
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                m_DrawSystem.Execute(cmd);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ClearDBuffers(CommandBuffer cmd, in CameraData cameraData)
        {
            // for alpha compositing, color is cleared to 0, alpha to 1
            // https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
            var clearSampleName = "Clear";
            cmd.BeginSample(clearSampleName);

            Vector4 scaleBias = new Vector4(1, 1, 0, 0);
            cmd.SetGlobalVector(ShaderPropertyId.scaleBias, scaleBias);
            if (cameraData.xr.enabled)
            {
                cmd.DrawProcedural(Matrix4x4.identity, m_DBufferClear, 0, MeshTopology.Quads, 4, 1, null);
            }
            else
            {
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity); // Prepare for manual blit
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_DBufferClear, 0, 0);
                cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);
            }

            cmd.EndSample(clearSampleName);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new System.ArgumentNullException("cmd");
            }

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT1, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT2, false);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.DBufferMRT3, false);
        }
    }
}
