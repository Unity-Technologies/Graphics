using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedTimeRotator : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        const float rotationsPerSecond = 0.1f;
        const float fixedFrameTime = 1.0f / 30.0f;
        float rotationPerFrame = fixedFrameTime * rotationsPerSecond * 360;
        transform.Rotate(0, 0, rotationPerFrame, Space.World);
    }
}
