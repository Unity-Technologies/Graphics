# Planar Reflection Probe

The Planar Reflection Probe component is one of the types of [Reflection Probe](Reflection-Probes-Intro.md) that the High Definition Render Pipeline (HDRP) provides to help you create reactive and accurate reflective Materials.

## Properties

Planar Reflection Probes share many properties with the the [built-in render pipeline Reflection Probe](https://docs.unity3d.com/Manual/class-ReflectionProbe.html), and the [HDRP cubemap Reflection Probe](Reflection-Probe.md).

Planar Reflection Probes use the same texture format than the one selected in [HDRP Asset](HDRP-Asset.md) for Color Buffer Format.

### General Properties

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Realtime Mode** | A Planar Reflection Probe updates in real time. Use this property to tell HDRP how often to update the Probe.<br />&#8226; **Every Frame**: Updates the Probe’s capture data every frame.<br />&#8226; **On Enable**: Updates the Probe’s capture data each time Unity calls the component’s `OnEnable()` function. This occurs whenever you enable the component in the Inspector or activate the GameObject that the component attaches to.<br/>&#8226; **On Demand**: Updates the Probe's capture data when you request it. To do this, access the Probe's `HDAdditionalReflectionData` and call the `RequestRenderNextUpdate()` function. |

### Projection Settings

The following properties control the projection settings for this Planar Reflection Probe.

| **Property**                             | **Description**                                              |
| ---------------------------------------- | ------------------------------------------------------------ |
| **Proxy Volume**                         | The [Reflection Proxy Volume](Reflection-Proxy-Volume.md) this Probe uses to correct displacement issues between the Probe’s capture point (**Mirror Position**) and the position of the reflective Material using the RenderTexture this Probe captures. Note: The **Proxy Volume** you assign must be the same **Shape** as the Influence Volume. |
| **Use Influence Volume As Proxy Volume** | Tick this checkbox to use the boundaries of the Influence Volume as the Proxy Volume.<br />This property only appears when you have not set a Reflection Proxy Volume to the **Proxy Volume** property. |

<a name="InfluenceVolume"></a>

### Influence Volume

The Influence Volume defines the area around the Probe in which reflective Materials use the results that the Probe captures to influence the reflective behavior of their surface. The Planar Reflection Probe also uses the bounds of the Influence Volume to calculate **Field Of View** if you don’t provide an override value.

For reflective objects not aligned with the planar probe direction, the reflection will smoothly fade out as the reflected rays leave the reflected camera field of view.

| **Property**       | **Description**                                              |
| ------------------ | ------------------------------------------------------------ |
| **Shape**          | Defines the shape of the Influence Volume. The possible values are **Box** and **Sphere**. The availability of properties below depends on the selected shape. |
| **Box Size**       | Defines the scale of each axis of the box that represents the Influence Volume. Only available with a **Box Shape**. |
| **Radius**         | Defines the radius of the sphere that represents the Influence Volume. Only available with a **Sphere Shape**. |
| **Per Axis Control** | Enable the checkbox to control the **Blend Distance** per axis. Only available with a **Box Shape**. |
| **Blend Distance** | The inward distance from the **Box Size** or **Radius** at which this Planar Reflection Probe blends with other Reflection Probes. In **Normal** mode, this property is a single value that modulates the distance at which this Reflection Probe blends with other Reflection Probes in every direction. This mode is available for **Box** or **Sphere** Influence Volumes. For the **Box** shape, when **Per Axis Control** is enabled, this property uses six values, one for each side of the box. Use each of the six input fields to define the blend distance in each direction. For example, **Y** defines the blending distance for the face at the top of the box and **-Y** defines the blending distance for the face on the bottom. This mode is only available for **Box** Influence Volumes.This feature is only available for [deferred](Forward-And-Deferred-Rendering.md) Reflection Probes. |

### Capture Settings

The following properties control the method that the Planar Reflection Probe uses to capture the directional view of its surroundings.

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Field Of View Mode**     | Defines the mode to use when computing the field of view. |
| **Clear Mode**             | Defines how to fill empty background areas of the RenderTexture this Probe captures.<br />&#8226; **Sky** uses the sky defined by the current [Volume](Volumes.md) settings to fill empty background areas.<br />&#8226; **Color** uses the **Background Color** setting to fill empty background areas.<br />&#8226; **None** reuses the previous value for each pixel that doesn’t represent a reflected GameObject, instead of filling in empty areas of the RenderTexture. |
| **Background Color**       | The color to fill empty background areas of the RenderTexture if you set the **Clear Mode** to **Background**. |
| **Clear Depth**            | Choose whether the Planar Reflection Probe clears the Depth Buffer or not. |
| **Volume Layer Mask**      | A LayerMask that defines which Volumes affect this Planar Reflection Probe’s capture. |
| **Volume Anchor Override** | Set the Transform that the [Volume](Volumes.md) system uses to handle the position of this Planar Reflection Probe. For example, if you want this Planar Reflection Probe to match post-processing effects with the view Camera, set this property to the view Camera’s Transform. The Volume system then uses the Camera’s position to process which Volume affects this Planar Reflection Probe. |
| **Use Occlusion Culling**  | Enables [Occlusion Culling](<https://docs.unity3d.com/Manual/OcclusionCulling.html>) for this Planar Reflection Probe. |
| **Culling Mask**           | A LayerMask that defines which Layers to include in the reflection. GameObjects on the Layers included in this LayerMask appear in the reflection. |
| **Clipping Planes - Near** | The closest point relative to the Planar Reflection Probe that the Probe captures reflections. |
| **Clipping Planes - Far**  | The furthest point relative to the Planar Reflection Probe that it  captures reflections. |
| **Probe Layer Mask**       | Acts as a culling mask for environment lights (light from other Planar Reflection Probes and Reflection Probes). This Planar Reflection Probe ignores all Reflection Probes that are on Layers not included in this Layer mask, so use this property to ignore certain Reflection Probes when rendering this one. |
| **Custom Frame Settings**  | Allows you to define custom [Frame Settings](Frame-Settings.md) for this Probe. Disable this property to use the **Default Frame Settings** in your Unity Project’s [HDRP Asset](HDRP-Asset.md). |
| **Resolution**             | Set the resolution of this Planar Reflection Probe. Use the drop-down to select which quality mode to derive the resolution from. If you select Custom, set the resolution, measured in pixels, in the input field. A higher resolution increases the fidelity of planar reflection at the cost of GPU performance and memory usage, so if you experience any performance issues, try using a lower value. |
| **Rough Reflections**      | Disable the checkbox to tell HDRP to use this Planar Reflection Probe as a mirror. If you do this, the receiving surface must be perfectly smooth or the reflection result is not accurate. If you want perfect reflection, disabling this option can be useful because it means HDRP does not need to process rough refraction and thus decreases the resource intensity of the effect.|
| **Mirror Position**        | Offsets the position of the mirror from the Transform Position.<br/>This property only appears when you enable [more options](More-Options.md) for this section. |
| **Range Compression Factor**  | The factor which HDRP divides the result of the probe's rendering by. This is useful to deal with very bright or dark objects in the reflections that would otherwise be saturated.<br/>This property only appears when you enable [more options](More-Options.md) for this section. |

### Render Settings

The following properties control extra behavior options for fine-tuning the behavior of your Planar Reflection Probes.

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Light Layer**   | A mask that allows you to choose which Light Layers this Reflection Probe affects. This Reflection Probe only affects Mesh Renderers or Terrain with a matching **Rendering Layer Mask**.<br/>Navigate to your Project’s **HDRP Asset > Render Pipeline Supported Features** and enable **Light Layers** to use this property. |
| **Multiplier**    | A multiplier that HDRP applies to the RenderTexture captured by the Planar Reflection Probe. Higher multiplier values make the queried RenderTexture brighter, and lower multiplier values make the queried RenderTexture darker. |
| **Weight**        | The overall weight of this Reflection Probe’s contribution to the reflective effect of Materials. When Reflection Probe’s blend together, the weight of each Probe determines their contribution to a reflective Material in the blend area. |
| **Fade Distance** | The distance, in meters, from the camera at which reflections begin to smoothly fade out before they disappear completely. |

## Gizmos

You can use Scene view gizmos to visually customize specific properties.

| **Gizmo**                             | **Property**                          | **Description**                                              |
| ------------------------------------- | ------------------------------------- | ------------------------------------------------------------ |
| ![](Images/ReflectionProbeGizmo1.png) | **Influence Volume boundary**         | Provides Scene view handles that allow you to move the boundaries of the [Influence Volume](#InfluenceVolume), which defines the area this Reflection Probe affects reflective Materials. Edits the **Box Size** or **Radius** value, depending on the **Shape** you select. |
| ![](Images/ReflectionProbeGizmo2.png) | **Blend Distance boundary**           | Provides Scene view handles that allows you to alter the inward distance from the **Box Size** or **Radius** at which this Planar Reflection Probe blends with other Reflection Probes. For the **Box** shape, when **Per Axis Control** is enabled, there is a separate handle for each size of the box. |
| ![](Images/ReflectionProbeGizmo3.png) | **Blend Normal Distance boundary**    | Provides Scene view handles that allow you to resize the boundary where pixels with a normal pointing away from the **Capture Position** don’t receive any influence from this Probe. |
| ![](Images/ReflectionProbeGizmo4.png) | **Mirror Position**                   | Changes the behavior of the Move Tool so that it alters the **Mirror** **Position** property, rather than the **Position** of the **Transform**. |
| ![](Images/ReflectionProbeGizmo5.png) | **Mirror Rotation**                   | Changes the behavior of the Rotate Tool so that it alters the **Mirror Rotation** property, rather than the **Rotation** of the **Transform**. |
| ![](Images/ReflectionProbeGizmo6.png) | **Chrome Gizmo**.                     | Displays a chrome quad to preview the probe's texture in the scene. |

## Best practices

If you use a Planar Reflection Probe as a mirror (i.e its influence volume overlap a GameObject with a Material that has its smoothness and metallic properties set to 1) it is best practice to disable the **Rough Refraction** property to decrease the resource intensity.
If a receiving surface isn't a perfect mirror and the **Rough Reflection** option is disabled, the surface still renders smooth, but the result is physically incorrect.

