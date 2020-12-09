using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    [HideRendererFeatureName]
    /// <summary>
    /// Targets _MainLightShadowmapTexture
    /// </summary>
    class MainLightShadowCasterFeature : ScriptableRendererFeature
    {
        MainLightShadowCasterPass m_MainLightShadowCasterPass;
        /// <inheritdoc/>
        public override void Create()
        {
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_MainLightShadowCasterPass.Setup(ref renderingData))
            {
                renderer.EnqueuePass(m_MainLightShadowCasterPass);
            }
        }
    }
}
