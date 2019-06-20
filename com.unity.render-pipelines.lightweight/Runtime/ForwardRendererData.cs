namespace UnityEngine.Experimental.Rendering.LWRP
{
    //[CreateAssetMenu()]
    public class ForwardRendererData : IRendererData
    {
        [SerializeField] Shader m_BlitShader = null;
        [SerializeField] Shader m_CopyDepthShader = null;
        [SerializeField] Shader m_ScreenSpaceShadowShader = null;
        [SerializeField] Shader m_SamplingShader = null;

        public override IRendererSetup Create()
        {
            return new ForwardRendererSetup(this);
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
    }
}
