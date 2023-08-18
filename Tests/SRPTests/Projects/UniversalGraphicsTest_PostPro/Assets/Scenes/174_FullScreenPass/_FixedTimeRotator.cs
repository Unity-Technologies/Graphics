
using UnityEngine;

public class FixedTimeRotator : MonoBehaviour
{
    public float RotationsPerSecond;

    void Update()
    {
        const float fixedFrameTime = 1.0f / 30.0f;
        float rotationPerFrame = fixedFrameTime * RotationsPerSecond * 360;
        transform.Rotate(0, 0, rotationPerFrame, Space.World);
    }
}
