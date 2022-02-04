using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class Renderer2DDepthFeature  : ScriptableRendererFeature
    {
        DepthOnlyPass m_DepthPrepass;
        private CopyDepthPass m_CopyDepthPass;
        RenderTargetHandle m_CameraDepthTexture;
        RenderTargetHandle m_CameraDepthAttachment;
        [Reload("Shaders/Utils/CopyDepth.shader")]
        public Shader copyDepthPS;
        Material m_CopyDepthMaterial = null;

        public override void Create()
        {
            m_CameraDepthTexture.Init("_CameraDepthTexture");
            m_CameraDepthAttachment.Init("_CameraDepthAttachment");
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(copyDepthPS);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, -1);

            ref var cameraData = ref renderingData.cameraData;
            var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;

            var renderer2DData = ((Renderer2D)renderer).GetRenderer2DData();
            cameraTargetDescriptor.width = (int)(cameraTargetDescriptor.width * renderer2DData.lightRenderTextureScale);
            cameraTargetDescriptor.height = (int)(cameraTargetDescriptor.height * renderer2DData.lightRenderTextureScale);

            m_DepthPrepass.Setup(cameraTargetDescriptor, m_CameraDepthTexture);
            renderer.EnqueuePass(m_DepthPrepass);

            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingPrePasses, m_CopyDepthMaterial);
            m_CopyDepthPass.Setup(m_CameraDepthTexture, m_CameraDepthAttachment);
            m_CopyDepthPass.AllocateRT = false;
            renderer.EnqueuePass(m_CopyDepthPass);
        }
    }
}
