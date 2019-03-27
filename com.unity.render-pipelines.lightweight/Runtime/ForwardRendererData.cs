namespace UnityEngine.Rendering.LWRP
{
    [CreateAssetMenu(fileName = "Custom Forward Renderer", menuName = "Rendering/Lightweight Render Pipeline/Forward Renderer", order = CoreUtils.assetCreateMenuPriority1)]
    public class ForwardRendererData : ScriptableRendererData
    {
        [SerializeField] Shader m_BlitShader = null;
        [SerializeField] Shader m_CopyDepthShader = null;
        [SerializeField] Shader m_ScreenSpaceShadowShader = null;
        [SerializeField] Shader m_SamplingShader = null;

        [SerializeField] Shader m_StopNaNShader = null;
        [SerializeField] Shader m_PaniniProjectionShader = null;
        [SerializeField] Shader m_LutBuilderLdrShader = null;
        [SerializeField] Shader m_LutBuilderHdrShader = null;
        [SerializeField] Shader m_BloomShader = null;
        [SerializeField] Shader m_UberPostShader = null;

        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;

        protected override ScriptableRenderer Create() => new ForwardRenderer(this);

        internal Shader blitShader => m_BlitShader;
        internal Shader copyDepthShader => m_CopyDepthShader;
        internal Shader screenSpaceShadowShader => m_ScreenSpaceShadowShader;
        internal Shader samplingShader => m_SamplingShader;

        internal Shader stopNaNShader => m_StopNaNShader;
        internal Shader paniniProjectionShader => m_PaniniProjectionShader;
        internal Shader lutBuilderLdrShader => m_LutBuilderLdrShader;
        internal Shader lutBuilderHdrShader => m_LutBuilderHdrShader;
        internal Shader bloomShader => m_BloomShader;
        internal Shader uberPostShader => m_UberPostShader;

        internal LayerMask opaqueLayerMask => m_OpaqueLayerMask;
        public LayerMask transparentLayerMask => m_TransparentLayerMask;
    }
}
