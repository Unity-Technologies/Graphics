<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This Feature is currently experimental and is subject to change in later major versions.</div>

# Mesh index count

**Menu Path : Operator > Sampling > Mesh Index Count**

The mesh index count operator allows you to retrieve the number of indices in a mesh or skinned mesh renderer geometry.

## Operator settings

| **Property** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Source**   | Enum     | Choose the kind of source geometry, either a **Mesh** or a **Skinned Mesh Renderer** |

### Operator Properties

| **Input**                 | **Type**              | **Description**                                              |
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Mesh**                  | Mesh                  | The source mesh asset to sample.<br/>This property only appears if you set **Source** to **Mesh** |
| **Skinned Mesh Renderer** | Skinned Mesh Renderer | The source skinned mesh renderer to sample, a reference to a component within the scene, has to be an exposed entry.<br/>This property only appears if you set **Source** to **Skinned Mesh Renderer** |

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **count**  | UInt     | Return the number of indices of the geometry. If the topology uses a default triangle list, the number of triangle can be deduced dividing by three. |

#### Limitations

The Mesh index count operator has the following limitations:

- If a Mesh is not [readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html), the **Mesh Index Count** Operator returns uint.MaxValue. For information on how to make a Mesh readable, see [Model import settings](https://docs.unity3d.com/Manual/FBXImporter-Model.html)

<img src="Images/ReadWrite.png" alt="image-20200320154843722" style="zoom:78%;" />