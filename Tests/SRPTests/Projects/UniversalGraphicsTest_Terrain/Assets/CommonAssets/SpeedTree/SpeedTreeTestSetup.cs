using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class SpeedTreeTestSetup : MonoBehaviour
{
    void OnEnable()
    {
        if (GraphicsSettings.defaultRenderPipeline != null)
        {
            var lwAsset = (UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline;
            shadowDistance = new QualityProfileShadowDistance(lwAsset, 1000.0f);
            lodBias = QualitySettings.lodBias;
            QualitySettings.lodBias = 1.0f;
        }
    }

    void OnDisable()
    {
        if (GraphicsSettings.defaultRenderPipeline != null)
        {
            shadowDistance?.Dispose();
            shadowDistance = null;
            QualitySettings.lodBias = lodBias;
        }
    }

    QualityProfileShadowDistance shadowDistance;
    float lodBias = 1.0f;
}
