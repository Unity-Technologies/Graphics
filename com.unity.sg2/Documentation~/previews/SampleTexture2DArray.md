## Description
Samples a 2D Texture Array.

## Inputs
**TextureArray** - The texture array asset to sample. The asset selector allows you to select a texture from the project.
**Index** - The slice of the array to sample.
**UV** - The texture coordinates to use for sampling the texture.
**Sampler** - The texture sampler to use for sampling the texture.
**LOD** - Explicitly defines the mip level to sample. (Available when Mip Sampling Mode is set to LOD.)
**DDX** - The horizontal derivitive used to calculate the mip level.(Available when Mip Sampling Mode is set to Gradient.)
**DDY** - The vertical derivitive used to calculate the mip level. (Available when Mip Sampling Mode is set to Gradient.)
**Bias** - Adds or substracts from the auto-generated mip level. (Available when Mip Sampling Mode is set to Bias.)

## Output
**RGBA** - A vector4 from the sampled texture.
**RGB** - A vector3 from the sampled texture.
**R** - The red channel of the sampled texture.
**G** - The green channel of the sampled texture.
**B** - The blue channel of the sampled texture.
**A** - The alpha channel of the sampled texture.

## Controls
**Type** - Select the type of texture to sample - standard, tangent space normal, or object space normal.
**Mip Sampling Mode** - selects the method used to choose the correct mip map to sample.  Standard allows the hardware to select the mip based on standard derivatives.  LOD allows the author to explicitly control the mip to select.  Bias allows the author to push the auto-selected mip level up or down.  Gradient allows the author to provide their own derivatives.