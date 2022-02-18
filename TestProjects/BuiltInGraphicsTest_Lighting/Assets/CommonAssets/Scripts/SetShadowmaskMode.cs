using UnityEngine;

public class SetShadowmaskMode : MonoBehaviour
{
    public ShadowmaskMode shadowmaskMode;
    private ShadowmaskMode oldShadowmaskMode;

    private void Start()
    {
        oldShadowmaskMode = QualitySettings.shadowmaskMode;
        QualitySettings.shadowmaskMode = shadowmaskMode;
    }

    private void OnDestroy()
    {
        QualitySettings.shadowmaskMode = oldShadowmaskMode;
    }
}
