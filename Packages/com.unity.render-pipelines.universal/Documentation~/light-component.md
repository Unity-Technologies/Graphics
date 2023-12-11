# Light component reference

Lights determine the shading of an object and the shadows it casts.

This page contains information on Light components in the Universal Render Pipeline (URP). For a general introduction to lighting in Unity and examples of common lighting workflows, refer to the [Lighting section of the Unity Manual](https://docs.unity3d.com/Manual/LightingOverview.html).

## Properties

The Light Inspector includes the following groups of properties:

![](Images/Inspectors/light-inspector.png)

* [General](#General)
* [Shape](#Shape)
* [Emission](#Emission)
* [Rendering](#Rendering)
* [Shadows](#Shadows)

### <a name="General"></a>General

| Property:| Function: |
|:---|:---|
| **Type**| The current type of light. Possible values are **Directional**, **Point**, **Spot** and **Area**.|
| **Mode**| Specify the [Light Mode](https://docs.unity3d.com/Manual/LightModes.html) used to determine if and how a light is "baked". Possible modes are **Realtime**, **Mixed** and **Baked**.|

### <a name="Shape"></a>Shape

| Property:| Function: |
|:---|:---|
| **Spot Angle**| Define the angle (in degrees) at the base of a spot light’s cone (**Spot** light only). |

### <a name="Emission"></a>Emission

| Property:| Function: |
|:---|:---|
| **Color**| Use the color picker to set the color of the emitted light. |
| **Intensity**| Set the brightness of the light. The default value for a **Directional** light is 0.5. The default value for a **Point**, **Spot** or **Area** light is 1.  |
| **Indirect Multiplier**| Use this value to vary the intensity of indirect light. Indirect light is light that has bounced from one object to another. The **Indirect Multiplier** defines the brightness of bounced light calculated by the global illumination (GI) system. If you set **Indirect Multiplier** to a value lower than **1,** the bounced light becomes dimmer with every bounce. A value higher than **1** makes light brighter with each bounce. This is useful, for example, when a dark surface in shadow (such as the interior of a cave) needs to be brighter in order to make detail visible. |
| **Range**| Define how far the light emitted from the center of the object travels (**Point** and **Spot** lights only). |
| **Cookie** | The RGB texture this Light projects into the scene. Use cookies to create silhouettes or patterned illumination. The texture format to use depends on the type of Light:<br/> &#8226; Directional: 2D texture<br/> &#8226; Spot: 2D texture<br/> &#8226; Point: [cubemap texture](https://docs.unity3d.com/Manual/class-Cubemap.html)<br/><br/>**Note**: URP doesn't support light cookies for Area lights.<br/><br/>For more information about light cookies, refer to [Cookies](https://docs.unity3d.com/Manual/Cookies.html). |
| &nbsp;&nbsp;**Cookie Size** | The per-axis scale Unity applies to the cookie texture. Use this property to set the size of the cookie.<br/><br/>This property is available only if you set **Type** to **Directional** and assign a texture to **Cookie**. |
| &nbsp;&nbsp;**Cookie Offset** | The per-axis offset Unity applies to the cookie texture. Use this property to move the cookie without moving the light itself. You can also animate this property to scroll the cookie. <br/><br/>This property is available only if you set **Type** to **Directional** and assign a texture to **Cookie**. |

## <a name="Rendering"></a>Rendering

| Property:| Function: |
|:---|:---|
| **Render Mode**| Use this drop-down to set the rendering priority of the selected Light. This can affect lighting fidelity and performance (refer to the *Performance Considerations*, below). |
|&nbsp;&nbsp;&nbsp;&nbsp;Auto| The rendering method is determined at run time, depending on the brightness of nearby lights and the current [Quality](https://docs.unity3d.com/Manual/class-QualitySettings.html) settings. |
|&nbsp;&nbsp;&nbsp;&nbsp;Important| The light is always rendered at per-pixel quality. Use **Important** mode only for the most noticeable visual effects (for example, the headlights of a player’s car). |
|&nbsp;&nbsp;&nbsp;&nbsp;Not Important| The light is always rendered in a faster, vertex/object light mode.  |
| **Culling Mask**| Use this to selectively exclude groups of objects from being affected by the Light. For more information, refer to [Layers](https://docs.unity3d.com/Manual/Layers.html).|

## <a name="Shadows"></a>Shadows

| Property:| Function: |
|:---|:---|
| **Shadow Type**| Determine whether this Light casts Hard Shadows, Soft Shadows, or no shadows at all. For information on hard and soft shadows, refer to documentation on [Lights](https://docs.unity3d.com/Manual/class-Light.html). |
|&nbsp;&nbsp;&nbsp;&nbsp;Baked Shadow Angle| If **Type** is set to **Directional** and **Shadow Type** is set to **Soft Shadows**, this property adds some artificial softening to the edges of shadows and gives them a more natural look. |
|&nbsp;&nbsp;&nbsp;&nbsp;Baked Shadow Radius| If **Type** is set to **Point** or **Spot** and **Shadow Type** is set to **Soft Shadows**, this property adds some artificial softening to the edges of shadows and gives them a more natural look. |
|&nbsp;&nbsp;&nbsp;&nbsp;Realtime Shadows| These properties are available when **Shadow Type** is set to **Hard Shadows** or **Soft Shadows**. Use these properties to control real-time shadow rendering settings. |
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Strength| Use the slider to control how dark the shadows cast by this Light are, represented by a value between 0 and 1. This is set to 1 by default. |
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Bias| Controls whether to use shadow bias settings from the URP Asset, or whether to define custom shadow bias settings for this Light. Possible values are **Use Pipeline Settings** or **Custom**.|
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Depth| Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts. This property is visible only when **Bias** is set to **Custom**.|
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Normal| Controls the distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts. This property is visible only when **Bias** is set to **Custom**.|
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Near Plane| Use the slider to control the value for the near clip plane when rendering shadows, defined as a value between 0.1 and 10. This value is clamped to 0.1 units or 1% of the light’s **Range** property, whichever is lower. This is set to 0.2 by default. |
|&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Soft&nbsp;Shadows&nbsp;Quality | Select the soft shadows quality. With the **Use Pipeline Settings** option selected Unity uses the value from the URP Asset. Options **Low**, **Medium**, and **High** let you specify the soft shadow quality value for this Light. For more information on the values, refer to the [Soft Shadows](universalrp-asset.md#soft-shadows) section. |

## Preset

When using Preset of Light Component, only a subset of properties are supported. Unsupported properties are hidden.
