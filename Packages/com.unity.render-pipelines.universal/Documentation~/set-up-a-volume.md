# Set up a volume

To set up a volume in your scene, you can configure the project's default volume settings, or add a new custom volume. For details, refer to the following sections:

- [Configure the default volumes](#configure-the-default-volumes) 
- [Add a volume](#add-a-volume). 

<a name="configure-the-default-volumes"></a>
## Configure the default volumes

You can configure the default global volumes that all URP scenes use.

### Configure the Default Volume

To configure the Default Volume, go to **Project Settings** > **URP Global Settings** > **Default Volume Profile**. 

By default, the Default Volume references a Volume Profile called `DefaultVolumeProfile`. `DefaultVolumeProfile` lists all possible Volume Overrides. You can change the properties, but you can't disable or remove Volume Overrides. Refer to [Volume Overrides](VolumeOverrides.md) for more information about changing the properties.

You can assign your own Volume Profile.

If you delete the Volume Profile, URP automatically reassigns `DefaultVolumeProfile`.

### Configure the global volume for a quality level

To configure the global volume for a quality level, follow these steps:

1. Go to **Project Settings** > **Quality** and select the quality level.
3. Go to **Rendering** > **Render Pipeline Asset** and open the URP Asset.
4. In the Inspector window for the URP Asset, go to **Volumes**.

You can add or remove Volume Overrides and edit their properties. Refer to [Volume Overrides](VolumeOverrides.md) for more information about changing the Volume Overrides and properties.

<a name="add-a-volume"></a>
## Add a volume

To add a volume to your scene and edit its Volume Profile, follow these steps:

1. Go to **GameObject** > **Volume** and select a GameObject.
2. In the **Scene** or **Hierarchy** view, select the new GameObject to view it in the Inspector.
3. In the **Volume** component, assign a Volume Profile asset. To create a new Volume Profile, select **New**.

The list of Volume Overrides that the Volume Profile contains appears below the Volume Profile asset. You can add or remove Volume Overrides and edit their properties. Refer to [Volume Overrides](VolumeOverrides.md) for more information about changing the Volume Overrides and properties.

### Example: Create a local post-processing effect

The following example shows how to use a local Box Volume to implement a location-based post-processing effect.

1. In a scene, create a new Box Volume using **GameObject** > **Volume** > **Box Volume**.

2. Select the Box Volume. In the Inspector, in the **Volume** component, select **New**.

    Unity creates the new Volume Profile.

3. Select **Add Override**, then select a post-processing effect.

4. In the **Box Collider** component, adjust the **Size** and **Center** properties so the collider occupies the volume where you want the local post-processing effect to be.

5. Ensure **Is Trigger** is enabled in the **Box Collider** component.

6. If you have other Volume components in the scene, change the value of the **Priority** property to ensure that the Volume Overrides from this volume have higher priority than those of other volumes.

Now, when the Camera is within the bounds of the GameObject's collider, URP uses the Volume Overrides from the **Volume** component.
