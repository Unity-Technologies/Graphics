# Baked GI Node

## Description

Provides access to the **Baked GI** values at the vertex or fragment's position. Requires **Position** and **Normal** input for light probe sampling, and lightmap coordinates **Static UV** and **Dynamic UV** for all potential lightmap sampling cases.

Note: The behavior of this [Node](Node.md) is undefined globally. Shader Graph does not define the function of the node. Instead, each Render Pipeline defines what HLSL code to execute for this [Node](Node.md).

Different Render Pipelines may produce different results. If you're building a shader in one Render Pipeline that you want to use in both, try checking it in both pipelines before production. A [Node](Node.md) might be defined in one Render Pipeline and undefined in the other. If this [Node](Node.md) is undefined, it returns 0 (black).

#### Unity Pipelines Supported
- HD Render Pipeline
- Lightweight Render Pipeline

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Position    | Input | Vector 3 | Position (world space) | Mesh vertex/fragment's **Position** |
| Normal      | Input | Vector 3 | Normal (world space) | Mesh vertex/fragment's **Normal** |
| Static UV   | Input | Vector 2 | UV1 | Lightmap coordinates for the static lightmap |
| Dynamic UV  | Input | Vector 2 | UV2 | Lightmap coordinates for the dynamic lightmap |
| Out       | Output | Vector 3 | None | Output color value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Apply Lightmap Scaling     | Toggle | True, False | If enabled lightmaps are automatically scaled and offset. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_BakedGI_float(float3 Position, float3 Normal, float2 StaticUV, float2 DynamicUV, out float Out)
{
    Out = SHADERGRAPH_BAKED_GI(Position, Normal, StaticUV, DynamicUV, false);
}
```