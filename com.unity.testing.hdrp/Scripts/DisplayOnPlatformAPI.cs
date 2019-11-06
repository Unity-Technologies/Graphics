using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DisplayOnPlatformAPI : MonoBehaviour
{
    [SerializeField] List<PlatformAPI> platformApis = new List<PlatformAPI>();

    void Start()
    {
        bool display = false;

        foreach (var platformApi in platformApis)
        {
            if (Application.platform == platformApi.platform && SystemInfo.graphicsDeviceType == platformApi.graphicsDeviceType)
            {
                display = true;
                break;
            }
        }

        gameObject.SetActive(display);
    }

    [System.Serializable]
    public struct PlatformAPI
    {
        public RuntimePlatform platform;
        public GraphicsDeviceType graphicsDeviceType;
    }
}
