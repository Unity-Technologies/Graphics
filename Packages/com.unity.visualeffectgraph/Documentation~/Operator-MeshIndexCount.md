# Get Mesh Index Count

Menu Path: **Operator > Sampling > Get Mesh Index Count**

Use the **Get Mesh Index Count** Operator to get the number of indices in a mesh.

## Operator settings

| **Property** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Source**   | Enum     | **(Inspector)** Specify the mesh type to input.<ul><li>**Mesh**: Input a mesh asset. The Operator becomes a [Get Mesh Index Count](Operator-MeshIndexCount.md) Operator.</li><li>**Skinned Mesh Renderer**: Input a [Skinned Mesh Renderer](https://docs.unity3d.com/Manual/class-SkinnedMeshRenderer.html) component. The Operator becomes a [Get Skinned Mesh Index Count](Operator-SkinnedMeshIndexCount.md) Operator.</li></ul> |

### Operator properties

| **Input**                 | **Type**              | **Description**                                              |
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Mesh**                  | Mesh                  | Specify the mesh asset to input. This property only appears if you set **Source** to **Mesh**. |

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **Count**  | UInt     | The number of indices in the mesh. If the mesh uses default triangle topology, you can divide the value by three to get the number of triangles. |

## Limitations

If the mesh isn't [readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html), this Operator returns 0. For information on how to make a mesh readable in the Editor, refer to the [Import Settings for a model file](https://docs.unity3d.com/Manual/FBXImporter-Model.html).