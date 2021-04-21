# Diffusion Profile Override

The High Definition Render Pipeline (HDRP) allows you to use up to 15 custom [Diffusion Profiles](Diffusion-Profile.md) in view at the same time. To use more than 15 custom Diffusion Profiles in a Scene, you can use the **Diffusion Profile Override** inside a [Volume](Volumes.md). This allows you to specify which Diffusion Profiles to use in a certain area (or in the Scene if the Volume is global).

## Using a Diffusion Profile Override

To add a **Diffusion Profile Override** to a Volume:

1. Select the Volume component in the Scene or Hierarchy to view it in the Inspector
2. In the Inspector, navigate to **Add Override** and click on **Diffusion Profile Override**.

[!include[](snippets/volume-override-api.md)]

## Properties

![](Images\Override-DiffusionProfile1.png)

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| **Property**                               | **Description**                                              |
| ------------------------------------------ | ------------------------------------------------------------ |
| **Diffusion Profile List**                 | Assign a Diffusion Profile to each field to create a list of Diffusion Profiles that Materials in this Volume can use. Click the plus icon to add another field. To remove a Diffusion Profile from the list, select it in the list and click the minus icon. |
| **Fill Profile List With Scene Materials** | Click this button to remove every Diffusion Profile in the **Diffusion Profile List** and then re-populate the list with Diffusion Profiles that Materials within the bounds of this local Volume use. Note that this does not work with Materials that use a ShaderGraph Shader.<br/>This property is only available when you select **Local** from the **Mode** drop-down in the Volume component. Add a Collider to this GameObject to set the bounds of the Volume. |

## Details

If a Material references a Diffusion Profile that is not in the list of available Diffusion Profiles, that Material uses the default Diffusion Profile, which has a green tint.


If the Volume with the Diffusion Profile Override is local, the **Fill Profiles With Scene Materials** button appears. Click this button to fetch the Diffusion Profiles from Materials inside the Volume's bounds and fill the **Diffusion Profile List** with them. Note that this does not work with Materials that use a ShaderGraph Shader.

If multiple Volumes overlap and affect the Camera simultaneously, HDRP interpolates between multiple values for the same Volume override property in order to handle overlapping values. However, interpolating a final value for the **Diffusion Profile List** is not possible. Instead, HDRP selects the **Diffusion Profile List** from the Volume with the highest **Priority**.

There is a small performance overhead to find which Diffusion Profile a Material users. This means that the fewer Diffusion Profiles you use, the faster this process is. Rather than limit the number of Diffusion Profiles you use, you can use the **Diffusion Profile Override** to optimize the search process. If you have multiple Scenes, and each one only uses a single Diffusion Profile, you can use this override on a global Volume in each Scene to select a Diffusion Profile per Scene, instead of placing the Diffusion Profile from each Scene into the HDRP Graphics Settings. This reduces the resource intensity of the search in the Shader. This technique is particularly effective if your Scene contains a lot of overdraw to produce visual effects like foliage and vegetation.
