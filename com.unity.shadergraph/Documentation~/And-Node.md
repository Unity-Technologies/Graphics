# And Node

## Description

Returns true if both the inputs **A** and **B** are true. This is useful for [Branching](Branch-Node.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| A      | Input | Boolean | None | First input value |
| B      | Input | Boolean | None | Second input value |
| Out | Output      |    Boolean | None | Output value |

## Generated Code Example

```
void Unity_And(float A, float B, out float Out)
{
    Out = A && B;
}
```
