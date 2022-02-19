using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class Renderer2DDepthFeature  : ScriptableRendererFeature
    {
        DepthOnlyPass m_DepthPrepass;
        private CopyDepthPass m_CopyDepthPass;
        RTHandle m_CameraDepthTexture;
        [Reload("Shaders/Utils/CopyDepth.shader")]
        public Shader copyDepthPS;
        Material m_CopyDepthMaterial = null;

        public override void Create()
        {
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

            RenderingUtils.ReAllocateIfNeeded(ref m_CameraDepthTexture, cameraTargetDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthTexture");
            m_DepthPrepass.Setup(cameraTargetDescriptor, m_CameraDepthTexture);
            m_DepthPrepass.sortingCriteriaOverride = SortingCriteria.QuantizedFrontToBack |
                                                     SortingCriteria.RenderQueue |
                                                     SortingCriteria.OptimizeStateChanges;
            renderer.EnqueuePass(m_DepthPrepass);

            var depthTextureHandle = ((Renderer2D)renderer).GetDepthHandle(ref cameraData);
            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingPrePasses, m_CopyDepthMaterial);
            m_CopyDepthPass.Setup(m_CameraDepthTexture, depthTextureHandle);
            renderer.EnqueuePass(m_CopyDepthPass);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
                m_CameraDepthTexture?.Release();
        }
    }
}
