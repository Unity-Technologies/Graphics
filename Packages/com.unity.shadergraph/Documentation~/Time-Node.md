# Time Node

## Description

Provides access to various **Time** parameters in the shader.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Time | Output      |    Float    | None | Elapsed time in seconds. |
| Sine Time | Output      |    Float    | None | Sine of the **Time** value. Output ranges from &minus;1 to 1. |
| Cosine Time | Output      |    Float    | None | Cosine of the **Time** value. Output ranges from &minus;1 to 1. |
| Delta Time | Output      |    Float    | None | The time that has elapsed between the current frame and the last frame, in seconds. |
| Smooth Delta | Output      |    Float    | None | The time that has elapsed between the current frame and the last frame, in seconds, averaged over several frames to reduce jitter. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float Time_Time = _Time.y;
float Time_SineTime = _SinTime.w;
float Time_CosineTime = _CosTime.w;
float Time_DeltaTime = unity_DeltaTime.x;
float Time_SmoothDelta = unity_DeltaTime.z;
```
