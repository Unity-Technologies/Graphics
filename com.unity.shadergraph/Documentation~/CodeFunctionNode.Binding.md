#### `Enumeration`

## Description

When using `CodeFunctionNode` you are able to define [Ports](Port.md) with any **Bindings** that available in [Shader Graph](Shader-Graph.md). For a full list of available **Bindings** see [Port Bindings](Port-Bindings.md). When using `CodeFunctionNode` **Bindings** are defined using [SlotAttribute](SlotAttribute.md).

For more information on how to create [Nodes](Node.md) using `CodeFunctionNode` see [Custom Nodes with CodeFunctionNode](Custom-Nodes-With-CodeFunctionNode.md).

Below is a full list including the [Port Bindings](Port-Bindings.md) they map to.

## Properties

| SlotAttribute Binding | Port Binding |
|:-------------|:------|
| ObjectSpaceNormal | Normal (in object space) |
| ObjectSpaceTangent | Tangent (in object space) |
| ObjectSpaceBitangent | Bitangent (in object space) |
| ObjectSpacePosition | Position (in object space) |
| ViewSpaceNormal | Normal (in view space) |
| ViewSpaceTangent | Tangent (in view space) |
| ViewSpaceBitangent | Bitangent (in view space) |
| ViewSpacePosition | Position (in view space) |
| WorldSpaceNormal | Normal (in world space) |
| WorldSpaceTangent | Tangent (in world space) |
| WorldSpaceBitangent | Bitangent (in world space) |
| WorldSpacePosition | Position (in world space) |
| TangentSpaceNormal | Normal (in tangent space) |
| TangentSpaceTangent | Tangent (in tangent space) |
| TangentSpaceBitangent | Bitangent (in tangent space) |
| TangentSpacePosition | Position (in tangent space) |
| MeshUV0 | UV (channel 0) |
| MeshUV1 | UV (channel 1) |
| MeshUV2 | UV (channel 2) |
| MeshUV3 | UV (channel 3) |
| ScreenPosition | Screen Position (Default mode) |
| ObjectSpaceViewDirection | View Direction (in object space) |
| ViewSpaceViewDirection | View Direction (in view space) |
| WorldSpaceViewDirection | View Direction (in world space) |
| TangentSpaceViewDirection | View Direction (in tangent space) |
| VertexColor | Vertex Color |