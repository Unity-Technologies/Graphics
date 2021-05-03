# Decal Projector

URP includes the Decal Projector component, which lets you project specific Materials (decals) onto other objects in the Scene. A Decal Projector can use a Material if the Material uses the [Decal Shader Graph asset](decal-shader.md). When the Decal Projector component projects decals onto other GameObjects, the decals interact with the Scene’s lighting and wrap around Meshes.


![Decal Projector in a sample Scene](Images/decal/decal-projector-scene-view.png)<br/>*Decal Projector in a sample Scene.*

## How to use Decal Projectors

To use Decal Projectors in your Scene:

1. Create a Material, and assign it the `Shader Graphs/Decal` shader. In the Material, select the Base Map and the Normal Map.

    ![Example decal Material](Images/decal/decal-example-material.png)

2. Create a new Decal Projector GameObject, or add a Decal Projector component to an existing GameObject.

The following illustration shows a Decal Projector in the Scene.

![Decal Projector in the Scene.](Images/decal/decal-projector-selected-with-inspector.png)

The Decal Projector component provides the Scene view editing tools.

![Scene view editing tools](Images/decal/decal-scene-view-editing-tools.png)

## Decal Scene view editing tools

When you select a Decal Projector, Unity shows its bounds and the projection direction.

The Decal Projector draws the decal Material on every Mesh inside the bounding box.

The white arrow shows the projection direction. The base of the arrow is the pivot point.

![Decal Projector bounding box](Images/decal/decal-projector-bounding-box.png)

The Decal Projector component provides the following Scene view editing tools.

![Scene view editing tools](Images/decal/decal-scene-view-editing-tools.png)

| __Icon__                                   | __Action__      | __Description__ |
| -------------------------------------------- |--------------- | --------------- |
|![](Images/decal/decal-projector-scale.png)   | __Scale__     | Select to scale the projector box and the decal. This tool changes the UVs of the Material to match the size of the projector box. The tool does not affect the pivot point. |
|![](Images/decal/decal-projector-crop.png)    | __Crop__      | Select to crop or tile the decal with the projector box. This tool changes the size of the projector box but not the UVs of the Material. The tool does not affect the pivot point. |
|![](Images/decal/decal-projector-pivotuv.png) | __Pivot / UV__| Select to move the pivot point of the decal without moving the projection box. This tool changes the transform position.<br/>This tool also affects the UV coordinates of the projected texture. |

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

## Performance

URP supports the GPU instancing of Materials. If the decals in your Scene use the same Material, and if the Material has the **Enable GPU Instancing** property turned on, URP instances the Materials and reduces the performance impact.

## Limitations

- Does not work on transparent surfaces.
- Does not work with negative odd scaling (When there are odd number of scale components).
