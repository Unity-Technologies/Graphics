# Normal Blend Node

## Description

Blends two normal maps defined by inputs **A** and **B** together, normalizing the result to create a valid normal map.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| A      | Input | Vector 3 | None | First input value |
| B      | Input | Vector 3 | None | Second input value |
| Out | Output      |    Vector 3 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_NormalBlend_float(float3 A, float3 B, out float3 Out)
{
    Out = normalize(float3(A.rg + B.rg, A.b * B.b));
}
```