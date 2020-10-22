# Sample Cubemap Node

## Description

Samples a **Cubemap** and returns a **Vector 4** color value for use in the shader. Requires a **Direction** input in world space to sample the **Cubemap**. You can achieve a blurring effect by sampling at a different Level of Detail using the **LOD** input. You can also define a custom **Sampler State** using the **Sampler** input.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Cube | Input      |    Cubemap | None | Cubemap to sample |
| Dir | Input | Vector 3 | Normal (world space) | Direction or Mesh's normal vector |
| Sampler | Input |	Sampler State | Default sampler state | Sampler for the **Cubemap** |
| LOD | Input      |    Float    | None | Level of detail for sampling |
| Out | Output      | Vector 4 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 _SampleCubemap_Out = SAMPLE_TEXTURECUBE_LOD(Cubemap, Sampler, Dir, LOD);
```
