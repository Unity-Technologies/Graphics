# Post-processing effect execution order

The post-processing system in the High Definition Render Pipeline applies post-processing effects in a specific order. The system also combines some effects into the same Compute Shader to minimize the number of passes.

## Execution order and effect grouping

HDRP executes post processing effects in the following order, from top to bottom.

| Post-processing passes     | Compute shader stack  | Final post-processing pass |
| -------------------------- | --------------------- | -------------------------- |
| NaN Killer                 |                       |                            |
| Anti-aliasing (TAA, SMAA)  |                       |                            |
| Depth of Field             |                       |                            |
| Motion Blur                |                       |                            |
| Panini Projection          |                       |                            |
| Bloom (Pyramid)            |                       |                            |
| Color Grading (LUT Baking) |                       |                            |
| Data driven lens flare     |                       |                            |
|                            | Lens Distortion       |                            |
|                            | Chromatic Aberration  |                            |
|                            | Bloom (Apply)         |                            |
|                            | Vignette              |                            |
|                            | Color Grading (Apply) |                            |
|                            |                       | Antialiasing (FXAA)        |
|                            |                       | Film Grain                 |
|                            |                       | 8-bit Dithering            |