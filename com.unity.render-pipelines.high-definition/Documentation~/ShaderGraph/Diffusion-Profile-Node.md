# Diffusion Profile Node

Provide an input for a diffusion profile asset.

## Output port

| name | type | description
--- | --- | ---
| Out | float | A unique float that is used in the shader to identify the diffusion profile.

See the [Diffusion Profile Documentation](https://github.com/Unity-Technologies/ScriptableRenderPipeline/wiki/Diffusion-Profile) for more detail.

Warning: the output is treated as a float but it's content is used to find the diffusion profile settings associated with the asset so if you modify it's value you'll loose the profile effect. Even though you can enable / disable the diffusion profile by multiplying by 1 or 0, it allow you to use multiple diffusion profile for certain part of your shader graph. Also please note that the blending between diffusion profiles is not supported (because only one evaluation of diffusion profile per pixel is allowed).