# Sample Cubemap Projection Node

## Description

Samples a **Cubemap** to being able to project it on a geometry and returns a **Vector 4** color value for use in the shader. Requires **World Position**, **Origin**, **Offset**, **Projection Intensity** inputs to sample the **Cubemap**. You can achieve a blurring effect by sampling at a different Level of Detail using the **LOD** input. You can also define a custom **Sampler State** using the **Sampler** input.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Cube | Input      |    Cubemap | None | Cubemap to sample |
| World Position      | Input | Vector 3 | Absolute World Position | World position used for projection |
| Origin Offset | Input      |    Vector 3 | Origin offset | Offset relative to the Object posittion |
| Position Offset | Input      |    Vector 3 | Normal (object space) | Mesh's normal vector |
| Projection Intensity | Input      |    Vector 1 | Scalar | Scale the projection |
| Sampler | Input |	Sampler State | Default sampler state | Sampler for the **Cubemap** |
| LOD | Input      |    Vector 1 | None | Level of detail for sampling |
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