using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortraitWarning : MonoBehaviour
{
    public GameObject WarningPanel;

    // Update is called once per frame
    void Update()
    {
        if (SystemInfo.deviceType == DeviceType.Handheld && Screen.orientation == ScreenOrientation.Portrait)
        {
            WarningPanel.SetActive(true);
        }
        else
        {
            WarningPanel.SetActive(false);
        }
    }
}
