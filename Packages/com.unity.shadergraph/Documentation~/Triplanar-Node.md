# Triplanar Node

## Description

Generates UVs and samples a texture by projecting in world space. This method is commonly used to texture large models such as terrain, where hand authoring UV coordinates would either be problematic or not performant. Samples the input **Texture** 3 times, once in each of the world x, y, and z axes. The resulting information is planar projected onto the model, blended by the normal, or surface angle. You can scale the generated UVs with the input **Tile** and you can control the final blending strength with the input **Blend**.

**Blend** controls the way the normal affects the blending of each plane sample and should be greater than or equal to 0. The larger **Blend** is, the more contribution will be given to the sample from the plane towards which the normal is most oriented. (The maximum blend exponent is between 17 and 158 depending on the platform and the precision of the node.) A **Blend** of 0 makes each plane get equal weight regardless of normal orientation. 

To choose the projection, change the **Input Space**. You can also modify the projection via the inputs **Position** and **Normal**. 

Use the **Type** dropdown to change the expected type of the input **Texture**. If set to **Normal**, the **Out** port returns the blended normals in **Normal Output Space**.

If you experience texture sampling errors while using this node in a graph which includes Custom Function Nodes or Sub Graphs, upgrade to version 10.3 or later.

NOTE: You can only use the Triplanar Node in the **Fragment** shader stage.

## Ports

| Name     | Direction | Type          | Binding              | Description |
|:---------|:----------|:--------------|:---------------------|:------------|
| Texture  | Input     | Texture       | None                 | Input texture value |
| Sampler  | Input     | Sampler State | None                 | Sampler for input **Texture** |
| Position | Input     | Vector 3      | Input Space Position | Fragment position |
| Normal   | Input     | Vector 3      | Input Space Normal   | Fragment normal |
| Tile     | Input     | Float         | None                 | Tiling amount for generated UVs |
| Blend    | Input     | Float         | None                 | Blend factor between different samples |
| Out      | Output    | Vector 4      | None                 | Output value |

## Controls

| Name      | Type        | Options         | Description |
|:--------- |:------------|:----------------|:------------|
| Type      | Dropdown    | Default, Normal | Type of input **Texture** |

## Node Settings Controls

The following controls appear on the Node Settings tab of the Graph Inspector, when you select the Triplanar Node.

| Name        | Type     | Options                                     | Description |
|:------------|:---------|:--------------------------------------------|:------------|
| Input Space | Dropdown | Object, View, World, Tangent, AbsoluteWorld | Controls the coordinate space used by the input ports **Position** and **Normal**.  When you change the **Input Space** value, it changes the bindings on the  **Position** and **Normal** ports to use the specified space. The default value is **AbsoluteWorld**. |
| Normal Output Space | Dropdown | Object, View, World, Tangent, AbsoluteWorld | Controls the coordinate space used for the **Out** port. The Normal Output Space control is only available when **Type** is set to **Normal**. The default value is **Tangent**. |

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
float3 Node_X = UnpackNormal(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.zy));
float3 Node_Y = UnpackNormal(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xz));
float3 Node_Z = UnpackNormal(SAMPLE_TEXTURE2D(Texture, Sampler, Node_UV.xy));
Node_X = float3(Node_X.xy + Normal.zy, abs(Node_X.z) * Normal.x);
Node_Y = float3(Node_Y.xy + Normal.xz, abs(Node_Y.z) * Normal.y);
Node_Z = float3(Node_Z.xy + Normal.xy, abs(Node_Z.z) * Normal.z);
float4 Out = float4(normalize(Node_X.zyx * Node_Blend.x + Node_Y.xzy * Node_Blend.y + Node_Z.xyz * Node_Blend.z), 1);
float3x3 Node_Transform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
Out.rgb = TransformWorldToTangent(Out.rgb, Node_Transform);
```
