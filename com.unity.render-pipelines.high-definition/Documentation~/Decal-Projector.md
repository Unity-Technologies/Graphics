# Decal Projector

The High Definition Render Pipeline (HDRP) includes the Decal Projector component, which allows you to project specific Materials (decals) into the Scene. Decals are Materials that use the [HDRP/Decal Shader](Decal-Shader.html). When the Decal Projector component projects decals into the Scene, they interact with the Scene’s lighting and wrap around Meshes. You can use thousands of decals in your Scene simultaneously because HDRP instances them. This means that the rendering process is not resource intensive as long as the decals use the same Material.

![](Images/DecalProjector1.png)

To edit a Decal Projector’s properties, select the GameObject with the Decal Projector component, and use the Scene view gizmo or the Inspector.

## Using the Scene view gizmo

The Decal Projector gizmo is a box that describes the 3D size of the projector. The projector draws its decal on every Material inside the box. It includes handles on every face that you can click and drag to alter the size of the box. It also has an arrow that indicates the direction the projector faces. 

![](Images/DecalProjector2.png)

## Using the Inspector

Using the Inspector allows you to change all of the Decal Projector properties, and lets you use numerical values for **Size**, which allows for greater precision than the click-and-drag gizmo method.

## Properties

![](Images/DecalProjector3.png)

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Crop Decal With Gizmo** | Enable this checkbox to crop the decal with the Scene view gizmo, rather than alter the projector’s size. |
| **Size**                  | The 3D size of the projector influence box, and thus the decal. The projector scales the decal to match the **X** and **Z** components of the **Size**. The Decal Projector component projects decals along the local y-axis. |
| **Material**              | The decal Material to project. The decal Material must use a HDRP/Decal Shader. |
| **Draw Distance**         | The distance from the Camera to the Decal at which this projector stops projecting the decal and HDRP no longer renders the decal. |
| **Distance Fade Scale**   | The distance from the Camera at which the projector begins to fade out the decal. Scales from 0 to 1 and represents a percentage of the **Draw Distance**. A value of 0.9 begins fading the decal out at 90% of the **Draw Distance** and finished fading it out at the **Draw Distance**. |
| **UV Scale**              | Scales the decal Material along its UV axes.                 |
| **UV Bias**               | Offsets the decal Material along its UV axes. Use this with the **UV Scale** when using a Material atlas for your decal. |
| **Fade Factor**           | Allows you to manually fade the decal in and out. A value of 0 makes the decal fully transparent, and a value of 1 makes the decal as opaque as defined by the **Material**. The **Material** manages the maximum opacity of the decal using **Global Opacity** and an opacity map. |
| **Transparent Surfaces**  | Check this box to allow HDRP to draw the projector’s decal on top of transparent surfaces. HDRP packs all Textures from decals with **Affects Transparency** enabled into an atlas, which can affect memory and performance. You can edit the dimensions of this atlas in the **Decals** section of your Unity Project’s [HDRP Asset](HDRP-Asset.html#Decals). |