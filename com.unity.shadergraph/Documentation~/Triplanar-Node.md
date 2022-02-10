# Triplanar Node

## Description

Triplanar is a method of generating UVs and sampling a texture by projecting in world space. The input **Texture** is sampled 3 times, once in each of the world x, y and z axes, and the resulting information is planar projected onto the model, blended by the normal, or surface angle. The generated UVs can be scaled with the input **Tile** and the final blending strength can be controlled with the input **Blend**. **Blend** controls the way the normal affects the blending of each plane sample and should be greater or equal to 0. The larger **blend** is, the more contribution will be given to the sample from the plane towards which the normal is most oriented. (The maximum blend exponent is between 17 and 158 depending on platform and the precision of the node.) A blend of 0 makes each plane get equal weight regardless of normal orientation. The projection can be modified by overriding the inputs **Position** and **Normal**. This is commonly used to texture large models such as terrain, where hand authoring UV coordinates would be problematic or not performant.

The expected type of the input **Texture** can be switched with the dropdown **Type**. If set to **Normal** the normals will be converted into world space so new tangents can be constructed then converted back to tangent space before output.

If you experience texture sampling errors while using this node in a graph which includes Custom Function Nodes or Sub Graphs, you can resolve them by upgrading to version 10.3 or later.

NOTE: This [Node](Node.md) can only be used in the **Fragment** shader stage.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture      | Input | Texture | None | Input texture value |
| Sampler      | Input | Sampler State | None | Sampler for input **Texture** |
| Position      | Input | Vector 3 | World Space Position | Fragment position |
| Normal      | Input | Vector 3 | World Space Normal | Fragment normal |
| Tile      | Input | Float    | None | Tiling amount for generated UVs |
| Blend      | Input | Float    | None | Blend factor between different samples |
| XYZ | Output      |    Vector 4 | None | Three planes projection value |
| XZ | Output      |    Vector 4 | None | X and Z axes projection value |
| Y | Output      |    Vector 4 | None | Y axis projection value |

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
float4 Out_XYZ = Node_X * Node_Blend.x + Node_Y * Node_Blend.y + Node_Z * Node_Blend.z;
float4 Out_XZ = Node_X * Node_Blend.x + Node_Z * Node_Blend.z;
float4 Out_Y = Node_Y * Node_Blend.y;
```

**Normal**

```
float3 Node_UV = Position * Tile;
float3 Node_Blend = max(pow(abs(Normal), Blend), 0);
Node_Blend /= (Node_Blend.x + Node_Blend.y + Node_Blend.z ).xxx;
float3 Node_X = UnpackNormal(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.zy));
float3 Node_Y = UnpackNormal(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xz));
float3 Node_Z = UnpackNormal(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xy));
Node_X = float3(Node_X.xy + Normal.zy, abs(Node_X.z) * Normal.x);
Node_Y = float3(Node_Y.xy + Normal.xz, abs(Node_Y.z) * Normal.y);
Node_Z = float3(Node_Z.xy + Normal.xy, abs(Node_Z.z) * Normal.z);
float4 Out_XYZ = float4(normalize(Node_X.zyx * Node_Blend.x + Node_Y.xzy * Node_Blend.y + Node_Z.xyz * Node_Blend.z), 1);
float4 Out_XZ = float4(normalize(Node_X.zyx * Node_Blend.x + Node_Z.xyz * Node_Blend.z), 1);
float4 Out_Y = float4(normalize(Node_Y.xzy * Node_Blend.y), 1);

float3x3 Node_Transform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
Out_XYZ.rgb = TransformWorldToTangent(Out_XYZ.rgb, Node_Transform);
Out_XZ.rgb = TransformWorldToTangent(Out_XZ.rgb, Node_Transform);
Out_Y.rgb = TransformWorldToTangent(Out_Y.rgb, Node_Transform);
```
