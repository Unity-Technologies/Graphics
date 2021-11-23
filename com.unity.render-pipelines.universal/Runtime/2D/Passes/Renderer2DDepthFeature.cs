using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class Renderer2DDepthFeature  : ScriptableRendererFeature
    {
        DepthOnlyPass m_DepthPrepass;
        RenderTargetHandle m_CameraDepthAttachment;

        public override void Create()
        {
            m_CameraDepthAttachment.Init("_CameraDepthAttachment");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrePasses, RenderQueueRange.opaque, -1);

            ref var cameraData = ref renderingData.cameraData;
            var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;

            m_DepthPrepass.Setup(cameraTargetDescriptor, m_CameraDepthAttachment);
            renderer.EnqueuePass(m_DepthPrepass);
        }
    }
}
