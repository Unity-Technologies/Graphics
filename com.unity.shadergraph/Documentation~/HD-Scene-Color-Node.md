# HD Scene Color Node

The HD Scene Color Node does the same thing as the Scene Color Node, but allows you to access the mips of the color buffer.

## Render pipeline compatibility

| **Node**       | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------- | ----------------------------------- | ------------------------------------------ |
| HD Scene Color | No                                  | Yes                                        |

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| **UV** | Input | Vector 4 | Screen Position | Sets the normalized screen coordinates to sample. |
| **Lod** | Input | float | None | Sets the mip level that the sampler uses to sample the color buffer. |
| **Output** | Output      |    Vector 3 | None | Output value |

## Notes
### Exposure

You can use the Exposure property to specify if you want to output the Camera color with exposure applied or not. By default, this property is disabled to avoid double exposure.

The sampler that this Node uses to sample the color buffer is in trilinear clamp mode. This allows the sampler to smoothly interpolate between the mip maps.
