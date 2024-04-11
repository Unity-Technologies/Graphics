# Screen Node

## Description

Provides access to parameters of the screen.

#### Unity Render Pipelines Support
- Universal Render Pipeline

Note: when dynamic resolution is enabled, this node will return the current viewport of the rendering camera. After the upscaling pass, the output of this node will be equal to the screen size.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Width | Output      |    Float    | None | Screen's width in pixels |
| Height | Output      |    Float    | None | Screen's height in pixels |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float _Screen_Width = _ScreenParams.x;
float _Screen_Height = _ScreenParams.y;
```
