# Replace Color Node

## Description

Replaces values in input **In** equal to input **From** to the value of input **To**. Input **Range** can be used to define a wider range of values around input **From** to replace. Input **Fuzziness** can be used to soften the edges around the selection similar to anti-aliasing.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value |
| From      | Input | Vector 3 | Color | Color to replace |
| To      | Input | Vector 3 | Color | Color to replace with |
| Range      | Input | Float    | None | Replace colors within this range from input **From** |
| Fuzziness      | Input | Float    | None | Soften edges around selection |
| Out | Output      |    Vector 3 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_ReplaceColor_float(float3 In, float3 From, float3 To, float Range, float Fuzziness, out float3 Out)
{
    float Distance = distance(From, In);
    Out = lerp(To, In, saturate((Distance - Range) / max(Fuzziness, 1e-5f)));
}
```