using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class AddCaptureAction : MonoBehaviour
{
    public Mesh Mesh;
    public Material Material;
    Camera m_Camera;

    void OnEnable()
    {
        m_Camera = GetComponent<Camera>();
        CameraCaptureBridge.AddCaptureAction(m_Camera, Capture);
    }

    void OnDisable()
    {
        CameraCaptureBridge.RemoveCaptureAction(m_Camera, Capture);
    }

    void Capture(RenderTargetIdentifier rtId, CommandBuffer cmd)
    {
        if (Mesh == null || Material == null)
            return;
        cmd.DrawMesh(Mesh, Matrix4x4.identity, Material, 0);
    }
}
