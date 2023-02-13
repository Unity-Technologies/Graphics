using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    // This tests is a variation of test 105.
    // It illustrates that ScriptableRenderer.ExecuteRenderPass will use the camera clearFlag if the camera target is one of the renderTargets in the MRT setup
    public sealed class Test106Renderer : ScriptableRenderer
    {
        RTHandle m_CameraColor;
        RTHandle m_CameraDepth;

        OutputColorsToMRTsRenderPass m_ColorsToMrtsPass;
        RTHandle[] m_ColorToMrtOutputs; // outputs of render pass "OutputColorsToMRTs"

        CopyToViewportRenderPass[] m_CopyToViewportPasses;
        Rect m_Viewport = new Rect(660, 200, 580, 320); // viewport to copy the results into

        FinalBlitPass m_FinalBlitPass;

        public Test106Renderer(Test106RendererData data) : base(data)
        {
            Material colorToMrtMaterial = CoreUtils.CreateEngineMaterial(data.shaders.colorToMrtPS);
            m_ColorsToMrtsPass = new OutputColorsToMRTsRenderPass(colorToMrtMaterial);

            m_ColorToMrtOutputs = new RTHandle[2];
            // m_ColorToMrtOutputs[0] = RTHandles.Alloc(m_ColorsToMrtsPass.destDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_ColorToMrtOutput0");
            m_ColorToMrtOutputs[1] = RTHandles.Alloc(m_ColorsToMrtsPass.destDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_ColorToMrtOutput1");

            Material copyToViewportMaterial = CoreUtils.CreateEngineMaterial(data.shaders.copyToViewportPS);
            m_CopyToViewportPasses = new CopyToViewportRenderPass[2];
            //m_CopyToViewportPasses[0] = new CopyToViewportRenderPass(copyToViewportMaterial);
            m_CopyToViewportPasses[1] = new CopyToViewportRenderPass(copyToViewportMaterial);

            Material blitMaterial = CoreUtils.CreateEngineMaterial(data.shaders.blitPS);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering, blitMaterial, null);
        }

        string m_profilerTag = "Test 106 Renderer";

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_profilerTag);

            int width = renderingData.cameraData.cameraTargetDescriptor.width;
            int height = renderingData.cameraData.cameraTargetDescriptor.height;

            var colorDescriptor = new RenderTextureDescriptor(width, height);
            var depthDescriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.Depth, 16);
            RenderingUtils.ReAllocateIfNeeded(ref m_CameraColor, colorDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraColor");
            RenderingUtils.ReAllocateIfNeeded(ref m_CameraDepth, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepth");

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            ConfigureCameraTarget(m_CameraColor, m_CameraDepth);


            // 1) Render different colors to the MRT outputs (render a blue quad to output#0 and a red quad to output#1)
            m_ColorToMrtOutputs[0] = m_CameraColor;
            m_ColorsToMrtsPass.Setup(ref renderingData, cameraColorTargetHandle, m_ColorToMrtOutputs);
            EnqueuePass(m_ColorsToMrtsPass);
            // Notice that the renderPass clearColor (yellow) is applied for output#1
            // Notice that the camera     clearColor (green)  is applied for output#0 (because output#0 is the camera target)


            // 2) Copy results to the camera target

            // layout (margin/blit/margin/..)
            // x: <-0.04-><-0.44-><-0.04-><-0.44-><-0.04->
            // y: <-0.25-><-0.50-><-0.25->

            //m_Viewport.x = 0.04f * width;
            m_Viewport.width = 0.44f * width;
            m_Viewport.y = 0.25f * height;
            m_Viewport.height = 0.50f * height;

            //m_CopyToViewportPasses[0].Setup(m_ColorToMrtOutputs[0].Identifier(), m_CameraColor, m_Viewport);
            //EnqueuePass(m_CopyToViewportPasses[0]);

            m_Viewport.x = (0.04f + 0.44f + 0.04f) * width;
            m_CopyToViewportPasses[1].Setup(m_ColorToMrtOutputs[1], m_CameraColor, m_Viewport);
            EnqueuePass(m_CopyToViewportPasses[1]);


            // 3) Final blit to the backbuffer
            m_FinalBlitPass.Setup(renderingData.cameraData.cameraTargetDescriptor, m_CameraColor);
            EnqueuePass(m_FinalBlitPass);
        }

        protected override void Dispose(bool disposing)
        {
            // m_ColorToMrtOutputs[0]?.Release();
            m_ColorToMrtOutputs[1]?.Release();
            m_CameraColor?.Release();
            m_CameraDepth?.Release();
            m_FinalBlitPass.Dispose();
        }
    }
}
