# Texture 2D Array Asset Node

## Description

Defines a constant **Texture 2D Array Asset** for use in the shader. To sample the **Texture 2D Array Asset** it should be used in conjunction with a [Sample Texture 2D Array Node](Sample-Texture-2D-Array-Node.md). When using a separate **Texture 2D Array Asset Node**, you can sample a **Texture 2D Array** twice, with different parameters, without defining the **Texture 2D Array** itself twice.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Out | Output      |    Texture 2D Array | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|      | Object Field (Texture 2D Array) |  | Defines the texture 2D array asset from the project. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
TEXTURE2D_ARRAY(_Texture2DArrayAsset);
SAMPLER(sampler_Texture2DArrayAsset);
```
