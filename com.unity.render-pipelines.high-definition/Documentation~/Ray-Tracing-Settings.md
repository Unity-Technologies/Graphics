# Ray Tracing Settings

In the High Definition Render Pipeline (HDRP), various ray-traced effects share common properties. Most of them are constants, but you may find it useful to control some of them. Use this [Volume Override](Volume-Components.md) to change these values.

## Setting the ray-tracing global parameters

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Ray Tracing** and click on **Ray Tracing Settings**.

## Manually building the Ray Tracing Acceleration Structure

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
        if (hdrp != null)
        {
            // Get the HDCamera for the current camera
            var hdCamera = HDCamera.GetOrCreate(GetComponent<Camera>());

            // Evaluate the effect params
            HDEffectsParameters hdEffectParams = HDRenderPipeline.EvaluateEffectsParameters(hdCamera, true, false);

            // Clear the rtas from the previous frame
            if (rtas != null)
                rtas.Dispose();

            // Create the RTAS
            rtas = new RayTracingAccelerationStructure();

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

    void OnDestroy()
    {
        if (rtas != null)
            rtas.Dispose();
    }
}
```

## Properties

| **Property**             | **Description**                                              |
| ------------------------ | ------------------------------------------------------------ |
| **Ray Bias** | Specifies the bias value HDRP applies when casting rays for all effects. This value should remain unchained unless your scene scale is significantly smaller or larger than average. |
| **Extend Shadow Culling** | Extends the sets of GameObjects that HDRP includes in shadow maps for more accurate shadows in ray traced effects. |
| **Extend Camera Culling** | Extends the sets of GameObjects that HDRP includes in the rendering. This is a way to force skinned mesh animations for GameObjects that are not in the frustum. |
| **Directional Shadow Ray Length** | Controls the maximal ray length for ray traced directional shadows. |
| **Directional Shadow Fallback Intensity** | The shadow intensity value HDRP applies to a point when there is a [Directional Light](Light-Component.md) in the Scene and the point is outside the Light's shadow cascade coverage. This property helps to remove light leaking in certain environments, such as an interior room with a Directional Light outside. |
| **Build Mode** | Specifies if HDRP handles automatically the building of the ray tracing acceleration structure internally or if it's provided by the user through the camera. When set to Manual, the RTAS build mode expects a ray tracing acceleration structure to be set on the camera. If not, all ray traced effects will be disabled. This option does not affect the scene view. |
