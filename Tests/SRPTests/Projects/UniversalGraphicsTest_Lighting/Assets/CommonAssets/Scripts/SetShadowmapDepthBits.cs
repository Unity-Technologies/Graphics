using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SetShadowmapDepthBits : MonoBehaviour
{
    public DepthBits mainLightShadowmapDepthBits = DepthBits.Depth32;
    public DepthBits additionalLightsShadowmapDepthBits = DepthBits.Depth32;

    UniversalRenderPipelineAsset m_Asset;
    DepthBits m_PrevMainLightShadowmapDepthBits;
    DepthBits m_PrevAdditionalLightsShadowmapDepthBits;

    void OnEnable()
    {
        m_Asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        m_PrevMainLightShadowmapDepthBits = m_Asset.mainLightShadowmapDepthBits;
        m_PrevAdditionalLightsShadowmapDepthBits = m_Asset.additionalLightsShadowmapDepthBits;
        m_Asset.mainLightShadowmapDepthBits = mainLightShadowmapDepthBits;
        m_Asset.additionalLightsShadowmapDepthBits = additionalLightsShadowmapDepthBits;
    }

    void OnDisable()
    {
        if (m_Asset == null)
            return;

        m_Asset.mainLightShadowmapDepthBits = m_PrevMainLightShadowmapDepthBits;
        m_Asset.additionalLightsShadowmapDepthBits = m_PrevAdditionalLightsShadowmapDepthBits;
        m_Asset = null;
    }
}
