# Performance Comparison

This table lists all of the shaders in this sample in order from least expensive to most costly to calculate on the GPU. Notice how the number of texture samples effects the performance. Reducing the number of texture samples in your shader will almost always improve performance.

| Shader                | Relative Performance Cost              | Texture Samples |
|:--------------------|:--------------------|:------------|
| CHNOS Array Dither          | 0.4  | 6  |
| CSNOH Simple Blend          | 0.56 | 9  |
| CSNOH Simple Height         | 0.56 | 9  |
| CSNOH Auto Material Height  | 0.72 | 10 |
| CNM Simple Blend            | 0.88 | 13 |
| CNM Simple Height           | 0.92 | 13 |
| Terrain Lit Blend           | 1.00 | 13 |
| CHNOS Array Hex             | 1.64 | 14 |
| CSNOH Rotation Height       | 1.68 | 21 |
| CNM Rotation Height         | 2.32 | 29 |
| CSNOH Procedural Height     | 2.56 | 17 |
| CSNOH Hex Blend             | 3.12 | 25 |
| CSNOH Hex Height            | 3.36 | 25 |
| CNM Procedural Height       | 3.56 | 25 |
| CNM Hex Height              | 4.08 | 37 |
| CSNOH Auto Material Hex     | 4.68 | 30 |
| CSNOH Auto Material Rotation| 5.04 | 20 |
| CSNOH Parallax Height       | 5.20 | variable |
| CSNOH Hex Parallax Height   | 19.80| variable - very high |

## Calculating Relative Performance Cost
The Relative Performance Cost is calculated by rendering a scene that only has a terrain in it with the shader applied.  We measure how long the scene takes to render for each of the shaders.  Then we subtract out the time it takes to render the scene using the empty shader.  The time remaining is the cost of just the specific shader without any of the additional costs of the scene.  Then we divide that score by the score of TerrainLitBlend - the existing terrain shader.  That means that each score is a multiple of the existing HLSL shader.  This makes it easy to see how each shader compares to the expense of the existing terrain shader.


## Performance Observations
- More texture samples make the shader slower.  Math in the shader is quite cheap compared to texture samples.

- CSNOH packing scheme is ~40% cheaper than CNM because it uses 31% fewer texture samples.

- The difference between simple blending and height blending is so small it's not measurable but the visual difference is huge. If you're using regular blending to save performance, it's **not** helping you.

- Hex blending is 4 times the cost of the existing shader because of the extra texture samples and math required.  It’s probably not good to use when performance is critical, like on mobile.

- CHNOS Array Dither is the most cost-effective solution because it gets you all four layers with all PBR data at less than half the cost of Terrain Lit.

- CSNOH Hex Parallax Height is 20 times as expensive as Terrain Lit.  It probably shouldn’t be used in real-time applications at all.


