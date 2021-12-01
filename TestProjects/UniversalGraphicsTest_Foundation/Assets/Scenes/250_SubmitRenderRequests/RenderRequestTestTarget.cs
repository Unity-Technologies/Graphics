using Unity.Mathematics;
using UnityEngine;

public class RenderRequestTestTarget : MonoBehaviour
{
    private float Radius => transform.localScale.x / 2.0f;

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
}
