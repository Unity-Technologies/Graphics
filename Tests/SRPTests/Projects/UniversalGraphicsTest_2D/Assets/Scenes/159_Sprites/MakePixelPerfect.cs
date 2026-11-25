using UnityEngine;

public class MakePixelPerfect : MonoBehaviour
{
    // Matches TargetHeight in the scene's UniversalGraphicsTestSettings, because Camera.pixelHeight is the Game view height
    const int k_CaptureHeight = 300;

    public Camera m_Camera;
    public int m_PixelsToUnits = 100;

    void Start()
    {
        float scale = (m_Camera.orthographicSize * m_PixelsToUnits * 2) / k_CaptureHeight;
        transform.localScale = new Vector3(scale, scale, 1.0f);
    }
}
