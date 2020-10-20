# Volumes

The Universal Render Pipeline (URP) uses a Volume framework. Each Volume can either be global or have local boundaries. They each contain Scene setting property values that URP interpolates between, depending on the position of the Camera, in order to calculate a final value. You can use local Volumes to control post processing effects. 

You can add a __Volume__ component to any GameObject, including a Camera, although it is good practice to create a dedicated GameObject for each Volume. The Volume component itself contains no actual data itself and instead references a [Volume Profile](VolumeProfile.md) which contains the values to interpolate between. The Volume Profile contains default values for every property and hides them by default. To view or alter these properties, you must add [Volume overrides](VolumeOverrides.md), which are structures containing overrides for the default values, to the Volume Profile.

Volumes also contain properties that control how they interact with other Volumes. A Scene can contain many Volumes. **Global** Volumes affect the Camera wherever the Camera is in the Scene and **Local** Volumes affect the Camera if they encapsulate the Camera within the bounds of their Collider.

At run time, URP looks at all of the enabled Volumes attached to active GameObjects in the Scene and determines each Volume’s contribution to the final Scene settings. URP uses the Camera position and the Volume properties described above to calculate this contribution. It then uses all Volumes with a non-zero contribution to calculate interpolated final values for every property in all Volume Components.

Volumes can contain different combinations of Volume Components.

## Properties

![](/Images/Inspectors/Volume1.png)

| Property           | Description                                                  |
| :----------------- | :----------------------------------------------------------- |
| **Mode**           | Use the drop-down to select the method that URP uses to calculate whether this Volume can affect a Camera:<br />&#8226; **Global**: Makes the Volume have no boundaries and allow it to affect every Camera in the Scene.<br />&#8226; **Local**: Allows you to specify boundaries for the Volume so that the Volume only affects Cameras inside the boundaries. Add a Collider to the Volume's GameObject and use that to set the boundaries. |
| **Blend Distance** | The furthest distance from the Volume’s Collider that URP starts blending from. A value of 0 means URP applies this Volume’s overrides immediately upon entry.<br />This property only appears when you select **Local** from the **Mode** drop-down. |
| **Weight**         | The amount of influence the Volume has on the Scene. URP applies this multiplier to the value it calculates using the Camera position and Blend Distance. |
| **Priority**       | URP uses this value to determine which Volume it uses when Volumes have an equal amount of influence on the Scene. URP uses Volumes with higher priorities first. |
| **Profile**        | A Volume Profile Asset that contains the Volume Components that store the properties URP uses to handle this Volume. |

## Volume Profiles

The __Profile__ field stores a [Volume Profile](VolumeProfile.md), which is an Asset that contains the properties that URP uses to render the Scene. You can edit this Volume Profile, or assign a different Volume Profile to the __Profile__ field. You can also create a Volume Profile or clone the current one by clicking the __New__ and __Clone__ buttons respectively.

## Configuring a local Volume

If your select __Local__ from the __Mode__ drop-down on your Volume, you must attach a Trigger Collider to the GameObject to define its boundaries. Click the Volume to open it in the Inspector and then select __Add Component > Physics > Box Collider__. To define the boundary of the Volume, adjust the __Size__ field of the Box Collider, and the __Scale__ field of the Transform.  You can use any type of 3D collider, from simple Box Colliders to more complex convex Mesh Colliders. However, for performance reasons, you should use simple colliders because traversing Mesh Colliders with many vertices is resource intensive. Local volumes also have a __Blend Distance__ that represents the outer distance from the Collider surface where URP begins to blend the settings for that Volume with the others affecting the Camera.
