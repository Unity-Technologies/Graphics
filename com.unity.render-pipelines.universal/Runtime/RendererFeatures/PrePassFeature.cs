using System;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class PrePassFeature : ScriptableRendererFeature
    {
        [SerializeField] RenderPassEvent Event = RenderPassEvent.BeforeRenderingPrePasses;
        [SerializeField] PrePassMode Mode = PrePassMode.Depth;

        // Private
        private DepthOnlyPass m_DepthPrepass;
        private DepthNormalOnlyPass m_DepthNormalPrepass;
        private RenderTargetHandle m_DepthTexture;
        private RenderTargetHandle m_NormalsTexture;

        // Enums
        enum PrePassMode
        {
            Depth,
            DepthNormals
        }

        /// <inheritdoc/>
        public override void Create()
        {
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            ForwardRendererData data = ScriptableObject.CreateInstance<ForwardRendererData>();
            data.ReloadAllNullProperties();

            m_DepthPrepass = new DepthOnlyPass(Event, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_DepthNormalPrepass = new DepthNormalOnlyPass(Event, RenderQueueRange.opaque, data.opaqueLayerMask);

            m_DepthTexture.Init("_CameraDepthTexture");
            m_NormalsTexture.Init("_CameraNormalsTexture");
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            switch (Mode)
            {
                case PrePassMode.Depth:
                    m_DepthPrepass.Setup(cameraTargetDescriptor, m_DepthTexture);
                    renderer.EnqueuePass(m_DepthPrepass);
                    break;
                case PrePassMode.DepthNormals:
                    m_DepthNormalPrepass.Setup(cameraTargetDescriptor, m_DepthTexture, m_NormalsTexture);
                    renderer.EnqueuePass(m_DepthNormalPrepass);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

