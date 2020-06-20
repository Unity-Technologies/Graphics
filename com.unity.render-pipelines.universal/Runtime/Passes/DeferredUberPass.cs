using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.Internal
{
    internal class DeferredUberPass : ScriptableRenderPass
    {
        enum SubPassType {
            GBufferPass = 0
        };

        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("GBuffer & Lighting");
        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;

        RenderTargetHandle m_ColorAttachment;
        RenderTargetHandle m_DepthAttachment;
        DeferredLights m_DeferredLights;

        ComputeShader m_GBufferInitCS;

        ShaderTagId[] m_ShaderTagIds = { 
            new ShaderTagId("UniversalGBuffer"),
        };

        const int tileWidth = 16;
        const int tileHeight = 16;

        public DeferredUberPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, DeferredLights deferredLights, ComputeShader GBufferInitCS)
        {
            base.renderPassEvent = evt;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_DeferredLights = deferredLights;
            m_GBufferInitCS = GBufferInitCS;
        }

        public void Setup(ref RenderingData renderingData, RenderTargetHandle colorAttachment, RenderTargetHandle depthAttachment)
        {
            m_ColorAttachment = colorAttachment;
            m_DepthAttachment = depthAttachment;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(m_ColorAttachment.Identifier(), m_DepthAttachment.Identifier());
        }

        public override void ConfigureTileParameters(ScriptableRenderContext context)
        {
            // It seems that half will be converted to float on metal platform
            // TODO: figure out why
            int threadGroupMemoryLength = 0;
            int imageBlockSampleLength = 40;
            context.SetTileParams(tileWidth, tileHeight, threadGroupMemoryLength, imageBlockSampleLength);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Combined GBuffer & Lighting");
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cmd.EnableShaderKeyword("METAL2_ENABLED");

                if (m_DeferredLights.accurateGbufferNormals)
                    cmd.EnableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
                else
                    cmd.DisableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);

                cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);
                // Note: a special case might be required if(renderingData.cameraData.isStereoEnabled) - see reference in ScreenSpaceShadowResolvePass.Execute

                cmd.DispatchComputePerTile(m_GBufferInitCS, 0, tileWidth, tileHeight, 1);

                context.ExecuteCommandBuffer(cmd); // send the cmd to the scriptableRenderContext - this should be done *before* calling scriptableRenderContext.DrawRenderers
                cmd.Clear();
                
                DrawingSettings drawingSettings = CreateDrawingSettings(
                    m_ShaderTagIds[(int)(SubPassType.GBufferPass)], 
                    ref renderingData, 
                    renderingData.cameraData.defaultOpaqueSortFlags
                );

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                if (cameraData.isStereoEnabled)
                {
                    context.StartMultiEye(camera, eyeIndex);
                }

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings/*, ref m_RenderStateBlock*/);

                m_DeferredLights.ExecuteDeferredPass(context, ref renderingData, cmd);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
