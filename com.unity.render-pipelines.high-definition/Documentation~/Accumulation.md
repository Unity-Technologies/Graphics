## Multiframe rendering and accumulation

Some rendering techniques, such as [path tracing](Ray-Tracing-Path-Tracing.md) and accumulation motion blur, combine information from multiple intermediate sub-frames to create a final "converged" frame. Each intermediate sub-frame can correspond to a slightly different point in time, effectively computing physically-based accumulation motion blur, which properly takes into account object rotations, deformations, material or lighting changes.

The High Definition Render Pipeline (HDRP) provides a scripting API that allows you to control the creation of sub-frames and the convergence of multi-frame rendering effects. In particular, the API allows you to control the number of intermediate sub-frames (samples) and the points in time that correspond to each one of them. Furthermore, you can use a shutter profile to control the weights of each sub-frame. A shutter profile describes how fast the physical camera opens and closes its shutter.

This API is particularly useful when recording path-traced movies. Normally, when editing a Scene, the convergence of path tracing restarts every time the Scene changes, to provide artists an interactive editing workflow that allows them to quickly visualize their changes. However such behavior is not desirable during recording.

The following image shows a rotating GameObject with path tracing and accumulation motion blur, recorded using the multi-frame recording API.

![](Images/path_tracing_recording.png)

## API overview
The recording API is available in HDRP and has three calls:
- **BeginRecording**: Call this when you want to start a multi-frame render.
- **PrepareNewSubFrame**: Call this before rendering a new subframe.
- **EndRecording**: Call this when you want to stop the multi-frame render.

The only call that takes any parameters is **BeginRecording**. Here is an explanation of the parameters:

| Parameter  | Description               |
|-------------------|---------------------------|
| **Samples**       | The number of sub-frames to accumulate. This parameter overrides the number of path tracing samples in the [Volume](Volumes.md). |
| **ShutterInterval** | The amount of time the shutter is open between two subsequent frames. A value of **0** results in an instant shutter (no motion blur). A value of **1** means there is no (time) gap between two subsequent frames. |
| **ShutterProfile** | An animation curve that specifies the shutter position during the shutter interval. Alternatively, you can also provide the time the shutter was fully open; and when the shutter begins closing. |

The example script below demonstrates how to use these API calls.

## Scripting API example
The following example demonstrates how to use the multi-frame rendering API in your scripts to properly record converged animation sequences with path tracing and/or accumulation motion blur. To use it, attach the script to a Camera in your Scene and, in the component's context menu, click the “Start Recording” and “Stop Recording” actions.

```
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

    bool m_Recording = false;
    int m_Iteration = 0;
    int m_RecordedFrames = 0;

    [ContextMenu("Start Recording")]
    void BeginMultiframeRendering()
    {
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

## Shutter profiles
The **BeginRecording** call allows you to specify how fast the camera shutter opens and closes. The speed of the camera shutter defines the so called “shutter profile”. The following image demonstrates how different shutter profiles affect the appearance of motion blur on a blue sphere moving from left to right.

![](Images/shutter_profiles.png)

In all cases, the speed of the sphere is the same. The only change is the shutter profile. The horizontal axis of the profile diagram corresponds to time, and the vertical axis corresponds to the openning of the shutter.

You can easily define the first three profiles without using an animation curve by setting the open, close parameters to (0,1), (1,1), and (0.25, 0.75) respectively. The last profile requires the use of an animation curve.

In this example, you can see that the slow open profile creates a motion trail appearance for the motion blur, which might be more desired for artists. On the other hand, the smooth open and close profile creates smoother animations than the slow open or uniform profiles.

## Limitations
The multi-frame rendering API internally changes the `Time.timeScale` of the Scene. This means that:
- You cannot have different accumulation motion blur parameters per camera.
- Projects that already modify this parameter per frame are not be compatible with this feature.
