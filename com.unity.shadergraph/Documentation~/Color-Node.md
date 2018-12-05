# Color Node

## Description

Defines a constant **Vector 4** value in the shader using a **Color** field. Can be converted to a **Color** type [Property](Property-Types.md) via the [Node's](Node.md) context menu. The value of the **Mode** parameter will also respected when generating the [Property](Property-Types.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Vector 4 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|       | Color |  | Defines the output value. |
| Mode  | Dropdown | Default, HDR | Sets properties of the Color field |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 _Color = IsGammaSpace() ? float4(1, 2, 3, 4) : float4(SRGBToLinear(float3(1, 2, 3)), 4);
```