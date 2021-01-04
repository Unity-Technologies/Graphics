# Sample Texture 2D Node

## Description

Samples a **Texture 2D** and returns a **Vector 4** color value for use in the shader. You can override the **UV** coordinates using the **UV** input and define a custom **Sampler State** using the **Sampler** input.

To use the **Sample Texture 2D Node** to sample a normal map, set the **Type** dropdown parameter to **Normal**.

NOTE: This [Node](Node.md) can only be used in the **Fragment** [Shader Stage](Shader-Stage.md). To sample a **Texture 2D** in the **Vertex** [Shader Stage](Shader-Stage.md) use a [Sample Texture 2D LOD Node](Sample-Texture-2D-LOD-Node.md) instead.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture |	Input |	Texture 2D  | None | Texture 2D to sample |
| UV      | Input |	Vector 2    | 	UV	| UV coordinates |
| Sampler | Input |	Sampler State | Default sampler state | Sampler for the texture |
| RGBA	| Output	| Vector 4	| None	| Output value as RGBA |
| R	    | Output	| Float   	| None	| red (x) component of RGBA output |
| G	    | Output	| Float   	| None	| green (y) component of RGBA output |
| B	    | Output	| Float   	| None	| blue (z) component of RGBA output |
| A     |	Output	| Float   	| None | alpha (w) component of RGBA output |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|  Type   | Dropdown | Default, Normal | Selects the texture type |

## Generated Code Example

The following example code represents one possible outcome of this node per **Type** mode.

**Default**

```
float4 _SampleTexture2D_RGBA = SAMPLE_TEXTURE2D(Texture, Sampler, UV);
float _SampleTexture2D_R = _SampleTexture2D_RGBA.r;
float _SampleTexture2D_G = _SampleTexture2D_RGBA.g;
float _SampleTexture2D_B = _SampleTexture2D_RGBA.b;
float _SampleTexture2D_A = _SampleTexture2D_RGBA.a;
```

**Normal**

```
float4 _SampleTexture2D_RGBA = SAMPLE_TEXTURE2D(Texture, Sampler, UV);
_SampleTexture2D_RGBA.rgb = UnpackNormalRGorAG(_SampleTexture2D_RGBA);
float _SampleTexture2D_R = _SampleTexture2D_RGBA.r;
float _SampleTexture2D_G = _SampleTexture2D_RGBA.g;
float _SampleTexture2D_B = _SampleTexture2D_RGBA.b;
float _SampleTexture2D_A = _SampleTexture2D_RGBA.a;
```