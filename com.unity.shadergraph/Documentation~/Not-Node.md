# Not Node

## Description

Returns the opposite of input **In**. If **In** is true the output will be false, otherwise it will be true. This is useful for [Branching](Branch-Node.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Boolean | None | Input value |
| Out | Output      |    Boolean | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_NormalUnpack_float(float In, out float Out)
{
    Out = !In;
}
```
