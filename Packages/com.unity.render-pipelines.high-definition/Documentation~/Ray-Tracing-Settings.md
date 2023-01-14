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

## Properties

| **Property**             | **Description**                                              |
| ------------------------ | ------------------------------------------------------------ |
| **Ray Bias** | Specifies the bias value HDRP applies when casting rays for all effects. This value should remain unchained unless your scene scale is significantly smaller or larger than average. |
| **Distant Ray Bias** | Specifies the Ray Bias value used when the distance between the pixel and the camera is close to the far plane. Between the near and far plane the Ray Bias and Distant Ray Bias are interpolated linearly. This does not affect Path Tracing or Recursive Rendering. This value can be increased to mitigate Ray Tracing z-fighting issues at a distance. |
| **Extend Shadow Culling** | Extends the region that HDRP includes in shadow maps, to create more accurate shadows in ray traced effects. See [Extended frustum culling](#extended-culling) for more information. For Directional lights, cascades are not extended, but additional objects may appear in the cascades.|
| **Extend Camera Culling** | Extends the region that HDRP includes in rendering. This is a way to force skinned mesh animations for GameObjects that aren't in the frustum. See [Extended frustum culling](#extended-culling) for more information. |
| **Directional Shadow Ray Length** | Controls the maximal ray length for ray traced directional shadows. |
| **Directional Shadow Fallback Intensity** | The shadow intensity value HDRP applies to a point when there is a [Directional Light](Light-Component.md) in the Scene and the point is outside the Light's shadow cascade coverage. This property helps to remove light leaking in certain environments, such as an interior room with a Directional Light outside. |
| **Acceleration Structure Build Mode** | Specifies if HDRP handles automatically the building of the ray tracing acceleration structure internally or if it's provided by the user through the camera. When set to Manual, the RTAS build mode expects a ray tracing acceleration structure to be set on the camera. If not, all ray traced effects will be disabled. This option does not affect the scene view. |
| **Culling Mode** | Specifies which technique HDRP uses to cull geometry out of the ray tracing acceleration structure. When set to Extended frustum, HDRP automatically generates a camera oriented bounding box that extends the camera's frustum. When set to Sphere, a bounding sphere is used for the culling step. |
| **Culling Distance** | Specifies the radius of the sphere used to cull objects out of the ray tracing acceleration structure when the culling mode is set to Sphere. |

### <a name="extended-culling"></a>Extended frustum culling

![](Images/RayTracingSettings_extended_frustum.png)

If you enable **Extend Shadow Culling** or **Extend Camera Culling**, HDRP sets the culling region to the width and height of the frustum at the far clipping plane, and sets the depth to twice the distance from the camera to the far clipping plane.
