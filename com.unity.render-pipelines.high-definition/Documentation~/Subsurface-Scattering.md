# Subsurface Scattering

__Subsurface Scattering__ handles light that penetrates and moves within the area under a surface. Use it to make organic materials, like skin, look smooth and natural rather than rough and plastic-like. HDRP implements subsurface scattering using a screen-space blur technique.

Subsurface scattering also handles the light that penetrates GameObjects from behind and makes those GameObjects look transparent. For certain types of objects, the screen-space blur effect may not make a large visual difference. Therefore, HDRP implements two material types: 

* __Subsurface Scattering__ implements both the screen-space blur effect and transmission (you can disable the latter)
* __Translucent__ only models transmission.

## Enabling Subsurface Scattering

To enable subsurface scattering in your [HDRP Asset](HDRP-Asset.html):

- In the HDRP Asset’s Inspector window, navigate to the __Material__ section and enable the __Subsurface Scattering__ checkbox.
- When you enable the __Subsurface Scattering__ checkbox, HDRP displays the __High Quality__ option. You can Enable the checkbox to increase the sample count and reduce the amount of visual noise the blur pass can cause by undersampling. Note that this is around two and a half times more resource intensive than the default quality.
- Within the __HDRP Asset__, locate the __Default Frame Settings__. Under the __Lighting__ section, enable __Subsurface Scattering__ and __Transmission__.

HDRP stores most subsurface scattering settings in a [Diffusion Profile Settings](Diffusion-Profile.html) Asset. The __Diffusion Profile List Asset__ contains a set of 15 Diffusion Profiles you can edit and later assign to your Materials.

To create a Diffusion Profile Asset, navigate to __Assets > Create > Rendering > Diffusion Profile Settings__. To use it, open your HDRP Asset and assign the new Diffusion Profile Asset to the __Diffusion Profile List__ property.

## Adding Subsurface Scattering to your Material

First, change the Material’s __Material Type__ to __Subsurface Scattering__ or __Transmission__, depending on the effect you want.

For the __Subsurface Scattering__ material type, uncheck the __Transmission__ checkbox to disable transmission.

### Customizing Subsurface Scattering behavior 

When you select __Subsurface Scattering__ or __Translucent__ from the __Material Type__ drop-down, Unity exposes several new properties in the Material UI. For information on how to use these properties to customize the behavior of the subsurface scattering effect, see the [Material Type documentation](Material-Type.html).

You can learn more about HDRP’s implementation in our [Efficient Screen-Space Subsurface Scattering](http://advances.realtimerendering.com/s2018/Efficient%20screen%20space%20subsurface%20scattering%20Siggraph%202018.pdf) presentation.
