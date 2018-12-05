# Time Node

## Description

Provides access to various **Time** parameters in the shader.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Time | Output      |    Vector 1 | None | Time value |
| Sine Time | Output      |    Vector 1 | None | Sine of Time value |
| Cosine Time | Output      |    Vector 1 | None | Cosine of Time value |
| Delta Time | Output      |    Vector 1 | None | Current frame time |
| Smooth Delta | Output      |    Vector 1 | None | Current frame time smoothed |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float Time_Time = _Time.y;
float Time_SineTime = _SinTime.w;
float Time_CosineTime = _CosTime.w;
float Time_DeltaTime = unity_DeltaTime.x;
float Time_SmoothDelta = unity_DeltaTime.z;
```