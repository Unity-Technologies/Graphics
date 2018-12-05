# Triplanar Node

## Description

Triplanar is a method of generating UVs and sampling a texture by projecting in world space. The input **Texture** is sampled 3 times, once in each of the world x, y and z axises, and the resulting information is planar projected onto the model, blended by the normal, or surface angle. The generated UVs can be scaled with the input **Tile** and the final blending strength can be controlled with the input **Blend**. The projection can be modified by overriding the inputs **Position** and **Normal**. This is commonly used to texture large models such as terrain, where hand authoring UV coordinates would be problematic or not performant.

The expected type of the input **Texture** can be switched with the dropdown **Type**. If set to **Normal** the normals will be converted into world space so new tangents can be constructed then converted back to tangent space before output.

NOTE: This [Node](Node.md) can only be used in the **Fragment** shader stage.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture      | Input | Texture | None | Input texture value |
| Sampler      | Input | Sampler State | None | Sampler for input **Texture** |
| Position      | Input | Vector 3 | World Space Position | Fragment position |
| Normal      | Input | Vector 3 | World Space Normal | Fragment normal |
| Tile      | Input | Vector 1 | None | Tiling amount for generated UVs |
| Blend      | Input | Vector 1 | None | Blend factor between different samples |
| Out | Output      |    Vector 4 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Type      | Dropdown | Default, Normal | Type of input **Texture** |

## Generated Code Example

The following example code represents one possible outcome of this node.

**Default**

```
float3 Node_UV = Position * Tile;
float3 Node_Blend = pow(abs(Normal), Blend);
Node_Blend /= dot(Node_Blend, 1.0);
float4 Node_X = SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.zy);
float4 Node_Y = SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xz);
float4 Node_Z = SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xy);
float4 Out = Node_X * Node_Blend.x + Node_Y * Node_Blend.y + Node_Z * Node_Blend.z;
```

**Normal**

```
float3 Node_UV = Position * Tile;
float3 Node_Blend = max(pow(abs(Normal), Blend), 0);
Node_Blend /= (Node_Blend.x + Node_Blend.y + Node_Blend.z ).xxx;
float3 Node_X = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.zy));
float3 Node_Y = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xz));
float3 Node_Z = UnpackNormalmapRGorAG(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xy));
Node_X = float3(Node_X.xy + Normal.zy, abs(Node_X.z) * Normal.x);
Node_Y = float3(Node_Y.xy + Normal.xz, abs(Node_Y.z) * Normal.y);
Node_Z = float3(Node_Z.xy + Normal.xy, abs(Node_Z.z) * Normal.z);
float4 Out = float4(normalize(Node_X.zyx * Node_Blend.x + Node_Y.xzy * Node_Blend.y + Node_Z.xyz * Node_Blend.z), 1);
float3x3 Node_Transform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
Out.rgb = TransformWorldToTangent(Out.rgb, Node_Transform);
```