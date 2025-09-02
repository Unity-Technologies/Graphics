# Normal Unpack Node

## Description

Unpacks a normal map defined by input **In**. This node is used to unpack a texture that is defined as a **Normal Map** in its Texture Import Settings when it is sampled as if it were a default texture.

Data is stored in textures from 0 to 1. But vectors need to be from -1 to 1. Unpacking the normal means to expand its range from the original range to a range of -1 to 1, so you can use it as a vector.

Note that in most cases this node is unnecessary as the normal map should be sampled as such by setting its **Type** parameter to **Normal** when it is sampled using a [Sample Texture 2D](Sample-Texture-2D-Node.md) or [Triplanar](Triplanar-Node.md) node.

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
    Out = UnpackNormalMapRGorAG(In);
}
```

**Object**

```
void Unity_NormalUnpackRGB_float(float4 In, out float3 Out)
{
    Out = UnpackNormalmapRGB(In);
}
```
