# Texture 3D Asset Node

## Description

Defines a constant **Texture 3D Asset** for use in the shader. To sample the **Texture 3D Asset** it should be used in conjunction with a [Sample Texture 3D Node](Sample-Texture-3D-Node.md). When using a separate **Texture 3D Asset Node**, you can sample a **Texture 3D** twice, with different parameters, without defining the **Texture 3D** itself twice.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Out | Output      |    Texture 3D | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|      | Object Field (Texture 3D) |  | Defines the texture 3D asset from the project. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
TEXTURE3D(_Texture3DAsset);
SAMPLER(sampler_Texture3DAsset);
```
