<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This Feature is currently experimental and is subject to change in later major versions.</div>

# Sample Mesh

**Menu Path : Operator > Sampling > Sample Mesh**

**Menu Path : Operator > Sampling > Sample Skinned Mesh**

The Sample Mesh or Skinned Mesh Operator allows you to fetch vertex data of a static or transformed geometry.

## Operator settings

| **Property**            | **Type** | **Description**                                              |
| ----------------------- | -------- | ------------------------------------------------------------ |
| **Output**              | Enum     | **(Inspector)** Select output to read Position/Color/TexcoordN/... |
| **Mode**                | Enum     | The wrap mode to use for the sequence. The options are:<br/>&#8226; **Clamp**: Clamps the index between the first and last vertices.<br/>&#8226; **Wrap**: Wraps the index around to the other side of the vertex list. <br/>&#8226; **Mirror**: Mirrors the vertex list so out of range indices move back and forth through the list. |
| **Placement mode**      | Enum     | The placement mode choose what kind of primitive to consider while sampling.<br/>&#8226; **Vertex**: Sample among all listed vertices without using any index buffer.<br/>&#8226; **Edge: **Interpolate among two consecutives vertices drawing a triangle. <br/>&#8226; **Surface:** Interpolate between three vertices defining a triangle. |
| **Surface coordinates** | Enum     | Choose the approach of sampling in triangle.<br/>&#8226; **Barycentric**: Raw barencentric coordinates.<br/>&#8226; **Uniform**: Uniform placement within the triangle area.<br/>This property only appears if you set **Placement mode** to **Surface** |
| **Source**              | Enum     | Choose the kind of geometry to sample, either a **Mesh** or a **Skinned Mesh Renderer** |

### Operator Properties

| **Input**                 | **Type**              | **Description**                                              |
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Mesh**                  | Mesh                  | The source mesh asset to sample.<br/>This property only appears if you set **Source** to **Mesh** |
| **Skinned Mesh Renderer** | Skinned Mesh Renderer | The source skinned mesh renderer to sample, a reference to a component within the scene, has to be an exposed entry.<br/>This property only appears if you set **Source** to **Skinned Mesh Renderer** |
| **Vertex**                | uint                  | The index of the vertex to sample.<br/>This property only appears if you set **Placement mode** to **Vertex**. |
| **Index**                 | uint                  | The start index of edge, this index correspond of all consecutive edges of every triangles.<br/>This property only appears if you set **Placement mode** to **Index**. |
| **Triangle**              | uint                  | The index of triangle to sample, assuming the index buffer express a triangle list.<br/>This property only appears if you set **Placement mode** to **Surface**. |
| **X**                     | float                 | Interpolation value between start and end edge position.<br/>This property only appears if you set **Placement mode** to **Edge**. |
| **Barycentric**           | Vector2               | Raw barycentric coordinate of the triangle, x and y are exposed and z is computed to respect the surface constraint : z = 1 - x - y. This sampling do **not** keep sampling value inside triangle.<br/>This property only appears if you set **Placement mode** to **Surface** and **Surface coordinates** to **Barycentric**. |
| **Square**                | Vector2               | Uniform placement inside the triangle describe by [this mapping](https://hal.archives-ouvertes.fr/hal-02073696v2/document).<br/>This property only appears if you set **Placement mode** to **Surface** and **Surface coordinates** to **Uniform**. |

| **Output**       | **Type** | **Description**                                              |
| ---------------- | -------- | ------------------------------------------------------------ |
| **Position**     | Vector3  | Return vertex attribute [Position](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Position.html)<br/>This property only appears if you select **Position** in **Output** |
| **Normal**       | Vector3  | Return vertex attribute [Normal](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Normal.html)<br/>This property only appears if you select **Normal** in **Output** |
| **Tangent**      | Vector3  | Return vertex attribute [Tangent](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Tangent.html)<br/>This property only appears if you select **Tangent** in **Output** |
| **Color**        | Vector4  | Return vertex attribute [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html)<br/>This property only appears if you select **Color** in **Output** |
| **TexCoord0-7**  | Vector4  | Return vertex attribute [TexCoord0 to 7](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.TexCoord0.html), return the zero value in unspecified dimensions.<br/>This property only appears if you select **TexCoord0-7** in **Output** |
| **BlendWeight**  | Vector4  | Return vertex attribute [BlendWeight](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.BlendWeight.html)<br/>This property only appears if you select **BlendWeight** in **Output** |
| **BlendIndices** | Vector4  | Return vertex attribute [BlendIndices](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.BlendIndices.html)<br/>This property only appears if you select **BlendIndices** in **Output** |

### Additional notes

TODO

#### Limitations

The Mesh sampling feature has the following limitations:

- Only support [VertexAttributeFormat.Float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html) for all [VertexAttribute](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.html) expect [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html) which has to be a four component attributes using [VertexAttributeFormat.UInt8](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.UInt8.html) format or [VertexAttributeFormat.Float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html).
- If a Mesh is not [readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html), the **Position (Mesh)** Block and **Sample Mesh** Operator return zero values when they attempt to sample it. For information on how to make a Mesh readable, see [Model import settings](https://docs.unity3d.com/Manual/FBXImporter-Model.html)

<img src="Images/ReadWrite.png" alt="image-20200320154843722" style="zoom:78%;" />