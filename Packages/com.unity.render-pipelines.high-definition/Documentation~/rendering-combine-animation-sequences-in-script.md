## Combine animation sequences in script

The following example demonstrates how to use the [multi-frame rendering API](rendering-multiframe-recording-api.md) in your script to record converged animation sequences with path tracing or accumulation motion blur. To use it, attach the script to a Camera in your Scene and, in the component's context menu, click the “Start Recording” and “Stop Recording” actions.

```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class FrameManager : MonoBehaviour
{
    // The number of samples used for accumumation.
    public int samples = 128;
    [Range(0.0f, 1.0f)]
    public float shutterInterval = 1.0f;

    // The time during shutter interval when the shutter is fully open
    [Range(0.0f, 1.0f)]
    public float shutterFullyOpen = 0.25f;

    // The time during shutter interval when the shutter begins closing.
    [Range(0.0f, 1.0f)]
    public float shutterBeginsClosing = 0.75f;

    // The desired frame rate when recording subframes.
    [Min(1)]
    public int captureFrameRate = 30;

    bool m_Recording = false;
    int m_Iteration = 0;
    int m_RecordedFrames = 0;
    float m_OriginalDeltaTime = 0;

    [ContextMenu("Start Recording")]
    void BeginMultiframeRendering()
    {
        // Set the desired capture delta time before using the accumulation API
        m_OriginalDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1.0f / captureFrameRate;

        RenderPipelineManager.beginFrameRendering += PrepareSubFrameCallBack;
        HDRenderPipeline renderPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        renderPipeline.BeginRecording(samples, shutterInterval, shutterFullyOpen, shutterBeginsClosing);
        m_Recording = true;
        m_Iteration = 0;
        m_RecordedFrames = 0;
    }

    [ContextMenu("Stop Recording")]
    void StopMultiframeRendering()
    {
        RenderPipelineManager.beginFrameRendering -= PrepareSubFrameCallBack;
        HDRenderPipeline renderPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        renderPipeline?.EndRecording();
        m_Recording = false;
        Time.captureDeltaTime = m_OriginalDeltaTime;
    }

    void PrepareSubFrameCallBack(ScriptableRenderContext cntx, Camera[] cams)
    {
        HDRenderPipeline renderPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (renderPipeline != null && m_Recording)
        {
            renderPipeline.PrepareNewSubFrame();
            m_Iteration++;
        }

        if (m_Recording && m_Iteration % samples == 0)
        {
            ScreenCapture.CaptureScreenshot($"frame_{m_RecordedFrames++}.png");
        }
    }

    void OnDestroy()
    {
        if (m_Recording)
        {
            StopMultiframeRendering();
        }
    }

    void OnValidate()
    {
        // Make sure the shutter will begin closing sometime after it is fully open (and not before)
        shutterBeginsClosing = Mathf.Max(shutterFullyOpen, shutterBeginsClosing);
    }
}
```
