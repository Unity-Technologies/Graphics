## Description
Samples a texture three times and projects in front/back, top/bottom, and left/right.

## Inputs
**Texture** - the texture asset to sample. The asset selector allows you to select a texture from the project.
**Sampler** - the texture sampler to use for sampling the texture
**Position** - position is used for projecting the texture onto the mesh
**Normal** - the normal is used to mask and blend between the projections
**Tile** - the number of texture tiles per meter
**Blend** - the focus or blurriness of the blending between the projections

## Output
**XYZ** - texture projected front/back, left/right, top/bottom
**XZ** - texture projected front/back and left/right
**Y** - texture projected top/bottom

## Controls
**Type** - Select the type of texture to project: Default, Normal Map, or Two Samples.
Default - samples a texture map and returns standard values
Normal Map - samples a normal map and returns a normal vector
2 Samples - a slightly faster method of triplanar projection that requires only two texture samples instead of three. Because this method only uses 2 samples, you may notice a slight singularity at the points where the three projections converge.