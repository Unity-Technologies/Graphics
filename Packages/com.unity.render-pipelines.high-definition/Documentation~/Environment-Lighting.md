# Create environment lighting

Environment lighting allows you to simulate lighting coming from the surroundings of your Scene. It is common to use environment lighting to simulate sky lighting, but you can also use it to simulate a colored ambient light, or a lighting studio.
In the High Definition Render Pipeline (HDRP), there are two parts to environment lighting:

* The visual environment, controlled by the [Visual Environment Volume override](visual-environment-volume-override-reference.md). This controls the skybox that you can see through the Camera and represents the visual side of the environment lighting. With the[ built-in render pipeline](https://docs.unity3d.com/Manual/SL-RenderPipeline.html), you customize visual environment lighting settings on a per-Scene basis. In contrast, HDRP's Visual Environment uses the [Volume](understand-volumes.md) framework to smoothly interpolate between different sets of environment lighting settings for your sky (and fog) within the same Scene.
* The lighting environment, controlled by the **Environment (HDRP)** section of the Lighting window. HDRP uses the lighting environment to calculate indirect ambient lighting for your Scene. It does not use the Volume framework as HDRP's indirect ambient lighting currently only supports one source of environment lighting.

Essentially, you use the visual environment to control how the sky looks in your Scene and use the lighting environment to control how the sky contributes to indirect ambient lighting.

For information about the [Lighting window](https://docs.unity3d.com/Manual/lighting-window.html) **Environment (HDRP)** properties, refer to 

## Visual Environment
The Visual Environment is a Volume override that tells HDRP what type of [sky](HDRP-Features.md#sky) you want to see through Cameras that the Volume affects. For information on how to customize Visual Environments, see the [Visual Environment](visual-environment-volume-override-reference.md) documentation.

Your Unity Project’s [HDRP Asset](HDRP-Asset.md) has the following properties that also affect all Visual Environments:

* **Reflection Size**: Controls the resolution of the sky cube map. Unity uses this cube map as a fallback Reflection Probe for areas that local Reflection Probes do not affect. It has no effect on the quality of the sky directly seen through the camera.
* **Lighting Override Mask**: A LayerMask that allows you to decouple the sky seen through the Camera from the one that affects the ambient lighting. For example, you might want a dark sky at night time, but to have brighter lighting so that you can still see clearly. See [Decoupling the visual environment from the lighting environment](#DecoupleVisualEnvironment).

### HDRP built-in sky types

HDRP has three built-in [sky types](HDRP-Features.md#sky):

* [HDRI Sky](create-an-hdri-sky.md)
* [Gradient Sky](create-a-gradient-sky.md)
* [Physically Based Sky](create-a-physically-based-sky.md)

HDRP also allows you to implement your own sky types that display a background and handle environment lighting. See the [Customizing HDRP](create-a-custom-sky.md) documentation for instructions on how to implement your own sky.

**Note**: The **Procedural Sky** is deprecated and no longer built into HDRP. For information on how to add Procedural Sky to your HDRP Project, see the [Upgrading from 2019.2 to 2019.3 guide](Upgrading-From-2019.2-to-2019.3.md#ProceduralSky).

<a name="DecoupleVisualEnvironment"></a>

## Decouple the visual environment from the lighting environment

You can use the sky **Lighting Override Mask** in your Unity Project’s HDRP Asset to separate the Visual Environment from the environment lighting. If you set the **Lighting Override Mask** to **Nothing**, or to a group of Layers that have no Volumes on them, then no Layer acts as an override. This means that environment lighting comes from all Volumes that affect a Camera. If you set the **Lighting Override Mask** to include Layers that have Volumes on them, HDRP only uses Volumes on these Layers to calculate environment lighting.

An example of where you would want to decouple the sky lighting from the visual sky, and use a different Volume Profile for each, is when you have an [HDRI Sky](create-an-hdri-sky.md) that includes sunlight. To make the sun visible at runtime in your application, your sky background must show an HDRI sky that features the sun. To achieve real-time lighting from the sun, you must use a Directional [Light](Light-Component.md) in your Scene and, for the baking process, use an HDRI sky that is identical to the first one but does not include the sun. If you were to use an HDRI sky that includes the sun to bake the lighting, the sun would contribute to the lighting twice (once from the Directional Light, and once from the baking process) and make the lighting look unrealistic.

## Ambient light probe

HDRP uses the ambient Light Probe as the final fallback for indirect diffuse lighting. For more information, refer to [Ambient light probe](ambient-light-probe.md).

## Ambient Reflection Probe

HDRP uses the ambient Reflection Probe as a fallback for indirect specular lighting. This means that it only affects areas that local Reflection Probes, Screen Space Reflection, and raytraced reflections do not affect.


## Reflection

Reflection Probes work like Cameras; they use the Volume system, and therefore use environment lighting from the sky, which you set in the Visual Environment of the Volume that affects them. For more information, refer to [Reflection](Reflection-in-HDRP.md).
