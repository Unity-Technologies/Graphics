# Branch Node

## Description

Provides a dynamic branch to the shader. If input **Predicate** is true, the return output is equal to input **True**. Otherwise, it is equal to input **False**. Shader Graph determines the return output per vertex or per pixel based on the shader stage. It evaluates both sides of the branch in the shader, and discards the unused branch.

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
    Out = Predicate ? True : False;
}
```
