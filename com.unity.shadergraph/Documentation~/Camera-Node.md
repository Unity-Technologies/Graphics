# Camera Node

## Description

Provides access to various parameters of the **Camera** currently being used for rendering. This is comprised of values the **Camera**'s GameObject, such as Position and Direction, as well as various projection parameters.

#### Unity Render Pipelines Support
- Universal Render Pipeline

The High Definition Render Pipeline does **not** support this Node.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Position      | Output | Vector 3 | None | Position of the Camera's GameObject in world space |
| Direction       | Output | Vector 3 | None | The Camera's forward vector direction |
| Orthographic    | Output | Float    | None | Returns 1 if the Camera is orthographic, otherwise 0 |
| Near Plane       | Output | Float    | None | The Camera's near plane distance |
| Far Plane       | Output | Float    | None | The Camera's far plane distance |
| Z Buffer Sign   | Output | Float    | None | Returns -1 when using a reversed Z Buffer, otherwise 1 |
| Width       | Output | Float    | None | The Camera's width if orthographic |
| Height       | Output | Float    | None | The Camera's height if orthographic |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3 _Camera_Position = _WorldSpaceCameraPos;
float3 _Camera_Direction = -1 * mul(UNITY_MATRIX_M, transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V)) [2].xyz);
float _Camera_Orthographic = unity_OrthoParams.w;
float _Camera_NearPlane = _ProjectionParams.y;
float _Camera_FarPlane = _ProjectionParams.z;
float _Camera_ZBufferSign = _ProjectionParams.x;
float _Camera_Width = unity_OrthoParams.x;
float _Camera_Height = unity_OrthoParams.y;
```