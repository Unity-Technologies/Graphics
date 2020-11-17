# Shadows

The **Shadows** [Volume component override](Volume-Components.html) controls the maximum distance at which HDRP renders shadow cascades and shadows from [punctual lights](Glossary.html#PunctualLight). It uses cascade splits to control the quality of shadows cast by Directional Lights over distance from the Camera.

## Using Shadows

**Shadows** uses the [Volume](Volumes.html) framework, so to enable and modify **Shadows** properties, you must add a **Shadows** override to a [Volume](Volumes.html) in your Scene.

The **Shadows** override comes as default when you create a **Scene Settings** GameObject (Menu: **GameObject > Rendering > Scene Settings**). You can also manually add a **Shadows** override to any [Volume](Volumes.html). To add **Shadows** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Shadowing** and click on **Shadows**. You can now use the **Shadows** override to alter shadow settings for this Volume.

## Properties

![](Images/Override-Shadows1.png)

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| **Property**     | **Description**                                              |
| ---------------- | ------------------------------------------------------------ |
| **Working Unit** | Use the drop-down to select the unit that you want to use to define the cascade splits.<br />&#8226; **Metric**: Defines cascade splits and borders in meters.<br />&#8226; **Percent**: Defines cascade splits and borders as a percentage of **Max Distance**. |
| **Max Distance**  | Set the maximum distance (in meters) at which HDRP renders shadows. HDRP uses this for punctual Lights and as the last boundary for the final cascade. |
| **Transmission Multiplier** | Sets the multiplier that HDRP applies to light transmitted by Directional Lights on thick objects. |
| **Cascade Count** | Use the slider to set the number of cascades to use for Directional Lights that can cast shadows. Cascades work as levels of detail (LOD) for shadows. Each cascade has its own shadow map, and the cascade area gets progressively larger as they get further from the Camera. HDRP spreads the same resolution shadow map over each cascade area, so cascades closer to the Camera have higher quality shadows than those further from the Camera. |
| **Split 1**       | Set the distance of the split between the first and second cascades. The **Working Unit** defines the unit this property uses. |
| **Split 2**       | Set the distance of the split between the second and third cascades. The **Working Unit** defines the unit this property uses. |
| **Split 3**       | Set the distance of the split between the third and final cascades. The **Working Unit** defines the unit this property uses. |
| **Border 1**      | Set the size of the border between the first and second cascade split. HDRP fades the shadow cascades between these two sections over this border.The **Working Unit** defines the unit this property uses. |
| **Border 2**      | Set the size of the border between the second and third cascade split. HDRP fades the shadow cascades between these two sections over this border.The **Working Unit** defines the unit this property uses. |
| **Border 3**      | Set the size of the border between the third and final cascade split. HDRP fades the shadow cascades between these two sections over this border.The **Working Unit** defines the unit this property uses. |
| **Border 4**      | Set the size of the border at the end of the last cascade split.HDRP fades the final shadow cascade out over this distance.The **Working Unit** defines the unit this property uses. |

## Visualizing the shadow cascades

You can use the Shadows override to visualize the cascade sizes in the Inspector, as well as the boundaries of the cascades as they appear inside your Scene in real time.

In the Inspector, use the **Cascade Splits** bar to see the size of each cascade relative to one another. You can also use the bar to:

- Move the position of each cascade split. To do so, click on the tab above a split and drag it to adjust the position of that cascade split.
- Move the position of each border. To do so, click on the tab below the split and drag it to adjust the position of the border

![](Images/Override-Shadows2.png)

In the Scene view and the Game view, the cascade visualization feature allows you to see the boundaries of each cascade in your Scene. Each color represents a separate cascade, and the colors match those in the **Cascade Splits** bar. This allows you to see which colored area matches which cascade.

![](Images/Override-Shadows3.png)

In the Scene view and the Game view, you can use the cascade visualization feature to see the boundaries and borders of each cascade in your Scene. Each color represents a separate cascade, and the colors match those in the **Cascade Splits** bar. To enable the cascade visualization feature, click the **Visualize Cascades** button at the top of the list of **Shadows** properties. You can now see the shadow maps in the Scene view and the Game view. 

- You can use the Scene view Camera to move around your Scene and quickly visualize the shadow maps of different areas.
- You can use the Game view Camera to visualize the shadow maps from the point of view of the end user. You can use the **Visualize Cascades** feature while in Play Mode, which is useful if you have some method of controlling the Camera’s position and rotation and want to see the shadow maps from different points of view in your Project.