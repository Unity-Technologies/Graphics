# Not node

The Not node outputs the opposite of an input. If the input is true the output is false, otherwise the output is true. This node is useful for [branching](Branch-Node.md).

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:------------ |:-------------|:-----|:---|:---|
| **In** | Input | Boolean | None | The input value. |
| **Out** | Output | Boolean | None | The opposite of **In**. |

## Generated code example

The following example code represents one possible outcome of this node.

```
Out = !In;
```
