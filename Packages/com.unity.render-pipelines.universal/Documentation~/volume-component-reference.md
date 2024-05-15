# Volume component reference

Volumes components contain properties that control how they affect Cameras and how they interact with other Volumes.

| Property           | Description                                                  |
| :----------------- | :----------------------------------------------------------- |
| **Mode**           | Use the drop-down to select the method that URP uses to calculate whether this Volume can affect a Camera:<br />&#8226; **Global**: Makes the Volume have no boundaries and allow it to affect every Camera in the scene.<br />&#8226; **Local**: Allows you to specify boundaries for the Volume so that the Volume only affects Cameras inside the boundaries. Add a Collider to the Volume's GameObject and use that to set the boundaries. |
| **Blend Distance** | The furthest distance from the Volume’s Collider that URP starts blending from. A value of 0 means URP applies this Volume’s overrides immediately upon entry.<br />This property only appears when you select **Local** from the **Mode** drop-down. |
| **Weight**         | The amount of influence the Volume has on the scene. URP applies this multiplier to the value it calculates using the Camera position and Blend Distance. |
| **Priority**       | URP uses this value to determine which Volume it uses if more than one Volume overrides the same property. URP uses the Volume with the highest value. |
| **Volume Profile**        | A Volume Profile Asset that contains the Volume Components that store the properties URP uses to handle this Volume. The **Profile** field stores a [Volume Profile](Volume-Profile.md), which is an Asset that contains the properties that URP uses to render the scene. You can edit this Volume Profile, or assign a different Volume Profile to the **Profile** field. You can also create a Volume Profile by selecting the **New** button. |

