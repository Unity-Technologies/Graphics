# Volumes

The High Definition Render Pipeline (HDRP) uses a Volume framework. Each Volume can either be global or have local boundaries. They each contain Scene setting property values that HDRP interpolates between, depending on the position of the Camera, to calculate a final value. For example, you can use local Volumes to change environment settings, such as fog color and density, to alter the mood of different areas of your Scene.

You can add a __Volume__ component to any GameObject, including a Camera, although it's good practice to create a dedicated GameObject for each Volume. The Volume component itself contains no actual data and instead references a [Volume Profile](Volume-Profile.md) which contains the values to interpolate between. The Volume Profile contains default values for every property and hides them by default. To view or alter these properties, you must add [Volume overrides](Volume-Components.md), which are structures containing overrides for the default values, to the Volume Profile.

A Scene can contain several Volumes so each Volume contains properties that control how it interacts with others in the Scene. **Global** Volumes affect the Camera wherever the Camera is in the Scene and **Local** Volumes affect the Camera if they encapsulate the Camera within the bounds of their Collider.

At runtime, HDRP looks at all the enabled Volumes attached to active GameObjects in the Scene and determines each Volume’s contribution to the final Scene settings. HDRP uses the Camera position and the Volume properties described above to calculate this contribution. It then uses all Volumes with a non-zero contribution to calculate interpolated final values for every property in every Volume override.

Volumes can contain different combinations of Volume overrides. For example, one Volume may hold a Physically Based Sky Volume override while other Volumes hold an Exponential Fog Volume override.

**Note**: For Volumes with the same priority, there is no guarantee on the order in which HDRP evaluates them. This means that, depending on creation order, a global Volume can take precedence over a local Volume. The result is that a Camera can go within the bounds of a local Volume but still exclusively use the Volume Override properties from a global Volume in the Scene.

## Properties

![image alt text](Images/Volumes1.png)

| Property| Description |
|:---|:---|
| **Mode** | Use the drop-down to select the method that HDRP uses to calculate whether this Volume can affect a Camera:<br />&#8226; **Global**: Makes the Volume have no boundaries and allow it to affect every Camera in the Scene.<br />&#8226; **Local**: Allows you to specify boundaries for the Volume so that the Volume only affects Cameras inside the boundaries. Add a Collider to the Volume's GameObject and use that to set the boundaries. |
| **Blend Distance** | The furthest distance from the Volume’s Collider that HDRP starts blending from. A value of 0 means HDRP applies this Volume’s overrides immediately upon entry.<br />This property only appears when you select **Local** from the **Mode** drop-down. |
| **Weight** | The amount of influence the Volume has on the Scene. HDRP applies this multiplier to the value it calculates using the Camera position and Blend Distance.  |
| **Priority** | HDRP uses this value to determine which Volume it uses when Volumes have an equal amount of influence on the Scene. HDRP uses Volumes with higher priorities first. |
| **Profile** | A Volume Profile Asset that contains the Volume overrides that store the properties HDRP uses to handle this Volume. |

## Volume Profiles

The __Profile__ field stores a [Volume Profile](Volume-Profile.md), which is an Asset that contains the properties that HDRP uses to render the Scene. You can edit this Volume Profile, or assign a different Volume Profile to the **Profile** field. You can also create a Volume Profile or clone the current one by clicking the __New__ and __Clone__ buttons respectively.

## Configuring a local Volume

If you select **Local** from the **Mode** drop-down on your Volume, you must attach a Trigger Collider to the GameObject to define its boundaries:

1. Select the Volume to open it in the Inspector.
2. Got to **Add Component** > **Physics** > **Box Collider**.
3. To define the boundary of the Volume, adjust the __Size__ field of the Box Collider, and the __Scale__ field of the Transform.

You can use any type of 3D Collider, from simple Box Colliders to more complex convex Mesh Colliders. However, for performance reasons, use simple colliders because traversing Mesh Colliders with many vertices is resource intensive. Local volumes also have a __Blend Distance__ that represents the outer distance from the Collider surface where HDRP begins to blend the settings for that Volume with the others affecting the Camera.
