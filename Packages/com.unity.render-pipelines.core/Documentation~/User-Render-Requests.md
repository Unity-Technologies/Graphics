# Render Requests

You can use a render request in a C# script to trigger a Camera to render to a render texture, outside the Unity rendering loop.

The request is processed sequentially in your script, so there's no callback involved.

## Use RenderPipeline.StandardRequest

`RenderPipeline.StandardRequest` renders the following:

- A full stack of cameras in the [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html) (URP).
- A single camera in the [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html) (HDRP).

The following code sample gets the output of the scriptable render pipeline when you select a GUI button. Attach the script to a camera and select **Enter Play Mode**.

```
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class StandardRenderRequest : MonoBehaviour
{
    [SerializeField]
    RenderTexture texture2D, texture2DArray, cubeMap, texture3D;

    // Render requests are sent when GUI button is selected
    private void OnGUI()
    {
        GUILayout.BeginVertical();
        if (GUILayout.Button("Render Request"))
        {
            SendRenderRequests();
        }
        GUILayout.EndVertical();
    }

    void SendRenderRequests()
    {
        Camera cam = GetComponent<Camera>();

        // Create a standard request
        RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();

        // Check if the request is supported by the active render pipeline
        if (RenderPipeline.SupportsRenderRequest(cam, request))
        {
            // Submit the render request to the active render pipeline with different destination textures

            // 2D Texture
            request.destination = texture2D;
            // Render camera and fill texture2D with its view
            RenderPipeline.SubmitRenderRequest(cam, request);

            // 2D Array Texture
            request.destination = texture2DArray;
            for (int i = 0; i < texture2DArray.volumeDepth; i++)
            {
                request.slice = i;
                // Render camera and fill slice i of texture2DArray with its view
                RenderPipeline.SubmitRenderRequest(cam, request);
            }

            // Cubemap
            var faces = new[] {
                CubemapFace.NegativeX, CubemapFace.PositiveX,
                CubemapFace.NegativeY, CubemapFace.PositiveY,
                CubemapFace.NegativeZ, CubemapFace.PositiveZ
            };
            request.destination = cubeMap;
            foreach (var face in faces)
            {
                request.face = face;
                // Render camera and fill face of cubeMap with its view
                RenderPipeline.SubmitRenderRequest(cam, request);
            }

            // 3D Texture
            request.destination = texture3D;
            for (int i = 0; i < texture3D.volumeDepth; i++)
            {
                request.slice = i;
                // Render camera and fill slice i of texture3D with its view
                RenderPipeline.SubmitRenderRequest(cam, request);
            }
        }
    }
}
```

