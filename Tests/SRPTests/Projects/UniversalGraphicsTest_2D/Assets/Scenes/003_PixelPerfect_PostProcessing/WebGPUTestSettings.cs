using UnityEngine;
using UnityEngine.Rendering;

public class WebGPUTestSettings : MonoBehaviour
{
    void Start()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU)
        {
            var settings = GetComponent<UniversalGraphicsTestSettings>();
            settings.ImageComparisonSettings.UseBackBuffer = false;
        }
    }
}
