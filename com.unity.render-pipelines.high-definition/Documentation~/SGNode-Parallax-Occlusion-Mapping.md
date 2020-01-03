# Parallax Occlusion Mapping Node

The Parallax Occlusion Mapping (POM) Node allows you to create a parallax effect that displaces a Material's UVs and depth to create the illusion of depth inside a Material.

## Ports

| Name | **Direction** | Type | Description |
| --- | --- | --- | --- |
| **Heightmap** | Input | Texture2D | The Texture that specifies the depth of the displacement. |
| **Heightmap Sampler** | Input | Sampler State | The Sampler to sample **Heightmap** with. |
| **Amplitude** | Input | Float | A multiplier to apply to the height of the Heightmap (in centimeters). |
| **Steps** | Input | Float | The number of steps that the linear search of the algorithm performs. |
| **UVs** | Input | Vector2 | The UVs that the sampler uses to sample the Texture. |
| **Lod** | Input | Float | The level of detail to use to sample **Heightmap**. |
| **Lod Threshold** | Input | Float | The **Heightmap** mip level where the POM effect begins to fade out. This is equivalent to the **Fading Mip Level Start** property in the High Definition Render Pipeline's (HDRP) [Lit Material](Lit-Shader.html). |
| **Depth Offset** | Output |Float | The offset to apply to the depth buffer to produce the illusion of depth. To enable effects that rely on the depth buffer, such as shadows and screen space ambient occlusion, connect this output to the **Depth Offset** on the Master Node. |
| **Parallax UVs** | Output| Vector2 | The UVs after adding the parallax offset. |