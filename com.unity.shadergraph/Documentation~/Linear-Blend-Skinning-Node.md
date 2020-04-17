# Linear Blend Skinning Node

## Description

Node for authorizing Linear Blend Vertex Skinning.
Requires skinned matrices to be provided by user in _SkinMatrices buffer.
The node uses the property _SkinMatrixIndex to know where the matrices associated with the current mesh are located in the _SkinMatrices buffer.

## Ports
| Name         | Direction  | Type    | Stage  | Description |
|:------------ |:-----------|:--------|:-------|:------------|
| OutPosition  | Output     | Vector3 | Vertex | Outputs the skinned position |
| OutNormal    | Output     | Vector3 | Vertex | Outputs the skinned normal |
| OutTangent   | Output     | Vector3 | Vertex | Outputs the skinned tangent |