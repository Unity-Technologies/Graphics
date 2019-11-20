# SpeedTree Hue Variation Node

## Description

Randomizes color through a hue shift on individual SpeedTree instances based on the instance transformations derived through object space normal, position, and transformation.  The approach is a sort of hash operation that results in a pseudo-random weight for blending into an tinted form of that color.  The alpha component of the tint color denotes a maximum weight value for the randomization..

The implementation used here is derived from IDV Inc.'s own technology for color randomization in large forests of SpeedTree instances.  While the node is usable in other contexts, it is safe to assume that it is purpose-made to provide results similar to those that might otherwise result from SpeedTree's world building tools.

## Ports

| Name                | Direction | Type           | Description                              |
| :------------------ | :-------- | :------------- | :--------------------------------------- |
| Position            | Input     | Dynamic Vector | Position of point (default Object Space) |
| Normal              | Input     | Dynamic Vector | Normal at point (default Object Space)   |
| Base Color          | Input     | Dynamic Color  | Original color of shaded point           |
| Hue Variation Color | Input     | Fixed Color    | Hue color to randomly shift through      |
| OutColor            | Output    | Dynamic Color  | Output color after shift (if any)        |

## Generated Code Example

The following code represents the most common/default of code generated from this node.  The example omits certain details which are dependent on compile state that may slightly alter the resulting code. 

```c++
void SpeedTree_HueVariation(float3 ObjPos, float3 ObjNorm, float3 BaseColor, float4 HueColor, out float4 OutColor)
{
    float3 objWorldPos = float3(objToWorld[0].w, objToWorld[1].w, objToWorld[2].w);
    float hueVariationAmount = frac(dot(objWorldPos, 1));
    hueVariationAmount += frac(ObjPos.x + ObjNorm.y + ObjNorm.z) * 0.5 - 0.3 ;
    hueVariationAmout = saturate(hueVariationAmount * HueColor.a);
    
    float3 shiftedColor = lerp(BaseColor, HueColor.rgb, hueVariationAmount);
    float maxBase = max(BaseColor.r, max(BaseColor.g, BaseColor.b));
    float maxShifted = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
    maxBase = (maxBase / maxShifted) * 0.5 + 0.5;
    
    OutColor = saturate(shiftedColor.rgb * maxBase);
}
```