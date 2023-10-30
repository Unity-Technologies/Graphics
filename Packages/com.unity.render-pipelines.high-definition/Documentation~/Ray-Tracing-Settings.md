# Set global ray-tracing parameters

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Ray Tracing** and click on **Ray Tracing Settings**.

For more information about the ray tracing settings properties, refer to [Ray Tracing Settings reference](reference-ray-tracing-settings.md).

## Add objects to the Ray Tracing Acceleration Structure

HDRP provides a utility function that adds objects to the ray tracing acceleration structure.
The function is `AddInstanceToRAS` and it takes a [Renderer](https://docs.unity3d.com/ScriptReference/Renderer.html)) a `HDEffectsParameters` parameter and two a booleans that tracks changes in the transform and material properties of the included game objects.

```
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class ManualRTASManager : MonoBehaviour
{
    RayTracingAccelerationStructure rtas = null;
    public List<GameObject> gameObjects = new List<GameObject>();

    void Update()
    {
        HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline is HDRenderPipeline ? (HDRenderPipeline)RenderPipelineManager.currentPipeline : null;
        if (hdrp != null && rtas != null)
        {
            // Get the HDCamera for the current camera
            var hdCamera = HDCamera.GetOrCreate(GetComponent<Camera>());

            // Evaluate the effect params
            HDEffectsParameters hdEffectParams = HDRenderPipeline.EvaluateEffectsParameters(hdCamera, true, false);

            // Clear the contents of rtas from the previous frame
            rtas.ClearInstances();

            // Add all the objects individually
            int numGameObjects = gameObjects.Count;
            for (int i = 0; i < numGameObjects; ++i)
                HDRenderPipeline.AddInstanceToRAS(rtas, gameObjects[i].GetComponent<Renderer>(), hdEffectParams, ref hdCamera.transformsDirty, ref hdCamera.materialsDirty);

            // Build the RTAS
            rtas.Build(transform.position);

            // Assign it to the camera
            hdCamera.rayTracingAccelerationStructure = rtas;
        }
    }

    void Start()
    {
        if (rtas == null)
            rtas = new RayTracingAccelerationStructure();
    }

    void OnDestroy()
    {
        if (rtas != null)
            rtas.Dispose();
    }
}
```


