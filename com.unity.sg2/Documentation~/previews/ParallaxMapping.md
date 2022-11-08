## Description
Creates a parallax effect that displaces a Material's UVs to create the illusion of depth.

## Inputs
**Heightmap** - the texture that specifies the depth of the displacement
**HeightmapSampler** - the sampler used to sample the Heightmap
**Amplitude** - a multiplier to apply to the height of the Heightmap (in centimeters)
**UVs** - the UVs that the sampler uses to sample the texture

## Output
**ParallaxUVs** - the UVs after adding the parallax offset

## Controls
**Heightmap Sample Channel** - the channel of the input texture that contains the height data: red, green, blue, or alpha.