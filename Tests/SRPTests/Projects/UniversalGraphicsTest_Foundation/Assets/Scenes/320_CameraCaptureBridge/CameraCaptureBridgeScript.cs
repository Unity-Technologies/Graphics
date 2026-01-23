using UnityEngine;
using UnityEngine.Rendering;

public class CameraCaptureBridgeScript : MonoBehaviour
{
    Camera m_Camera;

    public RenderTexture m_RenderTexture;

    void OnEnable()
    {
        m_Camera = GetComponent<Camera>();
        // the camera doesn't know the aspect ratio because we don't attach a render target explicitly
        m_Camera.aspect = 2;
        CameraCaptureBridge.enabled = true;
        CameraCaptureBridge.AddCaptureAction(m_Camera, Capture);
    }

    void OnDisable()
    {
        CameraCaptureBridge.RemoveCaptureAction(m_Camera, Capture);
        CameraCaptureBridge.enabled = false;
    }

    void Capture(RenderTargetIdentifier rtId, CommandBuffer cmd)
    {
        if (m_RenderTexture != null)
        {
            cmd.Blit(rtId, m_RenderTexture);
        }
    }
}
