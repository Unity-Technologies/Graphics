using UnityEngine;

[ExecuteInEditMode]
public class enableUseViewFrustumForShadowCasterCull : MonoBehaviour
{
    [SerializeField]
    bool m_UseViewFrustumForShadowCasterCull;

    Light m_light;
    void Start()
    {
        m_light = GetComponent<Light>();
        m_light.useViewFrustumForShadowCasterCull = m_UseViewFrustumForShadowCasterCull;
    }

    void Update()
    {
        m_light.useViewFrustumForShadowCasterCull = m_UseViewFrustumForShadowCasterCull;
    }
}
