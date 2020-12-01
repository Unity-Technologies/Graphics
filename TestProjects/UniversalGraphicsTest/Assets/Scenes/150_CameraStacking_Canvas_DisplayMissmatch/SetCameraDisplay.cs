using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SetCameraDisplay : MonoBehaviour
{
    public int displayIndex;

    void Awake()
    {
        var camera = GetComponent<Camera>();
        camera.targetDisplay = displayIndex;
    }
}
