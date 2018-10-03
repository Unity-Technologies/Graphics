## Description

Creates a mask from values in input **In** equal to input **Mask Color**. Input **Range** can be used to define a wider range of values around input **Mask Color** to create the mask. Colors within this range will return 1, otherwise the node will return 0. Input **Fuzziness** can be used to soften the edges around the selection similar to anti-aliasing.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value |
| Mask Color      | Input | Vector 3 | Color | Color to use for mask |
| Range      | Input | Vector 1 | None | Select colors within this range from input **From** |
| Fuzziness      | Input | Vector 1 | None | Soften edges around selection |
| Out | Output      |    Vector 1 | None | Output mask value |

## Shader Function

```
float Distance = distance(MaskColor, In);
Out = saturate(1 - (Distance - Range) / max(Fuzziness, 1e-5));
```