# Environment lighting

The High Definition Render Pipeline (HDRP) generates environment lighting from the area surrounding your Unity Scene. The most common source of environment lighting is sky lighting, but there are other types of background light, such as light from an interior lighting studio.

In HDRP, you create and customize environment lighting using the [Volume](Volumes.html) framework.

With the [built-in render pipeline](https://docs.unity3d.com/Manual/SL-RenderPipeline.html), you use the **Environment Lighting** settings in the **Lighting** window to customize environment lighting settings on a per Scene basis. In contrast, HDRP allows you to use the Volume framework to smoothly interpolate between different sets of environment lighting settings for your sky and fog within the same Scene.

The two key components for environment lighting are the:

- Visual Environment, controlled by the [Visual Environment](Override-Visual-Environment.html) Volume override.
- Baking environment, controlled by the [Static Lighting Sky](Static-Lighting-Sky.html) component.

## Visual Environment

The Visual Environment is a Volume component that tells HDRP what type of [sky](HDRP-Features.html#SkyOverview.html) and [fog](HDRP-Features.html#FogOverview.html) you want to render for Cameras that the Volume affects. For information on how to customize Visual Environments, see the [Visual Environment](Override-Visual-Environment.html) documentation .

Your Unity Project’s [HDRP Asset](HDRP-Asset.html#SkyLighting) has the following global sky properties that affect all Visual Environments:

- **Reflection Size**: Controls the resolution of the sky cube map. This handles fallback reflection when there are no local reflection probes present, and has no effect on the quality of the sky itself.
- **Lighting Override Mask**: A LayerMask that allows you to decouple the sky from the lighting. For example, when you have a dark sky at night time but want to have brighter lighting so that you can still see clearly.

<a name="BakingEnvironment"></a>

## Baking environment

The [Static Lighting Sky](Static-Lighting-Sky.html) component specifies the type of sky that the light baking process uses to bake environmental lighting. This component has two properties:

- **Profile:** A Volume Profile for the sky. This Volume Profile must include at least one sky type Volume override.
- **Static Lighting Sky:** The sky from the **Profile** to use for the light baking process. The drop-down only contains sky types that the **Profile** includes as Volume overrides.

You can assign the same Volume Profile to both a Static Lighting Sky and a Volume in your Scene. If you use the same sky settings for the baked lighting defined in the Static Lighting Sky and the visual background in the Volume, the baked lighting accurately matches the background at run time. If you want to control the light baking for the environment lighting separately to the visual background in your Scene, you can assign a different Volume Profile for each process .

<a name="DecoupleVisualEnvironment"></a>

## Decoupling Visual Environment from lighting

You can use the sky **Lighting Override Mask**, in your Unity Project’s HDRP Asset, to separate the Visual Environment from the environment lighting. If you set the **Lighting Override Mask** to **Nothing**, or to a group of Layers that have no Volumes on them, then no Layer acts as an override. This means that environment lighting comes from all Volumes that affect a Camera. If you set the **Lighting Override Mask** to include Layers that have Volumes on them, HDRP only uses Volumes on these Layers to calculate environment lighting.

An example of where you would want to decouple the sky lighting from the visual sky, and use a different Volume Profile for each, is when you have an [HDRI Sky](Override-HDRI-Sky.html) that includes sunlight.: To make the sun visible at run time in your application, your sky background must show an HDRI sky that features the sun. To achieve real-time lighting from the sun, you must use a Directional [Light](Light-Component.html) in your Scene and, for the baking process, use an HDRI sky that is identical to the first one but does not include the sun. If you were to use an HDRI sky that includes the sun to bake the lighting, the sun would contribute to the lighting twice, from the Directional Light and from the baking process, and make the lighting look unrealistic.

## HDRP built-in sky types

HDRP comes with three built-in [sky types](HDRP-Features.html#SkyOverview.html):

- [HDRI Sky](Override-HDRI-Sky.html)
- [Gradient Sky](Override-Gradient-Sky.html)
- [Procedural Sky](Override-Procedural-Sky.html)

HDRP allows you to implement your own sky types that display a background and handle environment lighting. See the [Customizing HDRP](Creating-a-Custom-Sky.html) documentation for instructions on how to implement your own sky.

## **Reflections**

Reflection Probes work just like Cameras. Like Cameras, they use the Volume system and therefore use environment lighting from the sky, which you set in the Visual Environment of the Volume that affects them.