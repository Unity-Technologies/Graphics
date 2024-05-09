# Linear Blend Skinning Node

## Description

This node lets you apply Linear Blend Vertex Skinning, and only works with the [Entities Graphics package](https://docs.unity3d.com/Packages/com.unity.entities.graphics@latest/). You must provide skinned matrices in the `_SkinMatrices` buffer. The node uses the `_SkinMatrixIndex` property to calculate where the matrices associated with the current mesh are located in the `_SkinMatrices` buffer.

## Ports
| Name      | Direction  | Type    | Stage  | Description |
|:--------- |:-----------|:--------|:-------|:------------|
| Position  | Input      | Vector3 | Vertex | Position of the vertex in object space. |
| Normal    | Input      | Vector3 | Vertex | Normal of the vertex in object space. |
| Tangent   | Input      | Vector3 | Vertex | Tangent of the vertex in object space. |
| Position  | Output     | Vector3 | Vertex | Outputs the skinned vertex position. |
| Normal    | Output     | Vector3 | Vertex | Outputs the skinned vertex normal. |
| Tangent   | Output     | Vector3 | Vertex | Outputs the skinned vertex tangent. |
