using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SetCameraViewport : MonoBehaviour
{
    public Rect viewport;

    void Start()
    {
        var camera = GetComponent<Camera>();
        camera.rect = viewport;
    }
}
