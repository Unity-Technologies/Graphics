# Scene Color Node

## Description

Provides access to the current **Camera**'s color buffer using input **UV**, which is expected to be normalized screen coordinates.

The behavior of the Scene Color node isn't defined globally. The executed HLSL code for the Scene Color node is defined per **Render Pipeline**, and different **Render Pipelines** can produce different results. Custom **Render Pipelines** that wish to support the Scene Color node need to explicitly define the behavior for it. If the behavior is undefined, the Scene Color node returns 0 (black).

In the **Universal Render Pipeline** the Scene Color node returns the value of the **Camera Opaque Texture**. Refer to the **Universal Render Pipeline** for more documentation on this feature. The contents of this texture are only available for **Transparent** objects. Set the **Surface Type** dropdown on the [**Graph Settings** tab](Graph-Settings-Tab.md) of the [**Graph Inspector**](Internal-inspector.md) to **Transparent** to receive the correct values from this node.

>[!NOTE] 
>You can only use the Scene Color node in the **Fragment** [Shader Stage](Shader-Stage.md).

#### Supported Unity render pipelines 

The following table indicates which render pipelines support the Scene Color node. When used with unsupported render pipelines, the Scene Color node returns 0 (black).

|Pipeline                         | Supported |
|:--------------------------------|:----------|
| Built-in Render Pipeline        | No        |
| Universal Render Pipeline       | Yes       |
| High Definition Render Pipeline | Yes       |

## Ports

| Name | Direction | Type     | Binding         | Description |
|:-----|:----------|:---------|:----------------|:------------|
| UV   | Input     | Vector 4 | Screen Position | Normalized screen coordinates |
| Out  | Output    | Vector 3 | None            | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_SceneColor_float(float4 UV, out float3 Out)
{
    Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV);
}
```
