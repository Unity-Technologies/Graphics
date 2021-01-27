# Sample Reflected Cubemap Node

## Description

Samples a Cubemap with reflected vector and returns a Vector 4 color value for use in the shader. Requires View Direction (**View Dir**) and **Normal** inputs to sample the Cubemap. You can achieve a blurring effect by using the **LOD** input to sample at a different Level of Detail. You can also use the **Sampler** input to define a custom Sampler State.

If you experience texture sampling errors while using this node in a graph which includes Custom Function Nodes or Sub Graphs, you can resolve them by upgrading to version 10.3 or later. 

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Cube | Input      |    Cubemap | None | Cubemap to sample |
| View Dir      | Input | Vector 3 | View Direction (object space) | Mesh's view direction |
| Normal | Input      |    Vector 3 | Normal (object space) | Mesh's normal vector |
| Sampler | Input |	Sampler State | Default sampler state | Sampler for the Cubemap |
| LOD | Input      |    Float    | None | Level of detail for sampling |
| Out | Output      | Vector 4 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 _SampleCubemap_Out = SAMPLE_TEXTURECUBE_LOD(Cubemap, Sampler, reflect(-ViewDir, Normal), LOD);
```
