# Decal Projector reference

To edit a Decal Projector’s properties, select the GameObject with the Decal Projector component and use the Inspector. If you just want to change the size of the projection, you can either use the Inspector or one of the Decal Projector's Scene view gizmos.

## Scene view

The Decal Projector includes a Scene view representation of its bounds and projection direction to help you position the projector. The Scene view representation includes:

* A box that describes the 3D size of the projector; the projector draws its decal on every Material inside the box.

* An arrow that indicates the direction the projector faces. The base of this arrow is on the pivot point.

![Decal Projector Scene view.](Images/DecalProjector2.png)

The decal Projector also includes three gizmos. The first two add handles on every face for you to click and drag to alter the size of the projector's bounds.

|**Button**|**Gizmo**|**Description**|
|-----|-----|-----|
|![Decal Projector Scale gizmo.](Images/DecalProjector3.png)|**Scale**|Scales the decal with the projector box. This changes the UVs of the Material to match the size of the projector box. This stretches the decal. The Pivot remains still.|
|![Decal Projector Crop gizmo.](Images/DecalProjector4.png)|**Crop**|Crops the decal with the projector box. This changes the size of the projector box but not the UVs of the Material. This crops the decal. The Pivot remains still.|
|![Decal Projector Pivot / UV gizmo.](Images/DecalProjector5.png)|**Pivot / UV**|Moves the decal's pivot point without moving the projection box. This changes the transform position.<br/>Note this also sets the UV used on the projected texture.|

The color of the gizmos can be set up in the Preference window inside Color panel.

## Inspector properties

Using the Inspector allows you to change all of the Decal Projector properties, and lets you use numerical values for **Size**, **Tiling**, and **Offset**, which allows for greater precision than the click-and-drag gizmo method.

## Properties

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Scale Mode**          | The scaling mode to apply to decals that use this Decal Projector. The options are:<br/>&#8226; **Scale Invariant**: Ignores the transformation hierarchy and uses the scale values in this component directly.<br/>&#8226; **Inherit from Hierarchy**: Multiplies the [lossy scale](https://docs.unity3d.com/ScriptReference/Transform-lossyScale.html) of the Transform with the Decal Projector's own scale then applies this to the decal. Note that since the Decal Projector uses orthogonal projection, if the transformation hierarchy is [skewed](https://docs.unity3d.com/Manual/class-Transform.html), the decal does not scale correctly. |
| **Size**                | The size of the projector influence box, and thus the decal along the projected plane. The projector scales the decal to match the **Width** (along the local x-axis) and **Height** (along the local y-axis) components of the **Size**. |
| **Projection Depth**    | The depth of the projector influence box. The projector scales the decal to match **Projection Depth**. The Decal Projector component projects decals along the local z-axis. |
| **Pivot**               | The offset position of the transform regarding the projection box. To  rotate the projected texture around a specific position, adjust the **X** and **Y** values. To set a depth offset for the projected texture, adjust the **Z** value. |
| **Material**            | The decal Material to project. The decal Material must use a HDRP/Decal Shader. |
| **Decal Layer**         | The layer that specifies the Materials to project the decal onto. Any Mesh Renderers or Terrain that uses a matching Decal Layer receives the decal. |
| **Draw Distance**       | The distance from the Camera to the Decal at which this projector stops projecting the decal and HDRP no longer renders the decal. |
| **Start Fade**          | Use the slider to set the distance from the Camera at which the projector begins to fade out the decal. Scales from 0 to 1 and represents a percentage of the **Draw Distance**. A value of 0.9 begins fading the decal out at 90% of the **Draw Distance** and finished fading it out at the **Draw Distance**. |
| **Angle Fade**          | Use the min-max slider to control the fade out range of the decal based on the angle between the Decal backward direction and the vertex normal of the receiving surface. Only available if [Decal Layers](use-decals.md) feature is enabled. |
| **Tiling**              | Scales the decal Material along its UV axes.                 |
| **Offset**              | Offsets the decal Material along its UV axes. Use this with the **UV Scale** when using a Material atlas for your decal. |
| **Fade Factor**         | Allows you to manually fade the decal in and out. A value of 0 makes the decal fully transparent, and a value of 1 makes the decal as opaque as defined by the **Material**. The **Material** manages the maximum opacity of the decal using **Global Opacity** and an opacity map. |
| **Affects Transparent** | Enable the checkbox to allow HDRP to draw the projector’s decal on top of transparent surfaces. HDRP packs all Textures from decals with **Affects Transparent** enabled into an atlas, which can affect memory and performance. You can edit the dimensions of this atlas in the **Decals** section of your Unity Project’s [HDRP Asset](HDRP-Asset.md#Decals). |
| **Transparent Texture Resolution** | Determines the size of the texture within the decal atlas. This is only being used if the selected material is a [Decal Master Stack](decal-master-stack-reference.md) material and **Affects Transparent** is enabled. The same resolution applies to all textures that the material affects. If multiple projectors use the same material but have different texture resolutions only the largest resolution is added to the atlas. The default values can be changed in the Decal section of your Unity Project’s [HDRP Asset](HDRP-Asset.md#Decals). |

## Limitations

- Emissive decals isn't supported on Transparent Material.
- Emissive decals always give an additive positive contribution. This property does not affect the existing emissive properties of the Materials assigned to a GameObject.
- The **Receive Decals** property of Materials in HDRP does not affect emissive decals. HDRP always renders emissive decals unless you use Decal Layers, which can disable emissive decals on a Layer by Layer basis.
- If you project a decal onto a transparent surface, HDRP ignores the decal's Texture tiling.
- [Decal Master Stack](decal-master-stack-reference.md) materials that have **Affects Transparent** enabled do not support changes to the vertex inputs. Geometry, scene, and buffer inputs are also not supported.
- In **Project Settings > Graphics**, if **Instancing Variants** is set to **Strip All**, Unity strips the Decal Shader this component references when you build your Project. This happens even if you include the Shader in the **Always Included Shaders** list. If Unity strips the Shader during the build process, the decal does not appear in your built Application.
