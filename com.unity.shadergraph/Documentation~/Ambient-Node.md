# Ambient Node

## Description

Provides access to the Scene's **Ambient** color values. When Environment Lighting Source is set to **Gradient** [Port](Port.md) **Color/Sky** returns the value **Sky Color**. When Environment Lighting Source is set to **Color** [Port](Port.md) **Color/Sky** returns the value **Ambient Color**. [Ports](Port.md) **Equator** and **Ground** always return the values **Equator Color** and **Ground Color** regardless of the current Environment Lighting Source.

Note: Values of this [Node](Node.md) are only updated when entering Play mode or saving the current Scene/Project.

Note: The behavior of this [Node](Node.md) is undefined globally. Shader Graph does not define the function of the node. Instead, each Render Pipeline defines what HLSL code to execute for this [Node](Node.md).

Different Render Pipelines may produce different results. If you're building a shader in one Render Pipeline that you want to use in both, try checking it in both pipelines before production. A [Node](Node.md) might be defined in one Render Pipeline and undefined in the other. If this [Node](Node.md) is undefined, it returns 0 (black).

#### Unity Pipelines Supported
- Lightweight Render Pipeline

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Color/Sky    | Output | Vector 3 | None | Color (Color) or Sky (Gradient) color value |
| Equator      | Output | Vector 3 | None | Equator (Gradient) color value |
| Ground       | Output | Vector 3 | None | Ground (Gradient) color value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3 _Ambient_ColorSky = SHADERGRAPH_AMBIENT_SKY;
float3 _Ambient_Equator = SHADERGRAPH_AMBIENT_EQUATOR;
float3 _Ambient_Ground = SHADERGRAPH_AMBIENT_GROUND;
```
