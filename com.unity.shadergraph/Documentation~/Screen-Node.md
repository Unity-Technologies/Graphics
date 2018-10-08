# Screen Node

## Description

Provides access to parameters of the screen.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Width | Output      |    Vector 1 | None | Screen's width in pixels |
| Height | Output      |    Vector 1 | None | Screen's height in pixels |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float _Screen_Width = _ScreenParams.x;
float _Screen_Height = _ScreenParams.y;
```