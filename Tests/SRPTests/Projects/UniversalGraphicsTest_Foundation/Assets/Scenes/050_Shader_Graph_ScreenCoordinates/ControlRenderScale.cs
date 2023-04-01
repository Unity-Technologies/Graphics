using UnityEngine;
using UnityEngine.Rendering.Universal;


public class ControlRenderScale : MonoBehaviour
{
    public UniversalRenderPipelineAsset urpAsset;
    public float playRenderScale = 0.25f;
    float originalScale = 1.0f;

    private void OnEnable()
    {
        if (urpAsset != null)
        {
            originalScale = urpAsset.renderScale;
            urpAsset.renderScale = playRenderScale;
        }
    }

    private void OnDisable()
    {
        if (urpAsset != null)
        {
            urpAsset.renderScale = originalScale;
        }
    }
}
