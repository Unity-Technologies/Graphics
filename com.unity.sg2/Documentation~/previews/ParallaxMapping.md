## Description
Creates a parallax effect that displaces a Material's UVs to create the illusion of depth.

## Inputs
**Heightmap** - The texture that specifies the depth of the displacement.
**HeightmapSampler** - The sampler used to sample the Heightmap.
**Amplitude** - A multiplier to apply to the height of the Heightmap (in centimeters).
**UVs** - The UVs that the sampler uses to sample the texture.

## Output
**ParallaxUVs** - The UVs after adding the parallax offset.

## Controls
**Heightmap Sample Channel** - The channel of the input texture that contains the height data: red, green, blue, or alpha.