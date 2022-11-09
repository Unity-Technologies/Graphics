## Description
Samples a 3D Texture.

## Inputs
**Texture** - The texture asset to sample. The asset selector allows you to select a texture from the project.
**UV** - The texture coordinates to use for sampling the texture.
**Sampler** - The texture sampler to use for sampling the texture.
**LOD** - Explicitly defines the mip level to sample. (Available when Mip Sampling Mode is set to LOD.)

## Output
**RGBA** - A vector4 from the sampled texture.
**RGB** - A vector3 from the sampled texture.
**R** - The red channel of the sampled texture.
**G** - The green channel of the sampled texture.
**B** - The blue channel of the sampled texture.
**A** - The alpha channel of the sampled texture.

## Controls
**Mip Sampling Mode** - selects the method used to choose the correct mip map to sample.  Standard allows the hardware to select the mip based on standard derivatives.  LOD allows the author to explicitly control the mip to select.