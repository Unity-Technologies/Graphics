# Planar Reflection Probe

Planar Reflection Probes are one of the [Reflection Probes](Reflection-Probes-Intro.html) that the High Definition Render Pipeline (HDRP) provides to help you create reactive and accurate reflective Materials.

Properties

Planar Reflection Probes share many properties with the the [built-in render pipeline Reflection Probe](<https://docs.unity3d.com/Manual/class-ReflectionProbe.html>), and the [HDRP cubemap Reflection Probe](Reflection-Probe.html).

![](Images/PlanarReflectionProbe1.png)

## General Properties

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Realtime Mode** | A Planar Reflection Probe updates in real time. Use this property to tell HDRP how often to update the Probe.<br />**Every Frame** updates the Probe’s capture data every frame.<br />**On Enable** updates the Probe’s capture data each time Unity calls the component’s `OnEnable()` function. This occurs whenever you enable the component in the Inspector or activate the GameObject that the component attaches to. |

## Projection Settings

The following properties control the projection settings for this Planar Reflection Probe.

| **Property**                             | **Description**                                              |
| ---------------------------------------- | ------------------------------------------------------------ |
| **Proxy Volume**                         | The [Reflection Proxy Volume](Reflection-Proxy-Volume.html) this Probe uses to correct displacement issues between the Probe’s capture point (**Mirror Position**) and the position of the reflective Material using the RenderTexture this Probe captures. Note: The **Proxy Volume** you assign must be the same **Shape** as the Influence Volume. |
| **Use Influence Volume As Proxy Volume** | Tick this checkbox to use the boundaries of the Influence Volume as the Proxy Volume. Do not assign the **Proxy Volume** property to expose this property. |

<a name=”InfluenceVolume”></a>

## Influence Volume

The Influence Volume defines the area around the Probe in which reflective Materials use the results that the Probe captures to influence the reflective behavior of their surface. The Planar Reflection Probe also uses the bounds of the Influence Volume to calculate **Field Of View** if you don’t provide an override value.

<a name=”Workflows”></a>

There are two workflows you can use to edit your Planar Reflection Probe’s Influence Volume: **Normal** mode and **Advanced** mode. The two buttons in the top right of the **Influence Volume** section allow you to select which mode to use.

- **Normal** mode allows you to set a single value for the **Blend Distance**. You can use **Normal** mode with **Box** and **Sphere** Influence Volumes. 
- **Advanced** mode allows you to define the **Blend Distance** on a per axis, per direction basis for an Influence Volume with a **Box Shape**. 

| **Property**       | **Description**                                              |
| ------------------ | ------------------------------------------------------------ |
| **Shape**          | Defines the shape of the Influence Volume. The possible values are **Box** and **Sphere**. Selecting **Sphere** disables **Advanced** mode because you can only use **Advanced** mode for **Box** Influence Volumes. |
| **Box Size**       | Defines the scale of each axis of the box that represents the Influence Volume. Only available with a **Box Shape**. |
| **Radius**         | Defines the radius of the sphere that represents the Influence Volume. Only available with a **Sphere Shape**. |
| **Blend Distance** | The inward distance from the **Box Size** or **Radius** at which this Planar Reflection Probe blends with other Reflection Probes. In **Normal** mode, this property is a single value that modulates the distance at which this Reflection Probe blends with other Reflection Probes in every direction. This mode is available for **Box** or **Sphere** Influence Volumes.In **Advanced** mode, this property uses six values, one for each side of the box. Use each of the six input fields to define the blend distance in each direction. For example, **Y** defines the blending distance for the face at the top of the box and **-Y** defines the blending distance for the face on the bottom. This mode is only available for **Box** Influence Volumes.This feature is only available for [deferred](Forward-And-Deferred-Rendering.html) Reflection Probes. |

## Capture Settings

The following properties control the method that the Planar Reflection Probe uses to capture the directional view of its surroundings.

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Mirror Position**        | Offsets the position of the mirror from the Transform Position. |
| **Mirror Rotation**        | Offsets the rotation of the mirror from the Transform Rotation. |
| **Clear Mode**             | Defines how to fill empty background areas of the RenderTexture this Probe captures.<br />**Sky** uses the sky defined by the current [Volume](Volumes.html) settings to fill empty background areas.<br />**Background** uses the **Background Color** setting to fill empty background areas.<br />**None** reuses the previous value for each pixel that doesn’t represent a reflected GameObject, instead of filling in empty areas of the RenderTexture. |
| **Background Color**       | The color to fill empty background areas of the RenderTexture if you set the **Clear Mode** to **Background**. |
| **Clear Depth**            | Choose whether the Planar Reflection Probe clears the Depth Buffer or not. |
| **Volume Layer Mask**      | A LayerMask that defines which Volumes affect this Planar Reflection Probe’s capture. |
| **Volume Anchor Override** | Set the Transform that the [Volume](Volumes.html) system uses to handle the position of this Planar Reflection Probe. For example, if you want this Planar Reflection Probe to match post-processing effects with the view Camera, set this property to the view Camera’s Transform. The Volume system then uses the Camera’s position to process which Volume affects this Planar Reflection Probe. |
| **Use Occlusion Culling**  | Enables [Occlusion Culling](<https://docs.unity3d.com/Manual/OcclusionCulling.html>) for this Planar Reflection Probe. |
| **Culling Mask**           | A LayerMask that defines which Layers to include in the reflection. GameObjects on the Layers included in this LayerMask appear in the reflection. |
| **Field Of View**          | The field of view of the capture Camera. Planar Reflection Probes normally calculate the capture Camera’s field of view (FoV) using the **Mirror Position** and the bounds of the **Influence Volume**. Enable this property to override the capture Camera’s FoV with the value you set here. |
| **Clip Planes - Near**     | The closest point relative to the Planar Reflection Probe that the Probe captures reflections. |
| **Clip Planes - Far**      | The furthest point relative to the Planar Reflection Probe that it  captures reflections. |
| **Probe Layer Mask**       | Acts as a culling mask for environment lights (light from other Planar Reflection Probes and Reflection Probes). This Planar Reflection Probe ignores all Reflection Probes that are on Layers not included in this Layer mask, so use this property to ignore certain Reflection Probes when rendering this one. |
| **Custom Frame Settings**  | Allows you to define custom [Frame Settings](Frame-Settings.html) for this Probe. Disable this property to use the **Default Frame Settings** in your Unity Project’s [HDRP Asset](HDRP-Asset.html). |

## Custom Settings

The following properties control extra behavior options for fine-tuning the behavior of your Planar Reflection Probes.

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Light Layer** | Use the drop-down to define a LayerMask of Light Layers. This Planar Reflection Probe only uses Lights on Light Layers in this LayerMask to capture its view of the Scene. Click on your Project’s **HDRP Asset** and enable **Light Layers** in the **Render Pipeline Supported Features** section to use this property. |
| **Multiplier**  | A multiplier that HDRP applies to the RenderTexture captured by the Planar Reflection Probe. Higher multiplier values make the queried RenderTexture brighter, and lower multiplier values make the queried RenderTexture darker. |
| **Weight**      | The overall weight of this Reflection Probe’s contribution to the reflective effect of Materials. When Reflection Probe’s blend together, the weight of each Probe determines their contribution to a reflective Material in the blend area. |

## Gizmos

You can use Scene view gizmos to visually customize specific properties.

| **Gizmo**                             | **Property**                          | **Description**                                              |
| ------------------------------------- | ------------------------------------- | ------------------------------------------------------------ |
| ![](Images/ReflectionProbeGizmo1.png) | **Influence Volume bounds boundary.** | Provides Scene view handles that allow you to move the boundaries of the [Influence Volume](#InfluenceVolume), which defines the area this Reflection Probe affects reflective Materials. Edits the **Box Size** or **Radius** value, depending on the **Shape** you select. |
| ![](Images/ReflectionProbeGizmo2.png) | **Blend Distance boundary**           | Provides Scene view handles that allows you to alter the inward distance from the **Box Size** or **Radius** at which this Planar Reflection Probe blends with other Reflection Probes. Its behavior depends on the [workflow mode](#Workflows) you are using. It scales all sides equally in **Normal** mode, scales just the side with the handle you control in **Advanced** mode. |
| ![](Images/ReflectionProbeGizmo4.png) | **Mirror Position**                   | Changes the behavior of the Move Tool so that it alters the **Mirror** **Position** property, rather than the **Position** of the **Transform**. |
| ![](Images/ReflectionProbeGizmo5.png) | **Mirror Rotation**                   | Changes the behavior of the Rotate Tool so that it alters the **Mirror Rotation** property, rather than the **Rotation** of the **Transform**. |