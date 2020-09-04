# Sample Texture 2D LOD Node

## Description

Samples a **Texture 2D** and returns a **Vector 4** color value for use in the shader. You can override the **UV** coordinates using the **UV** input and define a custom **Sampler State** using the **Sampler** input. Use the **LOD** input to adjust the level of detail of the sample.

To use the **Sample Texture 2D LOD Node** to sample a normal map, set the **Type** dropdown parameter to **Normal**.

This [Node](Node.md) is useful for sampling a **Texture** in the vertex [Shader Stage](Shader-Stage.md) as the [Sample Texture 2D Node](Sample-Texture-2D-Node.md) is unavailable in this [Shader Stage](Shader-Stage.md). 

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture |	Input |	Texture 2D  | None | Texture 2D to sample |
| UV      | Input |	Vector 2    | 	UV	| UV coordinates |
| Sampler | Input |	Sampler State | Default sampler state | Sampler for the texture |
| LOD   |	Input |	Vector 1  | None | Level of detail to sample |
| RGBA	| Output	| Vector 4	| None	| Output value as RGBA |
| R	    | Output	| Vector 1	| None	| red (x) component of RGBA output |
| G	    | Output	| Vector 1	| None	| green (y) component of RGBA output |
| B	    | Output	| Vector 1	| None	| blue (z) component of RGBA output |
| A     |	Output	| Vector 1	| None | alpha (w) component of RGBA output |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|  Type   | Dropdown | Default, Normal | Selects the texture type |

## Generated Code Example

The following example code represents one possible outcome of this node per **Type** mode.

**Default**

```
float4 _SampleTexture2DLOD_RGBA = SAMPLE_TEXTURE2D_LOD(Texture, Sampler, UV, LOD);
float _SampleTexture2DLOD_R = _SampleTexture2DLOD_RGBA.r;
float _SampleTexture2DLOD_G = _SampleTexture2DLOD_RGBA.g;
float _SampleTexture2DLOD_B = _SampleTexture2DLOD_RGBA.b;
float _SampleTexture2DLOD_A = _SampleTexture2DLOD_RGBA.a;
```

**Normal**

```
float4 _SampleTexture2DLOD_RGBA = SAMPLE_TEXTURE2D_LOD(Texture, Sampler, UV, LOD);
_SampleTexture2DLOD_RGBA.rgb = UnpackNormalRGorAG(_SampleTexture2DLOD_RGBA);
float _SampleTexture2DLOD_R = _SampleTexture2DLOD_RGBA.r;
float _SampleTexture2DLOD_G = _SampleTexture2DLOD_RGBA.g;
float _SampleTexture2DLOD_B = _SampleTexture2DLOD_RGBA.b;
float _SampleTexture2DLOD_A = _SampleTexture2DLOD_RGBA.a;
```