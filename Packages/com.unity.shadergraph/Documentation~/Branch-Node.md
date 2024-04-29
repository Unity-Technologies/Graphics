# Branch Node

## Description

Provides a dynamic branch to the shader. If input **Predicate** is true, this node returns input **True**, otherwise it returns input **False**. The **Branch Node** evaluates the **Predicate** per vertex or per pixel depending on shader stage. Both sides of the branch are evaluated in the shader, and the branch not used is discarded.

## Ports

| Name      | Direction | Type           | Binding | Description |
|:----------|:----------|:---------------|:--------|:------------|
| Predicate | Input     | Boolean        | None    | Determines which input to return. |
| True      | Input     | Dynamic Vector | None    | Returned if **Predicate** is true. |
| False     | Input     | Dynamic Vector | None    | Returned if **Predicate** is false. |
| Out       | Output    | Dynamic Vector | None    | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}
```
