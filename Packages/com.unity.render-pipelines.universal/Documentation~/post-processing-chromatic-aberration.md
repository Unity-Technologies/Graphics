# Chromatic Aberration

![Chromatic Aberration Off](Images/post-proc/chromatic-aberration-off.png)
<br/>_Scene with Chromatic Aberration effect turned off._

![Chromatic Aberration On](Images/post-proc/chromatic-aberration.png)
<br/>_Scene with Chromatic Aberration effect turned on._

Chromatic Aberration creates fringes of color along boundaries that separate dark and light parts of the image. It mimics the color distortion that a real-world camera produces when its lens fails to join all colors to the same point. See Wikipedia: Chromation aberration.

## Using Chromatic Aberration

**Chromatic Aberration** uses the [Volume](Volumes.md) system, so to enable and modify **Chromatic Aberration** properties, you must add a **Chromatic Aberration** override to a [Volume](Volumes.md) in your Scene.

To add **Chromatic Aberration** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override** &gt; **Post-processing** and click on **Chromatic Aberration**. Universal Render Pipeline applies **Chromatic Aberration** to any Camera this Volume affects.

## Properties

![](Images/Inspectors/ChromaticAberration.png)

| **Property**  | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| **Intensity** | Set the strength of the Chromatic Aberration effect. Values range between 0 and 1. The higher the value, the more intense the effect is. The default value is 0, which disables the effect. |
