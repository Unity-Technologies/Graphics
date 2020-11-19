# Decal Projector

The High Definition Render Pipeline (HDRP) includes the Decal Projector component, which allows you to project specific Materials (decals) into the Scene. Decals are Materials that use the [Decal Shader](Decal-Shader.md) or [Decal master node](Master-Node-Decal.md). When the Decal Projector component projects decals into the Scene, they interact with the Scene’s lighting and wrap around Meshes. You can use thousands of decals in your Scene simultaneously because HDRP instances them. This means that the rendering process is not resource intensive as long as the decals use the same Material.

The Decal Projector also supports [Decal Layers](Decal.md) which means you can control which Materials receive decals on a Layer by Layer basis.

![](Images/DecalProjector1.png)

To edit a Decal Projector’s properties, select the GameObject with the Decal Projector component and use the Inspector. If you just want to change the size of the projection, you can either use the Inspector or one of the Decal Projector's Scene view gizmos.

## Using the Scene view

The Decal Projector includes a Scene view representation of its bounds and projection direction to help you position the projector. The Scene view representation includes:

* A box that describes the 3D size of the projector; the projector draws its decal on every Material inside the box.

* An arrow that indicates the direction the projector faces.

![](Images/DecalProjector2.png)

The decal Projector also includes two gizmos that add handles on every face for you to click and drag to alter the size of the projector's bounds.

|**Button**|**Gizmo**|**Description**|
|-----|-----|-----|
|![](Images/DecalProjector3.png)|**Scale**|Scales the decal with the projector box. This changes the UVs of the Material to match the size of the projector box. This stretches the decal.|
|![](Images/DecalProjector4.png)|**Crop**|Crops the decal with the projector box. This changes the size of the projector box but not the UVs of the Material. This crops the decal.|

## Using the Inspector

Using the Inspector allows you to change all of the Decal Projector properties, and lets you use numerical values for **Size**, **Tiling**, and **Offset**, which allows for greater precision than the click-and-drag gizmo method.

## Properties

![](Images/DecalProjector5.png)

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Size**                | The size of the projector influence box, and thus the decal along the projected plane. The projector scales the decal to match the **Width** (along the local x-axis) and **Height** (along the local y-axis) components of the **Size**. |
| **Projection Depth**    | The depth of the projector influence box. The projector scales the decal to match **Projection Depth**. The Decal Projector component projects decals along the local z-axis. |
| **Material**            | The decal Material to project. The decal Material must use a HDRP/Decal Shader. |
| **Decal Layer**         | The layer that specifies the Materials to project the decal onto. Any Mesh Renderers or Terrain that uses a matching Decal Layer receives the decal. |
| **Draw Distance**       | The distance from the Camera to the Decal at which this projector stops projecting the decal and HDRP no longer renders the decal. |
| **Start Fade**          | Use the slider to set the distance from the Camera at which the projector begins to fade out the decal. Scales from 0 to 1 and represents a percentage of the **Draw Distance**. A value of 0.9 begins fading the decal out at 90% of the **Draw Distance** and finished fading it out at the **Draw Distance**. |
| **Angle Fade**          | Use the min-max slider to control the fade out range of the decal based on the angle between the Decal backward direction and the vertex normal of the receiving surface. Only available if [Decal Layers](Decal.md) feature is enabled. |
| **End Angle Fade**      | Use the slider to set the angle from the Decal backward direction at which the projector stop to fade out the decal. Scales from 0 to 180 and represents an angle in degree. Only available if [Decal Layers](Decal.md) feature is enabled. |
| **Tiling**              | Scales the decal Material along its UV axes.                 |
| **Offset**              | Offsets the decal Material along its UV axes. Use this with the **UV Scale** when using a Material atlas for your decal. |
| **Fade Factor**         | Allows you to manually fade the decal in and out. A value of 0 makes the decal fully transparent, and a value of 1 makes the decal as opaque as defined by the **Material**. The **Material** manages the maximum opacity of the decal using **Global Opacity** and an opacity map. |
| **Affects Transparent** | Enable the checkbox to allow HDRP to draw the projector’s decal on top of transparent surfaces. HDRP packs all Textures from decals with **Affects Transparency** enabled into an atlas, which can affect memory and performance. You can edit the dimensions of this atlas in the **Decals** section of your Unity Project’s [HDRP Asset](HDRP-Asset.md#Decals). |

## Limitations

- The Decal Projector can affect opaque Materials with either a [Decal Shader](Decal-Shader.md) or a [Decal master node](Master-Node-Decal.md). However, it can only affect transparent Materials with the [Decal Shader](Decal-Shader.md). 
- Decal Emissive isn't supported on Transparent Material. 
- The **Receive Decals** property of Materials in HDRP does not affect emissive decals. HDRP always renders emissive decals unless you use Decal Layers, which can disable emissive decals on a Layer by Layer basis.
- If you project a decal onto a transparent surface, HDRP ignores the decal's Texture tiling.
- In **Project Settings > Graphics**, if **Instancing Variants** is set to **Strip All**, Unity strips the Decal Shader this component references when you build your Project. This happens even if you include the Shader in the **Always Included Shaders** list. If Unity strips the Shader during the build process, the decal does not appear in your built Application.
