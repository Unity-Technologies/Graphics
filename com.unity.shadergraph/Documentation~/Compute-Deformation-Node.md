# Compute Deformation Node

## Description

Node for passing compute deformed data to vertex shader.
Requires DeformedVertexData to be provided by user in _DeformedMeshData buffer.
The node uses the property _ComputeMeshIndex to know where the DeformedVertexData associated with the current mesh are located in the _DeformedMeshData buffer.
Requires Hybrid Renderer and DOTS Animation to output data, unless a custom solution is being used.

## Ports
| Name         | Direction  | Type    | Stage  | Description |
|:------------ |:-----------|:--------|:-------|:------------|
| OutPosition  | Output     | Vector3 | Vertex | Outputs the deformed position |
| OutNormal    | Output     | Vector3 | Vertex | Outputs the deformed normal |
| OutTangent   | Output     | Vector3 | Vertex | Outputs the deformed tangent |