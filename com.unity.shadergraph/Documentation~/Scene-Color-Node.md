# Scene Color Node

## Description

Provides access to the current **Camera**'s color buffer using input **UV**, which is expected to be normalized screen coordinates.

Note: The behaviour of this [Node](Node.md) is undefined globally. The executed HLSL code for this [Node](Node.md) is defined per **Render Pipeline**, and different **Render Pipelines** may produce different results. Custom **Render Pipelines** that wish to support this [Node](Node.md) will also need to explicitly define the behaviour for it. If undefined this [Node](Node.md) will return 0 (black).

Note: In **Lightweight Render Pipeline** this [Node](Node.md) returns the value of the **Camera Opaque Texture**. See the **Lightweight Render Pipeline** for more documentation on this feature. The contents of this texture are only available for **Transparent** objects. Set the **Surface Type** dropdown on the **Material Options** panel of the [Master Node](Master-Node.md) to **Transparent** to receive the correct values from this node. 

NOTE: This [Node](Node.md) can only be used in the **Fragment** [Shader Stage](Shader-Stage.md).

#### Unity Pipelines Supported
- Lightweight Render Pipeline
- High Definition Render Pipeline

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV     | Input | Vector 4 | Screen Position | Normalized screen coordinates |
| Out | Output      |    Vector 3 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_SceneColor_float(float4 UV, out float3 Out)
{
    Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV);
}
```