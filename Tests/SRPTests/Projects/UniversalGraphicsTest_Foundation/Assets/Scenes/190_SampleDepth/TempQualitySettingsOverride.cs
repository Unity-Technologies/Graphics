using UnityEngine;
using UnityEngine.Rendering;

// TODO GFXRPF-87: remove this when BB MSAA is fixed
// Temp workaround for UUM-15980:
public class TempQualitySettingsOverride : MonoBehaviour
{
    int OldMSAA;
    void Awake()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan &&
            SystemInfo.operatingSystem.Contains("Android"))
        {
            OldMSAA = QualitySettings.antiAliasing;
            QualitySettings.antiAliasing = 1;
        }
    }

    void OnDestroy()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan &&
            SystemInfo.operatingSystem.Contains("Android"))
        {
            QualitySettings.antiAliasing = OldMSAA;
        }
    }
}
