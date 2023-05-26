# Sampler State Node

## Description

Defines a **Sampler State** for sampling textures. It should be used in conjunction with sampling [Nodes](Node.md) such as the [Sample Texture 2D Node](Sample-Texture-2D-Node.md). You can set a filter mode with the dropdown parameter **Filter** and a wrap mode with the dropdown parameter **Wrap**.

When using a separate **Sample State Node** you can sample a **Texture 2D** twice, with different sampler parameters, without defining the **Texture 2D** itself twice.

Not all filtering, wrap, and Anisotropic filtering modes are available on all platforms.

## Ports

| Name | Direction | Type          | Binding | Description  |
|:-----|:----------|:--------------|:--------|:-------------|
| Out  | Output    | Sampler State | None    | Output value |

## Controls

| Name   | Type     | Options                           | Description |
|:-------|:---------|:----------------------------------|:------------|
| Filter | Dropdown | Linear, Point, Trilinear          | Specifies which filtering mode to use for sampling. |
| Wrap   | Dropdown | Repeat, Clamp, Mirror, MirrorOnce | Specifies which wrap mode to use for sampling. |

## Node Settings Controls

The following control appears on the Node Settings tab of the Graph Inspector when you select the Sampler State Node.

| Name                  | Type     | Options               | Description |
|:----------------------|:---------|:----------------------|:------------|
| Anisotropic Filtering | Dropdown | None, x2, x4, x8, x16 | Specifies the level of Anisotropic filtering to use to sample textures. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
SamplerState _SamplerState_Out = _SamplerState_Linear_Repeat_sampler;
```
