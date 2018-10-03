## Description

Generates a checkerboard of alternating colors between inputs **Color A** and **Color B** based on input **UV**. The checkerboard scale is defined by input **Frequency**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Color A      | Input | Color RGB | None | First checker color |
| Color B      | Input | Color RGB | None | Second checker color |
| Frequency      | Input | Vector 2 | None | Scale of checkerboard per axis |
| Out | Output      |    Vector 2 | None | Output UV value |

## Shader Function

```
UV = UV + 0.25 / Frequency;
float4 derivatives = float4(ddx(UV), ddy(UV));
float2 duv_length = sqrt(float2(dot(derivatives.xz, derivatives.xz), dot(derivatives.yw, derivatives.yw)));
float width = 0.5;
float2 distance3 = 2.0 * abs(frac((UV.xy + 0.5) * Frequency) - 0.5) - width;
float2 scale = 0.5 / duv_length.xy;
float2 blend_out = saturate(scale / 3);
float2 vector_alpha = clamp(distance3 * scale.xy * blend_out.xy, -1.0, 1.0);
float alpha = saturate(vector_alpha.x * vector_alpha.y);
Out = lerp(ColorA, ColorB, alpha.xxx);
```