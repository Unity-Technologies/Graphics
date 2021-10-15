using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ModifyShadowSettings : MonoBehaviour
{
    ModifiedShadowSettings overrideShadowSettings = new ModifiedShadowSettings
    {
        maxDist = 1000,
        depthBias = 0.1f,
        normalBias = 0.1f,
    };

    UniversalRenderPipelineAsset urpAsset;
    ModifiedShadowSettings saveShadowSettings;
    struct ModifiedShadowSettings
    {
        public float maxDist;
        public float depthBias;
        public float normalBias;
        public void SaveSettings(UniversalRenderPipelineAsset asset)
        {
            maxDist = asset.shadowDistance;
            depthBias = asset.shadowDepthBias;
            normalBias = asset.shadowDepthBias;
        }
    }

    UniversalRenderPipelineAsset WriteSettings(ModifiedShadowSettings settings)
    {
        urpAsset.shadowDistance = settings.maxDist;
        urpAsset.shadowDepthBias = settings.depthBias;
        urpAsset.shadowNormalBias = settings.normalBias;
        return urpAsset;
    }

    void Awake()
    {
        urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        saveShadowSettings.SaveSettings(urpAsset);
        WriteSettings(overrideShadowSettings);
    }

    void OnDisable()
    {
        WriteSettings(saveShadowSettings);
    }
}
