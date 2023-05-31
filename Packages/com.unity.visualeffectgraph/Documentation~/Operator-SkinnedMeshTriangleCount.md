# Get Skinned Mesh Triangle Count

Menu Path: **Operator > Sampling > Get Mesh Triangle Count**

Use the **Get Skinned Mesh Triangle Count** Operator to get the number of triangles in a skinned mesh.

The Operator assumes the mesh uses default triangle topology, so it outputs the [index count](Operator-MeshIndexCount.md) divided by three.

## Operator settings

| **Property** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Source**   | Enum     | **(Inspector)** Specify the mesh type to input.<ul><li>**Mesh**: Input a mesh asset. The Operator becomes a [Get Mesh Triangle Count](Operator-MeshTriangleCount.md) Operator.</li><li>**Skinned Mesh Renderer**: Input a [Skinned Mesh Renderer](https://docs.unity3d.com/Manual/class-SkinnedMeshRenderer.html) component. The Operator becomes a [Get Skinned Mesh Triangle Count](Operator-SkinnedMeshTriangleCount.md) Operator.</li></ul> |

### Operator properties

| **Input**                 | **Type**              | **Description**                                              |
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Skinned Mesh Renderer** | Skinned Mesh Renderer | Specify the Skinned Mesh Renderer component to input. This is a reference to a component in your scene. To assign a Skinned Mesh Renderer, create a Skinned Mesh Renderer property in the [Blackboard](Blackboard.md) and expose it.<br/><br/>This property only appears if you set **Source** to **Skinned Mesh Renderer**. |

| **Output** | **Type** | **Description**                         |
| ---------- | -------- | --------------------------------------- |
| **Count**  | UInt     | The number of triangles in the mesh. |

## Limitations

If the mesh isn't [readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html), this Operator returns 0. For information on how to make a mesh readable in the Editor, refer to the [Import Settings for a model file](https://docs.unity3d.com/Manual/FBXImporter-Model.html).
