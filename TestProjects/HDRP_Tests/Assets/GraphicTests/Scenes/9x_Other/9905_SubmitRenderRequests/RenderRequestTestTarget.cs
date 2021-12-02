using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class RenderRequestTestTarget : MonoBehaviour
{
    private string m_Text = null;
    private Rect m_TextRect;

    private float Radius => transform.localScale.x / 2.0f;
    private const int kTextOffsetX = -20;
    private const int kTextOffsetY = -10;
    private const int kTextDim = 100;

    public float3 ExpectedWorldPosition(Camera viewedFrom)
    {
        return (float3)transform.position + ExpectedWorldNormal(viewedFrom) * Radius;
    }

    public float3 ExpectedWorldNormal(Camera viewedFrom)
    {
        return (viewedFrom.transform.position - transform.position).normalized;
    }

    public int ExpectedObjectId()
    {
        return GetComponent<MeshRenderer>().GetInstanceID();
    }

    public float3 ViewportSpacePosition(Camera camera)
    {
        return camera.WorldToViewportPoint(transform.position);
    }

    public void SetTestText(Camera camera, string text)
    {
        float2 xy = ((float3)camera.WorldToScreenPoint(transform.position)).xy;

        m_Text = text;
        m_TextRect = new Rect(xy.x + kTextOffsetX, camera.pixelHeight - xy.y + kTextOffsetY, kTextDim, kTextDim);
    }

    void OnGUI()
    {
        if (m_Text != null)
            GUI.Label(m_TextRect, m_Text);
    }
}
