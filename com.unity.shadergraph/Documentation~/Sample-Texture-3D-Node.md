## Description

Samples a **Texture 3D** and returns a **Vector 4** color value for use in the shader. You can override the **UV** coordinates using the **UV** input and define a custom **Sampler State** using the **Sampler** input.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture |	Input |	Texture 2D  | None | Texture 2D to sample |
| UV      | Input |	Vector 3    | None	| Mesh's normal vector |
| Sampler | Input |	Sampler State | Default sampler state | Sampler for the texture |
| RGBA	| Output	| Vector 4	| None	| Output value as RGBA |
