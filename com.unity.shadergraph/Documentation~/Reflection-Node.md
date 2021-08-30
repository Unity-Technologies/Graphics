# Reflection Node

## Description

Returns a reflection vector using input **In** and a surface normal **Normal**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Incident vector value |
| Normal      | Input      |   Dynamic Vector | Normal vector value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Reflection_float4(float4 In, float4 Normal, out float4 Out)
{
    Out = reflect(In, Normal);
}
```
