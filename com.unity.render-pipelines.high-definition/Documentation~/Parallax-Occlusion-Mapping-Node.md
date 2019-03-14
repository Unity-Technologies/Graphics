# Parallax Occlusion Mapping Node

The parallax occlusion mapping (POM) node is used to create parallax effects which displace the UVs and depth to create the illusion of depth inside a material.

## Input port

| Name | Type | Description |
| --- | --- | --- |
| Heightmap | Texture2D | Texture used to sample the depth of the displacement. |
| Heightmap Sampler | Sampler State | Sampler used to perform the sampling on the Heightmap. |
| Amplitude | Float | Multiplier applied to the height of the Heightmap in centimeter. |
| Steps | Float | Number of steps performed by the linear search of the algorithm. |
| UVs | Vector2 | UVs used to sample the texture. |
| Lod | Float | Lod of the Heightmap to use for the sampling. |
| Lod Threshold | Float | Equivalent of the `Fading Mip Level Start` option in the HDRP Lit Material, controls the Heightmap mip level where the POM effect begin to disappear. |

## Output port

| Name | Type | Description |
| --- | --- | --- |
| Depth Offset | Float | Offset to apply to the depth buffer for POM. Connect to depth offset on the Master node to enable effects relying on depth buffer such as shadows and SSAO. |
| Parallax UVs | Vector2 | UVs after adding the parallax offset.