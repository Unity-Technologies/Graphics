# Common properties of 2D Lights
Each 2D Light Type has various properties and options to customize their appearance and behavior. This page documents the properties that are common to all 2D Light Types.

 following are the common properties used by the different Light types. For properties specific to each of the available Light Types, refer to their respective sections:

- [Freeform](LightTypes.md#freeform)
- [Sprite](LightTypes.md#sprite)
- [Spot](LightTypes.md#spot) (**Note:** The **Point** Light Type has been renamed to the **Spot** Light Type from URP 11 onwards.)
- [Global](LightTypes.md#global)


## Creating a Light

![](Images/2D/2d-lights-gameobject-menu.png)

Create a __2D Light__ GameObject by going to __GameObject > Light > 2D__ and selecting one of the five available types:

- __Freeform__: You can edit the shape of this Light type with a spline editor.
- __Sprite__: You can select a Sprite to create this Light type.
- __Spot__: You can control the inner and outer radius, direction and angle of this Light type.
- __Global__: This 2D Light affects all rendered Sprites on all targeted sorting layers.

The following are the common properties used by the different Light types.

![](Images/2D/2DLightBasics.png)

| Property                                                     | Function                                                     |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| __Light Type__                                               | Select the type of Light you want the selected Light to be. The available types are __Freeform__, __Sprite__, __Parametric__, __Spot__, and __Global__. |
| __Color__                                                    | Use the color picker to set the color of the emitted light.  |
| __[Intensity](#intensity)__                                  | Enter the desired brightness value of the Light. The default value is 1. |
| __[Overlap Operation](#overlap-operation)__        | Select the overlap operation used by this light The operations available are __Additive__, and __Alpha Blend__. |
| __Target Sorting Layers__                                    | Select the sorting layers that this Light targets and affects. |
| __[Blend Style](LightBlendStyles.md)__                       | Select the blend style used by this Light. Different blend styles can be customized in the [2D Renderer Asset](2DRendererConfig). |
| __[Light Order](#light-order)__ (unavailable for __Global Lights__) | Enter a value here to specify the rendering order of this Light relative to other Lights on the same sorting layer(s). Lights with lower values are rendered first, and negative values are valid. |
| __Shadow Strength__                                         | Use the slider to control the amount of light that __Shadow Caster 2Ds__ block when they obscure this Light. The value scales from 0 (no light is blocked) to 1 (all light is blocked). |
| __Volumtric Intensity__                                           | Use the slider to select the opacity of the volumetric lighting. The value scales from 0 (transparent) to 1 (opaque). |
| __Volumetric Shadow Strength__                                  | Use the slider to control the amount of volumetric light that __Shadow Caster 2Ds__ block when they obscure this Light. The value scales from 0 (no light is blocked) to 1 (all light is blocked). |
| __[Normal Map Quality](#quality)__                                      | Select either __Disabled__ (degfault)m __Accurate__ or __Fast__ to adjust the accuracy of the lighting calculations used. |
| __[Normal Map Distance](#distance)__  (available when __Use Normal Map__ quality is not disabled) | Enter the desired distance (in Unity units) between the Light and the lit Sprite. This does not Transform the position of the Light in the Scene. |

## Overlap Operation

This property controls the way in the selected Light interacts with other rendered Lights. You can toggle between the two modes by enabling or disabling this property. The effects of both modes are shown in the examples below:

| ![Overlap Operation set to Additive ](Images/2D/image_9.png) | ![Overlap Operation set to Alpha Blend](Images/2D/image_10.png) |
| ------------------------------------------------------------ | ------------------------------------------------------ |
| __Overlap Operation__ set to __Additive__ | __Overlap Operation__ set to __Alpha Blend__                     |

When __Overlap Operation__ is set to __Additive__, the Light is blended with other Lights additively, where the pixel values of intersecting Lights are added together. This is the default Light blending behavior.

When __Overlap Operation__ is set to __Alpha Blend__, Lights are blended together based on their alpha values. This can be used to completely overwrite one Light with another where they intersect, but the render order of the Lights is also dependent on the [Light Order](#light-order) of the different Lights.

## Light Order

The __Light Order__ value determines the position of the Light in the Render queue relative to other Lights that target the same sorting layer(s). Lower numbered Lights are rendered first, with higher numbered Lights rendered above those below. This especially affects the appearance of blended Lights when __Overlap Operation__ is set to __Alpha Blend__.

## Intensity

Light intensity are available to all types of Lights. Color adjusts the lights color, while intensity allows this color to go above 1. This allows lights which use multiply to brighten a sprite beyond its original color.

## Use Normal Map

All lights except for global lights can be toggled to use the normal maps in the sprites material. When enabled, Distance and Accuracy will be visible as new properties.

| ![Use Normal Map: Disabled](Images/2D/image_11.png) | ![Use Normal Map: Disabled](Images/2D/image_12.png) |
| ------------------------------------------------ | ------------------------------------------------ |
| __Use Normal Map__: __Disabled                     | __Use Normal Map:__ Enabled                      |

## Distance

Distance controls the distance between the light and the surface of the Sprite, changing the resulting lighting effect. This distance does not affect intensity, or transform the position of the Light in the Scene. The following examples show the effects of changing the Distance values.

| ![Distance: 0.5](Images/2D/image_13.png) | ![Distance: 2](Images/2D/image_14.png) | ![Distance: 8](Images/2D/image_15.png) |
| ------------------------------------- | ----------------------------------- | ----------------------------------- |
| __Distance__: 0.5                     | __Distance__: 2                     | __Distance__: 8                     |

## Quality

Light quality allows the developer to choose between performance and accuracy. When choosing performance, artefacts may occur.  Smaller lights and larger distance values will reduce the difference between fast and accurate.

## Volume Opacity

Volumetric lighting is available to all Light types. Use the __Volume Opacity__ slider to control the visibility of the volumetric light. At a value of zero, no Light volume is shown while at a value of one, the Light volume appears at full opacity.

## Shadow Intensity

The Shadow Intensity property controls the amount of light that **Shadow Caster 2Ds** block from the Light source which affects the intensity of their shadows. This is available on all non global Light types. Use this slider to control the amount of light that Shadow Caster 2Ds block when they interact with or block this Light.

The slider ranges from 0 to 1. At 0, Shadow Caster 2Ds do not block any light coming from the Light source and they create no shadows. At the maximum value of 1, Shadow Caster 2Ds block all light from the Light source and create shadows at full intensity.

| ![](Images/2D/ShadowIntensity0.png) | ![](Images/2D/ShadowIntensity05.png) | ![](Images/2D/ShadowIntensity100.png) |
| -------------------------------- | --------------------------------- | ---------------------------------- |
| Shadow Intensity = 0.0           | Shadow Intensity = 0.5            | Shadow Intensity = 1.0             |

## Shadow Volume Intensity

Shadow Volume Intensity determines the amount of volumetric light __Shadow Caster 2Ds__ block from the Light source. It is available on all non global lights, and when Volume Opacity is above zero. Use this slider to control the amount of volumetric light that Shadow Caster 2Ds block when they interact with or block this Light.

The slider ranges from 0 to 1. At 0, Shadow Caster 2Ds do not block any light coming from the Light source and they create no shadows. At the maximum value of 1, Shadow Caster 2Ds block all light from the Light source and create shadows at full intensity.

## Target Sorting Layers

Lights only light up the Sprites on their targeted sorting layers. Select the desired sorting layers from the drop-down menu for the selected Light. To add or remove sorting layers, refer to the [Tag Manager - Sorting Layers](https://docs.unity3d.com/Manual/class-TagManager.html#SortingLayers) for more information.
