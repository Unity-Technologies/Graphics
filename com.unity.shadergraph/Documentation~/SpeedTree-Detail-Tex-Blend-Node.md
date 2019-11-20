# SpeedTree Detail Tex Blend Node

## Description

Blends between the detail texture and the base color texture based on one of two values.  This is purpose built for SpeedTree version 7 assets and is designed such that the node does nothing (i.e. only outputs base color) otherwise.  This node is a utility node that serves the same function as the standard behavior which is assumed upon export with SpeedTree version 7.

## Ports

| Name         | Direction | Type           | Description                            |
| :----------- | :-------- | :------------- | :------------------------------------- |
| Discriminant | Input     | Dynamic Scalar | Determines which value to use for lerp |
| Base Color   | Input     | Dynamic Color  | Sampled color of base texture          |
| Detail Color | Input     | Dynamic Color  | Sampled color of detail texture        |
| Out          | Output    | Dynamic Color  | Output color after blend               |

## Notes

For this node to have any effect, two things must be true.  

* The Master Node must be set to target SpeedTree 7 assets
* The Material using this shader must have its Geometry Type set to "Branch Detail"

The standard subgraph for usage of this node (assuming that the goal is to match SpeedTree's default behavior)

![](images\SpeedTreeDetailGraph.JPG)

The concept is that the "alpha" channel of the main UV channel (or potentially the alpha value of the detail texture) is used to blend between the main texture and detail texture.   Notably, the detail texture is also sampled using the third UV channel (UV2).  The reason for using UV2 and the alpha channel of UV0 for the "Discriminant" is simply because SpeedTree v7 just so happens to export assets with the salient data packed that way.

## Generated Code Sample

The following code represents the standard code generated from this node when it is actually active.  The code below will only be active on a SpeedTree v7 asset with the material labeled for a geometry piece of type "Branch Detail."  In any other setup, the node simply outputs Base Color.

```c++
void SpeedTree_DetailTexBlend(float Discriminant, float4 BaseColor, float4 DetailColor, out float4 OutColor)
{
    Out.rgb = lerp(BaseColor.rgb, DetailColor.rgb, Discriminant < 2.0 ? saturate(Discriminant) : DetailColor.a);
    Out.a = BaseColor.a;
}
```