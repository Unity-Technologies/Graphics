# Branch Node

## Description

Provides a dynamic branch to the shader. If input **Predicate** is true the return output will be equal to input **True**, otherwise it will be equal to input **False**. This is determined per vertex or per pixel depending on shader stage. Both sides of the branch will be calculated in the shader, even if one is never output.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Predicate      | Input | Boolean | None | Determines which input to returned |
| True     | Input | Dynamic Vector | None | Returned if **Predicate** is true |
| False      | Input | Dynamic Vector | None | Returned if **Predicate** is false |
| Out | Output      |    Boolean | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = lerp(False, True, Predicate);
}
```