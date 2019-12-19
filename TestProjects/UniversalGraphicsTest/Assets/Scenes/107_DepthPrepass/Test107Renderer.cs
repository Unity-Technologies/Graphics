using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    // This test checks that depth buffer is not cleared by ScriptableRenderer.ExecuteRenderPass the second time it is bound in the frame.
    // Cubes in the scene use a SimpleLit shader with ZWrite=off - therefore they will not write to depth during the forward pass.
    // If the depth buffer is correct cubes will look like they intersect ; if the depthbuffer is incorrect cubes will appear one in front of the other.
    public sealed class Test107Renderer : ScriptableRenderer
    {
        StencilState m_DefaultStencilState;

        DepthOnlyPass m_DepthPrepass;
        DrawObjectsPass m_RenderOpaqueForwardPass;
        FinalBlitPass m_FinalBlitPass;

        RenderTargetHandle m_CameraColor;
        RenderTargetHandle m_CameraDepth;

        public Test107Renderer(Test107RendererData data) : base(data)
        {
            m_DefaultStencilState = new StencilState();

            m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrepasses, RenderQueueRange.opaque, -1 /*data.opaqueLayerMask*/);
            m_RenderOpaqueForwardPass = new DrawObjectsPass("Render Opaques", false, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, -1 /*data.opaqueLayerMask*/, m_DefaultStencilState, 0 /*stencilData.stencilReference*/);

            Material blitMaterial = CoreUtils.CreateEngineMaterial(data.shaders.blitPS);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering, blitMaterial);

            m_CameraColor.Init("_CameraColor");
            m_CameraDepth.Init("_CameraDepth");
        }

        string m_profilerTag = "Test 107 Renderer";

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_profilerTag);

            cmd.GetTemporaryRT(m_CameraColor.id, 1280, 720);
            cmd.GetTemporaryRT(m_CameraDepth.id, 1280, 720, 16);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            ConfigureCameraTarget(m_CameraColor.Identifier(), m_CameraDepth.Identifier());

            // 1) Depth pre-pass
            m_DepthPrepass.Setup(renderingData.cameraData.cameraTargetDescriptor, m_CameraDepth);
            EnqueuePass(m_DepthPrepass);

            // 2) Forward opaque
            EnqueuePass(m_RenderOpaqueForwardPass); // will render to renderingData.cameraData.camera

            // 3) Final blit to the backbuffer
            m_FinalBlitPass.Setup(renderingData.cameraData.cameraTargetDescriptor, m_CameraColor);
            EnqueuePass(m_FinalBlitPass);
        }

        /// <inheritdoc />
        public override void FinishRendering(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_CameraColor.id);
            cmd.ReleaseTemporaryRT(m_CameraDepth.id);
        }
    }
}
