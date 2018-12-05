# Texture 2D Asset Node

## Description

Defines a constant **Texture 2D Asset** for use in the shader. To sample the **Texture 2D Asset** it should be used in conjunction with a [Sample Texture 2D Node](Sample-Texture-2D-Node.md). When using a separate **Texture 2D Asset Node**, you can sample a **Texture 2D** twice, with different parameters, without defining the **Texture 2D** itself twice.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Out | Output      |    Texture 2D | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|      | Object Field (Texture) |  | Defines the texture 3D asset from the project. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
TEXTURE2D(_Texture2DAsset); 
SAMPLER(sampler_Texture2DAsset);
```