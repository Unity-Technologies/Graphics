using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChangeShadowCascadeSettings : MonoBehaviour
{
    public ShadowCascadesOption cascadeOption;
    ShadowCascadesOption prevCascadeOption;

    void Awake()
    {
        UniversalRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        prevCascadeOption = asset.shadowCascadeOption;
        asset.shadowCascadeOption = cascadeOption;
    }

    void OnDestroy()
    {
        UniversalRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
        asset.shadowCascadeOption = prevCascadeOption;
    }
}
