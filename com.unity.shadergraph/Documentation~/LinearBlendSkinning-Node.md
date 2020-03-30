# And Node

## Description

Node for authorizing Linear Blend Vertex Skinning.
Requires skinned matrices to be provided by user in _SkinMatrices buffer.
The node uses the Skin Matrix Index Offset to know where the matrices associated with the current mesh are located in the _SkinMatrices buffer.


## Ports

| Name         | Direction  | Type    | Stage  | Description |
|:------------ |:-----------|:--------|:-------|:------------|
| Position     | Input      | Vector3 | Vertex | Defines object space position per vertex |
| Normal       | Input      | Vector3 | Vertex | Defines object space normal per vertex |
| Tangent      | Input      | Vector3 | Vertex | Defines object space tangent per vertex |
| Skin Matrix Index Offset  | Input   | Int | Vertex | Defines the offset for the skinned matrices in _SkinMatrices |
| OutPosition  | Output     | Vector3 | Vertex | Outputs the skinned position |
| OutNormal    | Output     | Vector3 | Vertex | Outputs the skinned normal |
| OutTangent   | Output     | Vector3 | Vertex | Outputs the skinned tangent |