## Description
Creates a normal from multiple samples of a height map.

## Inputs
**Texture** - The height map texture asset to sample. The asset selector allows you to select a texture from the project.
**UV** - The texture coordinates to use for sampling the texture.
**Sampler** - The texture sampler to use for sampling the texture.
**Offset** - The amount to offset samples in texels.
**Strength** - Normal strength multiplier.

## Output
**Out** - Normal created from the input height texture.

## Controls
**Output Space** - Controls whether the resulting normal will be in Tangent space or World space.
**Height Channel** - Selects the channel of the texture (red, green, blue, or alpha) that contains the height data.
**Sample Count** - The number of samples used to calculate the normal.  More samples provides higher quality but costs more performance.