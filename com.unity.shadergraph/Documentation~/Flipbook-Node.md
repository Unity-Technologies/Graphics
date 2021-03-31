# Flipbook Node

## Description

Creates a flipbook, or texture sheet animation, of the UVs supplied to input **UV**. The amount of tiles on the sheet are defined by the values of the inputs **Width** and **Height**. The index of the current tile is defined by the value of the input **Tile**.

This node can be used to create a texture animation functionality, commonly used for particle effects and sprites, by supplying [Time](Time-Node.md) to the input **Tile** and outputting to the UV input slot of a [Texture Sampler](Sample-Texture-2D-Node.md).

UV data is typically in the range of 0 to 1 starting from the bottom left of UV space. This can be seen by the black value at the bottom left corner of a UV preview. As flipbooks typically start from top left the parameter **Invert Y** is enabled by default, however you can change the direction of the Flipbook by switching the **Invert X** and **Invert Y** parameters.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Width      | Input | Float    | None | Amount of horizontal tiles |
| Height      | Input | Float    | None | Amount of vertical tiles |
| Tile      | Input | Float    | None | Current tile index |
| Out | Output      |    Vector 2 | None | Output UV value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Invert X      | Toggle | True, False | If enabled tiles are iterated from right to left |
| Invert Y      | Toggle | True, False | If enabled tiles are iterated from top to bottom |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float2 _Flipbook_Invert = float2(FlipX, FlipY);

void Unity_Flipbook_float(float2 UV, float Width, float Height, float Tile, float2 Invert, out float2 Out)
{
    Tile = fmod(Tile, Width * Height);
    float2 tileCount = float2(1.0, 1.0) / float2(Width, Height);
    float tileY = abs(Invert.y * Height - (floor(Tile * tileCount.x) + Invert.y * 1));
    float tileX = abs(Invert.x * Width - ((Tile - Width * floor(Tile * tileCount.x)) + Invert.x * 1));
    Out = (UV + float2(tileX, tileY)) * tileCount;
}
```
