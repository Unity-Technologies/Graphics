# Panini Projection

![Panini Projection off](Images/post-proc/panini-off.png)
<br/>_Scene with Panini Projection effect turned off._

![Panini Projection on](Images/post-proc/panini.png)
<br/>_Scene with Panini Projection effect turned on._

This effect helps you to render perspective views in Scenes with a very large field of view. Panini projection is a cylindrical projection, which means that it keeps vertical straight lines straight and vertical. Unlike other cylindrical projections, panini projection keeps radial lines through the center of the image straight too.

For more information about panini projection, refer to PanoTools’ wiki documentation on [General Panini Projection](https://wiki.panotools.org/The_General_Panini_Projection).

## Using Panini Projection

**Panini Projection** uses the [Volume](Volumes.md) framework, so to enable and modify **Panini Projection** properties, you must add a **Panini Projection** override to a [Volume](Volumes.md) in your Scene. To add **Panini Projection** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Panini Projection**. URP now applies **Panini Projection** to any Camera this Volume affects.

## Properties

![](Images/Inspectors/PaniniProjection.png)

| **Property**    | **Description**                                              |
| --------------- | ------------------------------------------------------------ |
| **Distance**    | Use the slider to set the strength of the distortion.        |
| **Crop to Fit** | Use the slider to crop the distortion to fit the screen. A value of 1 crops the distortion to the edge of the screen, but results in a loss of precision in the center if you set **Distance** to a high value. |
