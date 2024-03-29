using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChangeShadowCascadeSettings : MonoBehaviour
{
    public int shadowCascadeCount;
    int prevShadowCascadeCount;

    void Awake()
    {
        UniversalRenderPipelineAsset asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        prevShadowCascadeCount = asset.shadowCascadeCount;
        asset.shadowCascadeCount = shadowCascadeCount;
    }

    void OnDestroy()
    {
        UniversalRenderPipelineAsset asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        asset.shadowCascadeCount = prevShadowCascadeCount;
    }
}
