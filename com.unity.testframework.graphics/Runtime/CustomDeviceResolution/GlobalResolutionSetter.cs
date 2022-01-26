using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script sets global rendering resolution based on the predefined settings inside scriptable object that is attached
// to the component with this script.
// This is needed for tests consistency. For example by the date of writing this new Android devices are added to the
// test rig, which have different resolution (2280x1080) compared to the old ones (1920x1080). This difference causes
// majority of tests fail.

public class GlobalResolutionSetter : MonoBehaviour
{
    public CustomResolutionSettings customResolutionSettings;
    void Awake()
    {
        var currentPlatform = Application.platform;
        foreach (var resolutionSettingsField in customResolutionSettings.fields)
        {
            if (resolutionSettingsField.Platform == currentPlatform)
            {
                Debug.Log($"Setting new rendering resolution: {resolutionSettingsField.Width}x{resolutionSettingsField.Height}");
                Screen.SetResolution(resolutionSettingsField.Width, resolutionSettingsField.Height, resolutionSettingsField.isFullScreen);
                break;
            }
        }
    }
}
