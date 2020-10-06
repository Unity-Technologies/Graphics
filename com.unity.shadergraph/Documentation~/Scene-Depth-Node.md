# Scene Depth Node

## Description

Provides access to the current **Camera**'s depth buffer using input **UV**, which is expected to be normalized screen coordinates.

Note: Depth buffer access requires depth buffer to be enabled on the active **Render Pipeline**. This process is different per **Render Pipeline**. It is recommended you read the documentation of your active **Render Pipeline** for information on enabling the depth buffer. If the depth buffer is unavailable this [Node](Node.md) will return mid grey.

Note: The executed HLSL code for this [Node](Node.md) is defined per **Render Pipeline**, and different **Render Pipelines** may produce different results. Custom **Render Pipelines** that wish to support this [Node](Node.md) will also need to explicitly define the behaviour for it. If undefined this [Node](Node.md) will return 1 (white).

NOTE: This [Node](Node.md) can only be used in the **Fragment** [Shader Stage](Shader-Stage.md).

#### Unity Render Pipelines Support
- High Definition Render Pipeline
- Universal Render Pipeline

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV     | Input | Vector 4 | Screen Position | Normalized screen coordinates |
| Out | Output      |    Float    | None | Output value |

## Depth Sampling modes
| Name     | Description                        |
|----------|------------------------------------|
| Linear01 | Linear depth value between 0 and 1 |
| Raw      | Raw depth value                    |
| Eye      | Depth converted to eye space units |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_SceneDepth_Raw_float(float4 UV, out float Out)
{
    Out = SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV);
}
```