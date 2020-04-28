using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class DisplayOnPlatformAPI : MonoBehaviour
{
    public bool D3D11;
    public bool D3D12;
    public bool VukanWindows;

    List<PlatformAPI> platformApis = new List<PlatformAPI>();

    void OnValidate()
    {
        platformApis.Clear();

        if (D3D11)
            platformApis.Add(new PlatformAPI(RuntimePlatform.WindowsEditor, GraphicsDeviceType.Direct3D11));

        if (D3D12)
            platformApis.Add(new PlatformAPI(RuntimePlatform.WindowsEditor, GraphicsDeviceType.Direct3D12));

        if (VukanWindows)
            platformApis.Add(new PlatformAPI(RuntimePlatform.WindowsEditor, GraphicsDeviceType.Vulkan));

        bool display = false;

        foreach (var platformApi in platformApis)
        {
            if (Application.platform == platformApi.platform && SystemInfo.graphicsDeviceType == platformApi.graphicsDeviceType)
            {
                display = true;
                break;
            }
        }

        var textMeshRenderer = gameObject.GetComponent<MeshRenderer>();
        textMeshRenderer.enabled = display;
    }

    public struct PlatformAPI
    {
        public PlatformAPI(RuntimePlatform inPlatform, GraphicsDeviceType inGraphicsDeviceType)
        {
            platform = inPlatform;
            graphicsDeviceType = inGraphicsDeviceType;
        }

        public RuntimePlatform platform;
        public GraphicsDeviceType graphicsDeviceType;
    }
}
