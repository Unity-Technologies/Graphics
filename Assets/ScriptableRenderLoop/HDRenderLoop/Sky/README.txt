# Atmospheric Scattering

Atmospheric scattering is a solution developed for nicer aerial perspective in The Blacksmith. It provides a model emulating atmospheric Rayleigh, Mie and height scattering (and also takes a lot of shortcuts in the name of performance and artistic control). Take a look at the blog post at this address http://blogs.unity3d.com/?p=27218 to get a high-level overview of the components making up the final composition.


### Options description

This component comes with quite a lot of options - but you normally don't have to tweak more than a handful of them to get good results - start with the densities and colors and that will get you started nicely.


#### World Components

**World Rayleigh Color Ramp**:  The color or colors used for rayleigh scattering. It's a ramp instead of a single color because you can have different scattering colors depending on the angle to the unit being shaded. Values to the left in the ramp are mapped to angles below horizon, whereas values to the right are mapped to angles above the horizon.

**World Rayleigh Color Intensity**: An HDR color scale for the rayleigh color ramp.

**World Rayleigh Density**: The density of the rayleigh component.

**World Rayleigh Extinction Factor**: How much light is out-scattered or absorbed on its way to the eye. Basically how much to darken the shaded pixel.

**World Rayleigh Indirect Scatter**: Which percentage of the rayleigh scattering should be considered 'indirect' scattering. (relevant for occlusion only)

**World Mie Color Ramp**: The color or colors used for mie scattering. It's a ramp instead of a single color because you can have different scattering colors depending on the angle to the unit being shaded. Values to the left in the ramp are mapped to angles below horizon, whereas values to the right are mapped to angles above the horizon.

**World Mie Color Intensity**: An HDR color scale for the mie color ramp.

**World Mie Density**: The density of the mie component.

**World Mie Extinction Factor**: How much light is out-scattered or absorbed on its way to the eye. Basically how much to darken the shaded pixel.

**World Mie Phase Anisotropy**: How focused the forward directionality of the mie scattering is. Values close to 1 produce a small, very sharp mie component, whereas values below 0.5 creates a very large and unfocused mie component.

**World Near Scatter Push**: Allows the scattering to be pushed out to have no scattering directly in front of the camera, or pulled in to have more scattering close to the camera.

**World Normal Distance**: A measure of the scale of the scene. Essentially this desaturates the scattering near the camera, and interpolates into full color ramp at the edge of the specified distance.


#### Height Components

**Height Rayleigh Color**: The general global scattering color for height fog.

**Height Rayleigh Intensity**: An HDR color scale for the height global fog color.

**Height Rayleigh Density**: The density of the global height fog.

**Height Mie Density**:  The density of the mie scattering being added to the global height fog.

**Height Extinction Factor**: How much light is out-scattered or absorbed on its way to the eye. Basically how much to darken the shaded pixel.

**Height Sea Level**: Sea level height offset from origin.

**Height Distance**: Falloff distance from sea level

**Height Plane Shift**: An optional plane vector for a tilted sea level.

**Height Near Scatter Push**: Allows the scattering to be pushed out to have no scattering directly in front of the camera, or pulled in to have more scattering close to the camera.

**Height Normal Distance**: A measure of the scale of the scene. Essentially this desaturates the scattering near the camera, and interpolates into full color at the edge of the specified distance.


#### Sky Dome

**Sky Dome Scale**: The scale of the skydome. We use this to virtually squash the sky sphere into behaving like a dome. The xz-dimensions affect how much scattering the horizon picks up, whereas the y-dimension dictates how far up on the sky the scattering is blended.

**Sky Dome Rotation**: An optional rotation of the skydome. Usually, only Y-rotation makes sense.

**Sky Dome Tracked Yaw Rotation**: Optionally have the skydome rotate to track a transform, typically a light source. Use in combination with rotation to perfectly align and track the sun to the skydome.

**Sky Dome Vertical Flip**: We have the option of putting two skybox textures into the same cubemap. This flags flips the UV projection to show the bottom half instead of the top half.

**Sky Dome Cube**: An HDR cubemap to use as sky dome.

**Sky Dome Exposure**: The exposure of the sky cubemap.

**Sky Dome Tint**: Tint color for the cubemap.


#### Scatter Occlusion

**Use Occlusion**: This flag enables scatter occlusion.

**Occlusion Bias**: Controls how strongly occlusion affects direct scattering.

**Occlusion BiasIndirect**: Controls how strongly occlusion affects indirect scattering.

**Occlusion BiasClouds**: Controls how strongly occlusion affects placed clouds.

**Occlusion Downscale**: Controls the downscale factor for occlusion gathering.

**Occlusion Samples**: The number of samples to use in gathering.

**Occlusion DepthFixup**: Whether to attempt to fix upsampling across depth discontinuities (currently d3d11 only).

**Occlusion DepthThreshold**: The threshold defining discontinuous depth.

**Occlusion FullSky**: Whether to gather occlusion even for skydome pixels.

**Occlusion BiasSkyRayleigh **: Controls how strongly occlusion affects rayleigh scattering on the skydome.

**Occlusion BiasSkyMie **: Controls how strongly occlusion affects mie scattering on the skydome.


#### Other

**World ScaleExponent**: An option to exponentially scale the world for scattering calculations (fake aerial perspective in small scenes).

**Force Per Pixel**: Force all scatter calculations to run at per-pixel frequency.

**Force Post Effect**: Force all scatter calculations to run in a post-process (requires the AtmosphericScatteringDeferred component on the camera, and doesn't apply to transparent object)

**Depth Texture**: Whether to enable, disable or leave alone the camera depth texture settings. (required for depth fixup and soft cloud planes)

**Debug Mode**: Various debug visualizations of the different rendering components.
