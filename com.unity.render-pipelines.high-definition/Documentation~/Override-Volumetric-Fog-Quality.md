# Volumetric Fog Quality

The High Definition Render Pipeline evaluates volumetric lighting on a 3D grid mapped to the volumetric section of the frustum. The resolution of the grid is quite low (it is 240x135x64 using the default quality setting at 1080p), so it's important to keep the dimensions of the frustum as small as possible to maintain high quality. Use [Distant Fog](Override-Volumetric-Fog.html#DistantFog) for the less visually important background areas.

## Using Volumetric Fog Quality

**Volumetric Fog Quality** uses the [Volume](Volumes.html) framework, so to enable and modify **Volumetric Fog Quality** properties, you must add an **Volumetric Fog Quality** override to a [Volume](Volumes.html) in your Scene. To add **Volumetric Fog Quality** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Lighting** and click on **Volumetric Fog Quality**. You can now use the **Volumetric Fog Quality** override to alter the quality of volumetric effects in HDRP.

Adjust the **Depth Extent** on the Volumetric Lighting Controller component to define the maximum range for the volumetric fog relative to the Camera’s frustum.

## Properties

![](Images/Override-VolumetricFogQuality1.png)

|**Property**| **Description**|
|:----------------------------- |:------------------------------------------------------------ |
| **Depth Extent** | Determines the distance (in Unity Units) from the Camera at which the volumetric fog section of the frustum ends. |
| **Slice Distribution Uniformity** | Controls the uniformity of the distribution of slices along the Camera's focal axis. A value of 0 makes the distribution exponential (the spacing between the slices increases with the distance from the Camera), and the value of 1 results in a uniform distribution. |

