# Add swell, agitation, or ripples

To add swell, agitation or ripples, use a Water Mask to affect the influence the simulation has on specific areas of the water surface.

Masks take into account the Wrap Mode of the texture. For Ocean, Sea, or Lake water surface types, select **Clamp** rather than the default **Repeat** value.

To add a Water Mask:

1. Create and import a texture where the color channels represent the fluctuations.

	Refer to the following table:

    | Water surface type  | Red channel | Green channel | Blue channel |
    |---------------------|-------------|---------------|--------------|
    | Ocean               | Swell       | Agitation     | Ripples      |
    | River               | Agitation   | Ripples       | Not used     |
    | Pool                | Ripples     | Not used      | Not used     |

    > [!NOTE]
    > The water types use different channels for different effects to optimize texture packing, and use the first channel for the widest simulation band.

    The darker the color of a channel, the lesser the effect. For example, use white for 100% intensity and black for 0% intensity.

1. In the Water Volume Inspector window, drag the texture to the **Water Mask** property.

![A water mask that's more red towards the left and more blue towards the right.](Images/WaterMask_Example-22.2.png)

![An ocean rendered using the water mask.](Images/WaterMask_ExempleRender.PNG)

In this example, the red channel has a gradient that reduces the first and second simulation bands. The noise on the green channel reduces ripples. For more information, refer to the <a href="settings-and-properties-related-to-the-water-system.md#simulationmask">Simulation Mask property description.
