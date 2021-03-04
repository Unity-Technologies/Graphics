using System;
using UnityEngine;

[ExecuteAlways]
public class DynamicResSetter : MonoBehaviour
{

    public Camera m_camera;

    [Range(0.01f, 1f)]
    public float scale = 1;

    private void OnValidate()
    {
        UpdateCamera();
    }

    void Update()
    {
        UpdateCamera();
    }

    void UpdateCamera()
    {
        if (m_camera)
        {
            ScalableBufferManager.ResizeBuffers(scale, scale);
        }
    }
}
