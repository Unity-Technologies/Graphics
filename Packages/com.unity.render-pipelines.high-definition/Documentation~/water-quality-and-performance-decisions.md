
# Quality and performance decisions
If you wish to improve the performance of your project that includes a water surface, there are several adjustments you can make.

## Levels of Detail (LODs)
In the [Water Volume Override](water-the-water-system-volume-override.md), you can reduce memory demands if you keep **Triangle Size** down.
## Tessellation
You must enable [hidden properties](settings-and-properties-related-to-the-water-system.md) to see the Tessellation options, which are on the Water Surface component.
Reduce the **Max Tessellation Factor**, **Tessellation Factor Fade Range** and **Tessellation Factor Fade Start** to improve GPU time.

## Fade ranges and Simulation Bands
Water surface types with more simulation bands have higher memory requirements. This is why an **Ocean, Sea, or Lake** surface requires HDRP to do more calculations than a **Pool**. The **Start** value determines where HDRP begins to fade out the contribution of a Simulation Band.
One way to offset the performance demands of **Ocean, Sea, or Lake** surfaces is to lower the **Start** value. This begins to fade out the simulation band effect closer to the camera. Then reduce the difference between the **Start** and **Distance** values in the **Fade Range** for each band.
You must enable [hidden properties](settings-and-properties-related-to-the-water-system.md#) to see the **Fade Range** settings.

## Resolution
You can also specify lower resolutions for masks, decals, caustics, and the simulation to save memory.
See **Project Settings** > **Quality** > **HDRP** for the **Simulation Resolution** property.
The **Caustics** properties include **Caustics Resolution**.
For masks and decals, custom mesh and custom foam textures, adjust the resolution of your source files.

## Script interactions
Don't select CPU Simulation in Simulation Mode setting in the HDRP Asset unless you absolutely need accurate results. When enabled, it increases the number of calculations Unity performs because the GPU simulation has to be duplicated on the CPU.

# Additional resources
[Settings and properties related to the water system](settings-and-properties-related-to-the-water-system.md)
