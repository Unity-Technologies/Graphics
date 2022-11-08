# Screen Space Refraction override

Screen space refraction uses the color buffer or Reflection Probes to produce a refraction effect.

HDRP uses screen space refraction by default if you set a Material's [Surface Type](Surface-Type.md) to **Transparent**. For information about how screen space refraction works in HDRP, or to turn refraction off, see [Refraction in HDRP](Refraction-in-HDRP.md).

The **Screen Space Refraction** override controls **Screen Edge Fade Distance**, which sets how quickly screen space refractions fades from sampling colors from the color buffer to sampling colors from the next level of the [reflection and refraction hierarchy](reflection-refraction-hierarchy.md).

Increase **Screen Edge Fade Distance** to reduce visible seams on an object between refracted colors from the screen, and refracted colors from probes or the Skybox.

## Using Screen Edge Fade Distance

To use this setting, you must enable it on a [Volume](Volumes.md), as follows:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component.
2. In the Inspector for this object, select **Add Override** > **Lighting** > **Screen Space Refraction**.

![](Images/screen-weight-distance.png)<br/>
In the refractive cube on the left of the screen, **Screen Edge Fade Distance** affects the edges of the screen where HDRP fades from using the color buffer to using Reflection Probes.

## Properties

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Screen Edge Fade Distance** |   Adjust this value to set how quickly HDRP fades from sampling colors from the color buffer to sampling colors from the next level of the [reflection and refraction hierarchy](reflection-refraction-hierarchy.md). Use **Screen Edge Fade Distance** to reduce visible seams between refracted colors from the screen, and refracted colors from probes or the Skybox. |

You can also use the [Volume Scripting API](Volumes-API.md) to change **Screen Edge Fade Distance**.

## Additional resources

- [Refraction in HDRP](Refraction-in-HDRP.md)
