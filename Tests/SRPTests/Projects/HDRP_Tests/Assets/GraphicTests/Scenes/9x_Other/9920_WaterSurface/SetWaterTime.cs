using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class SetWaterTime : MonoBehaviour
{
    public WaterSurface surface;
    public float seconds = 10.0f;
    public int lastFrame = 10;

    int firstFrame, frameCount;

    private void Start()
    {
        firstFrame = Time.frameCount;
        frameCount = lastFrame + 4 + 4;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (UnityEditor.ShaderUtil.anythingCompiling)
        {
            firstFrame = Time.frameCount;
            return;
        }
#endif

        if (Time.frameCount - firstFrame == frameCount)
        {
            var time = DateTime.Now;
            time.AddSeconds(seconds);

            surface.simulationStart = time;
            surface.timeMultiplier = 0.0f;
        }
    }
}
