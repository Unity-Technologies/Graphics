# Compute Deformation Node

## Description

This node lets you pass compute deformed data to a vertex shader. You must provide `DeformedVertexData` in the `_DeformedMeshData` buffer. The node uses the `_ComputeMeshIndex` property to calculate where the `DeformedVertexData` associated with the current mesh are located in the `_DeformedMeshData` buffer. To output data, you must either install both the DOTS Hybrid Renderer and DOTS Animation packages, or use a custom solution.

## Ports
| Name         | Direction  | Type    | Stage  | Description |
|:------------ |:-----------|:--------|:-------|:------------|
| OutPosition  | Output     | Vector3 | Vertex | Outputs the deformed position |
| OutNormal    | Output     | Vector3 | Vertex | Outputs the deformed normal |
| OutTangent   | Output     | Vector3 | Vertex | Outputs the deformed tangent |