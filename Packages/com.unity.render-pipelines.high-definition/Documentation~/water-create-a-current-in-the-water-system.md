# Create a current in the water system
To create and control a current in the water system, apply a current map to a water surface.
A current map is a texture that modifies the local swell, agitation or ripples of a water surface current.
Current maps are different from flow maps because the water flow can’t stop and it always has a direction. In addition, the speed of the flow remains constant all along the surface.

To see a working current map, [open the River sample scene](#river-sample-scene).

<a name="river-sample-scene"></a>

## Open the River sample scene

HDRP includes the River sample scene which shows how a current map behaves.

To open the River sample scene:

1. Go to **Window** > **Package Management** > **Package Manager**.
2. Select **High Definition RP**.
3. In the Samples tab, import the **Environment Samples**.
4. Open the scene named **River**.

## Create a current map texture

You can create a Current map texture in any  image-editing software. The image can be in any non sRGB format. The resolution of a current map texture has a small impact on the current effect.

![Current map texture.](Images/watersystem-curent.png)

The Red and Green channels contain the 2D direction of the current and the Blue channel contains the influence of the current map. 
The default direction is +X, as a result, the neutral value for a current map is (1, 0.5, 1). 
When importing a current map in the editor, make sure to that the sRGB checkbox is disabled in the texture importer.

The following images display each channel of the current map included in the River sample scene.
* The red channel of a current map: ![The red channel of a current map.](Images/watersystem-curent-r.png)
* The green channel of a current map: ![The green channel of a current map.](Images/watersystem-curent-g.png)
* The blue channel of a current map: ![The blue channel of a current map.](Images/watersystem-curent-b.png)

## Apply a current Map to a water surface

To set the current map a water surface uses:

- Select a water surface to open its properties in the Inspector window
- Open the Simulation section
- In the Current Map property, select the picker (circle) and choose a texture to apply.

Current maps behave in a different way depending on the type of water surface:

- Ocean: Set the current map in the **Swell** subsection. This affects properties in the **First Band** and **Second Band** subsections. In the **Ripples** subsection, you can change the **Motion** property to I**nherit from Swell**, or set a custom current map in the **Swell** subsection.
- Rivers: Set the current map in the **Agitation** subsection. In the **Ripples** subsection, you can change the **Motion** property to **Inherit from Agitation**, or set a custom current map in the **Agitation** subsection.
- Pools: You can only set a current map in the **Ripples** subsection.

## Make an object follow a current map

If you use a script to [float an object on the water surface](float-objects-on-a-water-surface.md), you can get the current at the resulting location to make the object move with the flow of the water.

The [River sample scene](#river-sample-scene) includes a script that extends the Float script to make objects float along the current map.

# Debug a current map

To visualize the effect of a current map on a water surface:

- Select a water surface to view its properties in the Inspector.
- Open the **Miscellaneous** section.
- Locate **Debug Mode** and select **Current.**

![You can visualize the effect of a current map on a water surface.](Images/watersystem-current-debug.png)
