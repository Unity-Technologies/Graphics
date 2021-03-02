<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This Feature is currently experimental and is subject to change in later major versions.</div>

# Sample Mesh

Menu Path : **Operator > Sampling > Sample Mesh**

Menu Path : **Operator > Sampling > Sample Skinned Mesh**

The Sample Mesh or Skinned Mesh Operator allows you to fetch vertex data of a static or skinned geometry.

## Operator settings

| **Property**            | **Type** | **Description**                                              |
| ----------------------- | -------- | ------------------------------------------------------------ |
| **Output**              | Enum     | **(Inspector)** Select the vertex attributes to read from the vertex. |
| **Mode**                | Enum     | The wrap mode to use for the sequence. The options are:<br/>&#8226; **Clamp**: Clamps the index between the first and last vertices.<br/>&#8226; **Wrap**: Wraps the index around to the other side of the vertex list. <br/>&#8226; **Mirror**: Mirrors the vertex list so out of range indices move back and forth through the list. |
| **Placement mode**      | Enum     | Specifies which primitive part of the mesh to sample from:<br/>&#8226; **Vertex**: Samples positions from all listed vertices.<br/>&#8226; **Edge**: Samples from an interpolation between two consecutives vertices that are part of a triangle on the mesh. <br/>&#8226; **Surface**: Samples from an interpolation between three vertices that define a triangle on the mesh. |
| **Surface coordinates** | Enum     | Specifies the method this block uses to sample the surface of a triangle.<br/>&#8226; **Barycentric**: Samples the surface using raw barycentric coordinates. Using this method, sampled positions are not constrained by the triangle edges which is useful if you have baked a position outside of the Visual Effect Graph.<br/>&#8226; **Uniform**: Samples the surface uniformly within the triangle area.<br/>This property only appears if you set **Placement mode** to **Surface** and **Spawn Mode** to **Custom**. |
| **Source**              | Enum     | **(Inspector)** Specifies the kind of geometry to sample from. The options are:<br/>&#8226; **Mesh**: Samples from a mesh asset.<br/>&#8226; **Skinned Mesh Renderer**: Samples from a [Skinned Mesh Renderer](https://docs.unity3d.com/Manual/class-SkinnedMeshRenderer.html). |

### Operator Properties

| **Input**                 | **Type**              | **Description**                                              |
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Mesh**                  | Mesh                  | The source mesh asset to sample.<br/>This property only appears if you set **Source** to **Mesh** |
| **Skinned Mesh Renderer** | Skinned Mesh Renderer | The source Skinned Mesh Renderer component to sample. This is a reference to a component within the scene. To assign a Skinned Mesh Renderer to this port, create a Skinned Mesh Renderer property in the [Blackboard](Blackboard.md) and expose it.<br/>This property only appears if you set **Source** to **Skinned Mesh Renderer**. |
| **Vertex**                | uint                  | The index of the vertex to sample.<br/>This property only appears if you set **Placement mode** to **Vertex**. |
| **Index**                 | uint                  | The start index of the edge to sample from. The block uses this index and the following index to select the line to sample from.<br/>This property only appears if you set **Placement mode** to **Edge** and **Spawn Mode** to **Custom**. |
| **Triangle**              | uint                  | The index of triangle to sample, assuming the index buffer contains a triangle list.<br/>This property only appears if you set **Placement mode** to **Surface**, **Spawn Mode** to **Custom**, and **Spawn Mode** to **Custom**. |
| **Edge**                  | float                 | The interpolation value the block uses to sample along the edge. This is the percentage along the edge, from start position to end position, that the sample position is taken.<br/>This property only appears if you set **Placement mode** to **Edge** and **Spawn Mode** to **Custom**. |
| **Barycentric**           | Vector2               | The raw barycentric coordinate to sample from the triangle at. The input is two-dimensional (**X** and **Y**) and the block calculates the **Z** value using the values you input: **Z** = **1** - **X** - **Y**. This sampling method does not constrain the sampled position inside the triangle edges.<br/>This property only appears if you set **Placement mode** to **Surface**, **Surface coordinates** to **Barycentric**, and **Spawn Mode** to **Custom**. |
| **Square**                | Vector2               | The uniform coordinate to sample the triangle at. The block takes this value and maps it from a square coordinate to triangle space. To do this, it uses the method outline in the paper [A Low-Distortion Map Between Triangle and Square](https://hal.archives-ouvertes.fr/hal-02073696v2) (Heitz 2019).<br/>This property only appears if you set **Placement mode** to **Surface**, **Surface coordinates** to **Uniform**, and **Spawn Mode** to **Custom**. |

| **Output**       | **Type** | **Description**                                              |
| ---------------- | -------- | ------------------------------------------------------------ |
| **Position**     | Vector3  | The vertex attribute [Position](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Position.html)<br/>This property only appears if you select **Position** in **Output** |
| **Normal**       | Vector3  | The vertex attribute [Normal](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Normal.html)<br/>This property only appears if you select **Normal** in **Output** |
| **Tangent**      | Vector3  | The vertex attribute [Tangent](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Tangent.html)<br/>This property only appears if you select **Tangent** in **Output** |
| **Color**        | Vector4  | The vertex attribute [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html)<br/>This property only appears if you select **Color** in **Output** |
| **TexCoord0-7**  | Vector4  | The vertex attribute [TexCoord 0 to 7](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.TexCoord0.html). For unspecified dimensions, this is **0**.<br/>This property only appears if you select **TexCoord0-7** in **Output** |
| **BlendWeight**  | Vector4  | The vertex attribute [BlendWeight](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.BlendWeight.html).<br/>This property only appears if you select **BlendWeight** in **Output** |
| **BlendIndices** | Vector4  | The vertex attribute [BlendIndices](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.BlendIndices.html).<br/>This property only appears if you select **BlendIndices** in **Output** |

#### Limitations

The Mesh sampling feature has the following limitations:

- It only supports [VertexAttributeFormat.Float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html) for all [VertexAttributes](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.html) except [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html), which has to be a four component attribute using either [VertexAttributeFormat.UInt8](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.UInt8.html) or [VertexAttributeFormat.Float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html) format.
- If a Mesh is not [readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html), the **Position (Mesh)** Block and **Sample Mesh** Operator return zero values when they attempt to sample from it. For information on how to make a Mesh readable, see [Model import settings](https://docs.unity3d.com/Manual/FBXImporter-Model.html).

![](Images/ReadWrite.png)

## Reference list

* Heitz, Eric. 2019. "A Low-Distortion Map Between Triangle and Square". [hal-02073696v2](https://hal.archives-ouvertes.fr/hal-02073696v2)
