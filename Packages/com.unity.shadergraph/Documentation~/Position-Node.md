# Position node

The Position node returns the position of a vertex or fragment of a mesh.

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:------------ |:-------------|:-----|:---|:---|
| **Out** | Output | Vector 3 | None | Position of the vertex or fragment of the mesh, depending on the [shader stage](Shader-Stage.md) of the graph section. |

## Space

The **Space** dropdown determines the coordinate space of the output position. 

| **Options**   | **Description**  |
|-|-|
| **Object**  | Returns the vertex or fragment position relative to the origin of the object. | 
| **View** | Returns the vertex or fragment position relative to the camera, in meters. |
| **World**   | Returns the vertex or fragment position in the world, in meters. If you use the High Definition Render Pipeline (HDRP), **World** returns the position [relative to the camera](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?preview=1&subfolder=/manual/Camera-Relative-Rendering.html). |
| **Tangent** | Returns the vertex or fragment position relative to the tangent of the surface, in meters. For more information, refer to [Normal maps](https://docs.unity3d.com/6000.3/Documentation/Manual/StandardShaderMaterialParameterNormalMapLanding.html). |
| **Absolute World**| Returns the vertex or fragment position in the world, in meters. |
