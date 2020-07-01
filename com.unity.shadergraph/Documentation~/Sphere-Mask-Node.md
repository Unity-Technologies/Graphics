# Sphere Mask Node

## Description

Creates a sphere mask originating from input **Center**. The sphere is calculated using [Distance](Distance-Node.md) and modified using the **Radius** and **Hardness** inputs. Sphere mask functionality works in both 2D and 3D spaces, and is based on the vector coordinates in the **Coords** input. These vector coordinates can either be 3D like world space position, or 2D like UV coordinates.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Coords      | Input | Dynamic Vector | None | Coordinate space input |
| Center      | Input | Dynamic Vector | None | Coordinates of the sphere origin |
| Radius      | Input | Vector 1 | None | Radius of the sphere |
| Hardness      | Input | Vector 1 | None | Soften falloff of the sphere |
| Out | Output      |    Dynamic Vector | None | Output mask value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_SphereMask_float4(float4 Coords, float4 Center, float Radius, float Hardness, out float4 Out)
{
    Out = 1 - saturate((distance(Coords, Center) - Radius) / (1 - Hardness));
}
```