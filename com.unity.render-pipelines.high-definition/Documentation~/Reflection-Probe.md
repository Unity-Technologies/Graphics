# Reflection Probe

The Reflection Probe component is one of the types of [Reflection Probe](Reflection-Probes-Intro.html) that the High Definition Render Pipeline (HDRP) provides to help you create reactive and accurate reflective Materials.

## Properties

The HDRP Reflection Probe uses the [built-in render pipeline Reflection Probe](https://docs.unity3d.com/Manual/class-ReflectionProbe.html) as a base, and thus shares many properties with the built-in version. HDRP Reflection Probes also share many properties with the [HDRP Planar Reflection Probe](Planar-Reflection-Probe.html).

![img](Images/ReflectionProbe1.png)

### General Properties

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Type**          | Use the drop-down to select the mode this Reflection Probe uses to capture a view of the Scene. Reflective Materials query this capture to process reflections for their surface.<br />&#8226; **Realtime**: Makes the Reflection Probe capture a view of the Scene in real time. Use the **Realtime Mode** property to set the time period.<br />&#8226; **Custom**: Allows you to assign a cubemap Texture to act as the Reflection Probe's captured view of the Scene. Use the **Texture** property to assign the cubemap.<br />&#8226; **Baked**: Makes the Reflection Probe use a static cubemap Texture at runtime. You must bake this Texture before you build your Unity Project. |
| **Realtime Mode** | Use the drop-down to select how often the Reflection Probe should capture a view of the Scene.<br />&#8226;**Every Frame**: Updates the Probe’s capture data every frame.<br />&#8226; **On Enable**: Updates the Probe’s capture data each time Unity calls the component’s `OnEnable()` function. This occurs whenever you enable the component in the Inspector or activate the GameObject that the component attaches to.<br /><br /> This property only appears when you select **Realtime** from the **Type** drop-down. |
| **Texture**       | Assign a Texture for the Reflection Probe to use as its captured view of the Scene.<br />This property only appears when you select **Custom** from the **Type** drop-down. |

### Projection Settings

The following properties control the projection settings for this Reflection Probe.

| **Property**                             | **Description**                                              |
| ---------------------------------------- | ------------------------------------------------------------ |
| **Proxy Volume**                         | The [Reflection Proxy Volume](Reflection-Proxy-Volume.html) this Probe uses to correct displacement issues between the Probe’s capture point (**Mirror Position**) and the position of the reflective Material using the Texture this Probe captures. Note: The **Proxy Volume** you assign must be the same **Shape** as the Influence Volume. |
| **Use Influence Volume As Proxy Volume** | Enable the checkbox to use the boundaries of the Influence Volume as the Proxy Volume.<br />This property only appears when you have not set a Reflection Proxy Volume to the **Proxy Volume** property. |

<a name="InfluenceVolume"></a>

### Influence Volume

The Influence Volume defines the area around the Probe in which reflective Materials use the results that the Probe captures to influence the reflective behavior of their surface. The Probe also uses the bounds of the Influence Volume to calculate **Field Of View** if you don’t provide an override value.

<a name="Workflows"></a>

There are two workflows you can use to edit your Reflection Probe’s Influence Volume: **Normal** mode and **Advanced** mode. The two buttons in the top right of the **Influence Volume** section allow you to select which mode to use.

- **Normal** mode allows you to set a single value for the **Blend Distance**. You can use **Normal** mode with **Box** and **Sphere** Influence Volumes. 
- **Advanced** mode exposes the **Face Fade** property. It also allows you to set **Face Fade**, **Blend Distance**, and **Blend Normal Distance**, on a per axis, per direction basis for an Influence Volume with a **Box Shape**. 

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Shape**                 | Defines the shape of the Influence Volume. The possible values are **Box** and **Sphere**. Selecting **Sphere** disables **Advanced** mode because you can only use **Advanced** mode for **Box** Influence Volumes. |
| **Box Size**              | Defines the scale of each axis of the box that represents the Influence Volume. Only available with a **Box Shape**. |
| **Radius**                | Defines the radius of the sphere that represents the Influence Volume. Only available with a **Sphere Shape**. |
| **Blend Distance**        | The inward distance from the **Box Size** or **Radius** at which this Reflection Probe blends with other Reflection Probes. In **Normal** mode, this property is a single value that modulates the distance at which this Reflection Probe blends with other Reflection Probes in every direction. This mode is available for **Box** or **Sphere** Influence Volumes.In **Advanced** mode, this property uses six values, one for each side of the box. Use each of the six input fields to define the blend distance in each direction. For example, **Y** defines the blending distance for the face at the top of the box and **-Y** defines the blending distance for the face on the bottom. This mode is only available for **Box** Influence Volumes. This feature is only available for [deferred](Forward-And-Deferred-Rendering.html) Reflection Probes. |
| **Blend Normal Distance** | The area around the Reflection Probe where normals pointing away from the capture position don’t receive any influence from this probe.<br />1. A pixel on a reflective surface outside of the **Blend Normal Influence** volume receives a blended influence from this Probe.<br />2. The pixel receives no influence from this Probe if it has a normal pointing away from the **Capture Position**. This is useful when you have a building with a Probe inside that has an Influence Volume larger than the building itself. Setting the **Blend Normal Distance** to be less than the buildings size means that the Probe does not affect the outside facing walls of the building.<br />This property is only available for deferred Reflection Probes. |
| **Face Fade**             | Defines a fade value for each direction on each axis of an Influence Volume with a **Box Shape**. Reflection Probes fade out the Reflection Probe’s effect on reflective Materials based on these values. Only available in **Advanced** mode. |

### Capture Settings

The following properties control the method that the Reflection Probe uses to capture its surroundings..

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Capture Position**       | The position, relative to the Transform Position, from which the Reflection Probe captures its surroundings. |
| **Clear Mode**             | Defines how to fill empty background areas of the RenderTexture this Probe captures.<br />&#8226; **Sky** uses the sky defined by the current [Volume](Volumes.html) settings to fill empty background areas.<br />&#8226; **Background** uses the **Background Color** property to fill empty background areas.<br />&#8226; **None** reuses the previous value for each pixel that doesn’t represent a reflected GameObject, instead of filling in empty areas of the RenderTexture. |
| **Background Color**       | The color to fill empty background areas of the RenderTexture if you set the **Clear Mode** to **Background**. |
| **Clear Depth**            | Choose whether the Reflection Probe clears the Depth Buffer or not. |
| **Volume Layer Mask**      | A LayerMask that defines which Volumes affect this Reflection Probe’s capture. |
| **Volume Anchor Override** | Set the Transform that the [Volume](Volumes.html) system uses to handle the position of this Reflection Probe. For example, if you want this Reflection Probe to match post-processing effects with the view Camera, set this property to the view Camera’s Transform. The Volume system then uses the Camera’s position to process which Volume affects this Reflection Probe. |
| **Use Occlusion Culling**  | Enables [Occlusion Culling](https://docs.unity3d.com/Manual/OcclusionCulling.html) for this Reflection Probe. |
| **Culling Mask**           | A LayerMask that defines which Layers to include in the reflection. GameObjects on the Layers included in this LayerMask appear in the reflection. |
| **Clip Planes - Near**     | The closest point relative to the Reflection Probe that the Probe captures reflections. |
| **Clip Planes - Far**      | The furthest point relative to the Reflection Probe that it  captures reflections. |
| **Probe Layer Mask**       | Acts as a culling mask for environment lights (light from Planar Reflection Probes and Reflection Probes). This Reflection Probe ignores all Reflection Probes that are on Layers not included in this Layer mask, so use this property to ignore certain Reflection Probes when rendering this one. |
| **Custom Frame Settings**  | Allows you to define custom [Frame Settings](Frame-Settings.html) for this Probe. Disable this property to use the **Default Frame Settings** in your Unity Project’s [HDRP Asset](HDRP-Asset.html). |

### Custom Settings

The following properties control extra behavior options for fine-tuning the behavior of your Reflection Probes.

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Light Layer** | A mask that allows you to choose which Light Layers this Reflection Probe affects. This Reflection Probe only affects Mesh Renderers with a matching **Rendering Layer Mask**.<br/>Navigate to your Project’s **HDRP Asset > Render Pipeline Supported Features** and enable **Light Layers** to use this property. |
| **Multiplier**  | A multiplier for the RenderTexture the Reflection Probe captures. The Reflection Probe applies this multiplier when Reflective Materials query the RenderTexture. |
| **Weight**      | The overall weight of this Reflection Probe’s contribution to the reflective effect of Materials. When Reflection Probe’s blend together, the weight of each Probe determines their contribution to a reflective Material in the blend area. |
| **Range Compression Factor**      | The result of the rendering of the probe will be divided by this factor. When the probe is read, this factor is undone as the probe data is read. This is especially useful to deal with very bright or dark objects in the reflections that will otherwise be saturated. |

## Gizmos

You can use Scene view gizmos to visually customize specific properties.

| **Gizmo**                                                    | **Property**                        | **Description**                                              |
| ------------------------------------------------------------ | ----------------------------------- | ------------------------------------------------------------ |
| ![](Images/ReflectionProbeGizmo1.png) | **Influence Volume boundary**.      | Provides Scene view handles that allow you to resize the boundaries of the [Influence Volume](#InfluenceVolume), which defines the area this Reflection Probe affects reflective Materials. Edits the **Box Size** or **Radius** value, depending on the **Shape** you select. |
| ![](Images/ReflectionProbeGizmo2.png) | **Blend Distance boundary**.        | Provides Scene view handles that allows you to alter the inward distance from the **Box Size** or **Radius** at which this Reflection Probe blends with other Reflection Probes. Its behavior depends on the [workflow mode](#Workflows) you are using. It scales all sides equally in **Normal** mode, scales just the side with the handle you control in **Advanced** mode. |
| ![](Images/ReflectionProbeGizmo3.png) | **Blend Normal Distance boundary**. | Provides Scene view handles that allow you to resize the boundary where pixels with a normal pointing away from the **Capture Position** don’t receive any influence from this Probe. |
| ![](Images/ReflectionProbeGizmo4.png) | **Capture Position**.               | Changes the behavior of the Move Tool so that it alters the **Capture Position** property, rather than the **Position** of the **Transform**. |