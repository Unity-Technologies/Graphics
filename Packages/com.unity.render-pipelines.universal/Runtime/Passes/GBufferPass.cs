using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
        static readonly int s_CameraNormalsTextureID = Shader.PropertyToID("_CameraNormalsTexture");
        static ShaderTagId s_ShaderTagLit = new ShaderTagId("Lit");
        static ShaderTagId s_ShaderTagSimpleLit = new ShaderTagId("SimpleLit");
        static ShaderTagId s_ShaderTagUnlit = new ShaderTagId("Unlit");
        static ShaderTagId s_ShaderTagUniversalGBuffer = new ShaderTagId("UniversalGBuffer");
        static ShaderTagId s_ShaderTagUniversalMaterialType = new ShaderTagId("UniversalMaterialType");

        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render GBuffer");

        DeferredLights m_DeferredLights;

        ShaderTagId[] m_ShaderTagValues;
        RenderStateBlock[] m_RenderStateBlocks;

        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;

        public GBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference, DeferredLights deferredLights)
        {
            base.profilingSampler = new ProfilingSampler(nameof(GBufferPass));
            base.renderPassEvent = evt;

            m_DeferredLights = deferredLights;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            m_RenderStateBlock.stencilState = stencilState;
            m_RenderStateBlock.stencilReference = stencilReference;
            m_RenderStateBlock.mask = RenderStateMask.Stencil;

            m_ShaderTagValues = new ShaderTagId[4];
            m_ShaderTagValues[0] = s_ShaderTagLit;
            m_ShaderTagValues[1] = s_ShaderTagSimpleLit;
            m_ShaderTagValues[2] = s_ShaderTagUnlit;
            m_ShaderTagValues[3] = new ShaderTagId(); // Special catch all case for materials where UniversalMaterialType is not defined or the tag value doesn't match anything we know.

            m_RenderStateBlocks = new RenderStateBlock[4];
            m_RenderStateBlocks[0] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialLit);
            m_RenderStateBlocks[1] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialSimpleLit);
            m_RenderStateBlocks[2] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialUnlit);
            m_RenderStateBlocks[3] = m_RenderStateBlocks[0];
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTargetHandle[] gbufferAttachments = m_DeferredLights.GbufferAttachments;

            if (cmd != null)
            {
                // Create and declare the render targets used in the pass
                for (int i = 0; i < gbufferAttachments.Length; ++i)
                {
                    // Lighting buffer has already been declared with line ConfigureCameraTarget(m_ActiveCameraColorAttachment.Identifier(), ...) in DeferredRenderer.Setup
                    if (i == m_DeferredLights.GBufferLightingIndex)
                        continue;

                    // Normal buffer may have already been created if there was a depthNormal prepass before.
                    // DepthNormal prepass is needed for forward-only materials when SSAO is generated between gbuffer and deferred lighting pass.
                    if (i == m_DeferredLights.GBufferNormalSmoothnessIndex && m_DeferredLights.HasNormalPrepass)
                    {
                        if (m_DeferredLights.UseRenderPass)
                            m_DeferredLights.DeferredInputIsTransient[i] = false;
                        continue;
                    }

                    // No need to setup temporaryRTs if we are using input attachments as they will be Memoryless
                    if (m_DeferredLights.UseRenderPass && i != m_DeferredLights.GBufferShadowMask && i != m_DeferredLights.GBufferRenderingLayers && (i != m_DeferredLights.GbufferDepthIndex && !m_DeferredLights.HasDepthPrepass))
                        continue;

                    RenderTextureDescriptor gbufferSlice = cameraTextureDescriptor;
                    gbufferSlice.depthBufferBits = 0; // make sure no depth surface is actually created
                    gbufferSlice.stencilFormat = GraphicsFormat.None;
                    gbufferSlice.graphicsFormat = m_DeferredLights.GetGBufferFormat(i);
                    cmd.GetTemporaryRT(m_DeferredLights.GbufferAttachments[i].id, gbufferSlice);
                }
            }

            ConfigureTarget(m_DeferredLights.GbufferAttachmentIdentifiers, m_DeferredLights.DepthAttachmentIdentifier, m_DeferredLights.GbufferFormats);

            // We must explicitely specify we don't want any clear to avoid unwanted side-effects.
            // ScriptableRenderer will implicitely force a clear the first time the camera color/depth targets are bound.
            ConfigureClear(ClearFlag.None, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer gbufferCommands = CommandBufferPool.Get();
            using (new ProfilingScope(gbufferCommands, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(gbufferCommands);
                gbufferCommands.Clear();

                // User can stack several scriptable renderers during rendering but deferred renderer should only lit pixels added by this gbuffer pass.
                // If we detect we are in such case (camera is in overlay mode), we clear the highest bits of stencil we have control of and use them to
                // mark what pixel to shade during deferred pass. Gbuffer will always mark pixels using their material types.
                if (m_DeferredLights.IsOverlay)
                {
                    m_DeferredLights.ClearStencilPartial(gbufferCommands);
                    context.ExecuteCommandBuffer(gbufferCommands);
                    gbufferCommands.Clear();
                }

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                ShaderTagId lightModeTag = s_ShaderTagUniversalGBuffer;
                DrawingSettings drawingSettings = CreateDrawingSettings(lightModeTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                ShaderTagId universalMaterialTypeTag = s_ShaderTagUniversalMaterialType;

                NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(m_ShaderTagValues, Allocator.Temp);
                NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(m_RenderStateBlocks, Allocator.Temp);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, universalMaterialTypeTag, false, tagValues, stateBlocks);

                tagValues.Dispose();
                stateBlocks.Dispose();

                // Render objects that did not match any shader pass with error shader
                RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, m_FilteringSettings, SortingCriteria.None);

                // If any sub-system needs camera normal texture, make it available.
                gbufferCommands.SetGlobalTexture(s_CameraNormalsTextureID, m_DeferredLights.GbufferAttachmentIdentifiers[m_DeferredLights.GBufferNormalSmoothnessIndex]);
            }

            context.ExecuteCommandBuffer(gbufferCommands);
            CommandBufferPool.Release(gbufferCommands);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            RenderTargetHandle[] gbufferAttachments = m_DeferredLights.GbufferAttachments;

            for (int i = 0; i < gbufferAttachments.Length; ++i)
            {
                if (i == m_DeferredLights.GBufferLightingIndex)
                    continue;

                if (i == m_DeferredLights.GBufferNormalSmoothnessIndex && m_DeferredLights.HasNormalPrepass)
                    continue;

                cmd.ReleaseTemporaryRT(gbufferAttachments[i].id);
            }
        }
    }
}
