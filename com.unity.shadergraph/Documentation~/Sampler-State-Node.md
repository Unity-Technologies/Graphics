# Sampler State Node

## Description

Defines a **Sampler State** for sampling textures. It should be used in conjunction with sampling [Nodes](Node.md) such as the [Sample Texture 2D Node](Sample-Texture-2D-Node.md). You can set a filter mode with the dropdown parameter **Filter** and a wrap mode with the dropdown parameter **Wrap**.

When using a separate **Sample State Node** you can sample a **Texture 2D** twice, with different sampler parameters, without defining the **Texture 2D** itself twice.

Some filtering and wrap modes are only available on certain platforms.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      | Sampler State | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Filter | Dropdown | Linear, Point, Trilinear | Defines filtering mode for sampling. |
| Wrap   | Dropdown | Repeat, Clamp, Mirror, MirrorOnce | Defines wrap mode for sampling. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
SamplerState _SamplerState_Out = _SamplerState_Linear_Repeat_sampler;
```