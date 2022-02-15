using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(UniversalAdditionalCameraData))]
public class ConfigureScreenCoordOverride : MonoBehaviour
{
    static readonly Vector4 k_IdentityScaleBias = new Vector4(1, 1, 0, 0);

    [SerializeField]
    bool m_UseScreenCoordOverride;

    [SerializeField]
    Vector4 m_ScreenCoordScaleBias;

    [SerializeField]
    bool m_UseScreenSizeOverride;

    [SerializeField]
    Vector2 m_ScreenSizeOverride;

    void Reset()
    {
        m_UseScreenCoordOverride = false;
        m_ScreenCoordScaleBias = k_IdentityScaleBias;
        m_UseScreenSizeOverride = false;
        m_ScreenSizeOverride = AppendReciprocal(new Vector2(Screen.width, Screen.height));
        Configure();
    }

    void OnEnable()
    {
        Configure();
    }

    void OnValidate()
    {
        Configure();
    }

    void OnDisable()
    {
        var additionalCameraData = GetComponent<UniversalAdditionalCameraData>();
        additionalCameraData.useScreenCoordOverride = false;
    }

    void Configure()
    {
        var additionalCameraData = GetComponent<UniversalAdditionalCameraData>();
        additionalCameraData.useScreenCoordOverride = m_UseScreenCoordOverride;
        additionalCameraData.screenCoordScaleBias = m_ScreenCoordScaleBias;

        if (m_UseScreenSizeOverride)
        {
            additionalCameraData.screenSizeOverride = AppendReciprocal(m_ScreenSizeOverride);
        }
        else
        {
            additionalCameraData.screenSizeOverride = AppendReciprocal(new Vector2(Screen.width, Screen.height));
        }
    }

    static Vector4 AppendReciprocal(Vector2 value)
    {
        return new Vector4(value.x, value.y, 1.0f / value.x, 1.0f / value.y);
    }
}
