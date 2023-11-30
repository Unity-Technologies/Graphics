# Lighting environment reference

The **Environment (HDRP)** is a section in the [Lighting window](https://docs.unity3d.com/Manual/lighting-window.html) that allows you to specify which sky to use for indirect ambient light in HDRP. To open the window, select **Window > Rendering > Lighting > Environment**.

## Properties

| **Setting**                           | **Description**                                              |
| ------------------------------------- | ------------------------------------------------------------ |
| **Profile**                           | A [Volume Profile](create-a-volume-profile.md) for the sky. This Volume Profile must include at least one Sky Volume override. The interaction between the Volume Profile and the Scene's dominant directional light may affect the visual characteristics of baked light in the Scene. |
| **Static Lighting Sky**               | The sky to use for the indirect ambient light simulation. The drop-down only contains sky types that the **Profile** includes. For example, if the **Profile** includes a **Gradient Sky** Volume override, you can select **Gradient Sky** from this drop-down.<br/>You can only edit this setting if you assign a Volume Profile to the **Profile** field. |
| **Static Lighting Background Clouds** | The background clouds to use for the indirect ambient light simulation. The drop-down only contains cloud types that the **Profile** includes.<br/>You can only edit this setting if you assign a Volume Profile to the **Profile** field. |
| **Static Lighting Volumetric Clouds** | Enable this option to include Volumetric Clouds in the indirect ambient light simulation. |

You can assign the same Volume Profile to both the **Static Lighting Sky** field and a [Volume](understand-volumes.md) in your Scene. If you do this, and use the same sky settings for the baked lighting and the visual background in the Volume, the baked lighting accurately matches the background at runtime. If you want to control the light baking for the environment lighting separately to the visual background in your Scene, you can assign a different Volume Profile for each process.

**Note**: Changes to the baking environment only affect baked lightmaps and Light Probes during the baking process.