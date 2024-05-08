# Render Requests

For a general documentation see the [Core Package](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest/User-Render-Requests.html) about Render Requests.

## Use UniversalRenderPipeline.SingleCameraRequest

`UniversalRenderPipeline.SingleCameraRequest` renders a single camera, without taking into account the full stack of cameras.

You can still hook into callbacks from [RenderPipelineManager](https://docs.unity3d.com/ScriptReference/Rendering.RenderPipelineManager.html).

The following code sample shows that you can hook into [RenderPipelineManager.endContextRendering](https://docs.unity3d.com/ScriptReference/Rendering.RenderPipelineManager-endContextRendering.html) `UniversalRenderPipeline.SingleCameraRequest`

To try out this example:

- Attach the script to a **GameObject** in the **Scene**.
- Configure the **cams** and **rts**.
- Set **useSingleCameraRequestValues** to true or false depending on which type of render request you want to use.
- Select **Enter Play Mode**.
- See the **Console** Log.

```
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SingleCameraRenderRequestExample : MonoBehaviour
{
    public Camera[] cams;
    public RenderTexture[] rts;

    void Start()
    {
        if (cams == null || cams.Length == 0 || rts == null || cams.Length != rts.Length)
        {
            Debug.LogError("Invalid setup");
            return;
        }

        StartCoroutine(RenderSingleRequestNextFrame());
        RenderPipelineManager.endContextRendering += OnEndContextRendering;
    }

    void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        var stb = new StringBuilder($"Cameras Count from EndContextRendering: <b> {cameras.Count}</b>.");
        foreach (var cam in cameras)
        {
            stb.AppendLine($"- {cam.name}");
        }
        Debug.Log(stb.ToString());
    }

    void OnDestroy()
    {
        RenderPipelineManager.endContextRendering -= OnEndContextRendering;
    }

    IEnumerator RenderSingleRequestNextFrame()
    {
        yield return new WaitForEndOfFrame();

        SendSingleRenderRequests();

        yield return new WaitForEndOfFrame();

        StartCoroutine(RenderSingleRequestNextFrame());
    }

    void SendSingleRenderRequests()
    {
        for (int i = 0; i < cams.Length; i++)
        {
            UniversalRenderPipeline.SingleCameraRequest request =
                new UniversalRenderPipeline.SingleCameraRequest();

            // Check if the request is supported by the active render pipeline
            if (RenderPipeline.SupportsRenderRequest(cams[i], request))
            {
                request.destination = rts[i];
                RenderPipeline.SubmitRenderRequest(cams[i], request);
            }
        }
    }
}
```
