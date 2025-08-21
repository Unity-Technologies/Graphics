# Scene Depth node

The Scene Depth node samples the depth texture of the current camera, using the screen space coordinates you input. The node returns the depth of the closest object the camera sees along the path towards the coordinates, or 1 (white) if no object is present.

If you use the Universal Render Pipeline (URP), make sure the depth texture is enabled in the [URP asset](https://docs.unity3d.com/Manual/urp/universalrp-asset.html). Otherwise the Scene Depth node returns a value of 0.5 (mid-grey).

The Scene Depth node works only in the fragment [shader stage](Shader-Stage.md), and might not work if you set **Surface Type** to **Opaque** in the **Graph Inspector** window.

## Render pipeline support

This node supports the following render pipelines:

- High Definition Render Pipeline (HDRP)
- Universal Render Pipeline (URP)

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:------------ |:-------------|:-----|:---|:---|
| **UV** | Input | Vector 4 | Screen position | The normalized screen space coordinates to sample from. |
| **Out** | Output | Float | None | The depth value from the depth texture at the **UV** coordinates. |

## Sampling modes

| **Name** | **Description** |
|----------|------------------------------------|
| **Linear 01** | Returns the linear depth value. The range is from 0 to 1. 0 is the near clipping plane of the camera, and 1 is the far clipping plane of the camera. |
| **Raw** | Returns the non-linear depth value. The range is from 0 to 1. 0 is the near clipping plane of the camera, and 1 is the far clipping plane of the camera. |
| **Eye** | Returns the depth value as the distance from the camera in meters. |

For more information about clipping planes, refer to [Introduction to the camera view](https://docs.unity3d.com/Manual/UnderstandingFrustum.html).

## Generated code example

The HLSL code this node generates depends on the render pipeline you use. If you use your own custom render pipeline, you must define the behaviour of the node yourself, otherwise the node returns a value of 1 (white).

The following example code represents one possible outcome of this node.

```
void Unity_SceneDepth_Raw_float(float4 UV, out float Out)
{
    Out = SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV);
}
```