<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This Feature is currently experimental and is subject to change in later major versions.</div>

# Sample Mesh Index

**Menu Path : Operator > Sampling > Sample Mesh Index**

**Menu Path : Operator > Sampling > Sample Skinned Mesh Renderer Index**

The Sample Mesh or Skinned Mesh Renderer Index Operator allows you to fetch index buffer data of geometry. Both [UInt16](https://docs.unity3d.com/ScriptReference/ModelImporterIndexFormat.UInt16.html) and [UInt32](https://docs.unity3d.com/ScriptReference/ModelImporterIndexFormat.UInt32.html) format are supported the output of this operator is always an UInt.

## Operator settings

| **Property** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Source**   | Enum     | Choose the kind of geometry to sample, either a **Mesh** or a **Skinned Mesh Renderer** |

### Operator Properties

| **Input**                 | **Type**              | **Description**                                              |
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Mesh**                  | Mesh                  | The source mesh asset to sample.<br/>This property only appears if you set **Source** to **Mesh** |
| **Skinned Mesh Renderer** | Skinned Mesh Renderer | The source skinned mesh renderer to sample, a reference to a component within the scene, has to be an exposed entry.<br/>This property only appears if you set **Source** to **Skinned Mesh Renderer** |
| **Index**                 | uint                  | The index offset to sample the current index buffer.         |

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **index**  | UInt     | Returns the sampled index or zero if input **Index** is out of bounds. |

#### Limitations

The Mesh index sampling feature has the following limitations:

- If a Mesh is not [readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html), the **Position (Mesh)** Block and **Sample Mesh Index** Operator return zeros values when they attempt to sample it. For information on how to make a Mesh readable, see [Model import settings](https://docs.unity3d.com/Manual/FBXImporter-Model.html)

<img src="Images/ReadWrite.png" alt="image-20200320154843722" style="zoom:78%;" />