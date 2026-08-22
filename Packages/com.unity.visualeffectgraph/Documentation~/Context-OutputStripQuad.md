# Output ParticleStrip Quad Context

Menu Path: **Context** > **Output ParticleStrip [Unlit/HDRP Lit/URP Lit] Quad**

*(Output ParticleStrip Quad, Output ParticleStrip HDRP Lit Quad, Output ParticleStrip URP Lit Quad)*

The **Output ParticleStrip Quad** Context renders each particle strip as a continuous ribbon of quads. It comes in a regular (unlit) variety, and a [Lit](Context-OutputLitSettings.md) variety for HDRP and URP. This output is similar to [Output ParticleStrip ShaderGraph](Context-OutputShaderGraphStrip.md), but doesn't use a Shader Graph to define its surface.

The following table lists the settings and properties specific to the Output ParticleStrip Quad Context. For information about the generic output settings this Context shares with all other Contexts, refer to [Global Output Settings and Properties](Context-OutputSharedSettings.md).

## Context settings

| **Setting**      | **Type** | **Description**                                              |
| ----------------- | -------- | ------------------------------------------------------------ |
| **Tiling Mode**    | Enum     | Specifies how the output generates texture coordinates within a strip. The options are:<br/>&#8226; **Stretch**: Stretches the mapping along the whole strip.<br/>&#8226; **Repeat Per Segment**: Restarts the mapping for every segment of the strip.<br/>&#8226; **Custom**: Manually provides the reference texture coordinate. |
| **Swap UV**        | Bool     | Invert the two channels of texture coordinates. |

## Context properties

| **Input**     | **Type** | **Description**                                              |
| ------------- | -------- | ------------------------------------------------------------ |
| **Tex Coord** | float    | Custom texture coordinate that acts as a reference for each segment of a strip.<br/>This property only appears if you set **Tiling Mode** to **Custom**. |

## Additional resources

- [Global Output Settings and Properties](Context-OutputSharedSettings.md)
- [Lit Output Settings Context](Context-OutputLitSettings.md)
- [Output ParticleStrip ShaderGraph Context](Context-OutputShaderGraphStrip.md)
