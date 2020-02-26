# Sample Cubemap Projection Node

## Description

Samples a **Cubemap**, which allows you to project it to geometry, and returns a **Vector 4** color value for use in the Shader. This node requires **World Position**, **Origin**, **Offset**, and **Projection Intensity** inputs. You can achieve a blurring effect by using the **LOD** input to sample at a different Level of Detail. You can also use the **Sampler** input to define a custom **Sampler State**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Cube | Input      |    Cubemap | None | Cubemap to sample |
| World Position      | Input | Vector 3 | Absolute World Position | World position to use for projection |
| Origin Offset | Input      |    Vector 3 | Origin offset | Offset relative to the Object position |
| Position Offset | Input      |    Vector 3 | Normal (object space) | Mesh's normal vector |
| Projection Intensity | Input      |    Vector 1 | Scalar | Scale the projection |
| Sampler | Input |	Sampler State | Default sampler state | Sampler for the Cubemap |
| LOD | Input      |    Vector 1 | None | Level of Detail for sampling |
| Out | Output      | Vector 4 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3 Unity_SampleCubemapProjection(float3 worldPos, float3 originOffset, float3 positionOffset, float projIntensity)
{
    float3 delta = worldPos - (SHADERGRAPH_OBJECT_POSITION + originOffset);
    delta += projIntensity*SafeNormalize(originOffset - positionOffset);

    return delta;
}

float4 _SampleCubemapProjection_Out = SAMPLE_TEXTURECUBE_LOD(Cubemap, Sampler, Unity_SampleCubemapProjection(WorldPosition, OriginOffset, OffsetPosition, ProjectionInstensity), LOD;
```