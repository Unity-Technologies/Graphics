using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileControls : MonoBehaviour
{
    public GameObject movementControls;
    public GameObject aimControls;

    void Start()
    {
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            movementControls.SetActive(true);
            aimControls.SetActive(true);
        }
    }
}
