# Fresnel Effect Node

## Description

**Fresnel Effect** is the effect of differing reflectance on a surface depending on viewing angle, where as you approach the grazing angle more light is reflected. This effect is often used to achieve rim lighting, common in many art styles. 

The **Fresnel Effect** node approximates this by calculating the angle between the surface normal and the view direction.  The output value is a floating-point number between `0` and `1`. This value represents the viewing angle relative to the surface and the Fresnel effect that is being calculated:

**0**: The viewing angle is 0&deg; from the surface normal, as if you are looking straight at the object.  
**1**: The viewing angle is 90&deg; from the surface normal, as if you are looking along the surface or edge of the object.

A low Fresnel value (output value of `0`) means the surface appears as if viewed from the front, with minimal rim highlighting. A high Fresnel value(`1`) means the surface appears as if viewed from the side, with a strong rim highlight.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Normal      | Input | Vector 3 | Normal direction. By default bound to World Space Normal |
| View Dir      | Input | Vector 3 | View direction. By default bound to World Space View Direction |
| Power      | Input | Float    | Exponent of the power calculation |
| Out | Output      |   Float    | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_FresnelEffect_float(float3 Normal, float3 ViewDir, float Power, out float Out)
{
    Out = pow((1.0 - saturate(dot(normalize(Normal), ViewDir))), Power);
}
```
