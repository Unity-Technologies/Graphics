## Description
Samples a Cubemap.

## Inputs
**Cube** - the cubemap asset to sample. The asset selector allows you to select a texture from the project.
**Dir** - the direction vector used to sample the cubemap
**Sampler** - the texture sampler to use for sampling the cubemap
**LOD** - explicitly defines the mip level to sample. (Available when Mip Sampling Mode is set to LOD.)
**Bias** - adds or substracts from the auto-generated mip level. (Available when Mip Sampling Mode is set to Bias.)

## Output
**RGBA** - A vector4 from the sampled texture
**RGB** - A vector3 from the sampled texture
**R** - the red channel of the sampled texture
**G** - the green channel of the sampled texture
**B** - the blue channel of the sampled texture
**A** - the alpha channel of the sampled texture

## Controls
**Mip Sampling Mode** - selects the method used to choose the correct mip map to sample.  Standard allows the hardware to select the mip based on standard derivatives.  LOD allows the author to explicitly control the mip to select.  Bias allows the author to push the auto-selected mip level up or down.