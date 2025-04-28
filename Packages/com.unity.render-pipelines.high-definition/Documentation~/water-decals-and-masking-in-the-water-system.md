# Decals and masking in the water system

## Masks
You can use a simulation mask to affect the influence the simulation has on specific areas of the water surface.

Masks take into account the **Wrap Mode** of the texture on the importer. For **Ocean, Sea, or Lake** water surface types, choose **Clamp** rather than the default, **Repeat**.

![A square texture with a diffuse cloudy pattern of magenta, pink, light blue, and blue.](Images/WaterMask_Example-22.2.png)

![A mostly flat ocean with a gentle pattern of waves and ripples.](Images/WaterMask_ExempleRender.PNG)

In this example, the Red channel attenuates the First and Second bands with a gradient. The noise on the Green channel attenuates ripples. See the <a href="settings-and-properties-related-to-the-water-system.md#simulationmask">Simulation Mask property description</a> for more information.

## Decals
You can use a [decal](decals.md) with a water surface in the form of a **Decal Layer Mask**. You might use this to imitate debris floating on the water, for example.
**Global Opacity** determines the amount of influence the decal has on the appearance of the water surface.

Certain [Decal Shader](decal-material-inspector-reference.md) Surface Options do not work with water surfaces:
* **Affect Metal**
* **Affect Ambient Occlusion**
* **Affect Emission**
* **Affect Base Color** only produces monochromatic output.
