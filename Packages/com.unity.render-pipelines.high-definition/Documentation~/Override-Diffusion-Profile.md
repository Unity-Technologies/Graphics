# Diffusion Profile List

The High Definition Render Pipeline (HDRP) allows you to use up to 15 custom [Diffusion Profiles](Diffusion-Profile.md) in view at the same time. To use more than 15 custom Diffusion Profiles in a Scene, you can use the **Diffusion Profile List** inside a [Volume](Volumes.md). This allows you to specify which Diffusion Profiles to use in a certain area (or in the Scene if the Volume is global).

## Using a Diffusion Profile List

To add a **Diffusion Profile List** to a Volume:

1. Select the Volume component in the Scene or Hierarchy to view it in the Inspector
2. In the Inspector, go to **Add Override** and select **Diffusion Profile List**.

[!include[](snippets/volume-override-api.md)]

## Properties

![](Images\Override-DiffusionProfile1.png)

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| **Property**                               | **Description**                                              |
| ------------------------------------------ | ------------------------------------------------------------ |
| **Diffusion Profile List**                 | Assign a Diffusion Profile to each field to create a list of Diffusion Profiles that Materials in this Volume can use. Click the plus icon to add another field. To remove a Diffusion Profile from the list, select it in the list and click the minus icon. |
| **Fill Profile List With Scene Materials** | Select this to remove every Diffusion Profile in the **Diffusion Profile List** and then re-populate the list with Diffusion Profiles that Materials within the bounds of this local Volume use. <br/><br/>This property is only available when you select **Local** from the **Mode** drop-down in the Volume component. Add a Collider to this GameObject to set the bounds of the Volume. |

## Details

If a Material references a Diffusion Profile that's not in the list of available Diffusion Profiles, that Material uses the default Diffusion Profile, which has a fushia tint.


If the Volume with the Diffusion Profile List is local, the **Fill Profiles With Scene Materials** button appears. Select to fetch the Diffusion Profiles from Materials inside the Volume's bounds and fill the **Diffusion Profile List** with them.

If multiple Volumes overlap and affect the Camera simultaneously, HDRP interpolates between multiple values for the same Volume override property to handle overlapping values. However, interpolating a final value for the **Diffusion Profile List** isn't possible. Instead, HDRP additively selects the **Diffusion Profile List** from all Volumes, by prioritizing profiles from Volumes with the highest **Priority**. If the total count exceeds the maximum capacity of 15 Diffusion Profiles, the remaining profiles are ignored.

There is a small performance overhead to find which Diffusion Profile a Material uses. This means that the fewer Diffusion Profiles you use, the faster this process is. You can use the **Diffusion Profile List** component in local volumes to optimize the search process. If you have multiple Scenes, and each one only uses a single Diffusion Profile, you can use this override in a separate Volume in each Scene to select a Diffusion Profile per Scene, instead of placing the Diffusion Profile from each Scene into the volume of the HDRP Global Settings. This reduces the resource intensity of the search in the Shader.This is particularly effective if your Scene contains a lot of pixel overdraw to produce visual effects like foliage and vegetation.
