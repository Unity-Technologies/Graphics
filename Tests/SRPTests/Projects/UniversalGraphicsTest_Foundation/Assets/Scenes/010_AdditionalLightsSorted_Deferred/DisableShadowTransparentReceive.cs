using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DisableShadowTransparentReceive: MonoBehaviour
{
    [SerializeField] UniversalRendererData m_RendererData;
    bool m_PrevShadowTransparentReceiveState;

    void Start()
    {
        m_PrevShadowTransparentReceiveState = m_RendererData.shadowTransparentReceive;
        m_RendererData.shadowTransparentReceive = false;
    }

    void OnDestroy()
    {
        m_RendererData.shadowTransparentReceive = m_PrevShadowTransparentReceiveState;
    }
}
