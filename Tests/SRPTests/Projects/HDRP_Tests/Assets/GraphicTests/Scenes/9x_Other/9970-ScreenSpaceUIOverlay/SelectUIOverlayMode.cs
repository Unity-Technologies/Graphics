using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.HighDefinition;

[ExecuteInEditMode]
public class SelectUIOverlayMode : MonoBehaviour
{
    public Toggle button = null;
    private void Start()
    {
        button?.onValueChanged.AddListener(UpdateState);
    }

    [ContextMenu("Enable Overlay Triggering from SRP")]
    public void Enable()
    {
        UpdateState(true);
    }

    [ContextMenu("Disable Overlay Triggering from SRP")]
    public void Disable()
    {
        UpdateState(false);
    }

    void UpdateState(bool enabled)
    {
        UnityEngine.Rendering.SupportedRenderingFeatures.active.rendersUIOverlay = enabled;
    }

    private void Update()
    {
        button?.SetIsOnWithoutNotify(UnityEngine.Rendering.SupportedRenderingFeatures.active.rendersUIOverlay);
    }
}
