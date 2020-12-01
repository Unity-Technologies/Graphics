# Fresnel Effect Node

## Description

**Fresnel Effect** is the effect of differing reflectance on a surface depending on viewing angle, where as you approach the grazing angle more light is reflected. The **Fresnel Effect** node approximates this by calculating the angle between the surface normal and the view direction. The wider this angle is, the greater the return value will be. This effect is often used to achieve rim lighting, common in many art styles.

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
    Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
}
```