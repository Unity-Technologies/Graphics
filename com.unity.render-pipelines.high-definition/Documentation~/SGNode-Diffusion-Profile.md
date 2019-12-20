# Diffusion Profile Node

The Diffusion Profile Node allows you to sample a [Diffusion Profile](Diffusion-Profile.html) Asset in your Shader Graph. For information on what a Diffusion Profile is and the properties that it contains, see the [Diffusion Profile documentation](Diffusion-Profile.html).

## Output port

| name | **Direction** | type | description |
|--- | --- | --- | --- |
| **Out** | Output | float | Outputs a unique float that the Shader uses to identify the Diffusion Profile. |

The output of this Node is a float value that represents a Diffusion Profile. The Shader can use this value to find settings for the Diffusion Profile Asset that this value represents.

If you modify the output value, the Shader can no longer use it to find the settings for the Diffusion Profile Asset. You can use this behavior to enable and disable Diffusion Profiles in your Shader Graph. To disable a Diffusion Profile, multiply the output by **0**. To enable a Diffusion Profile, multiply the output by **1**. This allows you to use multiple Diffusion Profiles in different parts of your Shader Graph. Be aware that the High Definition Render Pipeline (HDRP) does not support blending between Diffusion Profiles. This is because HDRP can only evaluate a single Diffusion Profile per pixel.