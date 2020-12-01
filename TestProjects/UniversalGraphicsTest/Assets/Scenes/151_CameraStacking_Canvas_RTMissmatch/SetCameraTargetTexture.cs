using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SetCameraTargetTexture : MonoBehaviour
{
    public RenderTexture renderTexture;
    void Awake()
    {
        var camera = GetComponent<Camera>();
        camera.targetTexture = renderTexture;
    }
}
