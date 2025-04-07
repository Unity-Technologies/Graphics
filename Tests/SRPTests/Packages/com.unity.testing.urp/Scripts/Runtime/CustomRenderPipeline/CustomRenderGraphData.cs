using System;

namespace UnityEngine.Rendering.Universal
{
    //[CreateAssetMenu()]
    public class CustomRenderGraphData : ScriptableRendererData
    {
        protected override ScriptableRenderer Create()
        {
            return new CustomRenderer(this);
        }

        internal override bool stripShadowsOffVariants
        {
            get => m_StripShadowsOffVariants;
            set => m_StripShadowsOffVariants = value;
        }

        internal override bool stripAdditionalLightOffVariants
        {
            get => m_StripAdditionalLightOffVariants;
            set => m_StripAdditionalLightOffVariants = value;
        }

        [NonSerialized]
        bool m_StripShadowsOffVariants = true;
        [NonSerialized]
        bool m_StripAdditionalLightOffVariants = true;
    }
}
