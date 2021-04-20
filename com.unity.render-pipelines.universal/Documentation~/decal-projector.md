# Decal Projector

The Universal Render Pipeline (URP) includes the Decal Projector component, which allows you to project specific Materials (decals) into the Scene. Decals are Materials that use the [Decal Shader Graph](decal-shader.md). When the Decal Projector component projects decals into the Scene, they interact with the Scene’s lighting and wrap around Meshes. You can use thousands of decals in your Scene simultaneously because URP instances them. This means that the rendering process is not resource intensive as long as the decals use the same Material.

![](Images/decal/decal-projector-preview.png)

To edit a Decal Projector’s properties, select the GameObject with the Decal Projector component and use the Inspector. If you just want to change the size of the projection, you can either use the Inspector or one of the Decal Projector's Scene view gizmos.

## Using the Scene view

The Decal Projector includes a Scene view representation of its bounds and projection direction to help you position the projector. The Scene view representation includes:

* A box that describes the 3D size of the projector; the projector draws its decal on every Material inside the box.

* An arrow that indicates the direction the projector faces. The base of this arrow is on the pivot point.

![](Images/decal/decal-projector-gizmos.png)

The decal Projector also includes three gizmos. The first two add handles on every face for you to click and drag to alter the size of the projector's bounds.

| __Button__                                   | __Gizmo__      | __Description__ |
| -------------------------------------------- |--------------- | --------------- |
|![](Images/decal/decal-projector-scale.png)   | __Scale__     | Scales the decal with the projector box. This changes the UVs of the Material to match the size of the projector box. This stretches the decal. The Pivot remains still.|
|![](Images/decal/decal-projector-crop.png)    | __Crop__      | Crops the decal with the projector box. This changes the size of the projector box but not the UVs of the Material. This crops the decal. The Pivot remains still. |
|![](Images/decal/decal-projector-pivotuv.png) | __Pivot / UV__| Moves the decal's pivot point without moving the projection box. This changes the transform position.<br/>Note this also sets the UV used on the projected texture.|

The color of the gizmos can be set up in the Preference window inside Color panel.

## Using the Inspector

Using the Inspector allows you to change all of the Decal Projector properties, and lets you use numerical values for __Size__, __Tiling__, and __Offset__, which allows for greater precision than the click-and-drag gizmo method.

## Properties

![](Images/decal/decal-projector-inspector.png)

| __Property__            | __Description__                                              |
| ----------------------- | ------------------------------------------------------------ |
| __Scale Mode__          | The scaling mode to apply to decals that use this Decal Projector. The options are:<br/>&#8226; __Scale Invariant__: Ignores the transformation hierarchy and uses the scale values in this component directly.<br/>&#8226; __Inherit from Hierarchy__: Multiplies the [lossy scale](https://docs.unity3d.com/ScriptReference/Transform-lossyScale.html) of the Transform with the Decal Projector's own scale then applies this to the decal. Note that since the Decal Projector uses orthogonal projection, if the transformation hierarchy is [skewed](https://docs.unity3d.com/Manual/class-Transform.html), the decal does not scale correctly. |
| __Width__               | The width of the projector influence box, and thus the decal along the projected plane. The projector scales the decal to match the __Width__ (along the local x-axis). |
| __Height__              | The height of the projector influence box, and thus the decal along the projected plane. The projector scales the decal to match the __Height__ (along the local y-axis). |
| __Projection Depth__    | The depth of the projector influence box. The projector scales the decal to match __Projection Depth__. The Decal Projector component projects decals along the local z-axis. |
| __Pivot__               | The offset position of the transform regarding the projection box. To  rotate the projected texture around a specific position, adjust the __X__ and __Y__ values. To set a depth offset for the projected texture, adjust the __Z__ value. |
| __Material__            | The decal Material to project. The decal Material must use decal shader graph. |
| __Tiling__              | Scales the decal Material along its UV axes.                 |
| __Offset__              | Offsets the decal Material along its UV axes. Use this with the __UV Scale__ when using a Material atlas for your decal. |
| __Opacity__             | Allows you to manually fade the decal in and out. A value of 0 makes the decal fully transparent, and a value of 1 makes the decal as opaque as defined by the __Material__. |
| __Draw Distance__       | The distance from the Camera to the Decal at which this projector stops projecting the decal and URP no longer renders the decal. |
| __Start Fade__          | Use the slider to set the distance from the Camera at which the projector begins to fade out the decal. Scales from 0 to 1 and represents a percentage of the __Draw Distance__. A value of 0.9 begins fading the decal out at 90% of the __Draw Distance__ and finished fading it out at the __Draw Distance__. |
| __Angle Fade__          | Use the min-max slider to control the fade out range of the decal based on the angle between the Decal backward direction and the vertex normal of the receiving surface. |

## Limitations

- Does not work on transparent surfaces.
- Does not work with negative odd scaling (When there are odd number of scale components).
