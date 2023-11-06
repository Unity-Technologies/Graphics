
## Use high-quality antialiasing with accumulation in a script
You can use the [multiframe rendering API](rendering-multiframe-recording-api.md) to create a high-quality antialiased frame, similar to the [SuperSampling](https://en.wikipedia.org/wiki/Supersampling) method. The accumulation API uses fewer memory resources in the GPU than higher resolution rendering.

To do this, use the accumulation API to jitter the projection matrix of each rendered subframe. The following script example uses this method to perform high quality antialiasing :

```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;

public class SuperSampling : MonoBehaviour
{
    // The number of samples used for accumumation in the horizontal and verical directions.
    public int  samples = 8;
    public bool saveToDisk = true;

    bool m_Recording = false;
    int m_Iteration = 0;
    int m_RecordedFrames = 0;
    float m_OriginalDeltaTime = 0;
    List<Matrix4x4> m_OriginalProectionMatrix = new List<Matrix4x4>();

    [ContextMenu("Start Accumulation")]
    void BeginAccumulation()
    {
        m_OriginalDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1.0f / 30;

        RenderPipelineManager.beginContextRendering += PrepareSubFrameCallBack;
        RenderPipelineManager.endContextRendering += EndSubFrameCallBack;
        HDRenderPipeline renderPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        renderPipeline.BeginRecording(samples * samples, 1, 0.0f, 1.0f);
        m_Recording = true;
        m_Iteration = 0;
        m_RecordedFrames = 0;
    }

    [ContextMenu("Stop Accumulation")]
    void StopAccumulation()
    {
        RenderPipelineManager.beginContextRendering -= PrepareSubFrameCallBack;
        RenderPipelineManager.endContextRendering -= EndSubFrameCallBack;
        HDRenderPipeline renderPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        renderPipeline?.EndRecording();
        m_Recording = false;
        Time.captureDeltaTime = m_OriginalDeltaTime;
    }

    Matrix4x4 GetJitteredProjectionMatrix(Camera camera)
    {
        int totalSamples = samples * samples;
        int subframe = m_Iteration % totalSamples;
        int stratumX = subframe % samples;
        int stratumY = subframe / samples;
        float jitterX = stratumX * (1.0f / samples) - 0.5f;
        float jitterY = stratumY * (1.0f / samples) - 0.5f;
        var planes = camera.projectionMatrix.decomposeProjection;

        float vertFov = Mathf.Abs(planes.top) + Mathf.Abs(planes.bottom);
        float horizFov = Mathf.Abs(planes.left) + Mathf.Abs(planes.right);

        var planeJitter = new Vector2(jitterX * horizFov / camera.pixelWidth,
            jitterY * vertFov / camera.pixelHeight);

        planes.left += planeJitter.x;
        planes.right += planeJitter.x;
        planes.top += planeJitter.y;
        planes.bottom += planeJitter.y;

        return Matrix4x4.Frustum(planes);
    }

    void PrepareSubFrameCallBack(ScriptableRenderContext cntx, List<Camera> cameras)
    {
        HDRenderPipeline renderPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (renderPipeline != null && m_Recording)
        {
            renderPipeline.PrepareNewSubFrame();
            m_Iteration++;
        }

        m_OriginalProectionMatrix.Clear();
        foreach (var camera in cameras)
        {
            // Jitter the projection matrix
            m_OriginalProectionMatrix.Add(camera.projectionMatrix);
            camera.projectionMatrix = GetJitteredProjectionMatrix(camera);
        }
    }

    void EndSubFrameCallBack(ScriptableRenderContext cntx, List<Camera> cameras)
    {
        for (int i=0; i < cameras.Count; ++i)
        {
            cameras[i].projectionMatrix = m_OriginalProectionMatrix[i];
        }
    }

    void OnDestroy()
    {
        if (m_Recording)
        {
            StopAccumulation();
        }
    }

    void OnValidate()
    {
        // Make sure that we have at least one sample
        samples = Mathf.Max(1, samples);
    }

    void Update()
    {
        // Save a screenshot to disk when recording
        if (saveToDisk && m_Recording && m_Iteration % (samples * samples) == 0)
        {
            ScreenCapture.CaptureScreenshot($"frame_{m_RecordedFrames++}.png");
        }
    }
}
```
