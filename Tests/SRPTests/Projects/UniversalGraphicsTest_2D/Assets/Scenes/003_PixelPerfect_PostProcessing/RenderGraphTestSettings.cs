using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderGraphTestSettings : MonoBehaviour
{
    public static bool useCustomSettings
    {
        get => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ||
               SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 ||
               SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal ||
               SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan;
    }

    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        if (UniversalRenderPipeline.asset.enableRenderGraph && useCustomSettings)
        {
            var settings = FindAnyObjectByType<UniversalGraphicsTestSettings>();
            settings.ImageComparisonSettings.UseBackBuffer = true;
            settings.ImageComparisonSettings.ImageResolution = UnityEngine.TestTools.Graphics.ImageComparisonSettings.Resolution.w640h360;
        }
#endif
    }
}
