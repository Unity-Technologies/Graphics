# Volume component
The Volume component lets you configure a Volume. Refer to [Understand volumes](understand-volumes.md) for more information about Volumes.

## Properties

| Property| Description |
|:---|:---|
| **Mode** | Use the drop-down to select the method that HDRP uses to calculate whether this Volume can affect a Camera:<br />&#8226; **Global**: Makes the Volume have no boundaries and allow it to affect every Camera in the Scene.<br />&#8226; **Local**: Allows you to specify boundaries for the Volume so that the Volume only affects Cameras inside the boundaries. Add a Collider to the Volume's GameObject and use that to set the boundaries. |
| **Blend Distance** | The furthest distance from the Volume’s Collider that HDRP starts blending from. A value of 0 means HDRP applies this Volume’s overrides immediately upon entry.<br />This property only appears when you select **Local** from the **Mode** drop-down. |
| **Weight** | The amount of influence the Volume has on the Scene. HDRP applies this multiplier to the value it calculates using the Camera position and Blend Distance.  |
| **Priority** | HDRP uses this value to determine which Volume it uses when Volumes have an equal amount of influence on the Scene. HDRP uses Volumes with higher priorities first. If multiple volumes have the same priority, HDRP can evaluate them in any order. This means a global volume can take precedence over a local volume, even if the camera is inside the local volume. |
| **Profile** | A Volume Profile Asset that contains the Volume overrides that store the properties HDRP uses to handle this Volume. |

## Volume Profiles

The __Profile__ field stores a [Volume Profile](create-a-volume-profile.md), which is an Asset that contains the properties that HDRP uses to render the Scene. You can edit this Volume Profile, or assign a different Volume Profile to the **Profile** field. You can also create a Volume Profile or clone the current one by clicking the __New__ and __Clone__ buttons respectively.

## Configuring a local Volume

If you select **Local** from the **Mode** drop-down on your Volume, you must attach a Trigger Collider to the GameObject to define its boundaries:

1. Select the Volume to open it in the Inspector.
2. Got to **Add Component** > **Physics** > **Box Collider**.
3. To define the boundary of the Volume, adjust the __Size__ field of the Box Collider, and the __Scale__ field of the Transform.

You can use any type of 3D Collider, from simple Box Colliders to more complex convex Mesh Colliders. However, for performance reasons, use simple colliders because traversing Mesh Colliders with many vertices is resource intensive. Local volumes also have a __Blend Distance__ that represents the outer distance from the Collider surface where HDRP begins to blend the settings for that Volume with the others affecting the Camera.