# Reflection Probe Node

## Description

Provides access to the nearest **Reflection Probe** to the object. Requires **Normal** and **View Direction** to sample the probe. You can achieve a blurring effect by sampling at a different Level of Detail using the **LOD** input.

Note: The behavior of this [Node](Node.md) is undefined globally. Shader Graph does not define the function of the node. Instead, each Render Pipeline defines what HLSL code to execute for this [Node](Node.md).

Different Render Pipelines may produce different results. If you're building a shader in one Render Pipeline that you want to use in both, try checking it in both pipelines before production. A [Node](Node.md) might be defined in one Render Pipeline and undefined in the other. If this [Node](Node.md) is undefined, it returns 0 (black).

#### Unity Render Pipelines Support
- Universal Render Pipeline

The High Definition Render Pipeline does **not** support this Node.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| View Dir      | Input | Vector 3 | View Direction (object space) | Mesh's view direction |
| Normal | Input      |    Vector 3 | Normal (object space) | Mesh's normal vector |
| LOD | Input      |    Vector 1 | None | Level of detail for sampling |
| Out | Output      |    Vector 3 | None | Output color value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_ReflectionProbe_float(float3 ViewDir, float3 Normal, float LOD, out float3 Out)
{
    Out = SHADERGRAPH_REFLECTION_PROBE(ViewDir, Normal, LOD);
}
```
