using System;
using UnityEngine;

[ExecuteAlways]
public class DynamicResSetter : MonoBehaviour
{
    private Camera _camera;

    [Range(0.01f, 1f)]
    public float scale = 1;

    private void OnValidate()
    {
        TryGetComponent(out _camera);
        if(_camera)
            _camera.allowDynamicResolution = true;
        UpdateCamera();
    }

    void Update()
    {
        UpdateCamera();
    }

    void UpdateCamera()
    {
        if (_camera)
        {
            ScalableBufferManager.ResizeBuffers(scale, scale);
        }
    }
}
