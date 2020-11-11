# Sample Mesh

**Menu Path : Operator > Sampling > Sample Mesh**

**Menu Path : Operator > Sampling > Sample Skinned Mesh**

The Sample Mesh or Skinned Mesh Operator allows you to fetch vertex data of a static or transformed geometry.

## Operator settings

| **Property**            | **Type** | **Description**                                              |
| ----------------------- | -------- | ------------------------------------------------------------ |
| **Output**              | Enum     | **(Inspector)** Select output to read Position/Color/TexcoordN/... |
| **Mode**                | Enum     | The wrap mode to use for the sequence. The options are:<br/>&#8226; **Clamp**: Elements with an index greater than the last element of the sequence repeat the last element of the sequence.<br/>&#8226; **Wrap**: Elements with an index greater than the last element repeat from the first element. <br/>&#8226; **Mirror**: Elements with an index greater than the last element repeat in inverse order, then back into correct order after reaching zero. |
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

| **Output**   | **Type** | **Description** |
| ------------ | -------- | --------------- |
| **Position** | Vector3  | TODO            |

### Additional notes

TODO

#### Limitations

TODO