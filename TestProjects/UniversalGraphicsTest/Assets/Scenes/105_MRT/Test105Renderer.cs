using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    // This test illustrates that ScriptableRenderer.ExecuteRenderPass will also use the clearFlag defined as renderPass.clearFlag when the renderPass target is an MRT setup.
    // See also test 106.
    public sealed class Test105Renderer : ScriptableRenderer
    {
        RenderTargetHandle m_CameraColor;
        RenderTargetHandle m_CameraDepth;

        OutputColorsToMRTsRenderPass m_ColorsToMrtsPass;
        RenderTargetHandle[] m_ColorToMrtOutputs; // outputs of render pass "OutputColorsToMRTs"

        CopyToViewportRenderPass[] m_CopyToViewportPasses;
        Rect m_Viewport = new Rect(660, 200, 580, 320); // viewport to copy the results into

        FinalBlitPass m_FinalBlitPass;

        Material m_ColorToMrtMaterial;
        Material m_CopyToViewportMaterial;
        Material m_BlitMaterial;

        string m_profilerTag = "Test 105 Renderer";

        public Test105Renderer(Test105RendererData data) : base(data)
        {
            m_CameraColor.Init("_CameraColor");
            m_CameraDepth.Init("_CameraDepth");

            m_ColorToMrtMaterial = CoreUtils.CreateEngineMaterial(data.shaders.colorToMrtPS);
            m_ColorsToMrtsPass = new OutputColorsToMRTsRenderPass(m_ColorToMrtMaterial);

            m_ColorToMrtOutputs = new RenderTargetHandle[2];
            m_ColorToMrtOutputs[0].Init("_ColorToMrtOutput0");
            m_ColorToMrtOutputs[1].Init("_ColorToMrtOutput1");

            m_CopyToViewportMaterial = CoreUtils.CreateEngineMaterial(data.shaders.copyToViewportPS);
            m_CopyToViewportPasses = new CopyToViewportRenderPass[2];
            m_CopyToViewportPasses[0] = new CopyToViewportRenderPass(m_CopyToViewportMaterial);
            m_CopyToViewportPasses[1] = new CopyToViewportRenderPass(m_CopyToViewportMaterial);

            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.shaders.blitPS);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering, m_BlitMaterial);
        }

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_profilerTag);

            int width = renderingData.cameraData.cameraTargetDescriptor.width;
            int height = renderingData.cameraData.cameraTargetDescriptor.height;

            cmd.GetTemporaryRT(m_CameraColor.id, width, height);
            cmd.GetTemporaryRT(m_CameraDepth.id, width, height, 16);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            ConfigureCameraTarget(m_CameraColor.Identifier(), m_CameraDepth.Identifier());


            // 1) Render different colors to the MRT outputs (render a blue quad to output#0 and a red quad to output#1)

          //m_ColorToMrtOutputs[0] = m_CameraColor;
            m_ColorsToMrtsPass.Setup(ref renderingData, cameraColorTarget, m_ColorToMrtOutputs);
            EnqueuePass(m_ColorsToMrtsPass);
            // Notice that the renderPass clearColor (yellow) is applied.



            // 2) Copy results to the camera target

            // layout (margin/blit/margin/..)
            // x: <-0.04-><-0.44-><-0.04-><-0.44-><-0.04->
            // y: <-0.25-><-0.50-><-0.25->

            m_Viewport.x = 0.04f * width;
            m_Viewport.width = 0.44f * width;
            m_Viewport.y = 0.25f * height;
            m_Viewport.height = 0.50f * height;

            m_CopyToViewportPasses[0].Setup(m_ColorToMrtOutputs[0].Identifier(), m_CameraColor, m_Viewport);
            EnqueuePass(m_CopyToViewportPasses[0]);

            m_Viewport.x = (0.04f + 0.44f + 0.04f) * width;
            m_CopyToViewportPasses[1].Setup(m_ColorToMrtOutputs[1].Identifier(), m_CameraColor, m_Viewport);
            EnqueuePass(m_CopyToViewportPasses[1]);


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

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_ColorToMrtMaterial);
            CoreUtils.Destroy(m_CopyToViewportMaterial);
            CoreUtils.Destroy(m_BlitMaterial);
        }
    }
}
