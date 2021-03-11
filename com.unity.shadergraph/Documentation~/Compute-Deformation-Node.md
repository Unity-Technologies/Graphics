# Compute Deformation Node

## Description

This node lets you pass compute deformed vertex data to a vertex shader, and only works with the [DOTS Hybrid Renderer](https://docs.unity3d.com/Packages/com.unity.rendering.hybrid@latest/). You must provide `DeformedVertexData` in the `_DeformedMeshData` buffer. The node uses the `_ComputeMeshIndex` property to calculate where the `DeformedVertexData` associated with the current mesh are located in the `_DeformedMeshData` buffer. To output data, you must either install both the DOTS Hybrid Renderer and DOTS Animation packages, or use a custom solution.

## Ports
| Name      | Direction  | Type    | Stage  | Description |
|:--------- |:-----------|:--------|:-------|:------------|
| Position  | Output     | Vector3 | Vertex | Outputs the deformed vertex position. |
| Normal    | Output     | Vector3 | Vertex | Outputs the deformed vertex normal. |
| Tangent   | Output     | Vector3 | Vertex | Outputs the deformed vertex tangent. |
