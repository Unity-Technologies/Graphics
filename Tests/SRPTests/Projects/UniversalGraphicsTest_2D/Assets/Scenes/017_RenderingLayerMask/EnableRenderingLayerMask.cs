using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EnableRenderingLayerMask : MonoBehaviour
{
    void OnEnable()
    {
        var universalAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        universalAsset.useRenderingLayers = true;
    }

    void OnDisable()
    {
        var universalAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        universalAsset.useRenderingLayers = false;
    }
}
