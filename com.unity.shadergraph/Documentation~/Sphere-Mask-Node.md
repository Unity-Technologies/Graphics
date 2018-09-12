## Description

Creates a sphere mask originating from input **Center**. The sphere is calculated using [Distance](Distance-Node.md) and modified using the **Radius** and **Hardness** inputs. Sphere mask functionality works in both 2D and 3D spaces, and is based on the vector coordinates in the **Coords** input.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Coords      | Input | Dynamic Vector | None | Coordinate space input |
| Center      | Input | Dynamic Vector | None | Coordinates of the sphere origin |
| Radius      | Input | Vector 1 | None | Radius of the sphere |
| Hardness      | Input | Vector 1 | None | Soften falloff of the sphere |
| Out | Output      |    Dynamic Vector | None | Output mask value |

## Shader Function

`Out = 1 - saturate((distance(Coords, Center) - Radius) / (1 - Hardness));`