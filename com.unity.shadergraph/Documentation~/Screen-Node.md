# Screen Node

## Description

Provides access to parameters of the screen.

#### Unity Render Pipelines Support
- Universal Render Pipeline

The High Definition Render Pipeline does **not** support this Node.

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