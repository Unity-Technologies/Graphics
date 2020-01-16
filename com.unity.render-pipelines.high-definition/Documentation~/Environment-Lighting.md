# Environment lighting

Environment lighting allows you to simulate lighting coming from the surroundings of your Scene. It is common to use environment lighting to simulate sky lighting, but you can also use it to simulate a colored ambient light, or a lighting studio.
In the High Definition Render Pipeline (HDRP), there are two parts to environment lighting:

* The visual environment, controlled by the [Visual Environment Volume override](Override-Visual-Environment.html). This controls the skybox that you can see through the Camera. With the[ built-in render pipeline](https://docs.unity3d.com/Manual/SL-RenderPipeline.html), you customize visual environment lighting settings on a per-Scene basis. In contrast, HDRP's Visual Environment uses the Volume framework to smoothly interpolate between different sets of environment lighting settings for your sky (and fog) within the same Scene.

* The Lighting Environment, controlled by the **Environment (HDRP)** section of the Lighting window. This controls the sky that affects Global Illumination. It does not use the Volume framework as HDRP's Global Illumination currently only supports one source of environment lighting.

## Visual Environment
The Visual Environment is a Volume override that tells HDRP what type of [sky](HDRP-Features.html#SkyOverview) and [fog](HDRP-Features.html#FogOverview) you want to see through Cameras that the Volume affects. For information on how to customize Visual Environments, see the [Visual Environment](Override-Visual-Environment.html) documentation.

Your Unity Project’s [HDRP Asset](HDRP-Asset.html) has the following properties that also affect all Visual Environments:

* **Reflection Size**: Controls the resolution of the sky cube map. Unity uses this cube map as a fallback Reflection Probe for areas that local Reflection Probes do not affect. It has no effect on the quality of the sky directly seen through the camera.
* **Lighting Override Mask**: A LayerMask that allows you to decouple the sky seen through the Camera from the one that affects the ambient lighting. For example, you might want a dark sky at night time, but to have brighter lighting so that you can still see clearly. See [Decoupling the visual environment from the lighting environment](#DecoupleVisualEnvironment).

<a name="LightingEnvironment"></a>

## Ambient Light Probe

HDRP uses the ambient Light Probe as the final fallback for indirect diffuse lighting. It affects:

* All Mesh Renderers if there is no Global Illumination computed for the Scene (this applies when Unity has not computed any lightmaps or Light Probes for the Scene)
* Mesh Renderers that have their **Light Probe Mode** set to **Off**

* Volumetric fog if the Global Light Probe dimmer is set to a value above 0

The ambient Light Probe can be static (generated only once) or dynamic (updated at runtime).**Note**: If there is a **Light Probe group** in your Scene and you have computed **Global Illumination**, then the Ambient Light Probe only affects Mesh Renderers that have their **Light Probe Mode** set to **Off**, and that have **Volumetric fog** (if it’s enabled in the Scene).

## Ambient Reflection Probe

HDRP uses the ambient Reflection Probe as a fallback for indirect specular lighting. This means that it only affects areas that local Reflection Probes, Screen Space Reflection, and raytraced reflections do not affect.

<a name="LightingEnvironment"></a>

## Lighting environment

The **Environment (HDRP)** is a section in the Lighting window that allows you to specify which sky to use for Global Illumination. To open the window, select **Window > Lighting Settings**.

![](Images/EnvironmentLighting1.png)

The **Environment (HDRP)** section is at the top and has two settings that you can edit:

| **Setting**             | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Profile**             | A [Volume Profile](Volume-Profile.html) for the sky. This Volume Profile must include at least one Sky Volume override. |
| **Static Lighting Sky** | The sky to use for the Global Illumination simulation. The drop-down only contains sky types that the **Profile** includes. For example, if the **Profile** includes a **Gradient Sky** Volume override, you can select **Gradient Sky** from this drop-down.<br/>You can only edit this setting if you assign a Volume Profile to the **Profile** field. |

You can assign the same Volume Profile to both the **Static Lighting Sky** field and a Volume in your Scene. If you do this, and use the same sky settings for the baked lighting and the visual background in the Volume, the baked lighting accurately matches the background at runtime. If you want to control the light baking for the environment lighting separately to the visual background in your Scene, you can assign a different Volume Profile for each process.

**Note**: Changes to the baking environment only affect baked lightmaps and Light Probes during the baking process.

<a name="DecoupleVisualEnvironment"></a>

## Decoupling the visual environment from the lighting environment

You can use the sky **Lighting Override Mask** in your Unity Project’s HDRP Asset to separate the Visual Environment from the environment lighting. If you set the **Lighting Override Mask** to **Nothing**, or to a group of Layers that have no Volumes on them, then no Layer acts as an override. This means that environment lighting comes from all Volumes that affect a Camera. If you set the **Lighting Override Mask** to include Layers that have Volumes on them, HDRP only uses Volumes on these Layers to calculate environment lighting.

An example of where you would want to decouple the sky lighting from the visual sky, and use a different Volume Profile for each, is when you have an [HDRI Sky](Override-HDRI-Sky.html) that includes sunlight. To make the sun visible at runtime in your application, your sky background must show an HDRI sky that features the sun. To achieve real-time lighting from the sun, you must use a Directional [Light](Light-Component.html) in your Scene and, for the baking process, use an HDRI sky that is identical to the first one but does not include the sun. If you were to use an HDRI sky that includes the sun to bake the lighting, the sun would contribute to the lighting twice (once from the Directional Light, and once from the baking process) and make the lighting look unrealistic.

## HDRP built-in sky types

HDRP has three built-in [sky types](HDRP-Features.html#SkyOverview):

* [HDRI Sky](Override-HDRI-Sky.html)
* [Gradient Sky](Override-Gradient-Sky.html)
* [Physically Based Sky](Override-Physically-Based-Sky.html)

HDRP also allows you to implement your own sky types that display a background and handle environment lighting. See the [Customizing HDRP](Creating-a-Custom-Sky.html) documentation for instructions on how to implement your own sky.

**Note**: The **Procedural Sky** is deprecated and no longer built into HDRP. For information on how to add Procedural Sky to your HDRP Project, see the [Upgrading from 2019.2 to 2019.3 guide](Upgrading-From-2019.2-to-2019.3.html#ProceduralSky).

## Reflection

Reflection Probes work like Cameras; they use the Volume system, and therefore use environment lighting from the sky, which you set in the Visual Environment of the Volume that affects them.