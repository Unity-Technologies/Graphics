# Normal Unpack Node

## Description

Unpacks a normal map defined by input **In**. This node is used to unpack a texture that is defined as a **Normal Map** in its Texture Import Settings when it is sampled as if it were a default texture.

Note that in most cases this node is unnecessary as the normal map should be sampled as such by setting its **Type** parameter to **Normal** when it is sampled using a [Sample Texture 2D](Sample-Texture-2D-Node) or [Triplanar](Triplanar-Node) node.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 4 | None | Input value |
| Out | Output      |    Vector 3 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Space      | Dropdown | Tangent, Object | Sets the coordinate space of the input normal. |

## Generated Code Example

The following example code represents one possible outcome of this node per **Space** mode.

**Tangent**

```
void Unity_NormalUnpack_float(float4 In, out float3 Out)
{
    Out = UnpackNormalmapRGorAG(In);
}
```

**Object**

```
void Unity_NormalUnpackRGB_float(float4 In, out float3 Out)
{
    Out = UnpackNormalmapRGB(In);
}
```