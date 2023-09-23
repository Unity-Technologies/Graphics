using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[ExecuteInEditMode]
public class SelectHDRMode : MonoBehaviour
{
    public Toggle button = null;
    public UniversalRenderPipelineAsset rpAsset = null;

    private void Start()
    {
        button?.onValueChanged.AddListener(UpdateState);
    }

    [ContextMenu("Enable HDR Rendering")]
    public void Enable()
    {
        UpdateState(true);
    }

    [ContextMenu("Disable HDR Rendering")]
    public void Disable()
    {
        UpdateState(false);
    }

    void UpdateState(bool enabled)
    {
        if (rpAsset != null)
        {
            rpAsset.supportsHDR = enabled;

            if (HDROutputSettings.main.available)
            {
                HDROutputSettings.main.RequestHDRModeChange(enabled);
            }
        }
    }

    private void Update()
    {
        button?.SetIsOnWithoutNotify(rpAsset.supportsHDR && HDROutputSettings.main.available && HDROutputSettings.main.active);
    }
}
