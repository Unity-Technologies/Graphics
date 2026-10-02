using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SetLightFalloffMode : MonoBehaviour
{
    public LightFalloffMode lightFalloffMode = LightFalloffMode.Quadratic;

    LightFalloffMode prevLightFalloffMode;

#if UNITY_EDITOR
    void OnEnable()
    {
        var lightFalloffSettings = GraphicsSettings.GetRenderPipelineSettings<URPLightFalloffSettings>();
        prevLightFalloffMode = lightFalloffSettings.lightFalloffMode;
        lightFalloffSettings.lightFalloffMode = lightFalloffMode;
    }

    void OnDisable()
    {
        var lightFalloffSettings = GraphicsSettings.GetRenderPipelineSettings<URPLightFalloffSettings>();
        lightFalloffSettings.lightFalloffMode = prevLightFalloffMode;
    }
#endif
}
