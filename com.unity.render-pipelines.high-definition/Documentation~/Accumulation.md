## Multiframe Rendering and Accumulation

Some rendering techniques, such as path tracing and accumulation motion blur, create the final "converged" frame by combining information from multiple intermediate sub-frames. Each intermediate sub-frame can correspond to a slightly different point in time, effectively computing physically-based accumulation motion blur, which properly takes into account object rotations, deformations, material or lighting changes, etc.

HDRP provides a scripting API that allows you to control the creation of sub-frames and the convergence of multi-frame rendering effects. In particular, the API allows you to control the number of intermediate sub-frames (samples) and the points in time that correspond to each one of them. Furthermore, the weights of each sub-frame are controlled using shutter profiles that describe how fast was the opening and closing motion of the camera's shutter.

This API is particularly useful when recording path traced movies. Normally, when editing a scene, the convergence of path tracing restarts every time the scene changes, to provide artists an interactive editing workflow that allows them to quickly visualize their changes. However such behavior is not desirable during recording. 

The following images shows a rotating object with path tracing and accumulation motion blur, recorded using the multi-frame rendering API.

![](Images/path_tracing_recording.png)

## API Overview
The recording API is available in the HD Render Pipeline and has only three calls:
- BeginRecording should be called when starting a multi-frame render. 
- PrepareNewSubFrame should be called before rendering a new subframe.
- EndRecording which should be called to stop the multi-frame rendering mode. 

The script below demonstrates how to use these calls.

## Scripting Example
The following example demonstrates how to use the multi-frame rendering API in your scripts to properly record converged animation sequences with path tracing and/or accumulation motion blur. To use it, attach the script to the camera of your scene and select the “Start recording” and “stop recording” actions from the context menu. Setting the Shutter Interval parameter to zero will disable motion blur completely.

```
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class FrameManager : MonoBehaviour
{
    public int samples = 128;
    public float shutterInterval = 1.0f;
    public float shutterFullyOpen = 0.25f;
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
        renderPipeline.EndRecording();
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
}
```

## Shutter Profiles
The BeginRecording call allows you to specify how fast the camera shutter is opening and closing. The speed of the camera shutter defines the so called “shutter profile”. The following image demonstrates how different shutter profiles affect the appearance of motion blur on a blue sphere moving from left to right. 

![](Images/shutter_profiles.png)


In all cases, the speed of the sphere is the same. The only change is the shutter profile. The horizontal axis of the profile diagram corresponds to time, and the vertical axis corresponds to the openning of the shutter. 

In this example, we observe that the slow open profile creates a motion trail appearance for the motion blur, which might be more desired for the artists. On the other hand, the smooth open and close profile creates smoother animations than the slow open or uniform profiles.
