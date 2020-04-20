# Linear Blend Skinning Node

## Description

This node lets you apply Linear Blend Vertex Skinning, and only works with the [DOTS Hybrid Renderer](https://docs.unity3d.com/Packages/com.unity.rendering.hybrid@latest/). You must provide skinned matrices in the `_SkinMatrices` buffer. The node uses the `_SkinMatrixIndex` property to calculate where the matrices associated with the current mesh are located in the `_SkinMatrices` buffer.

## Ports
| Name         | Direction  | Type    | Stage  | Description |
|:------------ |:-----------|:--------|:-------|:------------|
| OutPosition  | Output     | Vector3 | Vertex | Outputs the skinned position |
| OutNormal    | Output     | Vector3 | Vertex | Outputs the skinned normal |
| OutTangent   | Output     | Vector3 | Vertex | Outputs the skinned tangent |