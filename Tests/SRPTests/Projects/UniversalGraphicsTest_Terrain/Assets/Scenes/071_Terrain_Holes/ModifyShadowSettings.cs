using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ModifyShadowSettings : MonoBehaviour
{
    const float k_MaxShadowDistance = 1000;

    ModifiedShadowSettings overrideShadowSettings = new ModifiedShadowSettings
    {
        depthBias = 0.1f,
        normalBias = 0.1f,
    };

    UniversalRenderPipelineAsset urpAsset;
    ModifiedShadowSettings saveShadowSettings;
    QualityProfileShadowDistance shadowDistance;

    struct ModifiedShadowSettings
    {
        public float depthBias;
        public float normalBias;
        public void SaveSettings(UniversalRenderPipelineAsset asset)
        {
            depthBias = asset.shadowDepthBias;
            normalBias = asset.shadowNormalBias;
        }
    }

    UniversalRenderPipelineAsset WriteSettings(ModifiedShadowSettings settings)
    {
        urpAsset.shadowDepthBias = settings.depthBias;
        urpAsset.shadowNormalBias = settings.normalBias;
        return urpAsset;
    }

    void Awake()
    {
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        saveShadowSettings.SaveSettings(urpAsset);
        WriteSettings(overrideShadowSettings);
        shadowDistance = new QualityProfileShadowDistance(urpAsset, k_MaxShadowDistance);
    }

    void OnDisable()
    {
        WriteSettings(saveShadowSettings);
        shadowDistance?.Dispose();
        shadowDistance = null;
    }
}
