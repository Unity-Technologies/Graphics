# Limitations related to environment effects

Environement effects like clouds, atmosphere, water or fog are volumetric effects that are complex to render.
HDRP uses different methods and approximations for rendering each of them and interactions between them are sometimes difficult to get right.
This page lists the limitations that apply when mixing environment effects or when integrating them with the scene geometry.

### Physically Based Sky

The [PBR sky](create-a-physically-based-sky.md) renders a spherical planet with an exponentially decreasing atmosphere density.
When viewing objects at a distance near the surface of the planet, light gets absorbed by the atmosphere, resulting for example in a color shift towards blue called Aerial Perspective.

To render this effect efficiently, HDRP computes a LUT that stores attenuation in the camera frustum, and applies it at runtime on all geometry as well as volumetric fog. The maximum distance at which this effect is correct is fixed to 128km, after which absorption doesn't change.
Additionally, atmosphere absorbs part of the sun light before it reaches the planet surface. This is precomputed on CPU before rendering depending on the camera position and premultiplied to the sun light color. This may give a slightly wrong result when looking at an object in space from the surface of the planet.
To have precise atmospheric attenuation and per pixel absorption of the sun light, you can disable the `PrecomputedAtmosphericAttenuation` option in the HDRP config package, which can be done using the [Render Pipeline Wizard](Render-Pipeline-Wizard.md). For more info, see [HDRP Config](configure-a-project-using-the-hdrp-config-package.md).


### Water system

Water is a refractive transparent object, however for performance reasons, it is necessary to render the depth of the water surface in the depth buffer.
As a result, other transparent objects that are located behind the water surface will get culled during rendering by z-testing.
To workaround this limitation, HDRP supports per pixel sorting of transparent objects with refractive objects. This works by copying the depth buffer before rendering water, and using the original buffer for z-test when rendering non refractive transparent objects. At the end of the frame, both buffers are composited to produce a frame containing all objects properly sorted.
For a transparent object to be rendered in the second depth buffer and composited at the end, its material needs to be placed in the **Before Refraction** renderqueue and the **Sort with Refractive** option needs to be enabled. During rendering, they are outputed to two possible color buffers, depending on wether they are located behind or in front of the refractive object. Transparents behind a refractive object need to be part of the color pyramid, so they can be blurred and seen through refraction, but transparents in front of refractive objects need to not be part of the pyramid to avoid leaking.

### Volumetric Clouds

Volumetric clouds are rendered as **Before Refraction** transparents with the **Sort with Refractive** option enabled. This means clouds will be correctly sorted with water surfaces by default. Additionally, you can also benefit from proper sorting with any transparent refractive objects.

### Volumetric Fog

Due to rendering order constraints, the multiple scattering effect of the fog does not consider the opacity of volumetric clouds or cloud layers. Additionally, the multiple-scattering effect exclusively incorporates the opacity of the fog on opaque objects, sky, or water surface. This implies that in a scenario where a pre-refractive object is rendered in front of the fog, the pre-refractive object will appear blurred, resembling a position behind the fog, although it doesn't exhibit the color attenuation effects of the fog.
