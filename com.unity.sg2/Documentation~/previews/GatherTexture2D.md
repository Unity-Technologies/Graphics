## Description
Takes 4 samples (red channel only) to use for bilinear interpolation during sampling.

## Inputs
**Texture** - the texture asset to sample. The asset selector allows you to select a texture from the project.
**UV** - the texture coordinates to use for sampling the texture
**Sampler** - the texture sampler to use for sampling the texture
**Offset** - the pixel offset to apply to the sample's UV coordinates

## Output
**RGBA** - the red channels of the 4 neighboring pixels from the specified sample position
**R** - the first neighboring pixel's red channel
**G** - the second neighboring pixel's red channel
**B** - the third neighboring pixel's red channel
**A** - the fourth neighboring pixel's red channel