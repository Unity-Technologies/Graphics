# Step Node

## Description

Per component, returns 1 if the value of input **In** is greater than or equal to the value of input **Edge**, otherwise returns 0.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Edge      | Input | Dynamic Vector | Step value |
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Step_float4(float4 Edge, float4 In, out float4 Out)
{
    Out = step(Edge, In);
}
```