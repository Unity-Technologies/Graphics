using System;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Write stencil bits for stencil-based cross-fade LOD.
    /// </summary>
    sealed class StencilCrossFadeRenderPass
    {
        private Material[] m_StencilDitherMaskSeedMaterials;
        private readonly int _StencilDitherPattern = Shader.PropertyToID("_StencilDitherPattern");
        private readonly int _StencilRefDitherMask = Shader.PropertyToID("_StencilRefDitherMask");
        private readonly int _StencilWriteDitherMask = Shader.PropertyToID("_StencilWriteDitherMask");

        private readonly ProfilingSampler m_ProfilingSampler;

        internal StencilCrossFadeRenderPass(Shader shader)
        {
            m_StencilDitherMaskSeedMaterials = new Material[3];
            m_ProfilingSampler = new ProfilingSampler("StencilDitherMaskSeed");

            int[] stencilRefs = {
                    (int)UniversalRendererStencilRef.CrossFadeStencilRef_0,
                    (int)UniversalRendererStencilRef.CrossFadeStencilRef_1,
                    (int)UniversalRendererStencilRef.CrossFadeStencilRef_All };
            int writeMask = (int)UniversalRendererStencilRef.CrossFadeStencilRef_All;
            Debug.Assert(writeMask < 0x100); // 8 bits for stencil

            for (int i = 0; i < m_StencilDitherMaskSeedMaterials.Length; ++i)
            {
                m_StencilDitherMaskSeedMaterials[i] = CoreUtils.CreateEngineMaterial(shader);
                m_StencilDitherMaskSeedMaterials[i].SetInteger(_StencilDitherPattern, i + 1);
                m_StencilDitherMaskSeedMaterials[i].SetFloat(_StencilWriteDitherMask, (float)writeMask);
                m_StencilDitherMaskSeedMaterials[i].SetFloat(_StencilRefDitherMask, (float)stencilRefs[i]);
            }
        }
        public void Dispose()
        {
            foreach (var m in m_StencilDitherMaskSeedMaterials)
                CoreUtils.Destroy(m);
            m_StencilDitherMaskSeedMaterials = null;
        }

        /// <summary>
        /// Shared pass data for render graph
        /// </summary>
        private class PassData
        {
            public TextureHandle depthTarget;
            public Material[] stencilDitherMaskSeedMaterials;
        }

        /// <summary>
        /// Set render graph pass
        /// </summary>
        public void Render(RenderGraph renderGraph, ScriptableRenderContext context, TextureHandle depthTarget)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Prepare Cross Fade Stencil", out var passData, m_ProfilingSampler))
            {
                builder.SetRenderAttachmentDepth(depthTarget, AccessFlags.Write);
                passData.stencilDitherMaskSeedMaterials = m_StencilDitherMaskSeedMaterials;
                passData.depthTarget = depthTarget;

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data.depthTarget, data.stencilDitherMaskSeedMaterials);
                });
            }
        }

        private static void ExecutePass(RasterCommandBuffer cmd, RTHandle depthTarget, Material[] stencilDitherMaskSeedMaterials)
        {
            Vector2Int scaledViewportSize = depthTarget.GetScaledSize(depthTarget.rtHandleProperties.currentViewportSize);
            Rect viewport = new Rect(0.0f, 0.0f, scaledViewportSize.x, scaledViewportSize.y);
            cmd.SetViewport(viewport);

            // render one stencil value in each pass because SV_StencilRef is not fully supported.
            for (int i = 0; i < stencilDitherMaskSeedMaterials.Length; ++i)
            {
                cmd.DrawProcedural(Matrix4x4.identity, stencilDitherMaskSeedMaterials[i], 0, MeshTopology.Triangles, 3, 1);
            }
        }
    }
}
