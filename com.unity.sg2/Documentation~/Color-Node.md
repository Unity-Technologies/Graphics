# Color Node

## Description

Defines a constant **Vector 4** value in the shader using a **Color** field. Can be converted to a **Color** [Property Type](Property-Types.md) via the [Node's](Node.md) context menu. The value of the **Mode** parameter will also respected when generating the [Property](Property-Types.md).

NOTE: In versions prior to 10.0, Shader Graph assumed that HDR colors from the Color Node were in gamma space. Version 10.0 corrected this behavior, and Shader Graph now interprets HDR colors in linear space. HDR Color nodes that you created with older versions maintain the old behavior, but you can use the [Graph Inspector](Internal-Inspector.md) to upgrade them. To mimic the old behavior on a new HDR Color node, you can use a [Colorspace Conversion Node](Colorspace-Conversion-Node.md) to convert the HDR color from **RGB** to **Linear**.

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
