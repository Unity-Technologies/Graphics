## Description
Calculates the billboard positon and the virtual UVs for sampling.

## Inputs
**InPosition** - The postiont in Object space.
**UV** - The UV coordinates of the mesh.
**Frames** - The amount of the imposter frames
**Offset** - The offset value from the origin.
**Size** - The size of the imposter.
**HemiSphere** - If it's true, calculate imposter grid and UVs base on hemisphere type.

## Output
**OutPosition** - The output billboard position..
**UV0** - The virtual UV for the base frame.
**UV1** - The virtual UV for the second frame.
**UV2** - The virtual UV for the third frame.
**Grid** - The current UV grid using to find the sample frames.

## Controls
**Sample Type** - Select whether to sample three frames or one frame, three frames for smoother result, one frame for better performance.
