# Linear Fog

The High Definition Render Pipeline (HDRP) provides a type of [fog](HDRP-Features.html#FogOverview.html) called Linear Fog). Linear Fog enables you to linearly increase the density of fog at a certain distance from the Camera. The fog density increases until it reaches a maximum density at a further distance.

## Using Linear Fog

**Linear Fog** uses the [Volume](Volumes.html) framework, so to enable and modify **Linear  Fog** properties, you must add a **Linear Fog** override to a [Volume](Volumes.html) in your Scene. To add **Linear Fog** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Fog** and click on **Linear Fog**.

After you add a **Linear Fog** override, you must set the Volume to use **Linear Fog**. The [Visual Environment](Override-Visual-Environment.html) override controls which type of fog the Volume uses. In the **Visual Environment** override, navigate to the **Fog** section and set the **Type** to **Linear Fog**. HDRP now renders **Linear Fog** for any Camera this Volume affects.

## Properties

![](Images/Override-LinearFog1.png)

| --------------------- | ------------------------------------------------------------ |
| **Density**           | Use the slider to set the maximum density of the fog. This acts as a global multiplier. Higher values produce thicker fog. |
| **Fog Start**         | The distance from the Camera at which the fog density begins to increase from 0. |
| **Fog End**           | The distance from the Camera at which the fog density reaches the value you set in **Density**. |
| **Fog Height Start**  | The height (in world space) at which the fog density begins to decrease. |
| **Fog Height End**    | The height at which the fog density reaches 0.               |
| **Max Fog Distance**  | The maximum distance of the fog from the Camera.             |
| **Color Mode**        | Use the drop-down to select the mode HDRP uses to calculate the color of the fog.<br />&#8226; **Sky Color**: HDRP shades the fog with a color it samples from the sky cubemap and its mipmaps.<br />&#8226; **Constant Color**: HDRP shades the fog with the color you set manually in the **Constant Color** field that appears when you select this option. |
| **- Mip Fog Near**    | The distance (in meters) from the Camera that HDRP stops sampling the lowest resolution mipmap for the fog color.<br />This property only appears when you select **Sky Color** from the **Color Mode** drop-down. |
| **- Mip Fog Far**     | The distance (in meters) from the Camera that HDRP starts sampling the highest resolution mipmap for the fog color.<br />This property only appears when you select **Sky Color** from the **Color Mode** drop-down. |
| **- Mip Fog Max Mip** | Use the slider to set the maximum mipmap that HDRP uses for the mip fog. This defines the mipmap that HDRP samples for distances greater than **Mip Fog Far**.<br />This property only appears when you select **Sky Color** from the **Color Mode** drop-down. |
| **- Constant Color**  | Use the color picker to select the color of the fog.<br />This property only appears when you select **Constant Color** from the **Color Mode** drop-down. |
