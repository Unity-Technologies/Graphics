# Custom Depth Node (HDRP)

The Custom Depth Node accesses the custom pass color buffer allocated by HDRP.

## Render pipeline compatibility

| **Node**       | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------- | ----------------------------------- | ------------------------------------------ |
| Custom Depth Node | No                                  | Yes                                        |

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| **UV** | Input | Vector 4 | Screen Position | Sets the normalized screen coordinates that this node samples. |
| **Output** | Output      |    Vector 4 | None | The output value of this node. |

## Depth Sampling modes
| Name     | Description                        |
|----------|------------------------------------|
| Linear01 | The linear depth value between 0 and 1. |
| Raw      | The raw depth value.                    |
| Eye      | The depth value converted to eye space units. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_CustomDepth_LinearEye_float(float4 UV, out float Out)
{
    Out = LinearEyeDepth(SampleCustomDepth(UV.xy), _ZBufferParams);
}
```
