# Sample Texture 3D Node

## Description

Samples a **Texture 3D** and returns a **Vector 4** color value for use in the shader. You can override the **UV** coordinates using the **UV** input and define a custom **Sampler State** using the **Sampler** input.

NOTE: This [Node](Node.md) can only be used in the **Fragment** [Shader Stage](Shader-Stage.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture |	Input |	Texture 3D  | None | Texture 3D to sample |
| UV      | Input |	Vector 3    | None	| 3 dimnensional UV coordinates |
| Sampler | Input |	Sampler State | Default sampler state | Sampler for the texture |
| RGBA	| Output	| Vector 4	| None	| Output value as RGBA |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 _SampleTexture3D_Out = SAMPLE_TEXTURE3D(Texture, Sampler, UV);
```