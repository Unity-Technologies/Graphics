# Visual Environment

The Visual Environment Volume component override specifies the **Sky Type** that HDRP renders in the Volume.

## Using the Visual Environment

The **Visual Environment** uses the [Volume](Volumes.md) framework, so to enable and modify **Visual Environment** properties, you must add a **Visual Environment** override to a [Volume](Volumes.md) in your Scene.

The **Visual Environment** override comes as default when you create a **Scene Settings** GameObject (Menu: **GameObject > Volumes > Sky and Fog Global Volume**). You can also manually add a **Visual Environment** override to any [Volume](Volumes.md). To add **Visual Environment** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override** and click on **Visual Environment**. You can now use the **Visual Environment** override to control the sky and fog for this Volume.

[!include[](snippets/volume-override-api.md)]

## Properties

![](Images/Override-VisualEnvironment1.png)

[!include[](snippets/Volume-Override-Enable-Properties.md)]

### Sky

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Sky Type**     | Use the drop-down to select the type of sky that HDRP renders when this Volume affects a Camera. This list automatically updates when you [create a custom Sky](Creating-a-custom-sky). <br />&#8226; **None**: HDRP does not render a sky for Cameras in this Volume.<br />&#8226; [Gradient Sky](Override-Gradient-Sky.md): Renders the top, middle, and bottom sections of the sky using three separate color zones. HDRP controls the size of these color zones using the Gradient Sky’s **Gradient Diffusion** property.<br />&#8226; [HDRI Sky](Override-HDRI-Sky.md): Uses a cubemap texture to represent the entire sky.<br />&#8226; [Physically Based Sky](Override-Physically-Based-Sky.md): Simulates the sky of a spherical planet with a two-part atmosphere which has an exponentially decreasing density with respect to its altitude.<br />&#8226; [Procedural Sky](Override-Procedural-Sky.md): Generates a sky based on properties such as, **Sky Tint**, **Ground Color**, and **Sun Size**. HDRP deprecated **Procedural Sky** in 2019.3 and replaced it with **Physically Based Sky**. To use Procedural Sky for HDRP Projects in Unity 2019.3 or later, follow the instructions on the [Upgrading from 2019.2 to 2019.3 guide](Upgrading-From-2019.2-to-2019.3.md#ProceduralSky).<br /><br />Note: If you select any option that is not **None**, make sure the respective sky [Volume override](Volume-Components.md) exists in a Volume in you Scene. For example, if you select **Gradient Sky**, your Scene must contain a Volume with a [Gradient Sky](Override-Gradient-Sky.md) override. |
| **Background Clouds**   | Use the drop-down to select the type of clouds that HDRP renders when this Volume affects a Camera. The options are:<br/>&#8226; **None**: Does not render any clouds.<br/>&#8226; **Cloud Layer**: Renders clouds using the [Cloud Layer system](Override-Cloud-Layer.md).<br/>This list automatically updates when you [create custom clouds](Creating-Custom-Clouds.md).<br/>For more information, refer to the [clouds in HDRP documentation](Clouds-In-HDRP.md). |
| **Ambient Mode** | Use the drop-down to select the mode this Volume uses to process ambient light.<br />&#8226; **Static**: Ambient light comes from the baked sky assigned to the **Static Lighting Sky** property in the Lighting window. This light affects both real-time and baked global illumination. For information on how to set up environment lighting, see the [Environment Lighting documentation](Environment-Lighting.md#lighting-environment).<br />&#8226; **Dynamic**: Ambient light comes from the sky that is set in the **Sky** > **Type** property of this override. This means that ambient light can change in real time depending on the current Volume affecting the Camera. If you use baked global illumination, changes to the environment lighting only affect GameObjects exclusively lit using Ambient Probes. If you use real-time global illumination, changes to the environment lighting affect both lightmaps and Ambient Probes. |

### Wind

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Global Orientation** | Controls the orientation of the wind relative to the world-space direction x-axis. |
| **Global Speed**       | Sets the global wind speed in kilometers per hour. |

## Changing sky settings

After you have set your **Sky Type**, if you want to override the default settings, you need to create an override for them in a Volume. For example, if you set the **Sky Type** to **Gradient Sky**, click **Add Override** on your Volume and add a **Gradient Sky** override. Then you can disable, or remove, the **Procedural Sky** override because the Visual Environment ignores it and uses the **Gradient Sky** instead. To disable the override, disable the checkbox to the left of the **Procedural Sky** title . To remove the override, click the drop-down menu to the right of the title and select **Remove** .

On the [Gradient Sky](Override-Gradient-Sky.md) override itself, you can enable the checkboxes next to each property to override the property with your own values. For example, enable the checkbox next to the **Middle** property and use the color picker to change the color to pink.

![](Images/Override-VisualEnvironment2.png)
