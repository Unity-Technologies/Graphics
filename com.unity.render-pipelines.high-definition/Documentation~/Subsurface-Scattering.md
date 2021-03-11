# Subsurface Scattering

__Subsurface Scattering__ handles light that penetrates and moves within the area under a surface. Use it to make organic materials, like skin, look smooth and natural rather than rough and plastic-like. HDRP implements subsurface scattering using a screen-space blur technique.

Subsurface scattering also handles the light that penetrates GameObjects from behind and makes those GameObjects look transparent. For certain types of objects, the screen-space blur effect may not make a large visual difference. Therefore, HDRP implements two material types:

* __Subsurface Scattering__ implements both the screen-space blur effect and transmission (you can disable the latter).
* __Translucent__ only models transmission.

## Enabling Subsurface Scattering

To enable subsurface scattering in your [HDRP Asset](HDRP-Asset.md):

1. In the HDRP Asset’s Inspector window, go to the __Material__ section and enable the __Subsurface Scattering__ checkbox.
2. When you enable the __Subsurface Scattering__ checkbox, HDRP displays the __High Quality__ option. You can Enable this option to increase the sample count and reduce the amount of visual noise the blur pass can cause by under sampling. Note that this is around two and a half times more resource intensive than the default quality.
3. Go to **Edit > Project Settings > HDRP Default Settings** and, in the **Default Frame Settings** section, under the __Lighting__ subsection, enable __Subsurface Scattering__ and __Transmission__.

HDRP stores most subsurface scattering settings in a [Diffusion Profile](Diffusion-Profile.md). HDRP supports up to 15 custom Diffusion Profiles in view at the same time, but you can override which Diffusion Profiles HDRP uses and thus use as many Diffusion Profiles as you want throughout your project. To do this, use the [Diffusion Profile Override](Override-Diffusion-Profile.md) in the [Volume](Volumes.md) system. This [override](Volume-Components.md) lets you specify 15 custom Diffusion Profiles which HDRP can use for a Camera within the override's Volume.

For information on how to create and use a Diffusion Profile, see the [Diffusion Profile documentation](Diffusion-Profile.md)

## Adding Subsurface Scattering to your Material

First, change the Material’s __Material Type__ to __Subsurface Scattering__ or __Transmission__, depending on the effect you want.

For the __Subsurface Scattering__ material type, uncheck the __Transmission__ checkbox to disable transmission.

### Customizing Subsurface Scattering behavior

When you select __Subsurface Scattering__ or __Translucent__ from the __Material Type__ drop-down, Unity exposes several new properties in the Material UI. For information on how to use these properties to customize the behavior of the subsurface scattering effect, see the [Material Type documentation](Material-Type.md).

You can learn more about HDRP’s implementation in our [Efficient Screen-Space Subsurface Scattering](http://advances.realtimerendering.com/s2018/Efficient%20screen%20space%20subsurface%20scattering%20Siggraph%202018.pdf) presentation.
