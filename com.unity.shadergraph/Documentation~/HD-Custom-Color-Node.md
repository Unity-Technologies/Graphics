# Custom Color Node (HDRP)

The Custom Color Node accesses the custom pass color buffer allocated by HDRP.

## Render pipeline compatibility

| **Node**       | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------- | ----------------------------------- | ------------------------------------------ |
| Custom Color Node | No                                  | Yes                                        |

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| **UV** | Input | Vector 4 | Screen Position | Sets the normalized screen coordinates to sample. |
| **Output** | Output      |    Vector 4 | None | The value the custom pass color buffer contains at the sampled coordinates. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_CustomDepth_LinearEye_float(float4 UV, out float Out)
{
    Out = SampleCustomColor(UV.xy);
}
```
