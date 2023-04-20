## Description
Calculates the billboard positon and the virtual UVs for sampling.

## Inputs
**InPosition** - The postion in Object space.
**UV** - The UV coordinates of the mesh.
**Frames** - The number of the imposter frames in each axis.
**Offset** - The offset value from the pivot.
**Size** - The size of the imposter.
**HemiSphere** - If it's true, calculates the imposter grid and UVs base on hemisphere type. Useful if the object is only seen from above.

## Output
**OutPosition** - The output billboard position.
**UV0** - The virtual UV for the base frame.
**UV1** - The virtual UV for the second frame.
**UV2** - The virtual UV for the third frame.
**Grid** - The current UV grid using to find the sample frames.

## Controls
**Sample Type** - Select whether to sample three frames or one frame, three frames for smoother result, one frame for better performance.
