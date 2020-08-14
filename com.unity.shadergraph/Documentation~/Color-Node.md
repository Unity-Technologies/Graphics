# Color Node

## Description

Defines a constant **Vector 4** value in the shader using a **Color** field. Can be converted to a **Color** type [Property](Property-Types.md) via the [Node's](Node.md) context menu. The value of the **Mode** parameter will also respected when generating the [Property](Property-Types.md).

NOTE: Previous versions of the Color Node would convert HDR colors into the incorrect colorspace. The behavior is corrected now, and previously created Color Nodes will continue to use old behavior unless explicilty upgraded through the [Graph Inspector](Internal-Inspector.md). You may use a new Color Node in HDR space passed through one [Colorspace Conversion Node](Colorspace-Conversion-Node.md) **RGB** to **Linear** to mimic old behavior in a linear space project, and two in a Gamma space project.

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
