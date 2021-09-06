# Sample Texture 2D Array Node

## Description

Samples a **Texture 2D Array** and returns a **Vector 4** color value for use in the shader. You can override the **UV** coordinates using the **UV** input and define a custom **Sampler State** using the **Sampler** input. Use the **Index** input to specify which index of the array to sample.

If you experience texture sampling errors while using this node in a graph which includes Custom Function Nodes or Sub Graphs, you can resolve them by upgrading to version 10.3 or later.

NOTE: This [Node](Node.md) can only be used in the **Fragment** shader stage.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture Array |   Input | Texture 2D Array  | None | Texture 2D Array to sample |
| Index   | Input | Float       | None  | Index of array to sample |
| UV      | Input | Vector 2    |   UV  | UV coordinates |
| Sampler | Input | Sampler State | Default sampler state | Sampler for the texture |
| RGBA  | Output    | Vector 4  | None  | Output value as RGBA |
| R     | Output    | Float     | None  | red (x) component of RGBA output |
| G     | Output    | Float     | None  | green (y) component of RGBA output |
| B     | Output    | Float     | None  | blue (z) component of RGBA output |
| A     |   Output  | Float     | None | alpha (w) component of RGBA output |


## Controls

| Name                     | Type     | Options         | Description |
|:------------------------ |:---------|:----------------|:-------------------------|
|  Enable Global Mip Bias | Toggle   | On, Off         | Enables the global mipmap bias that Unity automatically imposes at runtime. Unity sets this bias during certain dynamic resolution scaling algorithms to improve detail reconstruction. |


## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 _SampleTexture2DArray_RGBA = SAMPLE_TEXTURE2D_ARRAY(Texture, Sampler, UV, Index);
float _SampleTexture2DArray_R = _SampleTexture2DArray_RGBA.r;
float _SampleTexture2DArray_G = _SampleTexture2DArray_RGBA.g;
float _SampleTexture2DArray_B = _SampleTexture2DArray_RGBA.b;
float _SampleTexture2DArray_A = _SampleTexture2DArray_RGBA.a;
```
