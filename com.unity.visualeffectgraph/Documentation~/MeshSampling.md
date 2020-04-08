<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Experimental:</b> This Feature is currently experimental and is subject to change in later major versions.</div>


# Mesh sampling

The Visual Effect Graph is able to fetch data from a Mesh and use the result throughout the graph. Currently, it can only fetch the vertices from the Mesh and can't access triangle topology.



The Visual Effect Graph provides two ways to sample a Mesh:

- The [Position (Mesh) Block](#position-(mesh)).
- The [Sample Mesh Operator](#sample-mesh).

### Position (Mesh)

This block sets the position reading vertex position attribute and direction reading normal attribute.

<img src="Images/PositionMesh.png" style="zoom:78%;" />

#### Input Slot

- **Mesh** : The Mesh to fetch.

- **Vertex** : The vertex index to sample (if **Spawn Mode** is set to **Custom**)

#### Settings
- **Addressing Mode** : Sets the method Unity uses when a vertex index is out of range of the vertices.
  - **Wrap** : Wraps the index around to the other side of the vertex list.
  - **Clamp** : Clamps the index between the first and last vertices.
  - **Mirror** : Mirrors the vertex list so out of range indices move back and forth through the list.
- **Spawn Mode**
  - **Random** : Gets a random index selection per instance.
  - **Custom** : Set a specific vertex index to fetch.

### Sample Mesh

This operator provides a custom read of any vertex attribute. To select the output [VertexAttribute](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.html) in inspector.

<img src="Images/SampleMesh.png" alt="image-20200320154843722" style="zoom:67%;" />

#### Input Slot

- **Mesh** : The Mesh to fetch.
- **Vertex** : The vertex index to sample (if **Spawn Mode** is setup to **Custom**)

#### Output Slot

- **Position** ([Vector3](https://docs.unity3d.com/ScriptReference/Vector3.html)) : Return vertex attribute [Position](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Position.html)
- **Normal** ([Vector3](https://docs.unity3d.com/ScriptReference/Vector3.html)) : Return vertex attribute [Normal](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Normal.html)
- **Tangent** ([Vector3](https://docs.unity3d.com/ScriptReference/Vector3.html)) : Return vertex attribute [Tangent](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Tangent.html)
- **Color** ([Vector4](https://docs.unity3d.com/ScriptReference/Vector4.html)) : Return vertex attribute [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html)
- **TexCoord0-7** ([Vector2](https://docs.unity3d.com/ScriptReference/Vector2.html)) : Return vertex attribute [TexCoord0 to 7](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.TexCoord0.html)
- **BlendWeight** ([Vector4](https://docs.unity3d.com/ScriptReference/Vector4.html)) : Return vertex attribute [BlendWeight](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.BlendWeight.html)
- **BlendIndices** ([Vector4](https://docs.unity3d.com/ScriptReference/Vector4.html)) : Return vertex attribute [BlendIndices](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.BlendIndices.html)

#### Settings

- **Addressing Mode** : Sets the method Unity uses when a vertex index is out of range of the vertices.
  - **Wrap** : Wraps the index around to the other side of the vertex list.
  - **Clamp** :  Clamps the index between the first and last vertices.
  - **Mirror** : Mirrors the vertex list so out of range indices move back and forth through the list.
- **Output** : Specifies the output VertexAttribute. This setting is only visible in the Inspector.



## Limitation

The Mesh sampling feature has the following limitations:

- Only support [VertexAttributeFormat.Float32](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html) for all  [VertexAttribute](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.html) expect [Color](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttribute.Color.html) which has to be a four component attributes using [VertexAttributeFormat.UInt8](https://docs.unity3d.com/ScriptReference/Rendering.VertexAttributeFormat.Float32.html) format (see [Color32](https://docs.unity3d.com/ScriptReference/Color32.html)).
- The TexCoord attribute is limited and constrained to two dimension attributes.
- The TexCoord attribute is limited and constrained to two dimension attributes. If a Mesh is not [readable](https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html), the **Position (Mesh)** Block and **Sample Mesh** Operator return zero values when they attempt to sample it. For information on how to make a Mesh readable, see [Model import settings](https://docs.unity3d.com/Manual/FBXImporter-Model.html)

<img src="Images/ReadWrite.png" alt="image-20200320154843722" style="zoom:78%;" />

