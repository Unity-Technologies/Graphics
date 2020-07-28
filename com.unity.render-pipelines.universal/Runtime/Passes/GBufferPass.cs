using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
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
            base.renderPassEvent = evt;

            m_DeferredLights = deferredLights;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            m_RenderStateBlock.stencilReference = stencilReference;
            m_RenderStateBlock.mask = RenderStateMask.Stencil;
            if (stencilState.enabled)
            {
                m_RenderStateBlock.stencilState = stencilState;
            }
            else
            {
                m_RenderStateBlock.stencilState = new StencilState(
                    true,
                    0, 0,
                    CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep,
                    CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep
                );
            }

            m_ShaderTagValues = new ShaderTagId[4];
            m_ShaderTagValues[0] = s_ShaderTagLit;
            m_ShaderTagValues[1] = s_ShaderTagSimpleLit;
            m_ShaderTagValues[2] = s_ShaderTagUnlit;
            m_ShaderTagValues[3] = new ShaderTagId(); // Special catch all case for materials where UniversalMaterialType is not defined or the tag value doesn't match anything we know.

            m_RenderStateBlocks = new RenderStateBlock[4];
            m_RenderStateBlocks[0] = OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialLit);
            m_RenderStateBlocks[1] = OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialSimpleLit);
            m_RenderStateBlocks[2] = OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialUnlit);
            m_RenderStateBlocks[3] = m_RenderStateBlocks[0];
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTargetHandle[] gbufferAttachments = m_DeferredLights.GbufferAttachments;

                // Create and declare the render targets used in the pass
            for (int i = 0; i < gbufferAttachments.Length; ++i)
            {
                // Lighting buffer has already been declared with line ConfigureCameraTarget(m_ActiveCameraColorAttachment.Identifier(), ...) in DeferredRenderer.Setup
                if (i != m_DeferredLights.GBufferLightingIndex)
                {
                    RenderTextureDescriptor gbufferSlice = cameraTextureDescriptor;
                    gbufferSlice.graphicsFormat = m_DeferredLights.GetGBufferFormat(i);
                    cmd.GetTemporaryRT(m_DeferredLights.GbufferAttachments[i].id, gbufferSlice);
                }
            }

            ConfigureTarget(m_DeferredLights.GbufferAttachmentIdentifiers, m_DeferredLights.DepthAttachmentIdentifier);

            // If depth-prepass exists, do not clear depth here or we will lose it.
            // Lighting buffer is cleared independently regardless of what we ask for here.
            ConfigureClear(m_DeferredLights.HasDepthPrepass ? ClearFlag.None : ClearFlag.Depth, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer gbufferCommands = CommandBufferPool.Get();
            using (new ProfilingScope(gbufferCommands, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(gbufferCommands);
                gbufferCommands.Clear();

                if (m_DeferredLights.AccurateGbufferNormals)
                    gbufferCommands.EnableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
                else
                    gbufferCommands.DisableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);

                context.ExecuteCommandBuffer(gbufferCommands); // send the gbufferCommands to the scriptableRenderContext - this should be done *before* calling scriptableRenderContext.DrawRenderers
                gbufferCommands.Clear();

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
            }
            context.ExecuteCommandBuffer(gbufferCommands);
            CommandBufferPool.Release(gbufferCommands);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            RenderTargetHandle[] gbufferAttachments = m_DeferredLights.GbufferAttachments;

            for (int i = 0; i < gbufferAttachments.Length; ++i)
                if (i != m_DeferredLights.GBufferLightingIndex)
                    cmd.ReleaseTemporaryRT(gbufferAttachments[i].id);
        }

        RenderStateBlock OverwriteStencil(RenderStateBlock block, int stencilWriteMask, int stencilRef)
        {
            StencilState s = block.stencilState;
            CompareFunction funcFront = s.compareFunctionFront != CompareFunction.Disabled ? s.compareFunctionFront : CompareFunction.Always;
            CompareFunction funcBack = s.compareFunctionBack != CompareFunction.Disabled ? s.compareFunctionBack : CompareFunction.Always;
            StencilOp passFront = s.passOperationFront;
            StencilOp failFront = s.failOperationFront;
            StencilOp zfailFront = s.zFailOperationFront;
            StencilOp passBack = s.passOperationBack;
            StencilOp failBack = s.failOperationBack;
            StencilOp zfailBack = s.zFailOperationBack;

            block.mask |= RenderStateMask.Stencil;
            block.stencilReference |= stencilRef;
            block.stencilState = new StencilState(
                true,
                (byte)(s.readMask & 0x0F), (byte)(s.writeMask | stencilWriteMask),
                funcFront, passFront, failFront, zfailFront,
                funcBack, passBack, failBack, zfailBack
            );

            return block;
        }
    }
}
