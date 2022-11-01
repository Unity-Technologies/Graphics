
# Quality and performance decisions
If you wish to improve the performance of your project that includes a water surface, there are several adjustments you can make.
## Script interaction
Don't enable this setting unless you intend to use water height data in scripts. When you enable script interaction, this doubles the number of calculations Unity does, because the simulation runs on both the GPU and the CPU.
## Levels of Detail (LODs)
In the [Water Volume Override](WaterSystem-VolOverride.md), you can reduce memory demands if you keep grid size down. Reduce the **Max Grid Size** and **Elevation Transition**. You can also lower the **Num Levels of Detail** (number of [Levels of Detail](https://docs.unity3d.com/Manual/LevelOfDetail.html)).
## Tessellation
You must enable [hidden properties](WaterSystem-Properties.md#hidden) to see the Tessellation options, which are on the Water Volume Override.
Reduce the **Max Tessellation Factor**, **Tessellation Factor Fade Range** and **Tessellation Factor Fade Start** to save memory.

## Fade ranges and Simulation Bands
Water surface types with more simulation bands have higher memory requirements. This is why an **Ocean, Sea, or Lake** surface requires HDRP to do more calculations than a **Pool**. The **Start** value determines where HDRP begins to fade out the contribution of a Simulation Band.
One way to offset the performance demands of **Ocean, Sea, or Lake** surfaces is to lower the **Start** value. This begins to fade out the simulation band effect closer to the camera. Then reduce the difference between the **Start** and **Distance** values in the **Fade Range** for each band.
You must enable [hidden properties](WaterSystem-Properties.md#hidden) to see the **Fade Range** settings.
## Smoothness range
**Smoothness** values determine the distance from the camera at which the water surface becomes less detailed. In addition to the same **Fade Range** properties as the Simulation Bands, **Smoothness** has **Close** and **Distant** properties. As with Simulation bands, you can save memory by reducing the difference between the **Fade Range** values, but you can also reduce the **Close** and **Distant** values.
You must enable [hidden properties](WaterSystem-Properties.md#hidden) to see the **Fade Range** settings.

## Refraction strength
The **Maximum Distance** property determines how far into the water surface HDRP renders the **Refraction** effect. Reduce this value to conserve memory. This also reduces the visibility of the refraction effect.
## Resolution
You can also specify lower resolutions for masks, decals, caustics, and the simulation to save memory.
See **Project Settings** > **Quality** > **HDRP** for the **Simulation Resolution** property.
The **Caustics** properties include **Caustics Resolution**.
For masks and decals, custom mesh and custom foam textures, adjust the resolution of your source files.

# Additional resources
[Settings and properties related to the Water System](WaterSystem-Properties.md)