# Render from another camera inside a camera's rendering loop

Render from cameras nested inside the render loop of other cameras. 

Attach the provided script to a GameObject with a Camera component to nest multiple cameras, that are rendering with [RenderPipeline.SubmitRenderRequest](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.RenderPipeline.SubmitRenderRequest.html), inside the render loop of other cameras.

**Note**: If your project uses the [Universal Render Pipeline](https://docs.unity3d.com/Manual/urp/urp-introduction.html) (URP), the recommended best practice is to use [UniversalRenderPipeline.SingleCameraRequest](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.2/api/UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest.html) instead of [StandardRequest](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.RenderPipeline.StandardRequest.html), to make sure you only render the camera provided to the `RenderRequest` API instead of the full stack of cameras.

## Attach the script to nest 

Follow these steps:

1. Create a new C# script.  
2. Add the `using` statements shown below and the `RequireComponent` attribute with the `Camera` type.

    ```c#
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Rendering;

    [RequireComponent(typeof(Camera))]
    public class InLoopRenderRequest : MonoBehaviour
    {

    }
    ```

3. Add a property with the type `Camera` to the `InLoopRenderRequest` class. 
4. Add a property with the type `RenderTexture` for each callback the camera uses, as shown below:

    ```c#
    [RequireComponent(typeof(Camera))]
    public class InLoopRenderRequest : MonoBehaviour
    {
        public Camera renderRequestCamera;

        public RenderTexture onBeginCameraRendering;
        public RenderTexture onBeginContextRendering;
        public RenderTexture onEndCameraRendering;
        public RenderTexture onEndContextRendering;
    }
    ```

## Full code example

The following is an example of the finalized code which you attach to the secondary camera:

```c#
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class InLoopRenderRequest : MonoBehaviour
{
    // Add a reference to the secondary camera that will render the textures
    // It's recommended to disable the secondary camera.
    public Camera renderRequestCamera;

    // Add references to the Render Textures that will contain the rendered image from the secondary camera.
    public RenderTexture onBeginCameraRendering;
    public RenderTexture onBeginContextRendering;
    public RenderTexture onEndCameraRendering;
    public RenderTexture onEndContextRendering;

    void OnEnable()
    {
        // Subscribe to the RenderPipelineManager callbacks
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRender;
        RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRender;
        RenderPipelineManager.endContextRendering += OnEndContextRendering;
    }

    public void OnDisable()
    {
        // Unsubscribe to the callbacks from RenderPipelineManager when we disable the component
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRender;
        RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRender;
        RenderPipelineManager.endContextRendering -= OnEndContextRendering;
    }

    void SubmitStandardRenderRequest(RenderTexture rt, Camera cam)
    {
        RenderPipeline.StandardRequest request = new();

        // Check that the Scriptable Render Pipeline (SRP) we're using supports the given render data.
        if (RenderPipeline.SupportsRenderRequest(cam, request))
        {
            // Set the request RenderTexture
            request.destination = rt;

            // Render the camera output to the RenderTexture synchronously
            // When this is complete, the RenderTexture in renderTextures[i] contains the scene rendered from the point of view of the secondary cameras
            RenderPipeline.SubmitRenderRequest(cam, request);
        }
    }

    // StandardRequest and UniversalRenderPipeline.SingleCameraRequest also trigger RenderPipelineManager callbacks.
    // Check that the callbacks are from the GameObject's Camera component to avoid a recursive rendering of the same camera.
    private void OnBeginContextRendering(ScriptableRenderContext ctx, List<Camera> cams)
    {
        if (cams.Contains(GetComponent<Camera>()))
        {
            SubmitStandardRenderRequest(onBeginContextRendering, renderRequestCamera);
        }
    }

    private void OnEndContextRendering(ScriptableRenderContext ctx, List<Camera> cams)
    {
        if (cams.Contains(GetComponent<Camera>()))
        {
            SubmitStandardRenderRequest(onEndContextRendering, renderRequestCamera);
        }
    }

    private void OnBeginCameraRender(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam == GetComponent<Camera>())
        {
            SubmitStandardRenderRequest(onBeginCameraRendering, renderRequestCamera);
        }
    }

    private void OnEndCameraRender(ScriptableRenderContext ctx, Camera cam)
    {
        if (cam == GetComponent<Camera>())
        {
            SubmitStandardRenderRequest(onEndCameraRendering, renderRequestCamera);
        }
    }
}
```

## Additional resources
- [Render Requests](User-Render-Requests.md)
- [Creating a custom render pipeline](srp-custom.md)

