## Description
Takes 4 samples (red channel only) to use for bilinear interpolation during sampling.

## Inputs
**Texture** - The texture asset to sample. The asset selector allows you to select a texture from the project.
**UV** - The texture coordinates to use for sampling the texture.
**Sampler** - The texture sampler to use for sampling the texture.
**Offset** - The pixel offset to apply to the sample's UV coordinates.

## Output
**RGBA** - The red channels of the 4 neighboring pixels from the specified sample position.
**R** - The first neighboring pixel's red channel.
**G** - The second neighboring pixel's red channel.
**B** - The third neighboring pixel's red channel.
**A** - The fourth neighboring pixel's red channel.