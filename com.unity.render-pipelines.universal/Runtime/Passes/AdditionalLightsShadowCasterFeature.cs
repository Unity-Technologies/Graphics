using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    class AdditionalLightsShadowCasterFeature : ScriptableRendererFeature
    {
        AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        /// <inheritdoc/>
        public override void Create()
        {
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_AdditionalLightsShadowCasterPass.Setup(ref renderingData)) 
                renderer.EnqueuePass(m_AdditionalLightsShadowCasterPass);
        }
    }
}
