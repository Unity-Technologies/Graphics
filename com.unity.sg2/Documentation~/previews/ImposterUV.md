## Description
Calculates the billboard positon and the virtual UVs for sampling.

## Inputs
**InPosition** - The postion in Object space.
**In UV** - The UV coordinates of the mesh.
**Frames** - The number of the imposter frames in each axis.
**Size** - The size of the imposter.
**Offset** - The offset value from the pivot.
**Frame Clipping Threshold** - The clamping value for the neighboring frames most useful when parallax mapping is enabled.
**Texture Size** - The texture resolution.
**Height Map** - The height map texture to sample.
**Sampler** - The texture sampler to use for sampling the texture.
**Parallax** - Parallax strength.
**Height Map Channel** - The channle of the height map to sample for parallax mapping, if any.
**HemiSphere** - If it's true, calculates the imposter grid and UVs base on hemisphere type. Useful if the object is only seen from above.

## Output
**OutPosition** - The output billboard position.
**UV0** - The virtual UV for the base frame.
**UV1** - The virtual UV for the second frame.
**UV2** - The virtual UV for the third frame.
**Grid** - The current UV grid using to find the sample frames.

## Controls
**Sample Type** - Select whether to sample three frames or one frame, three frames for smoother result, one frame for better performance.
