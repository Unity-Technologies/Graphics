# Lerp Node

## Description

Returns the result of linearly interpolating between input **A** and input **B** by input **T**.

Unity calculates the output as:

A + T &times; (B &minus; A)

The value of input **T** acts as a weight factor applied to the difference between **B** and **A**:

- When **T** is `0`, the output equals **A**.
- When **T** is `1`, the output equals **B**.
- When **T** is `0.5`, the output is the midpoint between **A** and **B**.

The Lerp node uses Dynamic Vector slots, so **A**, **B**, and **T** always resolve to the same component count, which matches the smallest connected vector (larger vectors truncate). Scalars promote to the resolved size by duplicating their value across components.

## Ports

| Name | Direction | Type           | Description |
|:-----|:----------|:---------------|:------------|
| **A**    | Input     | Dynamic Vector | First input value  |
| **B**    | Input     | Dynamic Vector | Second input value |
| **T**    | Input     | Dynamic Vector | Time value. Typical range: 0 to 1. Though you can use values outside of this range they may cause unpredictable results. |
| **Out**  | Output    | Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}
```
