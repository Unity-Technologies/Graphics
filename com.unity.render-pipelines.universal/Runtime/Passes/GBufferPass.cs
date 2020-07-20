using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
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

            m_ShaderTagValues = new ShaderTagId[3];
            m_ShaderTagValues[0] = new ShaderTagId("Lit");
            m_ShaderTagValues[1] = new ShaderTagId("SimpleLit");
            m_ShaderTagValues[2] = new ShaderTagId("Unlit");

            m_RenderStateBlocks = new RenderStateBlock[3];
            m_RenderStateBlocks[0] = OverwriteStencil(m_RenderStateBlock, 96, 32);
            m_RenderStateBlocks[1] = OverwriteStencil(m_RenderStateBlock, 96, 64);
            m_RenderStateBlocks[2] = OverwriteStencil(m_RenderStateBlock, 96, 0);
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
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (m_DeferredLights.AccurateGbufferNormals)
                    gbufferCommands.EnableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
                else
                    gbufferCommands.DisableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);

                context.ExecuteCommandBuffer(gbufferCommands); // send the gbufferCommands to the scriptableRenderContext - this should be done *before* calling scriptableRenderContext.DrawRenderers
                gbufferCommands.Clear();

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                ShaderTagId lightModeTag = new ShaderTagId("UniversalGBuffer");
                DrawingSettings drawingSettings = CreateDrawingSettings(lightModeTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                ShaderTagId universalMaterialTypeTag = new ShaderTagId("UniversalMaterialType");

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

        RenderStateBlock OverwriteStencil(RenderStateBlock block, byte stencilWriteMask, int stencilRef)
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

            // Detect invalid parameter.
            if ((passFront != StencilOp.Replace && failFront != StencilOp.Replace && zfailFront != StencilOp.Replace)
             || (passBack != StencilOp.Replace && failBack != StencilOp.Replace && zfailBack != StencilOp.Replace))
                Debug.LogWarning("Stencil overrides for GBuffer pass will not write correct material types in stencil buffer");

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
