using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
        RenderTargetHandle[] m_ColorAttachments;
        RenderTargetHandle m_DepthBufferAttachment;

        DeferredLights m_DeferredLights;

        ShaderTagId m_ShaderTagId = new ShaderTagId("UniversalGBuffer");
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render GBuffer");

        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;

        public GBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference, DeferredLights deferredLights)
        {
            base.renderPassEvent = evt;
            m_DeferredLights = deferredLights;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            if (stencilState.enabled)
            {
                m_RenderStateBlock.stencilReference = stencilReference;
                m_RenderStateBlock.mask = RenderStateMask.Stencil;
                m_RenderStateBlock.stencilState = stencilState;
            }
        }

        public void Setup(AttachmentDescriptor[] gBufferAttachments, DeferredLights deferredLights)
        {
            gBufferAttachments[0] = m_DeferredLights.GBufferDescriptors[deferredLights.GBufferAlbedoIndex];
            gBufferAttachments[1] = m_DeferredLights.GBufferDescriptors[deferredLights.GBufferSpecularMetallicIndex];
            gBufferAttachments[2] = m_DeferredLights.GBufferDescriptors[deferredLights.GBufferNormalSmoothnessIndex];
            gBufferAttachments[3] = m_DeferredLights.GBufferDescriptors[deferredLights.GBufferLightingIndex];
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            gBufferAttachments[4] = m_DeferredLights.GBufferDescriptors[deferredLights.GBufferAdditionalDepthIndex];
#endif
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer gbufferCommands = CommandBufferPool.Get("Render GBuffer");
            using (new ProfilingScope(gbufferCommands, m_ProfilingSampler))
            {
                if (m_DeferredLights.AccurateGbufferNormals)
                    gbufferCommands.EnableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
                else
                    gbufferCommands.DisableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);

                gbufferCommands.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);

                context.ExecuteCommandBuffer(gbufferCommands); // send the gbufferCommands to the scriptableRenderContext - this should be done *before* calling scriptableRenderContext.DrawRenderers
                gbufferCommands.Clear();

                DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings/*, ref m_RenderStateBlock*/);
            }
            context.ExecuteCommandBuffer(gbufferCommands);
            CommandBufferPool.Release(gbufferCommands);
        }
    }
}
