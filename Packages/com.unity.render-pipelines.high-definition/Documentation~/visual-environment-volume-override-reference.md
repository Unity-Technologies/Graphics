# Visual Environment volume override reference

The Visual Environment Volume Override lets you change the type of sky the High Definition Render Pipeline (HDRP) uses, and configure the sky and clouds.

Refer to [Sky](sky.md) for more information.

## Properties

[!include[](snippets/Volume-Override-Enable-Properties.md)]

### Sky

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Sky Type**     | Use the drop-down to select the type of sky that HDRP renders when this Volume affects a Camera. This list automatically updates when you [create a custom Sky](create-a-custom-sky.md). <br />&#8226; **None**: HDRP doesn't render a sky for Cameras in this Volume.<br />&#8226; [Gradient Sky](create-a-gradient-sky.md): Renders the top, middle, and bottom sections of the sky using three separate color zones. HDRP controls the size of these color zones using the Gradient Sky’s **Gradient Diffusion** property.<br />&#8226; [HDRI Sky](create-an-hdri-sky.md): Uses a cubemap texture to represent the entire sky.<br />&#8226; [Physically Based Sky](create-a-physically-based-sky.md): Simulates the sky of a spherical planet with a two-part atmosphere which has an exponentially decreasing density with respect to its altitude.<br />&#8226; Procedural Sky: Generates a sky based on properties such as, **Sky Tint**, **Ground Color**, and **Sun Size**. HDRP deprecated **Procedural Sky** in 2019.3 and replaced it with **Physically Based Sky**. To use Procedural Sky for HDRP Projects in Unity 2019.3 or later, follow the instructions on the [Upgrading from 2019.2 to 2019.3 guide](Upgrading-From-2019.2-to-2019.3.md#ProceduralSky).<br /><br />Note: If you select any option that's not **None**, make sure the respective sky [Volume override](configure-volume-overrides.md) exists in a Volume in you Scene. For example, if you select **Gradient Sky**, your Scene must contain a Volume with a [Gradient Sky](gradient-sky-volume-override-reference.md) override. |
| **Background Clouds**   | Use the drop-down to select the type of clouds that HDRP renders when this Volume affects a Camera. The options are:<br/>&#8226; **None**: Doesn't render any clouds.<br/>&#8226; **Cloud Layer**: Renders clouds using the [Cloud Layer system](Override-Cloud-Layer.md).<br/>This list automatically updates when you [create custom clouds](Creating-Custom-Clouds.md).<br/>For more information, refer to the [clouds in HDRP documentation](Clouds-In-HDRP.md). |
| **Ambient Mode** | Use the drop-down to select the mode this Volume uses to process ambient light.<br />&#8226; **Static**: Ambient light comes from the baked sky assigned to the **Static Lighting Sky** property in the Lighting window. This light affects both real-time and baked global illumination. For information on how to set up environment lighting, see the [Environment Lighting documentation](Environment-Lighting.md#lighting-environment).<br />&#8226; **Dynamic**: Ambient light comes from the sky that you set in the **Sky** > **Type** property of this override. This means that ambient light can change in real time depending on the current Volume affecting the Camera. If you use baked global illumination, changes to the environment lighting only affect GameObjects exclusively lit using Ambient Probes. If you use real-time global illumination, changes to the environment lighting affect both lightmaps and Ambient Probes. |

### Planet

The planet settings will impact various environment effects like Volumetric Clouds, Fog and Physically Based Sky.

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Radius**                     | The radius of the planet in kilometers. The radius is the distance from the center of the planet to the sea level. |
| **Rendering Space**            | Indicates the space in which HDRP computes the various environement effects. The options are: <br/>&#8226; **Camera**: Use this option when the camera stays on the ground and do not need to go high in the atmosphere. This mode allow the various effects to use faster and more memory efficient variants. <br/>&#8226; **World**: Use this option when you need the camera to fly through the volumetric clouds or go outside of the atmosphere. |
| **- Center**                   | The center is used when defining where the planet's surface is. In automatic mode, the top of the planet is at the world's origin and the center is derived from the planet radius. Only available when **Rendering Space** is set to **World** |
| **- Position**                 | The world-space position of the planet's center in kilometers. Only available when **Center** is set to **Manual**. |

### Wind

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Global Orientation** | Controls the orientation of the wind relative to the world-space direction x-axis. |
| **Global Speed**       | Sets the global wind speed in kilometers per hour. |
