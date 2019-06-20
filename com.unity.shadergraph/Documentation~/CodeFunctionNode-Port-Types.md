## Description

When using `CodeFunctionNode` you are able to define [Ports](Port.md) of any type available in [Shader Graph](Shader-Graph.md). For a full list of available types see [Data Types](Data-Types.md). Defining [Ports](Port.md) using `CodeFunctionNode` requires using specific types when defining a port via a method argument. 

For more information on how to create [Nodes](Node.md) using `CodeFunctionNode` see [Custom Nodes with CodeFunctionNode](Custom-Nodes-With-CodeFunctionNode.md).

Below is a full list including the [Data Type](Data-Types) they map to.

## Port Types

| Argument Type | Data Type |
|:-------------|:------|
| Boolean | Boolean |
| Vector1 | Vector1 |
| Vector2 | Vector2 |
| Vector3 | Vector3 |
| Vector4 | Vector4 |
| Color | Vector4 (with a ColorRGBA [Port Binding](Port-Bindings.md)) |
| ColorRGBA | Vector4 (with a ColorRGBA [Port Binding](Port-Bindings.md))|
| ColorRGB | Vector3 (with a ColorRGB [Port Binding](Port-Bindings.md)) |
| Texture2D | Texture2D |
| Texture2DArray | Texture2DArray |
| Texture3D | Texture3D |
| Cubemap | Cubemap |
| SamplerState | SamplerState |
| DynamicDimensionVector | DynamicVector |
| Matrix4x4 | Matrix4 |
| Matrix3x3 | Matrix3 |
| Matrix2x2 | Matrix2 |
| DynamicDimensionMatrix | DynamicMatrix |
