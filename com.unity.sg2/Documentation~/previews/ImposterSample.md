## Description
Samples from the three virtual UVs and blends them base on the camera intersection point to get the correct result.
## Inputs
**Texture** - The texture asset to sample.
**Sampler** - The texture sampler to use for sampling the texture.
**UV0** - The virtual UV for the base frame.
**UV1** - The virtual UV for the second frame.
**UV2** - The virtual UV for the third frame.
**Grid** - The current UV grid using to find the sample frames.
**Frames** - The amount of the imposter frames
**Border Clamp** - The amount of clamping for a single frame.
**Parallax** - If Texture is a normal map, add parallax shif if the value is true.

## Output
**RGBA** - A vector4 from the sampled texture.

## Controls
**Sample Type** - Select whether to sample three frames or one frame, three frames for smoother result, one frame for better performance.
