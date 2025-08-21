# Replace Color node

The Replace Color node replaces a color in the input with another color.

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:------------ |:-------------|:-----|:---|:---|
| **In** | Input | Vector 3 | None | Sets the input you want to replace a color in. For example, a texture. |
| **From** | Input | Vector 3 | Color | Sets the color to replace. |
| **To** | Input | Vector 3 | Color | Sets the color to replace **From** with. |
| **Range** | Input | Float | None | Sets the range around **From** to replace. For example, if you set **From** to (0, 0, 0) and **Range** to 0.1, Unity replaces colors from (0, 0, 0) to (0.1, 0.1, 0.1) with **To**. |
| **Fuzziness** | Input | Float | None | Sets how much to soften the boundary between the replaced color and the rest of the colors. |
| **Out** | Output | Vector 3 | None | The **In** input, with the **From** color replaced with the **To** color. |

## Generated code example

The following example code represents one possible outcome of this node.

```
void Unity_ReplaceColor_float(float3 In, float3 From, float3 To, float Range, float Fuzziness, out float3 Out)
{
    float Distance = distance(From, In);

    // Use max to avoid division by zero
    Out = lerp(To, In, saturate((Distance - Range) / max(Fuzziness, 1e-5f)));
}
```
