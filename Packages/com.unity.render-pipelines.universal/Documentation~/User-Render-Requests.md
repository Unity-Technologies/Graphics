---
uid: urp-user-render-requests
---
# Render Requests

To trigger a camera to render to a render texture outside of the Universal Render Pipeline (URP) rendering loop, use the `SubmitRenderRequest` API in a C# script.

This example shows how to use render requests and callbacks to monitor the progress of these requests. You can see the full code sample in the [Example code](#example-code) section.

## Render a single camera from a camera stack

To render a single camera without taking into account the full stack of cameras, use the `UniversalRenderPipeline.SingleCameraRequest` API. Follow these steps:

1. Create a C# script with the name `SingleCameraRenderRequestExample` and add the `using` statements shown below.

    ```c#
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    public class SingleCameraRenderRequestExample : MonoBehaviour
    {

    }
    ```

2. Create arrays to store the cameras and Render Textures that you want to render from and to.

    ```c#
    public class SingleCameraRenderRequestExample : MonoBehaviour
    {
        public Camera[] cameras;
        public RenderTexture[] renderTextures;
    }
    ```

3. In the `Start` method, add a check to ensure the `cameras` and `renderTextures` arrays are valid and contain the correct data before continuing with running the script.

    ```c#
    void Start()
    {
        // Make sure all data is valid before you start the component
        if (cameras == null || cameras.Length == 0 || renderTextures == null || cameras.Length != renderTextures.Length)
        {
            Debug.LogError("Invalid setup");
            return;
        }
    }
    ```

4. Make a method with the name `SendSingleRenderRequests` and the return type `void` within the `SingleCameraRenderRequest` class.
5. In the `SendSingleRenderRequests` method, add a `for` loop that iterates over the `cameras` array as shown below.

    ```c#
    void SendSingleRenderRequests()
    {
        for (int i = 0; i < cameras.Length; i++)
        {

        }
    }
    ```

6. Inside the `for` loop, create a render request of the `UniversalRenderPipeline.SingleCameraRequest` type in a variable with the name `request`. Then check if the active render pipeline supports this render request type with `RenderPipeline.SupportsRenderRequest`.
7. If the active render pipeline supports the render request, set the destination of the camera output to the matching Render Texture from the `renderTextures` array. Then submit the render request with `RenderPipeline.SubmitRenderRequest`.

    ```c#
    void SendSingleRenderRequests()
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            UniversalRenderPipeline.SingleCameraRequest request =
                new UniversalRenderPipeline.SingleCameraRequest();

            // Check if the active render pipeline supports the render request
            if (RenderPipeline.SupportsRenderRequest(cameras[i], request))
            {
                // Set the destination of the camera output to the matching RenderTexture
                request.destination = renderTextures[i];
                
                // Render the camera output to the RenderTexture synchronously
                // When this is complete, the RenderTexture in renderTextures[i] contains the scene rendered from the point
                // of view of the Camera in cameras[i]
                RenderPipeline.SubmitRenderRequest(cameras[i], request);
            }
        }
    }
    ```

8. Above the `SendSingleRenderRequest` method, create an `IEnumerator` interface with the name `RenderSingleRequestNextFrame`.
9. Inside `RenderSingleRequestNextFrame`, wait for the main camera to finish rendering, then call `SendSingleRenderRequest`. Wait for the end of the frame before restarting `RenderSingleRequestNextFrame` in a coroutine with `StartCoroutine`.

    ```c#
    IEnumerator RenderSingleRequestNextFrame()
    {
        // Wait for the main camera to finish rendering
        yield return new WaitForEndOfFrame();

        // Enqueue one render request for each camera
        SendSingleRenderRequests();

        // Wait for the end of the frame
        yield return new WaitForEndOfFrame();

        // Restart the coroutine
        StartCoroutine(RenderSingleRequestNextFrame());
    }
    ```

10. In the `Start` method, call `RenderSingleRequestNextFrame` in a coroutine with `StartCoroutine`.

    ```c#
    void Start()
    {
        // Make sure all data is valid before you start the component
        if (cameras == null || cameras.Length == 0 || renderTextures == null || cameras.Length != renderTextures.Length)
        {
            Debug.LogError("Invalid setup");
            return;
        }

        // Start the asynchronous coroutine
        StartCoroutine(RenderSingleRequestNextFrame());
    }
    ```

11. In the Editor, create an empty GameObject in your scene and add `SingleCameraRenderRequestExample.cs` as a [component](xref:UsingComponents).
12. In the Inspector window, add the camera you want to render from to the **cameras** list, and the Render Texture you want to render into to the **renderTextures** list.

    > ![NOTE]
    > The number of cameras in the **cameras** list and the number of Render Textures in the **renderTextures** list must be the same.

Now when you enter Play mode, the cameras you added render to the Render Textures you added.

### Check when a camera finishes rendering

To check when a camera finishes rendering, use any callback from the [RenderPipelineManager](https://docs.unity3d.com/ScriptReference/Rendering.RenderPipelineManager.html) API.

The following example uses the [RenderPipelineManager.endContextRendering](https://docs.unity3d.com/ScriptReference/Rendering.RenderPipelineManager-endContextRendering.html) callback.

1. Add `using System.Collections.Generic` to the top of the `SingleCameraRenderRequestExample.cs` file.
2. At the end of the `Start` method, subscribe to the [`endContextRendering`](https://docs.unity3d.com/ScriptReference/Rendering.RenderPipelineManager-endContextRendering.html) callback.

    ```c#
    void Start()
    {
        // Make sure all data is valid before you start the component
        if (cameras == null || cameras.Length == 0 || renderTextures == null || cameras.Length != renderTextures.Length)
        {
            Debug.LogError("Invalid setup");
            return;
        }

        // Start the asynchronous coroutine
        StartCoroutine(RenderSingleRequestNextFrame());
        
        // Call a method called OnEndContextRendering when a camera finishes rendering
        RenderPipelineManager.endContextRendering += OnEndContextRendering;
    }
    ```

3. Create a method with the name `OnEndContextRendering`. Unity runs this method when the `endContextRendering` callback triggers.

    ```c#
    void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        // Create a log to show cameras have finished rendering
        Debug.Log("All cameras have finished rendering.");
    }
    ```

4. To unsubscribe the `OnEndContextRendering` method from the `endContextRendering` callback, add an `OnDestroy` method to the `SingleCameraRenderRequestExample` class.

    ```c#
    void OnDestroy()
    {
        // End the subscription to the callback
        RenderPipelineManager.endContextRendering -= OnEndContextRendering;
    }
    ```

This script now works as before, but logs a message to the Console Window about which cameras have finished rendering.

## Example code

```c#
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SingleCameraRenderRequest : MonoBehaviour
{
    public Camera[] cameras;
    public RenderTexture[] renderTextures;

    void Start()
    {
        // Make sure all data is valid before you start the component
        if (cameras == null || cameras.Length == 0 || renderTextures == null || cameras.Length != renderTextures.Length)
        {
            Debug.LogError("Invalid setup");
            return;
        }

        // Start the asynchronous coroutine
        StartCoroutine(RenderSingleRequestNextFrame());
        
        // Call a method called OnEndContextRendering when a camera finishes rendering
        RenderPipelineManager.endContextRendering += OnEndContextRendering;
    }

    void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        // Create a log to show cameras have finished rendering
        Debug.Log("All cameras have finished rendering.");
    }

    void OnDestroy()
    {
        // End the subscription to the callback
        RenderPipelineManager.endContextRendering -= OnEndContextRendering;
    }

    IEnumerator RenderSingleRequestNextFrame()
    {
        // Wait for the main camera to finish rendering
        yield return new WaitForEndOfFrame();

        // Enqueue one render request for each camera
        SendSingleRenderRequests();

        // Wait for the end of the frame
        yield return new WaitForEndOfFrame();

        // Restart the coroutine
        StartCoroutine(RenderSingleRequestNextFrame());
    }

    void SendSingleRenderRequests()
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            UniversalRenderPipeline.SingleCameraRequest request =
                new UniversalRenderPipeline.SingleCameraRequest();

            // Check if the active render pipeline supports the render request
            if (RenderPipeline.SupportsRenderRequest(cameras[i], request))
            {
                // Set the destination of the camera output to the matching RenderTexture
                request.destination = renderTextures[i];
                
                // Render the camera output to the RenderTexture synchronously
                RenderPipeline.SubmitRenderRequest(cameras[i], request);

                // At this point, the RenderTexture in renderTextures[i] contains the scene rendered from the point
                // of view of the Camera in cameras[i]
            }
        }
    }
}
```