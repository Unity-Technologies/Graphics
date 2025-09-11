# Normal Vector node

The Normal Vector node outputs the normal of a vertex or fragment of a mesh.

For more information about normals, refer to [Normal maps](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMapLanding.html).

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:------------ |:-------------|:-----|:---|:---|
| **Out** | Output | Vector 3 | None | The normal of the vertex or fragment of the mesh, depending on the [shader stage](Shader-Stage.md) of the graph section. |

## Space

The **Space** dropdown determines the coordinate space of the normal vector. 

| **Option** | **Description** |
|-|-|
| **Object**  | Returns the vertex or fragment normal in object space, where up is the up axis of local space. |
| **View** | Returns the vertex or fragment normal in view space, where up is the up direction of the camera. |
| **World**   | Returns the vertex or fragment normal in world space, where up is the up direction of the scene. |
| **Tangent** | Returns the vertex or fragment normal in tangent space, where up is away from the surface of the mesh. |
