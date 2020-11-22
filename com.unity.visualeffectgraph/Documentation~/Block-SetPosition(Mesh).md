# Set Position (Mesh)

Menu Path : **Position > Set Position (Mesh)**

Menu Path : **Position > Set Position (Skinned Mesh)**

The **Set Position (Mesh)** block fetch a position based on an vertex data of and stores the result in the [position attribute](Reference-Attributes.md), based on composition. 


This Block also calculates a direction vector based on the mesh normal, and stores it to the [direction attribute](Reference-Attributes.md), based on composition.

Note: [Velocity from Direction and Speed](Block-VelocityFromDirectionAndSpeed.md) Blocks can then process the direction attribute.



## Block compatibility

This Block is compatible with the following Contexts:

- [Initialize](Context-Initialize.md)
- [Update](Context-Update.md)
- Any output Context

## Block settings

| **Setting**               | **Type** | **Description**                                              |
| ------------------------- | -------- | ------------------------------------------------------------ |
| **Spawn Mode**            | Enum     | Specifies how to distribute particles among primitive within the mesh. The options are:<br/>&#8226; **Random**: Calculates a per-particle random progress uniform sampling among the chosen primitive in **Placement Mode** .<br/>&#8226; **Custom**: Allows you to manually specify the sampling parameters. |
| **Placement mode**        | Enum     | The placement mode choose what kind of primitive to consider while sampling.<br/>&#8226; **Vertex**: Sample among all listed vertices without using any index buffer.<br/>&#8226; **Edge: **Interpolate among two consecutives vertices drawing a triangle. <br/>&#8226; **Surface:** Interpolate between three vertices defining a triangle. |
| **Surface coordinates**   | Enum     | Choose the approach of sampling in triangle.<br/>&#8226; **Barycentric**: Raw barencentric coordinates.<br/>&#8226; **Uniform**: Uniform placement within the triangle area.<br/>This property only appears if you set **Placement mode** to **Surface** and **Spawn Mode** to **Custom** |
| **Source**                | Enum     | **(Inspector)** Choose the kind of geometry to sample, either a **Mesh** or a **Skinned Mesh Renderer** |
| **Composition Position**  | Enum     | **(Inspector)** Specifies how this Block composes the position attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |
| **Composition Direction** | Enum     | **(Inspector)** Specifies how this Block composes the direction attribute. The options are:<br/>&#8226; **Set**: Overwrites the position attribute with the new value.<br/>&#8226; **Add**: Adds the new value to the position attribute value.<br/>&#8226; **Multiply**: Multiplies the position attribute value by the new value.<br/>&#8226; **Blend**: Interpolates between the position attribute value and the new value. You can specify the blend factor between 0 and 1. |

## Block properties

| **Input**                 | **Type**              | **Description**                                              |
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Mesh**                  | Mesh                  | The source mesh asset to sample.<br/>This property only appears if you set **Source** to **Mesh** |
| **Skinned Mesh Renderer** | Skinned Mesh Renderer | The source skinned mesh renderer to sample, a reference to a component within the scene, has to be an exposed entry.<br/>This property only appears if you set **Source** to **Skinned Mesh Renderer** |
| **Vertex**                | uint                  | The index of the vertex to sample.<br/>This property only appears if you set **Placement mode** to **Vertex** and **Spawn Mode** to **Custom**. |
| **Index**                 | uint                  | The start index of edge, this index correspond of all consecutive edges of every triangles.<br/>This property only appears if you set **Placement mode** to **Edge** and **Spawn Mode** to **Custom**. |
| **Triangle**              | uint                  | The index of triangle to sample, assuming the index buffer express a triangle list.<br/>This property only appears if you set **Placement mode** to **Surface** and **Spawn Mode** to **Custom** and **Spawn Mode** to **Custom**. |
| **X**                     | float                 | Interpolation value between start and end edge position.<br/>This property only appears if you set **Placement mode** to **Edge** and **Spawn Mode** to **Custom**. |
| **Barycentric**           | Vector2               | Raw barycentric coordinate of the triangle, x and y are exposed and z is computed to respect the surface constraint : z = 1 - x - y. This sampling do **not** keep sampling value inside triangle.<br/>This property only appears if you set **Placement mode** to **Surface** and **Surface coordinates** to **Barycentric** and **Spawn Mode** to **Custom**. |
| **Square**                | Vector2               | Uniform placement inside the triangle described by [this mapping](https://hal.archives-ouvertes.fr/hal-02073696v2/document).<br/>This property only appears if you set **Placement mode** to **Surface** and **Surface coordinates** to **Uniform** and **Spawn Mode** to **Custom**. |
| **Blend Position**        | Float                 | The blend percentage between the current position attribute value and the newly calculated position value.<br/>This property only appears if you set **Composition Position** to **Blend**. |
| **Blend Direction**       | Float                 | The blend percentage between the current direction attribute value and the newly calculated direction value.<br/>This property only appears if you set **Composition Direction** to **Blend**. |

#### Limitations

The Mesh sampling feature has the following limitations:

- Only support [VertexAttributeFormat.Float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html) for all [VertexAttribute](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.html) expect [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html) which has to be a four component attributes using [VertexAttributeFormat.UInt8](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.UInt8.html) format or [VertexAttributeFormat.Float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html).
- If a Mesh is not [readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html), the **Position (Mesh)** Block and **Sample Mesh** Operator return zero values when they attempt to sample it. For information on how to make a Mesh readable, see [Model import settings](https://docs.unity3d.com/Manual/FBXImporter-Model.html)

<img src="G:/Unity/Dev_VFX_Extra/com.unity.visualeffectgraph/Documentation~/Images/ReadWrite.png" alt="image-20200320154843722" style="zoom:78%;" />