using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCameraSize : MonoBehaviour
{
    void Start()
    {
        var camera = Camera.main;
        float cameraHeight = camera.orthographicSize;
        float cameraWidth = cameraHeight * camera.aspect;
        var renderer = GetComponent<Renderer>();
        renderer.material.SetFloat("_CameraHeight", cameraHeight);
        renderer.material.SetFloat("_CameraWidth", cameraWidth);
    }
}
