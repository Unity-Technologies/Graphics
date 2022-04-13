## Multiframe rendering and accumulation

Some rendering techniques, such as [path tracing](Ray-Tracing-Path-Tracing.md) and accumulation motion blur, combine information from multiple intermediate sub-frames to create a final "converged" frame. Each intermediate sub-frame can correspond to a slightly different point in time, effectively computing physically based accumulation motion blur, which properly takes into account object rotations, deformations, material or lighting changes.

The High Definition Render Pipeline (HDRP) provides a scripting API that allows you to control the creation of sub-frames and the convergence of multi-frame rendering effects. In particular, the API allows you to control the number of intermediate sub-frames (samples) and the points in time that correspond to each one of them. Furthermore, you can use a shutter profile to control the weights of each sub-frame. A shutter profile describes how fast the physical camera opens and closes its shutter.

This API is particularly useful when recording path-traced movies. Normally, when editing a Scene, the convergence of path tracing restarts every time the Scene changes, to provide artists an interactive editing workflow that allows them to quickly visualize their changes. However such behavior isn't desirable during recording.

The following image shows a rotating GameObject with path tracing and accumulation motion blur, recorded using the multi-frame recording API.

![](Images/path_tracing_recording.png)

## API overview
The recording API is available in HDRP and has three calls:
- `BeginRecording`: Call this when you want to start a multi-frame render.
- `PrepareNewSubFrame`: Call this before rendering a new subframe.
- `EndRecording`: Call this when you want to stop the multi-frame render.

The only call that takes any parameters is **BeginRecording**. Here is an explanation of the parameters:

| Parameter  | Description               |
|-------------------|---------------------------|
| **Samples**       | The number of sub-frames to accumulate. This parameter overrides the number of path tracing samples in the [Volume](Volumes.md). |
| **ShutterInterval** | The amount of time the shutter is open between two subsequent frames. A value of **0** results in an instant shutter (no motion blur). A value of **1** means there is no (time) gap between two subsequent frames. |
| **ShutterProfile** | An animation curve that specifies the shutter position during the shutter interval. Alternatively, you can also provide the time the shutter was fully open; and when the shutter begins closing. |

Before calling the accumulation API, the application should also set the desired Time.captureDeltaTime. The example script below demonstrates how to use these API calls.

## Scripting API example
The following example demonstrates how to use the multi-frame rendering API in your scripts to properly record converged animation sequences with path tracing or accumulation motion blur. To use it, attach the script to a Camera in your Scene and, in the component's context menu, click the “Start Recording” and “Stop Recording” actions.

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
        Time.captureDeltaTime = m_OriginalDeltaTime;
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

In this example, you can see that the slow open profile creates a motion trail appearance for the motion blur, which might be more desired for artists. Although, the smooth open and close profile creates smoother animations than the slow open or uniform profiles.


## High Quality Anti-aliasing with Accumulation
You can use the accumulation API to create a high quality antialiased frame, similar to the [SuperSampling](https://en.wikipedia.org/wiki/Supersampling) method. The accumulation API uses fewer memory resources in the GPU than higher resolution rendering.

To do this, use the accumulation API to jitter the projection matrix of each rendered subframe. The following script example uses this method to perform high quality antialiasing :

```C#
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
        Time.captureDeltaTime = m_OriginalDeltaTime;
        RenderPipelineManager.beginContextRendering -= PrepareSubFrameCallBack;
        RenderPipelineManager.endContextRendering -= EndSubFrameCallBack;
        HDRenderPipeline renderPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        renderPipeline?.EndRecording();
        m_Recording = false;
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

        if (saveToDisk && m_Recording && m_Iteration % (samples * samples) == 0)
        {
            ScreenCapture.CaptureScreenshot($"frame_{m_RecordedFrames++}.png");
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
}
```

## Limitations
The multi-frame rendering API internally changes the `Time.timeScale` of the Scene. This means that:
- You can't have different accumulation motion blur parameters per camera.
- Projects that already modify this parameter per frame aren't be compatible with this feature.
