using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class TextInfoModes : MonoBehaviour
{
    public Text UIOverlayMode;
    public Text XRMode;
    public Text HDRMode;
    public Text GraphicsAPIMode;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Is UIOverlay rendered from native engine or URP?
        if (UIOverlayMode != null)
        {
            if(SupportedRenderingFeatures.active.rendersUIOverlay)
            {
                UIOverlayMode.text = $"UI Overlay from HDRP";
            }
            else
            {
                UIOverlayMode.text = $"UI Overlay from native";
            }
        }

        // XR Mode : We enforce triggering of UI Overlay rendering from native side
        if (XRMode != null)
        {
            if(XRSystem.displayActive)
            {
                XRMode.text = $"Active XR display";
            }
            else
            {
                XRMode.text = $"No active XR display";
            }
        }

        // HDR Mode : If no XR/no NRP without RG, we enforce triggering of UI Overlay rendering from URP
        if (HDRMode != null)
        {
            HDROutputSettings display = HDROutputSettings.main;
            if (display.available)
            {
                HDRMode.text = $"\n{display.displayColorGamut}\n{display.minToneMapLuminance} / {display.maxToneMapLuminance} / {display.paperWhiteNits}";
            }
            else
            {
                HDRMode.text = "HDR unavailable";
            }
        }

        if (GraphicsAPIMode != null)
        {
            HDRDisplaySupportFlags supportFlags = SystemInfo.hdrDisplaySupportFlags;
            GraphicsAPIMode.text = $"{SystemInfo.graphicsDeviceType}\n{(supportFlags.HasFlag(HDRDisplaySupportFlags.Supported) ? "Supports HDR" : "Doesn't support HDR")}\n{(supportFlags.HasFlag(HDRDisplaySupportFlags.RuntimeSwitchable) ? "Switchable" : "Not switchable")}";
        }
    }
}
