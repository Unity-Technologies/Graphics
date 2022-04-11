# Scene Depth Difference

## Description

Provide a difference between a World Space Position and a Depth value for a given UV.

## Ports

| Name   | Direction  | Type  | Binding | Description |
|:-------|:-----------|:------|:--------|:------------|
| Scene UV | Input     | Vector4 | None    | UV where to sample the depth. |
| Position WS | Input     | Vector3 | None    | The world space position to compare with scene depth. |
| Out    | Output     | Float | None    | The difference between PositionWS and the depth. The difference is given relative to camera with **Eye** mode, in depth-buffer-value with **Raw** mode and in Linear value remap between 0 and 1 with the **Linear01** Mode. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mode      | Dropdown | Select **Linear01** to have a value between 0 and 1, **Eye** to have a World-Space value comparable to unit used on the scene and **Raw** if it's used with SceneDepthBuffer. |
