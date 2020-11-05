# Volumes

The Universal Render Pipeline (URP) uses the Volume framework. Volumes can override or extend Scene properties depending on the Camera position relative to each Volume. 

URP uses the Volume framework for [post-processing](integration-with-post-processing.md#post-proc-how-to) effects. 

URP implements dedicated GameObjects for Volumes: **Global Volume**, **Box Volume**, **Sphere Volume**, **Convex Mesh Volume**.

![Volume types](Images/post-proc/volume-volume-types.png)

The Volume component contains the **Mode** property that defines whether the Volume is Global or Local.

![Volume Mode property](Images/post-proc/volume-mode-prop.png)

With Mode set to **Global**, Volumes affect the Camera everywhere in the Scene. With Mode set to **Local**, Volumes affect the Camera if the Camera is within the bounds of the Collider. For more information, see [How to use Local Volumes](#volume-local).

You can add a __Volume__ component to any GameObject. A Scene can contain multiple GameObjects with Volume components. You can add multiple Volume components to a GameObject.

The Volume component references a [Volume Profile](VolumeProfile.md), which contains the Scene properties. A Volume Profile contains default values for every property and hides them by default. [Volume Overrides](VolumeOverrides.md) let you change or extend the default properties in a [Volume Profile](VolumeProfile.md).

At runtime, URP goes through all of the enabled Volume components attached to active GameObjects in the Scene, and determines each Volume's contribution to the final Scene settings. URP uses the Camera position and the Volume component properties to calculate the contribution. URP interpolates values from all Volumes with a non-zero contribution to calculate the final property values.

## Volume component properties

Volumes components contain properties that control how they affect Cameras and how they interact with other Volumes.

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

## <a name="volume-local"></a>How to use Local Volumes

This section describes how to use a Local Volume to implement a location-based post-processing effect.

In this example, URP applies a post-processing effect when the Camera is within a certain Box Collider.

1. In the Scene, create a new Box Volume (**GameObject > Volume > Box Volume**).

2. Select the Box Volume. In Inspector, In the **Volume** component, In the **Profile** field, click **New**.

    ![Create new Volume Profile.](Images/post-proc/volume-box-new-profile.png)

    Unity creates the new Volume Profile and adds the **Add Override** button to the Volume component.

    ![New Volume Profile created.](Images/post-proc/volume-new-profile-created.png)

3. If you have other Volumes in the Scene, change the value of the Priority property to ensure that the Overrides from this Volume have higher priority than those of other Volumes.

    ![Volume priority](Images/post-proc/volume-priority.png)

3. Click [Add Override](VolumeOverrides.md#volume-add-override). In the Volume Overrides dialog box, select a post-processing effect.

4. In the Collider component, adjust the Size and the Center properties so that the Collider occupies the volume where you want the local post-processing effect to be.

    ![Adjust the Box Collider size and position.](Images/post-proc/volume-box-collider.png)

    Ensure that the **Is Trigger** check box is selected.

Now, when the Camera is within the bounds of the Volume's Box Collider, URP uses the Volume Overrides from the Box Volume.
