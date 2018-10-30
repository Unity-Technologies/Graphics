# Tiling And Offset Node

## Description

Tiles and offsets the value of input **UV** by the inputs **Tiling** and **Offset** respectively. This is commonly used for detail maps and scrolling textures over [Time](Time-Node.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Tiling      | Input | Vector 2 | None | Amount of tiling to apply per channel |
| Offset      | Input | Vector 2 | None | Amount of offset to apply per channel |
| Out | Output      |    Vector 2 | None | Output UV value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
{
    Out = UV * Tiling + Offset;
}
```