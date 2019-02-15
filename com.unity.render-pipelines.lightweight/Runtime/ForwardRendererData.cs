namespace UnityEngine.Rendering.LWRP
{
    //[CreateAssetMenu]
    public class ForwardRendererData : ScriptableRendererData
    {
        [SerializeField] Shader m_BlitShader = null;
        [SerializeField] Shader m_CopyDepthShader = null;
        [SerializeField] Shader m_ScreenSpaceShadowShader = null;
        [SerializeField] Shader m_SamplingShader = null;

        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;

        public override ScriptableRenderer Create()
        {
            return new ForwardRenderer(this);
        }

        public Shader blitShader
        {
            get => m_BlitShader;
            set => m_BlitShader = value;
        }

        public Shader copyDepthShader
        {
            get => m_CopyDepthShader;
            set => m_CopyDepthShader = value;
        }

        public Shader screenSpaceShadowShader
        {
            get => m_ScreenSpaceShadowShader;
            set => m_ScreenSpaceShadowShader = value;
        }

        public Shader samplingShader
        {
            get => m_SamplingShader;
            set => m_SamplingShader = value;
        }

        public LayerMask opaqueLayerMask
        {
            get => m_OpaqueLayerMask;
        }

        public LayerMask transparentLayerMask
        {
            get => m_TransparentLayerMask;
        }
    }
}
