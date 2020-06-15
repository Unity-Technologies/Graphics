using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChangeShadowCascadeSettings : MonoBehaviour
{
    public int cascadeShadowSplitCount;
    int prevCascadeShadowSplitCount;

    void Awake()
    {
        UniversalRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        prevCascadeShadowSplitCount = asset.cascadeShadowSplitCount;
        asset.cascadeShadowSplitCount = cascadeShadowSplitCount;
    }

    void OnDestroy()
    {
        UniversalRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        asset.cascadeShadowSplitCount = prevCascadeShadowSplitCount;
    }
}
